using CodeRefractor.CompilerBackend.Optimizations.Common;
using CodeRefractor.RuntimeBase.MiddleEnd;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations;

namespace CodeRefractor.CompilerBackend.Optimizations.Purity
{
    public class AnalyzeFunctionIsSetter : ResultingGlobalOptimizationPass
    {
        public static bool ReadProperty(MethodInterpreter intermediateCode)
        {
            return intermediateCode.MidRepresentation.GetProperties().IsSetter;
        }
        public override void OptimizeOperations(MethodInterpreter methodInterpreter)
        {
            if (ReadProperty(methodInterpreter))
                return;
            var functionIsPure = ComputeFunctionPurity(methodInterpreter);
            if (!functionIsPure) return;
            methodInterpreter.MidRepresentation.GetProperties().IsSetter = true;
        }

        private static bool ComputeFunctionPurity(MethodInterpreter intermediateCode)
        {
            if (intermediateCode == null)
                return false;
            var operations = intermediateCode.MidRepresentation.UseDef.GetLocalOperations();
            foreach (var localOperation in operations)
            {
                switch (localOperation.Kind)
                {
                    case OperationKind.SetField:
                    case OperationKind.Assignment:
                    case OperationKind.AlwaysBranch:
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