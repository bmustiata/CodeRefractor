﻿using System.Collections.Generic;
using CodeRefractor.CompilerBackend.Optimizations.Common;
using CodeRefractor.CompilerBackend.Optimizations.Util;
using CodeRefractor.RuntimeBase.Analyze;
using CodeRefractor.RuntimeBase.MiddleEnd;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations.Identifiers;

namespace CodeRefractor.CompilerBackend.Optimizations.SimpleDce
{

    class DeadStoreAssignment : ResultingInFunctionOptimizationPass
    {
        static readonly List<OperationKind> NoSideEffectsOperationKinds =new List<OperationKind>
        {
            OperationKind.Assignment,
            OperationKind.BinaryOperator,
            OperationKind.NewArray,
            OperationKind.NewObject,
            OperationKind.GetArrayItem,
            OperationKind.BinaryOperator,
            OperationKind.GetField,
            OperationKind.UnaryOperator
        };
        readonly Dictionary<LocalVariable, int> _definitions = new Dictionary<LocalVariable, int>();

        public override void OptimizeOperations(MethodInterpreter methodInterpreter)
        {
            var localOperations = methodInterpreter.MidRepresentation.LocalOperations.ToArray();

            var useDef = methodInterpreter.MidRepresentation.UseDef;
            _definitions.Clear();
            ComputeDefinitions(localOperations);
            RemoveUsages(localOperations,useDef);
            if (_definitions.Count == 0)
                return;
            var toRemove = BuildRemoveInstructions(localOperations);
            if (toRemove.Count == 0)
                return;
            methodInterpreter.MidRepresentation.DeleteInstructions(toRemove);
            toRemove.Clear();
            Result = true;
        }

        private void RemoveUsages(LocalOperation[] localOperations, UseDefDescription useDef)
        {
            for (int index = 0; index < localOperations.Length; index++)
            {
                var usages = useDef.GetUsages(index);
                foreach (var localVariable in usages)
                {
                    _definitions.Remove(localVariable);
                }
            }
        }

        private void ComputeDefinitions(LocalOperation[] localOperations)
        {
            for (int index = 0; index < localOperations.Length; index++)
            {
                var op = localOperations[index];
                var variableDefinition = op.GetDefinition();
                if (variableDefinition == null)
                    continue;
                _definitions[variableDefinition] = index;
            }
        }

        private List<int> BuildRemoveInstructions(LocalOperation[] localOperations)
        {
            var toRemove = new List<int>();
            foreach (var definition in _definitions)
            {
                var index = definition.Value;
                var op = localOperations[index];
                var opKind = op.Kind;
                if (NoSideEffectsOperationKinds.Contains(opKind))
                {
                    toRemove.Add(index);
                }
            }
            return toRemove;
        }
    }
}
