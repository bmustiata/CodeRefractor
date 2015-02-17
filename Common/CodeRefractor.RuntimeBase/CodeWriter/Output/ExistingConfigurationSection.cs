using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeRefractor.CodeWriter.Output
{
    public class ExistingConfigurationSection : ConfigurationSection
    {
        private Dictionary<string, string> entries;

        public string Get(string name, string defaultValue = null)
        {
            string result;

            return entries.TryGetValue(name, out result)
                ? result
                : defaultValue;
        }
    }
}
