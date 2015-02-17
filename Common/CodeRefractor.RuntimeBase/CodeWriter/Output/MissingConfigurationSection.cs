using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeRefractor.CodeWriter.Output
{
    /**
     * Implements a missing configuration section from an application
     * configuration. Since the section is not at all defined, returns
     * always the default value for the given variable.
     */
    public class MissingConfigurationSection : ConfigurationSection
    {
        public string Get(string name, string defaultValue = null)
        {
            return defaultValue;
        }
    }
}
