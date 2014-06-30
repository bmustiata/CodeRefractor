#region Uses

using System;
using CodeRefractor.RuntimeBase.Analyze;

#endregion

namespace CodeRefractor.MiddleEnd.SimpleOperations.Identifiers
{
    public class ConstValue : IdentifierValue
    {
        private object _value;

        public object Value
        {
            get { return _value; }
            set
            {
                if (value is ConstValue)
                {
                    throw new Exception(
                        "Invalid value, you try to set the constant description instead of constant itself");
                }
                _value = value;
            }
        }

        public ConstValue()
        {
        }

        public ConstValue(object value)
        {
            Value = value;
            FixedType = new TypeDescription(Value.GetType());
        }

        public override IdentifierValue Clone()
        {
            return new ConstValue(Value);
        }

        public override TypeDescription ComputedType()
        {
            return new TypeDescription(Value == null ? typeof (void) : Value.GetType());
        }

        public string Description
        {
            get { return string.Format("c: '{0}'", Value); }
        }

        public bool ValueEquals(ConstValue otherValue)
        {
            if (Value == null)
                return otherValue.Value == null;
            return Value.Equals(otherValue.Value);
        }

        public override string FormatVar()
        {
            if (Value == null)
                return "nullptr";
            var typeCode = Type.GetTypeCode(Value.GetType());
            if (typeCode == TypeCode.String)
            {
                return "'" + Value + "'";
            }else
            return Value.ToString();
        }

       
    }
}