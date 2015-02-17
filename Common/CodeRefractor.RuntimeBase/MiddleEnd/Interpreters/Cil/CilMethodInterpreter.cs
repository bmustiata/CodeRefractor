using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodeRefractor.ClosureCompute;
using CodeRefractor.FrontEnd;
using CodeRefractor.FrontEnd.SimpleOperations;
using CodeRefractor.FrontEnd.SimpleOperations.Methods;
using CodeRefractor.MiddleEnd.SimpleOperations;
using CodeRefractor.MiddleEnd.SimpleOperations.Methods;
using CodeRefractor.RuntimeBase;
using CodeRefractor.Util;
using Ninject;

namespace CodeRefractor.MiddleEnd.Interpreters.Cil
{
    /**
     * A CilMethodInterpreter provider.
     */
    public class CilMethodInterpreterProvider
    {
        private readonly Provider<MethodMidRepresentationBuilder> _methodMidRepresentationBuilderProvider;

        public CilMethodInterpreterProvider(
            Provider<MethodMidRepresentationBuilder> methodMidRepresentationBuilderProvider)
        {
            _methodMidRepresentationBuilderProvider = methodMidRepresentationBuilderProvider;
        }

        public CilMethodInterpreter Get(MethodBase method)
        {
            return new CilMethodInterpreter(
                method,
                _methodMidRepresentationBuilderProvider); // provided
        }
    }

    public class CilMethodInterpreter : MethodInterpreter, IEnumerable<LocalOperation>
    {
        private readonly Provider<MethodMidRepresentationBuilder> _methodMidRepresentationBuilderProvider;

        internal CilMethodInterpreter(MethodBase method, 
            Provider<MethodMidRepresentationBuilder> methodMidRepresentationBuilderProvider)
            : base(method)
        {
            _methodMidRepresentationBuilderProvider = methodMidRepresentationBuilderProvider;
            Kind = MethodKind.CilInstructions;
        }

        public readonly MetaMidRepresentation MidRepresentation = new MetaMidRepresentation();

        public bool Interpreted { get; set; }

        public void Process(ClosureEntities closureEntities)
        {
            if (Kind != MethodKind.CilInstructions)
                return;
            if (Interpreted)
                return;
            Ensure.AreEqual(false, PlatformInvokeMethod.IsPlatformInvoke(Method),
                string.Format("Should not run it on current method: {0}", Method)
                );
            if (Method.GetMethodBody() == null)
                return;

            MidRepresentation.Vars.SetupLocalVariables(Method);
            _methodMidRepresentationBuilderProvider.Value
            var midRepresentationBuilder = new MethodMidRepresentationBuilder(this, Method);
            midRepresentationBuilder.ProcessInstructions(closureEntities);
            Interpreted = true;
        }

        public IEnumerator<LocalOperation> GetEnumerator()
        {
            return MidRepresentation.LocalOperations.GetEnumerator();
        }

        public override string ToString()
        {
            var method = Method;
            var declaringName = method.DeclaringType.Name;
            if (method.IsConstructor)
            {
                return string.Format("{0}.ctor", declaringName);
            }

            var startName = string.Format("{0}.{1}", declaringName, method.Name);
            var declaringParams = string.Join(", ",
                method.GetParameters().Select(p => p.ParameterType.Name)
                ).ToArray();

            return string.Format("{0}({1})",startName, declaringParams);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}