﻿#region Uses

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodeRefractor.MiddleEnd;
using CodeRefractor.MiddleEnd.Interpreters;
using CodeRefractor.MiddleEnd.Interpreters.Cil;
using CodeRefractor.Runtime.Annotations;
using CodeRefractor.RuntimeBase;
using CodeRefractor.RuntimeBase.Shared;

#endregion

namespace CodeRefractor.ClosureCompute.Resolvers
{
    /**
     * A factory class for ResolveRuntimeMethod items.
     */
    public class ResolveRuntimeMethodProvider
    {
        private readonly CilMethodInterpreterProvider _cilMethodInterpreterProvider;

        public ResolveRuntimeMethodProvider(CilMethodInterpreterProvider cilMethodInterpreterProvider)
        {
            _cilMethodInterpreterProvider = cilMethodInterpreterProvider;
        }

        public ResolveRuntimeMethod Get(Assembly assembly, ClosureEntities closureEntities)
        {
            return new ResolveRuntimeMethod(
                assembly,
                closureEntities,
                _cilMethodInterpreterProvider);
        }
    }

    public class ResolveRuntimeMethod : MethodResolverBase
    {
        private readonly ClosureEntities _closureEntities;
        private readonly CilMethodInterpreterProvider _cilMethodInterpreterProvider;
        private readonly Dictionary<Type, Type> _solvedTypes;

        public ResolveRuntimeMethod(
            Assembly assembly,
            ClosureEntities closureEntities,
            CilMethodInterpreterProvider cilMethodInterpreterProvider) // provided
        {
            _closureEntities = closureEntities;
            _cilMethodInterpreterProvider = cilMethodInterpreterProvider;
            _solvedTypes = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<MapTypeAttribute>() != null)
                .ToDictionary(
                    tp => tp.GetCustomAttribute<MapTypeAttribute>().MappedType
                );
        }

        public override MethodInterpreter Resolve(MethodBase method)
        {
            var declaringType = method.DeclaringType;
            //Check for mapped methods
            Type resolvingType= _solvedTypes.Values.FirstOrDefault(h=>h==declaringType);
            if (resolvingType==null)
            {
                //Check for non-mapped methods
                _solvedTypes.TryGetValue(declaringType, out resolvingType);
                if(resolvingType==null)
                    return null;
            }

           

            if (method.IsConstructor)
            {
                return HandleConstructor(method, resolvingType);
            }
            var allMethods = resolvingType.GetMethods(ClosureEntitiesBuilder.AllFlags)
                .Where(m => m.Name == method.Name)
                .ToList();
            var resultMethod = CalculateResultMethod(method, allMethods,_closureEntities);

            if (resultMethod == null)
            {
                return null;
            }
            return ResolveMethodWithResult(resultMethod, method.DeclaringType);
        }

        private MethodInterpreter HandleConstructor(MethodBase method, Type resolvingType)
        {
            var allConstuctors = resolvingType.GetConstructors(ClosureEntitiesBuilder.AllFlags).ToArray();
            var methodParameters = method.GetParameters();
            
            foreach (var constuctor in allConstuctors)
            {
                var ctorParameters = constuctor.GetParameters();
                if (DoParametersMatch(methodParameters, ctorParameters))
                    return ResolveMethodWithResult(constuctor, resolvingType);
            }

            return null;
        }

        private bool DoParametersMatch(ParameterInfo[] srcParams, ParameterInfo[] targetParams)
        {
            if (srcParams.Length != targetParams.Length)
                return false;
            for (var index = 0; index < srcParams.Length; index++)
            {
                var param = srcParams[index];
                var targetParam = targetParams[index];
                if (param.ParameterType == targetParam.ParameterType) continue;
                return false;
            }
            return true;
        }


        public MethodInfo CalculateResultMethod(MethodBase method, List<MethodInfo> allMethods, ClosureEntities closureEntities)
        {
            var srcParams = method.GetParameters().Select(par => par.ParameterType).ToList();
            if (!method.IsStatic)
            {
                srcParams.Insert(0, method.DeclaringType);
            }
            foreach (var methodInfo in allMethods)
            {
                var methodName = methodInfo.Name;
                var attributeMethod = methodInfo.GetCustomAttributeT<MapMethod>();
                if (attributeMethod != null && !string.IsNullOrEmpty(attributeMethod.Name))
                    methodName = attributeMethod.Name;
                if (methodName != method.Name)
                    continue;
                var targetParams = methodInfo.GetParameters().Select(p=>p.ParameterType).ToList();
                if (!methodInfo.IsStatic)
                {
                    targetParams.Insert(0, method.DeclaringType);
                }
                if (srcParams.Count != targetParams.Count)
                    continue;
                var found = true;
                for (var index = 0; index < srcParams.Count; index++)
                {
                    var param = srcParams[index];
                    var reversedMappedType = targetParams[index]
                        .GetReversedMappedType(closureEntities);
                    if (param == reversedMappedType) continue;
                    found = false;
                    break;
                }
                if (found)
                    return methodInfo;

            }
            return null;
        }

        public MethodInterpreter ResolveMethodWithResult(MethodBase resultMethod, Type overrideType)
        {
            if (!CppMethodInterpreter.IsCppMethod(resultMethod))
            {
                var result = _cilMethodInterpreterProvider.Get(resultMethod);
                result.OverrideDeclaringType = overrideType;

                return result;
            }
            var cppResult = new CppMethodInterpreter(resultMethod)
            {
                OverrideDeclaringType = overrideType
            };

            cppResult.SetupInternalFields(resultMethod);
            return cppResult;

        }
    }
}