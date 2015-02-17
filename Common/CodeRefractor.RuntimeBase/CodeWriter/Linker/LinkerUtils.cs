#region Usings

using System;
using System.Collections.Generic;
using System.Reflection;
using CodeRefractor.ClosureCompute;
using CodeRefractor.FrontEnd.SimpleOperations.Identifiers;
using CodeRefractor.MiddleEnd;
using CodeRefractor.MiddleEnd.Interpreters;
using CodeRefractor.MiddleEnd.Interpreters.Cil;
using CodeRefractor.MiddleEnd.Optimizations.Purity;
using CodeRefractor.MiddleEnd.SimpleOperations.Identifiers;
using CodeRefractor.MiddleEnd.SimpleOperations.Methods;
using CodeRefractor.Runtime;
using CodeRefractor.RuntimeBase;
using CodeRefractor.RuntimeBase.Analyze;
using CodeRefractor.RuntimeBase.MiddleEnd;
using Ninject;

#endregion

namespace CodeRefractor.CodeWriter.Linker
{
    public class LinkerUtils
    {
        private readonly LinkingData _linkingData;
        private readonly AnalyzeFunctionPurity _analyzeFunctionPurity;

        [Inject]
        public LinkerUtils(LinkingData linkingData,
            AnalyzeFunctionPurity analyzeFunctionPurity)
        {
            _linkingData = linkingData;
            _analyzeFunctionPurity = analyzeFunctionPurity;
        }

        public string ComputedValue(IdentifierValue identifierValue)
        {
            var constValue = identifierValue as ConstValue;
            if (constValue == null)
            {
                return identifierValue.Name;
            }
            var computeType = identifierValue.ComputedType();
            if (computeType.ClrTypeCode == TypeCode.String)
            {
                
                var stringTable = _linkingData.Strings;
                var stringId = stringTable.GetStringId((string) constValue.Value);
                    
                return String.Format("_str({0})", stringId);
            }
            return constValue.Name;
        }

        public MethodInterpreter GetInterpreter(CallMethodStatic callMethodStatic, ClosureEntities crRuntime)
        {
            return crRuntime.ResolveMethod(callMethodStatic.Info);
        }

        public MethodInterpreter GetInterpreter(MethodBase methodBase, ClosureEntities crRuntime)
        {
            return crRuntime.ResolveMethod(methodBase);
        }

        public const string EscapeName = "NonEscapingArgs";

        public Dictionary<int, bool> EscapingParameterData(MethodBase info, ClosureEntities crRuntime)
        {
            var interpreter = this.GetInterpreter(info, crRuntime) as CilMethodInterpreter;
            if (interpreter == null)
                return null;
            var calledMethod = interpreter.MidRepresentation;
            var otherMethodData = (Dictionary<int, bool>) calledMethod.GetAdditionalProperty(EscapeName);
            if (otherMethodData == null)
            {
                return null;
            }
            return otherMethodData;
        }

        public EscapingMode[] BuildEscapeModes(MethodInterpreter interpreter)
        {
            var parameters = new List<EscapingMode>();
            var analyzeProperties = interpreter.AnalyzeProperties;
            var methodArguments = analyzeProperties.Arguments;
            foreach (var argument in methodArguments)
            {
                var argumentData = analyzeProperties.GetVariableData(argument);
                parameters.Add(argumentData);
            }

            return parameters.ToArray();
        }

        public bool[] BuildEscapingBools(MethodBase method, ClosureEntities crRuntime)
        {
            var parameters = method.GetParameters();
            var escapingBools = new bool[parameters.Length + 1];

            var escapeData = this.EscapingParameterData(method, crRuntime);
            if (escapeData != null)
            {
                foreach (var escaping in escapeData)
                {
                    if (escaping.Value)
                        escapingBools[escaping.Key] = true;
                }
            }
            else
            {
                for (var index = 0; index <= parameters.Length; index++)
                {
                    escapingBools[index] = true;
                }
            }
            return escapingBools;
        }

        public bool ReadPurity(MethodBase methodBase, ClosureEntities crRuntime)
        {
            var method = this.GetInterpreter(methodBase, crRuntime);
            return _analyzeFunctionPurity.ReadPurity(method as CilMethodInterpreter);
        }


        public bool ReadNoStaticSideEffects(MethodBase methodBase, ClosureEntities crRuntime)
        {
            var method = this.GetInterpreter(methodBase, crRuntime) as CilMethodInterpreter;
            if (method != null && method.MidRepresentation != null)
            {
                return method.AnalyzeProperties.IsReadOnly;
            }
            return false;
        }

    }
}