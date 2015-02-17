#region Uses

using System.Reflection;
using CodeRefractor.MiddleEnd;
using CodeRefractor.MiddleEnd.Interpreters.Cil;

#endregion

namespace CodeRefractor.ClosureCompute
{
    /// <summary>
    /// Are various algorithms which find if new types
    /// or methods are added to the CR closure
    /// </summary>
    public abstract class ClosureComputeBase
    {
        public abstract bool UpdateClosure(ClosureEntities closureEntities);

        // TODO: move this utility method into a different class.
        protected static void AddMethodToClosure(
            CilMethodInterpreterProvider cilMethodInterpreterProvider, 
            ClosureEntities closureEntities, 
            MethodBase method)
        {
            var interpreter = closureEntities.ResolveMethod(method) ?? cilMethodInterpreterProvider.Get(method);
            var intepreter = interpreter as CilMethodInterpreter;
            if (intepreter != null)
            {
                intepreter.Process(closureEntities);
            }
            closureEntities.UseMethod(method, interpreter);
        }
    }
}