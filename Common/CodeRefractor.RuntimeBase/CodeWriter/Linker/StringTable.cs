#region Usings

using System;
using System.Collections.Generic;
using System.Text;
using CodeRefractor.CodeWriter.Output;
using CodeRefractor.RuntimeBase;
using CodeRefractor.Util;
using Ninject;

#endregion

namespace CodeRefractor.CodeWriter.Linker
{
    public class StringTable
    {
        private readonly Dictionary<string, int> _stringsDictionary = new Dictionary<string, int>();
        private readonly List<string> _table = new List<string>();

        private Provider<CodeOutput> _codeOutputProvider;

        [Inject]
        public StringTable(Provider<CodeOutput> codeOutputProvider)
        {
            this._codeOutputProvider = codeOutputProvider;
        }

        public int GetStringId(string text)
        {
            int result;
            if (_stringsDictionary.TryGetValue(text, out result)) return result;
            result = _table.Count;
            _stringsDictionary[text] = result;
            _table.Add(text);
            return result;
        }

        private static short[] TextData(string text)
        {
            var result = new short[text.Length + 1];
            for (var i = 0; i < text.Length; i++)
            {
                result[i] = (short) text[i];
            }
            result[text.Length] = 0;
            return result;
        }

        public string BuildStringTable()
        {
            var sb = _codeOutputProvider.Value;
            sb.BlankLine()
                .Append("System_Void buildStringTable()")
                .BracketOpen();

            var stringDataBuilder = new List<string>();

            var jump = 0;
            foreach (var strItem in _table)
            {
                sb.AppendFormat("_AddJumpAndLength({0}, {1});\n", jump, strItem.Length);
                var itemTextData = TextData(strItem);
                AddTextToStringTable(stringDataBuilder, itemTextData, strItem);

                jump += strItem.Length + 1;
            }


            sb.BracketClose(assignedStatement: true)
                .Append(" // buildStringTable\n");

            var stringTableContent = String.Join(", " + Environment.NewLine, stringDataBuilder);
            var length = jump == 0 ? 1 : jump;
            sb.BlankLine()
                .AppendFormat("const System_Char _stringTable[{0}] =", length)
                .BracketOpen();
            sb.Append(jump == 0 ? "0" : stringTableContent);
            sb.BracketClose(assignedStatement: true)
                .Append("; // _stringTable\n");

            return sb.ToString();
        }

        private static void AddTextToStringTable(List<string> stringDataBuilder, short[] itemTextData, string strItem)
        {
            var itemsText = String.Join(", ", itemTextData);
            var commentedString = String.Format("/* {0} */", strItem.ToEscapedString());
            var resultItem = String.Format("{0} {1}", itemsText, commentedString);
            stringDataBuilder.Add(resultItem);
        }
    }
}