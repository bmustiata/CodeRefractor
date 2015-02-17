#region Usings

using System.Collections.Generic;
using CodeRefractor.CodeWriter.Linker;
using CodeRefractor.MiddleEnd.Interpreters.Cil;
using CodeRefractor.MiddleEnd.Optimizations.Common;
using CodeRefractor.MiddleEnd.SimpleOperations;
using CodeRefractor.MiddleEnd.SimpleOperations.Methods;
using CodeRefractor.MiddleEnd.UseDefs;
using CodeRefractor.RuntimeBase.Analyze;
using CodeRefractor.RuntimeBase.Optimizations;
using Ninject;

#endregion

namespace CodeRefractor.MiddleEnd.Optimizations.Purity
{
	[Optimization(Category = OptimizationCategories.Purity)]
    public class RemoveDeadPureFunctionCalls : ResultingInFunctionOptimizationPass
    {
	    private readonly LinkerUtils _linkerUtils;

	    [Inject]
	    public RemoveDeadPureFunctionCalls(LinkerUtils linkerUtils)
	    {
	        _linkerUtils = linkerUtils;
	    }

	    public override void OptimizeOperations(CilMethodInterpreter methodInterpreter)
        {
            var useDef = methodInterpreter.MidRepresentation.UseDef;
            var localOperations = useDef.GetLocalOperations();

            var toRemove = new List<int>();
            var callIndices = useDef.GetOperationsOfKind(OperationKind.Call);
            foreach (var index in callIndices)
            {
                var operation = localOperations[index];
                var methodData = operation.Get<CallMethodStatic>();
                var interpreter = _linkerUtils.GetInterpreter(methodData, Closure);
                if (interpreter == null)
                    continue;
                if (methodData.Result != null)
                    continue;
                var properties = interpreter.AnalyzeProperties;
                if (properties.IsReadOnly || properties.IsPure)
                {
                    toRemove.Add(index);
                }
            }
            if (toRemove.Count == 0)
                return;
            methodInterpreter.DeleteInstructions(toRemove);
            Result = true;
        }
    }
}