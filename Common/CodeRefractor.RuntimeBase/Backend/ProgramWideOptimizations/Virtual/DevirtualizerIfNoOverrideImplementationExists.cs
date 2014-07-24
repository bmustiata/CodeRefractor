﻿using System.Linq;
using System.Reflection;
using CodeRefractor.ClosureCompute;
using CodeRefractor.ClosureCompute.Steps;
using CodeRefractor.CompilerBackend.ProgramWideOptimizations;
using CodeRefractor.FrontEnd.SimpleOperations.Methods;
using CodeRefractor.MiddleEnd.Interpreters.Cil;
using CodeRefractor.MiddleEnd.SimpleOperations;
using CodeRefractor.MiddleEnd.SimpleOperations.Identifiers;
using CodeRefractor.MiddleEnd.SimpleOperations.Methods;
using CodeRefractor.Util;

namespace CodeRefractor.Backend.ProgramWideOptimizations.Virtual
{
    public class DevirtualizerIfNoOverrideImplementationExists : ResultingProgramOptimizationBase
    {
        protected override void DoOptimize(ClosureEntities closure)
        {
            var methodInterpreters = closure.MethodImplementations.Values
                .Where(m => m.Kind == MethodKind.CilInstructions)
                .Select(mth => (CilMethodInterpreter)mth)
                .ToArray();
            foreach (var interpreter in methodInterpreters)
            {
                HandleInterpreterInstructions(interpreter, closure);
            }
        }

        private void HandleInterpreterInstructions(CilMethodInterpreter interpreter, ClosureEntities closure)
        {
            var useDef = interpreter.MidRepresentation.UseDef;
            var calls = useDef.GetOperationsOfKind(OperationKind.CallVirtual).ToList();
            var allOps = useDef.GetLocalOperations();
            foreach (var callOp in calls)
            {
                var op = allOps[callOp];
                var methodData = (CallMethodStatic)op;
                var thisParameter = (LocalVariable)methodData.Parameters.First();
                var clrType = thisParameter.FixedType.ClrType;
                if (clrType == methodData.Info.DeclaringType)
                    continue;

                var overridenTypes = clrType.ImplementorsOfT(closure);
                if (overridenTypes.Count > 1)
                    continue;
                
                //TODO: map correct method
                var resolvedMethod = AddVirtualMethodImplementations.GetImplementingMethod(clrType, (MethodInfo) methodData.Info);
                methodData.Interpreter = closure.ResolveMethod(resolvedMethod);
                interpreter.MidRepresentation.LocalOperations[callOp] = new CallMethodStatic(methodData.Interpreter)
                {
                    Result = methodData.Result,
                    Parameters = methodData.Parameters
                };
                Result = true;
            }
            if (Result)
            {
                interpreter.MidRepresentation.UpdateUseDef();
            }
        }
    }
}