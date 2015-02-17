#region Uses

using System;
using CodeRefractor.FrontEnd.SimpleOperations.Identifiers;
using CodeRefractor.MiddleEnd.SimpleOperations.Identifiers;
using CodeRefractor.RuntimeBase.Analyze;
using CodeRefractor.Util;
using Ninject;

#endregion

namespace CodeRefractor.MiddleEnd.SimpleOperations.ConstTable
{
    public class ConstByteArrayValue : ConstValue
    {
        public readonly int Id;

        private readonly Provider<ConstByteArrayList> _constByteArrayList;

        [Inject]
        public ConstByteArrayValue(Provider<ConstByteArrayList> constByteArrayList)
        {
            this._constByteArrayList = constByteArrayList;
        }

        public ConstByteArrayValue(int id) : base(id)
        {
            Id = id;
            FixedType = new TypeDescription(typeof (byte));
            Value = _constByteArrayList.Value.ItemList[id];
        }
    }
}