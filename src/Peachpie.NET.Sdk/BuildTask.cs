using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        /// <summary>
        /// Optimization level.
        /// Can be a boolean value (true/false), an integer specifying the level(0-9), or an optimization name (debug, release).</summary>
        public string Optimization { get; set; } = bool.TrueString;

        /// <summary></summary>
        public string DebugType { get; set; }

        /// <summary></summary>
        public string PdbFile { get; set; }

        /// <summary></summary>
        public string DocumentationFile { get; set; }

        /// <summary></summary>
        public string Version { get; set; }

        /// <summary></summary>
        public string OutputType { get; set; }

        /// <summary></summary>
        public bool GenerateFullPaths { get; set; }

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

        /// <summary> <c>/codepage</c> switch</summary>
        public string CodePage { get; set; }

        /// <summary></summary>
        public string[] DefineConstants { get; set; }

        /// <summary></summary>
        public string[] ReferencePath { get; set; }

        /// <summary></summary>
        public string[] Compile { get; set; }

        // TODO: embed

        /// <summary></summary>
        public ITaskItem[] Resources { get; set; }

        /// <summary>Autoload PSR-4 map. Each item provides properties:<br/>
        /// - Prefix<br/>
        /// - Path<br/>
        /// </summary>
        public ITaskItem[] Autoload_PSR4 { get; set; }

        /// <summary>Set of files to be included in autoload class-map.</summary>
        public string[] Autoload_ClassMap { get; set; }

        /// <summary>
        /// Used for debugging purposes.
        /// If enabled a debugger is attached to the current process upon the task execution.
        /// </summary>
        public bool DebuggerAttach { get; set; } = false;

        /// <summary></summary>
        public override bool Execute()
        {
            _cancellation = new CancellationTokenSource();

            // initiate our assembly resolver within MSBuild process:
            AssemblyResolver.InitializeSafe();

            if (IsCanceled())
            {
                return false;
            }

            //
            // compose compiler arguments:
            var args = new List<string>(1024)
            {
                "/output-name:" + OutputName,
                "/target:" + (string.IsNullOrEmpty(OutputType) ? "library" : OutputType),
                "/o:" + Optimization,
                "/fullpaths:" + GenerateFullPaths.ToString(),
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
            AddNoEmpty(args, "codepage", CodePage);
            AddNoEmpty(args, "subdir", PhpRelativePath);
            
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

            if (Resources != null)
            {
                foreach (var res in Resources)
                {
                    args.Add(FormatArgFromItem(res, "res", "LogicalName", "Access"));
                }
            }

            if (Autoload_PSR4 != null)
            {
                foreach (var psr4map in Autoload_PSR4)
                {
                    //args.Add(FormatArgFromItem(psr4map, "autoload", "Prefix", "Path")); // Prefix can be empty!
                    args.Add($"/autoload:psr-4,{psr4map.GetMetadata("Prefix")},{psr4map.GetMetadata("Path")}");
                }
            }

            if (Autoload_ClassMap != null)
            {
                foreach (var fname in Autoload_ClassMap.Distinct())
                {
                    args.Add("/autoload:classmap," + fname);
                }
            }

            // sources at the end:
            if (Compile != null)
            {
                foreach (var s in Compile)
                {
                    args.Add(s);
                }
            }

#if DEBUG
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
#endif

            //
            // run the compiler:
            string libs = Environment.GetEnvironmentVariable("LIB") + @";C:\Windows\Microsoft.NET\assembly\GAC_MSIL";

            if (IsCanceled())
            {
                return false;
            }

            // Debugger.Launch
            if (DebuggerAttach)
            {
                Debugger.Launch();
            }

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
            catch (Exception ex)
            {
                LogException(ex);
                return false;
            }
        }

        void LogException(Exception ex)
        {
            if (ex is AggregateException aex && aex.InnerExceptions != null)
            {
                foreach (var innerEx in aex.InnerExceptions)
                {
                    LogException(innerEx);
                }
            }
            else
            {
                this.Log.LogErrorFromException(ex, true);
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

        private string FormatArgFromItem(ITaskItem item, string switchName, params string[] metadataNames)
        {
            var arg = new StringBuilder($"/{switchName}:{item.ItemSpec}");

            foreach (string name in metadataNames)
            {
                string value = item.GetMetadata(name);
                if (string.IsNullOrEmpty(value))
                {
                    // The values are expected in linear order, so we have to end at the first missing one
                    break;
                }

                arg.Append(',');
                arg.Append(value);
            }

            return arg.ToString();
        }

        private CancellationTokenSource _cancellation = new CancellationTokenSource();

        /// <summary>
        /// Cancels the task nicely.
        /// </summary>
        public void Cancel()
        {
            _cancellation.Cancel();
        }

        /// <summary>
        /// Gets value indicating user has canceled the task.
        /// </summary>
        public bool IsCanceled()
        {
            return _cancellation != null && _cancellation.IsCancellationRequested;
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
            TaskLoggingHelper Log { get; }

            StringBuilder Buffer { get; } = new StringBuilder();

            public override Encoding Encoding => Encoding.UTF8;

            public LogWriter(TaskLoggingHelper log)
            {
                Debug.Assert(log != null);

                this.Log = log;
                this.NewLine = "\n";
            }

            bool TryLogCompleteMessage()
            {
                string line = null;

                lock (Buffer)   // accessed in parallel
                {
                    // get line from the buffer:
                    for (int i = 0; i < Buffer.Length; i++)
                    {
                        if (Buffer[i] == '\n')
                        {
                            line = Buffer.ToString(0, i);

                            Buffer.Remove(0, i + 1);
                        }
                    }
                }

                //
                return line != null && LogCompleteMessage(line);
            }

            bool LogCompleteMessage(string line)
            {
                // TODO: following logs only Warnings and Errors,
                // to log Info diagnostics properly, parse it by ourselves

                return this.Log.LogMessageFromText(line.Trim(), MessageImportance.High);
            }

            public override void Write(char value)
            {
                lock (Buffer) // accessed in parallel
                {
                    Buffer.Append(value);
                }

                if (value == '\n')
                {
                    TryLogCompleteMessage();
                }
            }

            public override void Write(string value)
            {
                lock (Buffer)
                {
                    Buffer.Append(value);
                }

                TryLogCompleteMessage();
            }
        }
    }
}
