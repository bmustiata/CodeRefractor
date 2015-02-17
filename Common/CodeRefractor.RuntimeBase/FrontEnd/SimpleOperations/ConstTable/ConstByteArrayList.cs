#region Uses

using System;
using System.Collections.Generic;
using System.Text;
using CodeRefractor.CodeWriter.Output;
using CodeRefractor.Util;
using Ninject;

#endregion

namespace CodeRefractor.MiddleEnd.SimpleOperations.ConstTable
{
    public class ConstByteArrayList
    {
        public Dictionary<ConstByteArrayData, int> Items =
            new Dictionary<ConstByteArrayData, int>(new ConstByteArrayData.EqualityComparer());

        public List<ConstByteArrayData> ItemList = new List<ConstByteArrayData>();

        private Provider<CodeOutput> _codeOutputProvider;

        [Inject]
        public ConstByteArrayList(Provider<CodeOutput> codeOutputProvider)
        {
            this._codeOutputProvider = codeOutputProvider;
        }

        public int RegisterConstant(byte[] values)
        {
            var data = new ConstByteArrayData(values);
            int resultId;
            
            if (Items.TryGetValue(data, out resultId))
            {
                return resultId;
            }
            
            var id = Items.Count;
            
            ItemList.Add(data);
            Items[data] = id;

            return id;
        }

        public string BuildConstantTable()
        {
            var sb = _codeOutputProvider.Value;

            sb.BlankLine()
                .Append("System_Void RuntimeHelpersBuildConstantTable()")
                .BracketOpen();

            foreach (var item in ItemList)
            {
                var rightArray = item.Data;
                var rightArrayItems = string.Join(", ", rightArray);

                sb.AppendFormat("AddConstantByteArray(new byte[{0}]);", rightArray.Length)
                    .BracketOpen()
                    .AppendFormat("{1}", rightArrayItems)
                    .BracketClose();
            }

            return sb.BracketClose()
                .ToString();
        }
    }
}