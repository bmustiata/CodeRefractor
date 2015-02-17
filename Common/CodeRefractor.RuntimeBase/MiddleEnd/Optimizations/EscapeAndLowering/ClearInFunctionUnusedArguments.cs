#region Usings

using System.Linq;
using CodeRefractor.FrontEnd.SimpleOperations.Identifiers;
using CodeRefractor.MiddleEnd;
using CodeRefractor.MiddleEnd.Interpreters.Cil;
using CodeRefractor.MiddleEnd.Optimizations.Common;
using CodeRefractor.MiddleEnd.SimpleOperations.Identifiers;
using CodeRefractor.MiddleEnd.UseDefs;
using CodeRefractor.RuntimeBase.Analyze;
using CodeRefractor.RuntimeBase.MiddleEnd;
using CodeRefractor.RuntimeBase.Optimizations;

#endregion

namespace CodeRefractor.RuntimeBase.Backend.Optimizations.EscapeAndLowering
{
    [Optimization(Category = OptimizationCategories.DeadCodeElimination)]
    internal class ClearInFunctionUnusedArguments : ResultingGlobalOptimizationPass
    {
        public override void OptimizeOperations(CilMethodInterpreter interpreter)
        {
            var midRepresentation = interpreter.MidRepresentation;
            var useDef = midRepresentation.UseDef;
            var arguments = interpreter.AnalyzeProperties.Arguments;
            if (arguments.Count == 0)
                return;
            var properties = interpreter.AnalyzeProperties;
            var argList = arguments
                .Where(argVar => properties.GetVariableData(argVar) != EscapingMode.Unused)
                .ToList();
            argList = UseDefDescription.ComputeUnusedArguments(argList, useDef);
            foreach (var variable in argList)
            {
                Result = properties.SetVariableData(variable, EscapingMode.Unused);
            }
        }
    }
}