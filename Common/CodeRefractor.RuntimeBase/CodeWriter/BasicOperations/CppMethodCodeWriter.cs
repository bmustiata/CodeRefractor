#region Usings

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CodeRefractor.ClosureCompute;
using CodeRefractor.CodeWriter.Output;
using CodeRefractor.FrontEnd.SimpleOperations;
using CodeRefractor.FrontEnd.SimpleOperations.Identifiers;
using CodeRefractor.MiddleEnd;
using CodeRefractor.MiddleEnd.Interpreters;
using CodeRefractor.MiddleEnd.Interpreters.Cil;
using CodeRefractor.MiddleEnd.SimpleOperations;
using CodeRefractor.MiddleEnd.SimpleOperations.ConstTable;
using CodeRefractor.MiddleEnd.SimpleOperations.Identifiers;
using CodeRefractor.RuntimeBase.Analyze;
using CodeRefractor.RuntimeBase.CodeWriter.BasicOperations;
using CodeRefractor.RuntimeBase.TypeInfoWriter;
using CodeRefractor.Util;
using Ninject;

#endregion

namespace CodeRefractor.CodeWriter.BasicOperations
{
    // Singleton
    public class CppMethodCodeWriter
    {
        private Provider<CodeOutput> _codeOutputProvider;
        private readonly CppCastRelatedOperations _cppCastRelatedOperations;
        private readonly CppHandleOperators _cppHandleOperators;
        private readonly CppHandleCalls _cppHandleCalls;
        private readonly CppWriteSignature _cppWriteSignature;

        [Inject]
        public CppMethodCodeWriter(Provider<CodeOutput> codeOutputProvider,
                    CppCastRelatedOperations cppCastRelatedOperations,
                    CppHandleOperators cppHandleOperators,
                    CppHandleCalls cppHandleCalls,
                    CppWriteSignature cppWriteSignature)
        {
            _codeOutputProvider = codeOutputProvider;
            _cppCastRelatedOperations = cppCastRelatedOperations;
            _cppHandleOperators = cppHandleOperators;
            _cppHandleCalls = cppHandleCalls;
            _cppWriteSignature = cppWriteSignature;
        }

        public string WriteCode(CilMethodInterpreter interpreter, TypeDescriptionTable typeTable,
            ClosureEntities crRuntime)
        {
            var operations = interpreter.MidRepresentation.LocalOperations;
            var headerSb = _codeOutputProvider.Value;
            _cppWriteSignature.WriteSignature(headerSb, interpreter, crRuntime);

            var bodySb = ComputeBodySb(operations, interpreter.MidRepresentation.Vars, typeTable, interpreter, crRuntime);
            var variablesSb = ComputeVariableSb(interpreter.MidRepresentation, interpreter, crRuntime);
            var finalSb = _codeOutputProvider.Value;

            finalSb.Append(headerSb.ToString())
                    .BracketOpen()
                    .Append(variablesSb.ToString())
                    .Append(bodySb.ToString())
                    .BracketClose();

            return finalSb.ToString();
        }

        private CodeOutput ComputeBodySb(List<LocalOperation> operations, MidRepresentationVariables vars,
            TypeDescriptionTable typeTable, MethodInterpreter interpreter, ClosureEntities crRuntime)
        {
            CodeOutput bodySb = _codeOutputProvider.Value;
            foreach (var operation in operations)
            {
                bodySb.Append("\n");
                if (_cppHandleOperators.HandleAssignmentOperations(bodySb, operation, operation.Kind, typeTable,
                    interpreter, crRuntime))
                    continue;
                if (_cppCastRelatedOperations.HandleCastRelatedOperations(
                        typeTable, crRuntime, operation, bodySb, operation.Kind
                    ))
                    continue;
                if (HandleCallOperations(vars, interpreter, crRuntime, operation, bodySb))
                    continue;

                switch (operation.Kind)
                {
                    case OperationKind.Label:
                        WriteLabel(bodySb, ((Label) operation).JumpTo);
                        break;
                    case OperationKind.AlwaysBranch:
                        HandleAlwaysBranchOperator(operation, bodySb);
                        break;
                    case OperationKind.BranchOperator:
                        CppHandleBranches.HandleBranchOperator(operation, bodySb);
                        break;
                    case OperationKind.Return:
                        _cppHandleCalls.HandleReturn(operation,bodySb,interpreter);
                        break;

                    case OperationKind.CopyArrayInitializer:
                        HandleCopyArrayInitializer(operation, bodySb);
                        break;

                    case OperationKind.Switch:
                        HandleSwitch(operation, bodySb);
                        break;

                    case OperationKind.Comment:
                        HandleComment(operation.ToString(), bodySb);
                        break;

                        
                    default:
                        throw new InvalidOperationException(
                            string.Format(
                                "Invalid operation '{0}' is introduced in intermediary representation\nValue: {1}",
                                operation.Kind,
                                operation));
                }
            }

            return bodySb;
        }

        private bool HandleCallOperations(MidRepresentationVariables vars, MethodInterpreter interpreter,
            ClosureEntities crRuntime, LocalOperation operation, CodeOutput bodySb)
        {
            switch (operation.Kind)
            {
                case OperationKind.Call:
                    _cppHandleCalls.HandleCall(operation, bodySb, vars, interpreter, crRuntime);
                    break;
                case OperationKind.CallInterface:
                    _cppHandleCalls.HandleCallInterface(operation, bodySb, vars, interpreter, crRuntime);
                    break;
                case OperationKind.CallVirtual:
                    _cppHandleCalls.HandleCallVirtual(operation, bodySb, interpreter, crRuntime);
                    break;
                case OperationKind.CallRuntime:
                    _cppHandleCalls.HandleCallRuntime(operation, bodySb, crRuntime);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private static void HandleComment(string toString, CodeOutput bodySb)
        {
            bodySb
                .AppendFormat("// {0}", toString);
        }

        private static void HandleSwitch(LocalOperation operation, CodeOutput bodySb)
        {
            var assign = (Assignment) operation;
            var instructionTable = (int[]) ((ConstValue) assign.Right).Value;

            var instructionLabelIds = instructionTable;
            bodySb.AppendFormat("switch({0})", assign.AssignedTo.Name);
            bodySb.BracketOpen();
            var pos = 0;
            foreach (var instructionLabelId in instructionLabelIds)
            {
                bodySb.AppendFormat("case {0}:", pos++);
                bodySb.AppendFormat("\tgoto label_{0};", instructionLabelId.ToHex());
            }
            bodySb.BracketClose();
        }

        private static void HandleCopyArrayInitializer(LocalOperation operation, CodeOutput sb)
        {
            var assignment = (Assignment) operation;
            var left = assignment.AssignedTo;
            var right = (ConstByteArrayValue) assignment.Right;
            var rightArrayData = (ConstByteArrayData) right.Value;
            var rightArray = rightArrayData.Data;
            sb.AppendFormat("{0} = std::make_shared<Array<System::Byte> >(" +
                            "{1}, RuntimeHelpers_GetBytes({2}) ); ",
                left.Name,
                rightArray.Length,
                right.Id);
        }

        private static StringBuilder ComputeVariableSb(MetaMidRepresentation midRepresentation, MethodInterpreter interpreter, ClosureEntities closureEntities)
        {
            var variablesSb = new StringBuilder();
            var vars = midRepresentation.Vars;
            foreach (var variableInfo in vars.LocalVars)
            {
                AddVariableContent(variablesSb, "{0} local_{1};", variableInfo, interpreter, closureEntities);
            }
            foreach (var localVariable in vars.VirtRegs)
            {
                AddVariableContent(variablesSb, "{0} vreg_{1};", localVariable, interpreter, closureEntities);
            }
            return variablesSb;
        }

        private static string ComputeCommaSeparatedParameterTypes(LocalVariable localVariable)
        {/*
            var methodInfo = (MethodInfo) localVariable.CustomData;

            var parameters = methodInfo.GetMethodArgumentTypes().ToArray();

            var parametersFormat = TypeNamerUtils.GetCommaSeparatedParameters(parameters);
            return parametersFormat;*/
            //TODO: handle funciton pointers in a more clean way
            return localVariable.VarName;
        }

        private static void AddVariableContent(StringBuilder variablesSb, string format, LocalVariable localVariable, MethodInterpreter interpreter, ClosureEntities closureEntities)
        {
            var localVariableData = interpreter.AnalyzeProperties.GetVariableData(localVariable);
            if (localVariableData == EscapingMode.Stack)
                return;
            if (localVariable.ComputedType().GetClrType(closureEntities).IsSubclassOf(typeof(MethodInfo)))
            {
                variablesSb
                    .AppendFormat("System_Void (*{0})({1}*);", //TODO: Added * to deal with pointers, is this the right approach ?
                        localVariable.Name,
                        ComputeCommaSeparatedParameterTypes(localVariable))
                    .AppendLine();
                return;
            }
            if (localVariableData == EscapingMode.Pointer)
            {
                var cppName = localVariable.ComputedType()
                    .GetClrType(closureEntities).ToDeclaredVariableType(localVariableData);
                variablesSb
                    .AppendFormat(format, cppName, localVariable.Id)
                    .AppendLine();
                return;
            }
            variablesSb
                .AppendFormat(format, localVariable.ComputedType()
                    .GetClrType(closureEntities).ToDeclaredVariableType(localVariableData), localVariable.Id)
                .AppendLine();
        }

        private static void HandleAlwaysBranchOperator(LocalOperation operation, CodeOutput sb)
        {
            sb.AppendFormat("goto label_{0};", ((AlwaysBranch)operation).JumpTo.ToHex());
        }


        private static void WriteLabel(CodeOutput sb, int value)
        {
            sb.AppendFormat("label_{0}:", value.ToHex());
        }

        #region Call

        #endregion
    }
}