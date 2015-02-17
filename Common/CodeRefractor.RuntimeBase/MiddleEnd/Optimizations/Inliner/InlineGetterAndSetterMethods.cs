#region Usings

using CodeRefractor.CodeWriter.Linker;
using CodeRefractor.MiddleEnd.Interpreters.Cil;
using CodeRefractor.MiddleEnd.Optimizations.Common;
using CodeRefractor.MiddleEnd.Optimizations.Purity;
using CodeRefractor.MiddleEnd.SimpleOperations;
using CodeRefractor.MiddleEnd.SimpleOperations.Methods;
using CodeRefractor.RuntimeBase.Backend.Optimizations.Inliner;
using CodeRefractor.RuntimeBase.Optimizations;
using Ninject;

#endregion

namespace CodeRefractor.MiddleEnd.Optimizations.Inliner
{

    [Optimization(Category = OptimizationCategories.Inliner)]
    public class InlineGetterAndSetterMethods : ResultingGlobalOptimizationPass
    {
        private readonly LinkerUtils _linkerUtils;

        [Inject]
        public InlineGetterAndSetterMethods(LinkerUtils linkerUtils)
        {
            _linkerUtils = linkerUtils;
        }

        public override void OptimizeOperations(CilMethodInterpreter methodInterpreter)
        {
            var localOperations = methodInterpreter.MidRepresentation.UseDef.GetLocalOperations();
            for (var index = 0; index < localOperations.Length; index++)
            {
                var localOperation = localOperations[index];

                if (localOperation.Kind != OperationKind.Call) continue;

                var methodData = (CallMethodStatic) localOperation;
                var interpreter = _linkerUtils.GetInterpreter(methodData, Closure) 
                    as CilMethodInterpreter;

                if (interpreter == null)
                    continue;

                if (AnalyzeFunctionIsGetter.ReadProperty(interpreter)
                    || AnalyzeFunctionIsSetter.ReadProperty(interpreter)
                    || AnalyzeFunctionIsEmpty.ReadProperty(interpreter)
                    )
                {
                    SmallFunctionsInliner.InlineMethod(methodInterpreter.MidRepresentation, methodData,
                        index);
                    Result = true;
                    return;
                }
            }
        }
    }
}