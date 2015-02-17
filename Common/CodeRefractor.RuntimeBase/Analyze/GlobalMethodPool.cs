#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CodeRefractor.ClosureCompute;
using CodeRefractor.MiddleEnd;
using CodeRefractor.MiddleEnd.Interpreters;
using CodeRefractor.MiddleEnd.Interpreters.Cil;
using CodeRefractor.Runtime.Annotations;
using CodeRefractor.RuntimeBase;
using CodeRefractor.RuntimeBase.Analyze;
using CodeRefractor.RuntimeBase.MiddleEnd;

#endregion

namespace CodeRefractor.Analyze
{
    public class GlobalMethodPool
    {
        private readonly CilMethodInterpreterProvider _cilMethodInterpreterProvider;

        private readonly SortedDictionary<MethodInterpreterKey, MethodInterpreter> Interpreters =
            new SortedDictionary<MethodInterpreterKey, MethodInterpreter>();

        public readonly Dictionary<Assembly, CrTypeResolver> TypeResolvers
            = new Dictionary<Assembly, CrTypeResolver>();

        private readonly Dictionary<MethodBase, string> CachedKeys = new Dictionary<MethodBase, string>();

        public GlobalMethodPool(CilMethodInterpreterProvider cilMethodInterpreterProvider)
        {
            _cilMethodInterpreterProvider = cilMethodInterpreterProvider;
        }

        public void Register(MethodInterpreter interpreter)
        {
            var method = interpreter.Method;
            if (method == null)
                throw new InvalidDataException("Method is not mapped correctly");
            Interpreters[interpreter.ToKey()] = interpreter;
        }

        public MethodInterpreter Register(MethodBase method)
        {
            SetupTypeResolverIfNecesary(method);
            var interpreter = _cilMethodInterpreterProvider.Get(method);
            Register(interpreter);

            var resolved = Resolve(method);
            if (resolved!=null)
            {
                return resolved;
            }
            return null;
        }

        public MethodInterpreter Resolve(MethodBase interpreter)
        {
            SetupTypeResolverIfNecesary(interpreter);
            var resolvers = GetTypeResolvers();
            foreach (var resolver in resolvers)
            {
                var resolved = resolver.Resolve(interpreter);
                if (resolved!=null)
                    return resolved;
            }
            return null;
        }

        public CrTypeResolver[] GetTypeResolvers()
        {
            var resolvers = TypeResolvers.Values
                .Where(r => r != null)
                .ToArray();
            return resolvers;
        }

        private void SetupTypeResolverIfNecesary(MethodBase method)
        {
            try
            {

            
            if (method.DeclaringType == null) return;
            }
            catch (Exception ex)
            {

                
            }
            var assembly = method.DeclaringType.Assembly;

            var hasValue = TypeResolvers.ContainsKey(assembly);
            if (hasValue)
                return;
            var resolverType = assembly.GetTypes().FirstOrDefault(t => t.Name == "TypeResolver");

            CrTypeResolver resolver = null;
            if (resolverType != null)
                resolver = (CrTypeResolver) Activator.CreateInstance(resolverType);
            TypeResolvers[assembly] = resolver;
        }

        public MethodBase GetReversedMethod(MethodBase methodInfo, ClosureEntities crRuntime)
        {
            var reverseType = methodInfo.DeclaringType.GetMappedType(crRuntime);
            if (reverseType == methodInfo.DeclaringType)
                return methodInfo;
            var originalParameters = methodInfo.GetParameters();
            var memberInfos = reverseType.GetMember(methodInfo.Name);

            foreach (var memberInfo in memberInfos)
            {
                var methodBase = memberInfo as MethodBase;
                if (methodBase == null)
                    continue;
                var parameters = methodBase.GetParameters();
                if (parameters.Length != originalParameters.Length)
                    continue;
                var found = true;
                for (var index = 0; index < parameters.Length; index++)
                {
                    var parameter = parameters[index];
                    var originalParameter = originalParameters[index];
                    if (parameter.ParameterType == originalParameter.ParameterType) continue;
                    found = false;
                    break;
                }
                if (found)
                {
                    return methodBase;
                }
            }
            return methodInfo;
        }
    }
}