﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeRefractor.CompilerBackend.Optimizations.ConstantFoldingAndPropagation;
using CodeRefractor.CompilerBackend.Optimizations.Purity;
using CodeRefractor.RuntimeBase;
using CodeRefractor.RuntimeBase.MiddleEnd;
using CodeRefractor.CompilerBackend.OuputCodeWriter;
using System.Reflection;
using CodeRefractor.RuntimeBase.Shared;

namespace CodeRefractor.CompilerBackend.Linker
{
    class LinkerInterpretersTable
    {
        static LinkerInterpretersTable()
        {
            Instance = new LinkerInterpretersTable();
        }
        public Dictionary<string, MetaMidRepresentation> Methods =
            new Dictionary<string, MetaMidRepresentation>();
        public Dictionary<string, MethodBase> RuntimeMethods =
            new Dictionary<string, MethodBase>();
        public static LinkerInterpretersTable Instance { get; private set; }
        public static void Register(MetaMidRepresentation method)
        {
            var methodName = method.Method.WriteHeaderMethod(false);
            Instance.Methods[methodName] = method;
        }
        public static bool ReadPurity(MethodBase methodBase)
        {
            var method = methodBase.GetMethod();
            if(method!=null)
            {
                return AnalyzeFunctionPurity.ReadPurity(method);
            }

            var methodRuntimeInfo = methodBase.GetMethodDescriptor();
            if (!Instance.RuntimeMethods.ContainsKey(methodRuntimeInfo))
                return false;
            var runtimeMethod = Instance.RuntimeMethods[methodRuntimeInfo];
            return runtimeMethod.GetCustomAttribute<PureMethodAttribute>() != null;
        }

        public void RegisterRuntimeMethod(KeyValuePair<string, MethodBase> usedMethod)
        {
            RuntimeMethods[usedMethod.Key] = usedMethod.Value;
        }
    }
}