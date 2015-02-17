using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeRefractor.CodeWriter.Output
{
    /**
     * Holds a section based application configuration (key/value pairs)
     */
    class ApplicationConfiguration
    {
        public Dictionary<string, ConfigurationSection> ConfigurationSections;

        /**
         * Gets the given section of the configuration, or a 
         * Noop section that just resolves configuration defaults.
         */
        public ConfigurationSection GetSection(string name)
        {
            ConfigurationSection result;

            if (ConfigurationSections.TryGetValue(name, out result))
            {
                return result;
            }

            return null;
        }
    }
}
