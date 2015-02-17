#region Usings

using System;
using System.Collections.Generic;
using CodeRefractor.ClosureCompute;
using CodeRefractor.CodeWriter.BasicOperations;
using CodeRefractor.CodeWriter.Output;
using CodeRefractor.CodeWriter.Platform;
using CodeRefractor.MiddleEnd;
using CodeRefractor.MiddleEnd.Interpreters;
using CodeRefractor.MiddleEnd.Interpreters.Cil;
using CodeRefractor.MiddleEnd.Optimizations.Common;
using CodeRefractor.RuntimeBase.TypeInfoWriter;
using Ninject;

#endregion

namespace CodeRefractor.Backend.ComputeClosure
{
    public class MethodInterpreterCodeWriter
    {
        private readonly CppMethodCodeWriter _cppMethodCodeWriter;
        private readonly CppWriteSignature _cppWriteSignature;
        private readonly PlatformInvokeCodeWriter _platformInvokeCodeWriter;

        [Inject]
        public MethodInterpreterCodeWriter(
            CppMethodCodeWriter cppMethodCodeWriter,
            CppWriteSignature cppWriteSignature,
            PlatformInvokeCodeWriter platformInvokeCodeWriter)
        {
            _cppMethodCodeWriter = cppMethodCodeWriter;
            _cppWriteSignature = cppWriteSignature;
            _platformInvokeCodeWriter = platformInvokeCodeWriter;
        }

        public string WriteMethodCode(CilMethodInterpreter interpreter, TypeDescriptionTable typeTable, ClosureEntities closureEntities)
        {
            return _cppMethodCodeWriter.WriteCode(interpreter, typeTable, closureEntities);
        }

        public void WriteMethodSignature(CodeOutput codeOutput, 
            MethodInterpreter interpreter, ClosureEntities closureEntities)
        {
            if (interpreter.Method == null)
            {
                Console.WriteLine("Should not be null");
                return;
            }

            _cppWriteSignature.WriteSignature(codeOutput, interpreter, closureEntities, true);
        }

        internal string WritePInvokeMethodCode(PlatformInvokeMethod interpreter, ClosureEntities crRuntime)
        {
            return _platformInvokeCodeWriter.WritePlatformInvokeMethod(interpreter, crRuntime);
        }

        public string WriteDelegateCallCode(MethodInterpreter interpreter)
        {
            return _platformInvokeCodeWriter.WriteDelegateCallCode(interpreter);
        }

        public bool ApplyLocalOptimizations(IEnumerable<ResultingOptimizationPass> optimizationPasses, CilMethodInterpreter interpreter, ClosureEntities entities)
        {
            if (optimizationPasses == null)
                return false;
            if (interpreter.Method.IsAbstract)
                return false;
            var result = false;
            var optimizationsList = new List<ResultingOptimizationPass>(optimizationPasses);
            var areOptimizationsAvailable = true;
            while (areOptimizationsAvailable)
            {
                interpreter.MidRepresentation.UpdateUseDef();
                areOptimizationsAvailable = false;
                foreach (var optimizationPass in optimizationsList)
                {
                    var optimizationName = optimizationPass.GetType().Name;
                    if (!optimizationPass.CheckPreconditions(interpreter, entities))
                        continue;
                    areOptimizationsAvailable = optimizationPass.Optimize(interpreter);

                    if (!areOptimizationsAvailable) continue;
                    var useDef = interpreter.MidRepresentation.UseDef;
                    interpreter.MidRepresentation.UpdateUseDef();
                    Console.WriteLine("Applied optimization: {0}", optimizationName);
                    result = true;
                    break;
                }
            }
            return result;
        }
    }
}