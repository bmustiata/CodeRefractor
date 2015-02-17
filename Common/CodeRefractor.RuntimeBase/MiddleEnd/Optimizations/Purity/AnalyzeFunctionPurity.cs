#region Usings

using CodeRefractor.CodeWriter.Linker;
using CodeRefractor.MiddleEnd.Interpreters;
using CodeRefractor.MiddleEnd.Interpreters.Cil;
using CodeRefractor.MiddleEnd.Optimizations.Common;
using CodeRefractor.MiddleEnd.SimpleOperations;
using CodeRefractor.MiddleEnd.SimpleOperations.Methods;
using CodeRefractor.RuntimeBase.Optimizations;

#endregion

namespace CodeRefractor.MiddleEnd.Optimizations.Purity
{
	[Optimization(Category = OptimizationCategories.Analysis)]
    public class AnalyzeFunctionPurity : ResultingGlobalOptimizationPass
    {
	    private readonly LinkerUtils _linkerUtils;

	    public AnalyzeFunctionPurity(LinkerUtils linkerUtils)
	    {
	        _linkerUtils = linkerUtils;
	    }

	    public bool ReadPurity(MethodInterpreter intermediateCode)
        {
            if (intermediateCode == null)
                return false;
            return intermediateCode.AnalyzeProperties.IsPure;
        }

        public override void OptimizeOperations(CilMethodInterpreter interpreter)
        {
            if (ReadPurity(interpreter))
                return;
            var functionIsPure = ComputeFunctionPurity(interpreter);
            if (!functionIsPure) return;

            if (interpreter.AnalyzeProperties.IsPure)
                return;
            interpreter.AnalyzeProperties.IsPure = true;
            Result = true;
        }

        public bool ComputeFunctionPurity(CilMethodInterpreter intermediateCode)
        {
            if (intermediateCode == null)
                return false;
            var operations = intermediateCode.MidRepresentation.UseDef.GetLocalOperations();
            foreach (var localOperation in operations)
            {
                switch (localOperation.Kind)
                {
                    case OperationKind.SetStaticField:
                    case OperationKind.GetStaticField:
                    case OperationKind.CallRuntime:
                    case OperationKind.SetField:
                        return false;

                    case OperationKind.Call:
                        var operationData = (CallMethodStatic) localOperation;
                        var readPurity = _linkerUtils.ReadPurity(operationData.Info, Closure);
                        if (!readPurity)
                            return false;
                        break;

                    case OperationKind.BranchOperator:
                    case OperationKind.AlwaysBranch:
                    case OperationKind.UnaryOperator:
                    case OperationKind.BinaryOperator:
                    case OperationKind.Assignment:
                    case OperationKind.Label:
                    case OperationKind.Return:
                        break;
                    default:
                        return false;
                }
            }
            return true;
        }
    }
}