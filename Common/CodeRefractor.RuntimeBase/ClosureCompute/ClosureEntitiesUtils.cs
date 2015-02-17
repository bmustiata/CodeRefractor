using System;
using System.Linq;
using System.Reflection;
using CodeRefractor.ClosureCompute.Resolvers;
using CodeRefractor.RuntimeBase;
using CodeRefractor.Util;

namespace CodeRefractor.ClosureCompute
{
    public class ClosureEntitiesUtils
    {
        private Provider<ClosureEntities> _closureEntitiesProvider;
        private readonly ResolveRuntimeMethodProvider _resolveRuntimeMethodProvider;

        public ClosureEntitiesUtils(
            Provider<ClosureEntities> closureEntitiesProvider,
            ResolveRuntimeMethodProvider resolveRuntimeMethodProvider)
        {
            this._closureEntitiesProvider = closureEntitiesProvider;
            _resolveRuntimeMethodProvider = resolveRuntimeMethodProvider;
        }

        public ClosureEntities BuildClosureEntities(MethodInfo definition, Assembly runtimeAssembly)
        {
            var closureEntities = _closureEntitiesProvider.Value;

            closureEntities.EntryPoint = definition;

            var resolveRuntimeMethod = _resolveRuntimeMethodProvider.Get(runtimeAssembly, closureEntities);
            closureEntities.AddMethodResolver(resolveRuntimeMethod);

            closureEntities.AddMethodResolver(new ResolvePlatformInvokeMethod());

            var extensionsResolverMethod = new ResolveRuntimeMethodUsingExtensions(runtimeAssembly, closureEntities);
            closureEntities.AddMethodResolver(extensionsResolverMethod);

            closureEntities.EntitiesBuilder.AddTypeResolver(new ResolveRuntimeType(runtimeAssembly));

            closureEntities.ComputeFullClosure();
            closureEntities.OptimizeClosure(closureEntities);

            return closureEntities;
        }
    }
}