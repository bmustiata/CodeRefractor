#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CodeRefractor.Analyze;
using CodeRefractor.Backend.ComputeClosure;
using CodeRefractor.ClosureCompute;
using CodeRefractor.CodeWriter.BasicOperations;
using CodeRefractor.CodeWriter.Linker;
using CodeRefractor.CodeWriter.Output;
using CodeRefractor.CodeWriter.Platform;
using CodeRefractor.CodeWriter.Types;
using CodeRefractor.FrontEnd.SimpleOperations.Methods;
using CodeRefractor.MiddleEnd;
using CodeRefractor.MiddleEnd.Interpreters;
using CodeRefractor.MiddleEnd.Interpreters.Cil;
using CodeRefractor.MiddleEnd.SimpleOperations.ConstTable;
using CodeRefractor.MiddleEnd.SimpleOperations.Methods;
using CodeRefractor.Runtime.Annotations;
using CodeRefractor.RuntimeBase.Analyze;
using CodeRefractor.RuntimeBase.Shared;
using CodeRefractor.RuntimeBase.TypeInfoWriter;
using CodeRefractor.Util;

#endregion

namespace CodeRefractor.Backend
{
    /// <summary>
    /// Writes output.cpp file
    /// </summary>
    public class CppCodeGenerator
    {
        private readonly Provider<CodeOutput> _codeOutputProvider;
        private readonly PlatformInvokeCodeWriter _platformInvokeCodeWriter;
        private readonly MethodInterpreterCodeWriter _methodInterpreterCodeWriter;
        private readonly ConstByteArrayList _constByteArrayList;
        private readonly CppWriteSignature _cppWriteSignature;
        private readonly VirtualMethodTableCodeWriter _virtualMethodTableCodeWriter;
        private readonly LinkingData _linkingData;

        public CppCodeGenerator(Provider<CodeOutput> codeOutputProvider,
            PlatformInvokeCodeWriter platformInvokeCodeWriter,
            MethodInterpreterCodeWriter methodInterpreterCodeWriter,
            ConstByteArrayList constByteArrayList,
            CppWriteSignature cppWriteSignature,
            VirtualMethodTableCodeWriter virtualMethodTableCodeWriter,
            LinkingData linkingData)
        {
            _codeOutputProvider = codeOutputProvider;
            _platformInvokeCodeWriter = platformInvokeCodeWriter;
            _methodInterpreterCodeWriter = methodInterpreterCodeWriter;
            _constByteArrayList = constByteArrayList;
            _cppWriteSignature = cppWriteSignature;
            _virtualMethodTableCodeWriter = virtualMethodTableCodeWriter;
            _linkingData = linkingData;
        }

        public CodeOutput GenerateSourceCodeOutput(
            MethodInterpreter interpreter,
            TypeDescriptionTable table,
            List<MethodInterpreter> closure,
            ClosureEntities closureEntities)
        {
            var headerSb = _codeOutputProvider.Value;

            headerSb.Append("#include \"sloth.h\"")
                .BlankLine();
            if (!string.IsNullOrEmpty(TypeNamerUtils.SmartPtrHeader))
            {
                headerSb
                    .AppendFormat("#include {0}", TypeNamerUtils.SmartPtrHeader)
                    .BlankLine();
            }

            var initializersCodeOutput = _codeOutputProvider.Value;
            TypeBodiesCodeGenerator.WriteClosureStructBodies(initializersCodeOutput, closureEntities);
            WriteClosureDelegateBodies(closure, initializersCodeOutput);
            WriteClosureHeaders(closure, initializersCodeOutput, closureEntities);

            initializersCodeOutput.BlankLine();
            initializersCodeOutput.Append("#include \"runtime_base.hpp\"");
            initializersCodeOutput.BlankLine();

            var bodySb = _codeOutputProvider.Value;
            bodySb.Append(_virtualMethodTableCodeWriter.GenerateTypeTableCode(table, closureEntities)); // We need to use this type table to generate missing jumps for subclasses  that dont override a base method
            WriteCppMethods(closure, bodySb, closureEntities);
            WriteClosureMethods(closure, bodySb, table, closureEntities);

            WriteMainBody(interpreter, bodySb, closureEntities);
            bodySb.Append(_platformInvokeCodeWriter.LoadDllMethods());
            bodySb.Append(_constByteArrayList.BuildConstantTable());

            _linkingData.IsInstTable.BuildTypeMatchingTable(table, closureEntities);
            bodySb.Append(_linkingData.Strings.BuildStringTable());

            //Add Logic to Automatically include std class features that are needed ...
            if (closureEntities.Features.Count > 0)
            {
                foreach (var runtimeFeature in closureEntities.Features)
                {
                    if (runtimeFeature.IsUsed)
                    {
                        if (runtimeFeature.Headers.Count > 0)
                        {
                            headerSb
                                .Append("//Headers For Feature: " + runtimeFeature.Name + "\n" +
                                    runtimeFeature.Headers.Select(g => "#include<" + g + ">").Aggregate((a, b) => a + "\n" + b) + "\n//End Of Headers For Feature: " + runtimeFeature.Name + "\n")
                                    .BlankLine();
                        }
                        if (runtimeFeature.Declarations.Count > 0)
                            initializersCodeOutput.Append("//Initializers For: " + runtimeFeature.Name + "\n" + runtimeFeature.Declarations.Aggregate((a, b) => a + "\n" + b) + "\n//End OF Initializers For: " + runtimeFeature.Name + "\n");
                        if (!String.IsNullOrEmpty(runtimeFeature.Functions))
                            bodySb.Append("//Functions For Feature: " + runtimeFeature.Name + "\n" + runtimeFeature.Functions + "\n//End Of Functions For Feature: " + runtimeFeature.Name + "\n");
                    }
                }
            }

            return headerSb
                .Append(initializersCodeOutput.ToString())
                .Append(bodySb.ToString());
        }

        private void WriteCppMethods(List<MethodInterpreter> closure, CodeOutput sb, ClosureEntities crRuntime)
        {
            var cppMethods = closure
                .Where(m => m.Kind == MethodKind.RuntimeCppMethod)
                .ToArray();

            var methodInterpreter = cppMethods.FirstOrDefault();
            if (methodInterpreter == null) return;
            foreach (var interpreter in cppMethods)
            {
                var cppInterpreter = (CppMethodInterpreter)interpreter;
                var runtimeLibrary = cppInterpreter.CppRepresentation;
                if (_linkingData.SetInclude(runtimeLibrary.Header))
                    sb.AppendFormat("#include \"{0}\"\n", runtimeLibrary.Header);
                _cppWriteSignature.WriteSignature(sb, interpreter, crRuntime, false);
                sb.BracketOpen()
                    .Append(runtimeLibrary.Source)
                    .BracketClose();
            }
        }

        private void WriteClosureMethods(List<MethodInterpreter> closure, CodeOutput sb, TypeDescriptionTable typeTable, ClosureEntities closureEntities)
        {
            WriteClosureBodies(closure, sb, typeTable, closureEntities);
        }

        private void WriteClosureHeaders(IEnumerable<MethodInterpreter> closure, CodeOutput codeOutput, ClosureEntities closureEntities)
        {
            closure.Where(interpreter => !interpreter.Method.IsAbstract)
                .Each(interpreter =>
                {
                    _methodInterpreterCodeWriter.WriteMethodSignature(
                        codeOutput, interpreter, closureEntities);
                    codeOutput.Append("\n");
                });
        }




        private void WriteClassFieldsBody(CodeOutput sb, Type mappedType, ClosureEntities crRuntime)
        {
            var typeDesc = UsedTypeList.Set(mappedType, crRuntime);
            typeDesc.WriteLayout(sb);
        }

        private void WriteClosureDelegateBodies(List<MethodInterpreter> closure, CodeOutput codeOutput)
        {
            foreach (var interpreter in closure)
            {
                if (interpreter.Kind != MethodKind.Delegate)
                    continue;
                codeOutput.Append(_methodInterpreterCodeWriter.WriteDelegateCallCode(interpreter));
            }

            codeOutput.Append(DelegateManager.Instance.BuildDelegateContent());
        }

        private void WriteClosureBodies(List<MethodInterpreter> closure, CodeOutput sb, TypeDescriptionTable typeTable, ClosureEntities closureEntities)
        {
            sb.Append("///--- PInvoke code ---\n");
            foreach (var interpreter in closure)
            {
                if (interpreter.Kind != MethodKind.PlatformInvoke)
                    continue;
                sb.Append(_methodInterpreterCodeWriter.WritePInvokeMethodCode(
                    (PlatformInvokeMethod)interpreter, closureEntities));
            }

            sb.Append("///---Begin closure code ---\n");
            foreach (var interpreter in closure)
            {
                if (interpreter.Kind != MethodKind.CilInstructions)
                    continue;

                if (interpreter.Method.IsAbstract)
                    continue;
                sb.Append(_methodInterpreterCodeWriter.WriteMethodCode(
                    (CilMethodInterpreter)interpreter, typeTable, closureEntities));
            }
            sb.Append("///---End closure code ---\n");
        }

        private void WriteUsedCppRuntimeMethod(KeyValuePair<string, MethodBase> methodBodyAttribute, StringBuilder sb, ClosureEntities crRuntime)
        {
            var method = methodBodyAttribute.Value;
            var typeData = method.DeclaringType;
            if (typeData == null)
                throw new InvalidDataException("Method's declaring type should be valid");
            var methodNativeDescription = method.GetCustomAttribute<CppMethodBodyAttribute>();
            if (methodNativeDescription == null)
                throw new InvalidDataException(
                    "Cpp runtime method is called but is not marked with CppMethodBody attribute");
            if (_linkingData.SetInclude(methodNativeDescription.Header))
                sb.AppendFormat("#include \"{0}\"", methodNativeDescription.Header).AppendLine();
            var methodHeaderText = method.WriteHeaderMethod(crRuntime, writeEndColon: false);
            sb.Append(methodHeaderText);
            sb.AppendFormat("{{ {0} }}", methodNativeDescription.Code).AppendLine();
        }

        private static void WriteMainBody(MethodInterpreter interpreter, CodeOutput sb, ClosureEntities crRuntime)
        {
            sb.Append("System_Void initializeRuntime();\n");
            sb.Append("int main(int argc, char**argv)").BracketOpen();
            sb.Append("auto argsAsList = System_getArgumentsAsList(argc, argv);\n");
            sb.Append("initializeRuntime();\n");

            if (crRuntime.Features.Count > 0)
            {
                foreach (var runtimeFeature in crRuntime.Features)
                {
                    sb.Append(runtimeFeature.Initializer + "\n");
                }
            }
            var entryPoint = interpreter.Method as MethodInfo;
            if (entryPoint.ReturnType != typeof(void))
                sb.Append("return ");
            var parameterInfos = entryPoint.GetParameters();
            var args = String.Empty;
            if (parameterInfos.Length != 0)
            {
                args = "argsAsList";
            }
            sb.AppendFormat("{0}({1});\n", entryPoint.ClangMethodSignature(crRuntime), args);
            sb.BlankLine();
            sb.Append("return 0;");
            sb.BracketClose();
        }
    }
}