#region Usings

using System.Collections.Generic;
using CodeRefractor.CodeWriter.Platform;
using Ninject;

#endregion

namespace CodeRefractor.CodeWriter.Linker
{
    // Singleton
    public class LinkingData
    {
        public static readonly List<PlatformInvokeDllImports> Libraries = new List<PlatformInvokeDllImports>();
        public static int LibraryMethodCount;

        public StringTable Strings;
        public GenerateTypeTableForIsInst IsInstTable = new GenerateTypeTableForIsInst();
        public readonly HashSet<string> Includes = new HashSet<string>();

        [Inject]
        public LinkingData(StringTable stringTable)
        {
            this.Strings = stringTable;
        }

        public bool SetInclude(string include)
        {
            if (string.IsNullOrWhiteSpace(include))
                return false;
            if (Includes.Contains(include))
                return false;
            Includes.Add(include);
            return true;
        }
    }
}