using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Peachpie.NET.Sdk.Tools
{
    /// <summary>
    /// Compilation task.
    /// </summary>
    public class BuildTask : Task, ICancelableTask // TODO: ToolTask
    {
        /// <summary>
        /// Path to <c>Peachpie.CodeAnalysis</c> executable.
        /// </summary>
        [Required]
        public string PeachpieCompilerFullPath { get; set; }

        /// <summary></summary>
        [Required]
        public string OutputPath { get; set; }

        /// <summary></summary>
        [Required]
        public string OutputName { get; set; }

        /// <summary></summary>
        [Required]
        public string TempOutputPath { get; set; }

        /// <summary>
        /// Optional <c>.rsp</c> file to be created.
        /// </summary>
        public string ResponseFilePath { get; set; }

        /// <summary></summary>
        [Required]
        public string TargetFramework { get; set; }

        /// <summary></summary>
        public string NetFrameworkPath { get; set; }

        /// <summary>
        /// The base project directory. Script paths are stored relatively to this path.
        /// If no value is specified, the current working directory is used.
        /// </summary>
        public string BasePath { get; set; }

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

        /// <summary></summary>
        /// <remarks>https://learn.microsoft.com/en-us/dotnet/standard/assembly/set-attributes-project-file#set-arbitrary-attributes</remarks>
        public ITaskItem[] AssemblyAttribute { get; set; }

        /// <summary>Autoload PSR-4 map. Each item provides properties:<br/>
        /// - Prefix<br/>
        /// - Path<br/>
        /// </summary>
        public ITaskItem[] Autoload_PSR4 { get; set; }

        /// <summary>Set of files to be included in autoload class-map.</summary>
        public string[] Autoload_ClassMap { get; set; }

        /// <summary>Set of files to be autoloaded (included) on each request.</summary>
        public string[] Autoload_Files { get; set; }

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

            if (AssemblyAttribute != null)
            {
                foreach (var attr in AssemblyAttribute)
                {
                    // metadata names
                    var props = new HashSet<string>(
                        attr.MetadataNames.OfType<string>(),
                        StringComparer.OrdinalIgnoreCase
                    );

                    // /attr:FQN("value1","value2","value3")
                    args.Add($@"/attr:{attr.ItemSpec}({string.Join(
                            ",",
                            Enumerable.Range(1, 128)
                            .Select(n => $"_Parameter{n}")
                            .TakeWhile(prop => props.Contains(prop))
                            .Select(prop => attr.GetMetadata(prop))
                            .Select(value => $"\"{value.Replace("\"", "\\\"")}\"")
                        )})");
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

            if (Autoload_Files != null)
            {
                foreach (var fname in Autoload_Files.Distinct())
                {
                    args.Add("/autoload:files," + fname);
                }
            }

            if (string.IsNullOrWhiteSpace(BasePath))
            {
                BasePath = Directory.GetCurrentDirectory();
            }

            if (!Directory.Exists(BasePath))
            {
                this.Log.LogWarning("Specified base directory '{0}' does not exist.", BasePath);
            }

            // sources at the end:
            if (Compile != null)
            {
                args.AddRange(Compile.Distinct(StringComparer.InvariantCulture));
            }

            if (DebuggerAttach)
            {
                args.Add("/attach");
                Debugger.Launch();
            }

            // save the arguments as .rsp file for debugging purposes:
            if (!string.IsNullOrEmpty(ResponseFilePath))
            {
                try
                {
                    File.WriteAllText(
                        Path.Combine(BasePath, ResponseFilePath), string.Join(Environment.NewLine, args.Select(line => $"\"{line.Replace("\\", "\\\\")}\""))
                    );
                }
                catch (Exception ex)
                {
                    this.Log.LogWarningFromException(ex);
                }
            }

            //
            // run the compiler:
            string libs = Environment.GetEnvironmentVariable("LIB") + @";C:\Windows\Microsoft.NET\assembly\GAC_MSIL";

            if (IsCanceled())
            {
                return false;
            }

            // compile using out-of-process compiler:
            try
            {
                //var resultCode = PhpCompilerDriver.Run(
                //    PhpCommandLineParser.Default,
                //    null,
                //    args: args.ToArray(),
                //    clientDirectory: null,
                //    baseDirectory: BasePath,
                //    sdkDirectory: NetFrameworkPath,
                //    additionalReferenceDirectories: libs,
                //    analyzerLoader: new SimpleAnalyzerAssemblyLoader(),
                //    output: new LogWriter(this.Log),
                //    cancellationToken: _cancellation.Token);

                var compilerArgs = new List<string>()
                {
                    $"/baseDirectory:{BasePath}",
                    $"/sdkDirectory:{NetFrameworkPath}",
                    $"/additionalReferenceDirectories:{libs}",
                };

                if (ResponseFilePath != null)
                {
                    compilerArgs.Add(
                        $"/responseFile:{ResponseFilePath}"
                    );
                }
                else
                {
                    // pass all arguments instead of a single .rsp file
                    compilerArgs.AddRange(
                        args
                    );
                }

                return RunCompiler(
                    compilerArgs.ToArray(),
                    _cancellation.Token
                );
            }
            catch (Exception ex)
            {
                LogException(ex);
                return false;
            }
        }

        bool RunCompiler(string[] args, CancellationToken cancellation = default(CancellationToken))
        {
            cancellation.ThrowIfCancellationRequested();

            var compilerPath = Path.GetFullPath(PeachpieCompilerFullPath);

            Debug.Assert(File.Exists(compilerPath));

            //
            var pi = new ProcessStartInfo(compilerPath)
            {
                Arguments = FlatternArgs(args),
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                UseShellExecute = false,
            };

            var output = new DataReceivedEventHandler((o, e) =>
            {
                if (e.Data != null)
                {
                    if (this.Log.LogMessageFromText(e.Data, MessageImportance.High) == false)
                    {
                        // plain text
                        this.Log.LogMessage(MessageImportance.High, e.Data);
                        Console.WriteLine(e.Data);
                    }
                }
            });

            // non-windows?
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                pi.FileName = "dotnet";
                pi.Arguments = $"\"{Path.ChangeExtension(compilerPath, ".dll")}\" {pi.Arguments}";
            }

            //
            this.Log.LogCommandLine(MessageImportance.High, $"{pi.FileName} {pi.Arguments}");

            //
            using (var process = new Process() { StartInfo = pi, })
            {
                process.OutputDataReceived += output;
                process.ErrorDataReceived += output;

                process.Start();
                
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                //
                using (var cancellationHandler = cancellation.Register(() =>
                {
                    if (process.HasExited == false)
                    {
                        this.Log.LogMessageFromText("Cancelled by user", MessageImportance.High);
                        // TODO: send signal first
                        process.Kill();
                    }
                }))
                {
                    process.WaitForExit();

                    //
                    return process.ExitCode == 0;
                }
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

        static bool AddNoEmpty(List<string> args, string optionName, string optionValue)
        {
            if (string.IsNullOrEmpty(optionValue))
            {
                return false;
            }

            args.Add("/" + optionName + ":" + optionValue);
            return true;
        }

        static string FlatternArgs(string[] args)
        {
            var sb = new StringBuilder(args.Length * 8);

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.IsNullOrEmpty(arg))
                {
                    continue;
                }

                if (sb.Length != 0)
                {
                    sb.Append(' ');
                }

                // sanitize {arg}
                sb.Append('\"');
                sb.Append(arg.Trim()
                //.Replace("\\", "\\\\")
                //.Replace("\"", "\\\"")
                );
                sb.Append('\"');
            }

            //
            return sb.ToString();
        }

        static string FormatArgFromItem(ITaskItem item, string switchName, params string[] metadataNames)
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
    }
}
