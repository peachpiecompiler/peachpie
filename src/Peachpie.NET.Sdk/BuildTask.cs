using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Pchp.CodeAnalysis.CommandLine;

namespace Peachpie.NET.Sdk.Tools
{
    /// <summary>
    /// Compilation task.
    /// </summary>
    public class BuildTask : Task, ICancelableTask // TODO: ToolTask
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
        public string PhpRelativePath { get; set; }

        /// <summary></summary>
        public string[] DefineConstants { get; set; }

        /// <summary></summary>
        public string[] ReferencePath { get; set; }

        /// <summary></summary>
        public string[] Compile { get; set; }

        // TODO: embed

        /// <summary></summary>
        public override bool Execute()
        {
            _cancellation = new CancellationTokenSource();

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
            AddNoEmpty(args, "subdir", PhpRelativePath);
            AddNoEmpty(args, "logger", "Peachpie.Compiler.Diagnostics.Observer,Peachpie.Compiler.Diagnostics");

			if (DefineConstants != null)
			{
				foreach (var d in DefineConstants)
				{
					args.Add("/d:" + d);
				}
			}

            if (ReferencePath != null && ReferencePath.Length != 0)
            {
                foreach (var r in ReferencePath)

                {
                    args.Add("/r:" + r);
                }
            }
            else
            {
                Log.LogWarning("No references specified.");
            }

            // sources at the end:
            if (Compile != null)
            {
                foreach (var s in Compile)
                {
                    args.Add(s);
                }
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
            try
            {
                var resultCode = PhpCompilerDriver.Run(
                    PhpCommandLineParser.Default,
                    null,
                    args: args.ToArray(),
                    clientDirectory: null,
                    baseDirectory: Directory.GetCurrentDirectory(),
                    sdkDirectory: NetFrameworkPath,
                    additionalReferenceDirectories: libs,
                    analyzerLoader: new SimpleAnalyzerAssemblyLoader(),
                    output: new LogWriter(this.Log),
                    cancellationToken: _cancellation.Token);
                
                return resultCode == 0;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions != null)
                {
                    foreach (var innerEx in ex.InnerExceptions)
                    {
                        this.Log.LogErrorFromException(innerEx);
                    }
                }
                else
                    this.Log.LogErrorFromException(ex);

                return false;
            }
            catch (Exception ex)
            {
                this.Log.LogErrorFromException(ex);
                return false;
            }
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

        private CancellationTokenSource _cancellation = new CancellationTokenSource();

        /// <summary>
        /// Cancels the task nicely.
        /// </summary>
        public void Cancel()
        {
            _cancellation.Cancel();
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

        // honestly I don't know why msbuild in VS does not handle Console.Output,
        // so we have our custom TextWriter that we pass to Log
        sealed class LogWriter : TextWriter
        {
            public LogWriter(TaskLoggingHelper log)
            {
                _log = log;
                this.NewLine = "\n";
            }

            TaskLoggingHelper _log;
            StringBuilder _text = new StringBuilder();
            
            public override Encoding Encoding => Encoding.UTF8;

            bool _logLine()
            {
                for (int i = 0; i < _text.Length; i++)
                {
                    if (_text[i] == '\n')
                    {
                        _logLine(_text.ToString(0, i));
                        _text.Remove(0, i + 1);
                        return true;
                    }
                }

                return false;
            }

            void _logLine(string line)
            {
                _log.LogMessageFromText(line, MessageImportance.High);
            }

            public override void Write(char value)
            {
                _text.Append(value);

                if (value == '\n')
                {
                    _logLine();
                }
            }

            public override void Write(string value)
            {
                _text.Append(value);
                _logLine();
            }
        }
    }
}
