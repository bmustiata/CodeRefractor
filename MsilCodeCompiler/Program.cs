﻿#region Usings

using System;
using System.IO;
using System.Reflection;
using CodeRefactor.OpenRuntime;
using CodeRefractor.ClosureCompute;
using CodeRefractor.Config;
using CodeRefractor.MiddleEnd.Optimizations.Util;
using CodeRefractor.RuntimeBase;
using CodeRefractor.RuntimeBase.Config;
using CodeRefractor.RuntimeBase.Optimizations;
using CodeRefractor.Util;
using Ninject;
using Ninject.Extensions.Factory;

#endregion

namespace CodeRefractor.Compiler
{
    public static class Program
    {
        /**
         *  Parses the command line, loads the assembly, builds a closure of all the items
         *  to be built, and transforms the into source code, and writes an output C++
         *  program.
         */
        public static string CallCompiler(IKernel kernel, string inputAssemblyName)
        {
            var commandLineParse = kernel.Get<CommandLineParse>();

            if (!String.IsNullOrEmpty(inputAssemblyName))
            {
                commandLineParse.ApplicationInputAssembly = inputAssemblyName;
            }

            var dir = Directory.GetCurrentDirectory();
            inputAssemblyName = Path.Combine(dir, commandLineParse.ApplicationInputAssembly);
            
            var asm = Assembly.LoadFile(inputAssemblyName);
            var definition = asm.EntryPoint; // TODO: what if this is not an application, but a library without an entry point?
            var start = Environment.TickCount;

            var closureEntities = kernel.Get<ClosureEntitiesUtils>()
                .BuildClosureEntities(definition, typeof(CrString).Assembly);

            var sb = closureEntities.BuildFullSourceCode();
            var compilationTime = Environment.TickCount - start;
            Console.WriteLine("Compilation time: {0} ms", compilationTime);

            var fullPath = commandLineParse.OutputCpp.GetFullFileName();
            sb.ToFile(fullPath);

            Console.WriteLine("Wrote output CPP file '{0}'.", fullPath);
            return fullPath;

            // TODO: this should be a flag, not necessarily do it automatically.
            //NativeCompilationUtils.CompileAppToNativeExe(commandLineParse.OutputCpp,
            //                                             commandLineParse.ApplicationNativeExe);
        }
        private static void Main(string[] args)
        {
            try
            {
                IKernel kernel = new StandardKernel(
                    new CodeRefractorNInjectModule()
                );

                var commandLineParse = kernel.Get<CommandLineParse>();
                commandLineParse.Process(args);

                OptimizationLevelBase.Instance = new OptimizationLevels();
                OptimizationLevelBase.OptimizerLevel = 2;
                OptimizationLevelBase.Instance.EnabledCategories.Add(OptimizationCategories.All);

                CallCompiler(kernel, "");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Calling the compiler failed: {0},\nStack trace: {1}",
                    e.Message, 
                    e.StackTrace);

                Console.ReadKey();

                Environment.Exit(1);
            }
        }
    }
}