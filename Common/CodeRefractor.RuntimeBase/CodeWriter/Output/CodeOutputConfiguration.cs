using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeRefractor.Config;
using Ninject;
using Ninject.Activation;
using Ninject.Modules;

namespace CodeRefractor.CodeWriter.Output
{
    /**
     * Annotation that keeps in a string the enter characters.
     * Can be either: CR, LF, or CRLF.
     */
    public class EnterStyleAttribute : Attribute
    {
    }

    /**
     * Class that configures DI objects for CodeOutput.
     */
    class CodeOutputConfiguration : NinjectModule
    {
        public override void Load()
        {
            Bind<string>()
                .ToMethod(EnterStyle)
                .WhenClassHas<EnterStyleAttribute>();
        }

        /**
         * Reads the enter style.
         */
        private string EnterStyle(IContext context)
        {
            var applicationConfiguration = context.Kernel.Get<ApplicationConfiguration>();
            string lineEndingType = applicationConfiguration.GetSection("code-output")
                .Get("enter", "CRLF");

            switch (lineEndingType)
            {
                case "CRLF":
                    return "\r\n";
                case "CR":
                    return "\r";
                case "LF":
                    return "\n";
                case "LFCR":
                    return "\n\r";
                default:
                    return "\r\n";
            }
        }
    }
}
