using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Pchp.CodeAnalysis.CommandLine;

namespace Peachpie.NET.Sdk.Tools
{
    /// <summary>
    /// Compilation task.
    /// </summary>
    public class BuildTask : Task
    {
        /// <summary></summary>
        [Required]
        public string OutputPath { get; set; }

        /// <summary></summary>
        [Required]
        public string OutputName { get; set; }

        /// <summary></summary>
        [Required]
        public string TempOutputPath { get; set; }

        /// <summary></summary>
        [Required]
        public string TargetFramework { get; set; }

        /// <summary></summary>
        public string NetFrameworkPath { get; set; }

        /// <summary></summary>
        public bool Optimize { get; set; } = true;

        /// <summary></summary>
        public string DebugType { get; set; }

        /// <summary></summary>
        public string PdbFile { get; set; }

        /// <summary></summary>
        public string DocumentationFile { get; set; }

        /// <summary></summary>
        public string Version { get; set; }

        /// <summary></summary>
        public bool EmitEntryPoint { get; set; }

        /// <summary></summary>
        public string EntryPoint { get; set; }

        /// <summary></summary>
        public string LangVersion { get; set; }

        /// <summary></summary>
        public string PhpDocTypes { get; set; }

        /// <summary></summary>
        public bool ShortOpenTags { get; set; }

        /// <summary></summary>
        public string NoWarn { get; set; }

        /// <summary></summary>
        public string KeyFile { get; set; }

        /// <summary></summary>
        public string PublicSign { get; set; } // empty, true, false

        /// <summary></summary>
        public string SourceLink { get; set; }

        /// <summary></summary>
        public string[] DefineConstants { get; set; }

        /// <summary></summary>
        public string[] ReferencePath { get; set; }

        /// <summary></summary>
        public string[] Compile { get; set; }

        /// <summary></summary>
        public override bool Execute()
        {
            //
            // compose compiler arguments:
            var args = new List<string>(1024)
            {
                "/output-name:" + OutputName,
                "/target:" + (EmitEntryPoint ? "exe" : "library"),
                Optimize ? "/o+" : "/o-",
            };

            if (HasDebugPlus)
            {
                args.Add("/debug+");
            }

            if (ShortOpenTags)
            {
                args.Add("/shortopentag+");
            }

            if (string.Equals(PublicSign, "true", StringComparison.OrdinalIgnoreCase))
                args.Add("/publicsign+");
            else if (string.Equals(PublicSign, "false", StringComparison.OrdinalIgnoreCase))
                args.Add("/publicsign-");

            AddNoEmpty(args, "target-framework", TargetFramework);
            AddNoEmpty(args, "temp-output", TempOutputPath);
            AddNoEmpty(args, "out", OutputPath);
            AddNoEmpty(args, "m", EntryPoint);
            AddNoEmpty(args, "pdb", PdbFile);
            AddNoEmpty(args, "debug-type", DebugType);// => emitPdb = true
            AddNoEmpty(args, "keyfile", KeyFile);
            AddNoEmpty(args, "xmldoc", DocumentationFile);
            AddNoEmpty(args, "langversion", LangVersion);
            AddNoEmpty(args, "v", Version);
            AddNoEmpty(args, "nowarn", NoWarn);
            AddNoEmpty(args, "phpdoctypes", PhpDocTypes);
            AddNoEmpty(args, "sourcelink", SourceLink);
            AddNoEmpty(args, "logger", "Peachpie.Compiler.Diagnostics.Observer, Peachpie.Compiler.Diagnostics");

            foreach (var d in DefineConstants)
            {
                args.Add("/d:" + d);
            }

            foreach (var r in ReferencePath)
            {
                args.Add("/r:" + r);
            }

            // sources at the end:
            foreach (var s in Compile)
            {
                args.Add(s);
            }

            //
            // save the arguments as .rsp file for debugging purposes:
            try
            {
                File.WriteAllText(Path.Combine(TempOutputPath, "dotnet-php.rsp"), string.Join(Environment.NewLine, args));
            }
            catch (Exception ex)
            {
                this.Log.LogWarningFromException(ex);
            }

            //
            // run the compiler:
            string libs = Environment.GetEnvironmentVariable("LIB") + @";C:\Windows\Microsoft.NET\assembly\GAC_MSIL";

            // compile
            var resultCode = PhpCompilerDriver.Run(
                PhpCommandLineParser.Default,
                null,
                args: args.ToArray(),
                clientDirectory: null,
                baseDirectory: Directory.GetCurrentDirectory(),
                sdkDirectory: NetFrameworkPath,
                additionalReferenceDirectories: libs,
                analyzerLoader: new SimpleAnalyzerAssemblyLoader(),
                output: Console.Out);

            return resultCode == 0;
        }

        bool AddNoEmpty(List<string> args, string optionName, string optionValue)
        {
            if (string.IsNullOrEmpty(optionValue))
            {
                return false;
            }

            args.Add("/" + optionName + ":" + optionValue);
            return true;
        }

        bool HasDebugPlus
        {
            get
            {
                if (DefineConstants != null)
                {
                    foreach (var c in DefineConstants)
                    {
                        if (string.Equals(c, "DEBUG", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }
    }
}
