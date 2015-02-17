#region Uses

using CodeRefractor.CodeWriter.Linker;
using CodeRefractor.CodeWriter.Output;
using CodeRefractor.FrontEnd.SimpleOperations.Identifiers;
using CodeRefractor.MiddleEnd.Interpreters;
using CodeRefractor.MiddleEnd.SimpleOperations;
using CodeRefractor.MiddleEnd.SimpleOperations.Identifiers;
using CodeRefractor.RuntimeBase;
using CodeRefractor.Util;
using Ninject;

#endregion

namespace CodeRefractor.FrontEnd.SimpleOperations.Methods
{
    /**
     * A factory for return objects.
     */
    public class ReturnProvider
    {
        private readonly Provider<LinkerUtils> _linkerUtilsProvider;

        [Inject]
        public ReturnProvider(Provider<LinkerUtils> linkerUtilsProvider)
        {
            _linkerUtilsProvider = linkerUtilsProvider;
        }

        public Return Get(IdentifierValue returnValue)
        {
            return new Return(returnValue, _linkerUtilsProvider);
        }
    }

    public class Return : LocalOperation
    {
        private readonly Provider<LinkerUtils> _linkerUtils;

        [Inject]
        internal Return(IdentifierValue returnValue, Provider<LinkerUtils> linkerUtils)
            : base(OperationKind.Return)
        {
            _linkerUtils = linkerUtils;
            Returning = returnValue;
        }

        public IdentifierValue Returning { get; set; }
        
        public void WriteCodeToOutput(CodeOutput bodySb, MethodInterpreter interpreter)
        {
            bodySb.Append("\n");

            if (Returning == null)
            {
                bodySb.Append("return;");
            }
            else
            {
                //Need to expand this for more cases
                if (Returning is ConstValue)
                {
                    var retType = interpreter.Method.GetReturnType();
                    if (retType == typeof(string))
                    {
                        bodySb.AppendFormat("return {0};", _linkerUtils.Value.ComputedValue(Returning));
                    }
                    else
                    {
                        bodySb.AppendFormat("return {0};", Returning.Name);
                    }
                }
                else
                {
                    bodySb.AppendFormat("return {0};", Returning.Name);
                }
            }
        }
    }
}