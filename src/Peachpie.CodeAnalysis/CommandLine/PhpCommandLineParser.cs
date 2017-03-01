using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.CommandLine
{
    public sealed class PhpCommandLineParser : CommandLineParser
    {
        public static PhpCommandLineParser Default { get; } = new PhpCommandLineParser();

        protected override string RegularFileExtension { get; } = Constants.ScriptFileExtension;
        protected override string ScriptFileExtension { get; } = Constants.ScriptFileExtension;

        internal PhpCommandLineParser()
            : base(Errors.MessageProvider.Instance, false)
        {
        }

        static bool TryParseOption2(string arg, out string name, out string value)
        {
            // additional support for "--argument:value"
            // TODO: remove once implemented in CodeAnalysis
            if (arg.StartsWith("--"))
            {
                var colon = arg.IndexOf(':');
                if (colon > 0)
                {
                    name = arg.Substring(2, colon - 2).ToLowerInvariant();
                    value = arg.Substring(colon + 1);
                }
                else
                {
                    name = arg.Substring(2).ToLowerInvariant();
                    value = null;
                }
                return true;
            }

            //
            return TryParseOption(arg, out name, out value);
        }

        IEnumerable<CommandLineSourceFile> ExpandFileArgument(string path, string baseDirectory, List<Diagnostic> diagnostics)
        {
            if (string.IsNullOrEmpty(path))
            {
                return Array.Empty<CommandLineSourceFile>();
            }

            var gindex = path.IndexOf('*');
            if (gindex < 0)
            {
                return ParseFileArgument(path, baseDirectory, diagnostics);
            }

            var dir = baseDirectory;

            // root
            if (PathUtilities.IsDirectorySeparator(path[0])) // unix root
            {
                dir = "/";
            }
            else if (path[1] == ':') // windows root
            {
                dir = path.Substring(0, 3); // C:\
                path = path.Substring(3);
            }

            // process the path parts and go through the directory
            var parts = path.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                var p = parts[i];
                if (p == ".")
                {
                    // do nothing
                }
                else if (p == "..")
                {
                    // parent dir
                    dir = PathUtilities.GetDirectoryName(dir);
                }
                else if (p == "**")
                {
                    // all subdirs
                    return new[] { dir }.Concat(System.IO.Directory.GetDirectories(dir, "*", System.IO.SearchOption.AllDirectories))
                        .SelectMany((subdir) => ExpandFileArgument(string.Join("/", parts.Skip(i + 1)), subdir, diagnostics));
                }
                else
                {
                    if (i == parts.Length - 1)
                    {
                        // file
                        return ParseFileArgument(p, dir, diagnostics);
                    }
                    else
                    {
                        return System.IO.Directory.GetDirectories(dir, p, System.IO.SearchOption.TopDirectoryOnly)
                            .SelectMany((subdir) => ExpandFileArgument(string.Join("/", parts.Skip(i)), subdir, diagnostics));
                    }
                }
            }

            //return ParseFileArgument(path, baseDirectory, diagnostics);
            throw new ArgumentException();
        }

        internal override CommandLineArguments CommonParse(IEnumerable<string> args, string baseDirectory, string sdkDirectoryOpt, string additionalReferenceDirectories)
        {
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            List<string> flattenedArgs = new List<string>();
            List<string> scriptArgs = IsScriptRunner ? new List<string>() : null;
            FlattenArgs(args, diagnostics, flattenedArgs, scriptArgs, baseDirectory);

            var sourceFiles = new List<CommandLineSourceFile>();
            var metadataReferences = new List<CommandLineReference>();
            var analyzers = new List<CommandLineAnalyzerReference>();
            var additionalFiles = new List<CommandLineSourceFile>();
            var managedResources = new List<ResourceDescription>();
            string outputDirectory = baseDirectory;
            string outputFileName = null;
            string documentationPath = null;
            string moduleName = null;
            string runtimeMetadataVersion = null; // will be read from cor library if not specified in cmd
            string compilationName = null;
            bool optimize = false;
            bool concurrentBuild = true;
            PhpDocTypes phpdocTypes = PhpDocTypes.None;
            OutputKind outputKind = OutputKind.ConsoleApplication;
            bool optionsEnded = false;
            bool displayHelp = false, displayLogo = true;
            bool emitPdb = true, debugPlus = false;
            string mainTypeName = null, pdbPath = null;
            DebugInformationFormat debugInformationFormat = DebugInformationFormat.Pdb;
            List<string> referencePaths = new List<string>();
            if (sdkDirectoryOpt != null) referencePaths.Add(sdkDirectoryOpt);
            if (!string.IsNullOrEmpty(additionalReferenceDirectories)) referencePaths.AddRange(additionalReferenceDirectories.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

            foreach (string arg in flattenedArgs)
            {
                Debug.Assert(optionsEnded || !arg.StartsWith("@", StringComparison.Ordinal));

                string name, value;
                if (optionsEnded || !TryParseOption2(arg, out name, out value))
                {
                    sourceFiles.AddRange(ExpandFileArgument(arg, baseDirectory, diagnostics));
                    continue;
                }

                switch (name)
                {
                    case "?":
                    case "help":
                        displayHelp = true;
                        continue;

                    case "r":
                    case "reference":
                        metadataReferences.AddRange(ParseAssemblyReferences(arg, value, diagnostics, embedInteropTypes: false));
                        continue;

                    case "debug":
                        emitPdb = true;

                        // unused, parsed for backward compat only
                        if (!string.IsNullOrEmpty(value))
                        {
                            switch (value.ToLower())
                            {
                                case "full":
                                case "pdbonly":
                                    debugInformationFormat = DebugInformationFormat.Pdb;
                                    break;
                                case "portable":
                                    debugInformationFormat = DebugInformationFormat.PortablePdb;
                                    break;
                                case "embedded":
                                    debugInformationFormat = DebugInformationFormat.Embedded;
                                    break;
                                default:
                                    //AddDiagnostic(diagnostics, ErrorCode.ERR_BadDebugType, value);
                                    break;
                            }
                        }
                        continue;

                    case "debug+":
                        //guard against "debug+:xx"
                        if (value != null)
                            break;

                        emitPdb = true;
                        debugPlus = true;
                        continue;

                    case "debug-":
                        if (value != null)
                            break;

                        emitPdb = false;
                        debugPlus = false;
                        continue;

                    case "o":
                    case "optimize":
                    case "o+":
                    case "optimize+":
                        if (value != null)
                            break;

                        optimize = true;
                        continue;

                    case "o-":
                    case "optimize-":
                        if (value != null)
                            break;

                        optimize = false;
                        continue;

                    case "p":
                    case "parallel":
                    case "p+":
                    case "parallel+":
                        if (value != null)
                            break;

                        concurrentBuild = true;
                        continue;

                    case "p-":
                    case "parallel-":
                        if (value != null)
                            break;

                        concurrentBuild = false;
                        continue;

                    case "nologo":
                        displayLogo = false;
                        continue;

                    case "m":
                    case "main":
                        // Remove any quotes for consistent behavior as MSBuild can return quoted or 
                        // unquoted main.    
                        var unquoted = RemoveQuotesAndSlashes(value);
                        if (string.IsNullOrEmpty(unquoted))
                        {
                            //AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<text>", name);
                            //continue;
                            throw new ArgumentException("main");    // TODO: ErrorCode
                        }

                        mainTypeName = unquoted;
                        continue;

                    case "pdb":
                        if (string.IsNullOrEmpty(value))
                        {
                            //AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, arg);
                            throw new ArgumentException("pdb"); // TODO: ErrorCode
                        }
                        else
                        {
                            pdbPath = ParsePdbPath(value, diagnostics, baseDirectory);
                        }
                        continue;

                    case "out":
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            //AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, arg);
                            throw new ArgumentException("out"); // TODO: ErrorCode
                        }
                        else
                        {
                            ParseOutputFile(value, diagnostics, baseDirectory, out outputFileName, out outputDirectory);
                        }

                        continue;

                    case "t":
                    case "target":
                        if (value == null)
                        {
                            break; // force 'unrecognized option'
                        }

                        if (string.IsNullOrEmpty(value))
                        {
                            //AddDiagnostic(diagnostics, ErrorCode.FTL_InvalidTarget);
                            throw new ArgumentException("target"); // TODO: ErrorCode
                        }
                        else
                        {
                            outputKind = ParseTarget(value, diagnostics);
                        }

                        continue;

                    case "xmldoc":
                    case "doc":
                        documentationPath = value ?? string.Empty;
                        break;

                    case "phpdoctypes+":
                        phpdocTypes = PhpDocTypes.All;
                        break;
                    case "phpdoctypes-":
                        phpdocTypes = PhpDocTypes.None;
                        break;
                    case "phpdoctypes":
                        if (value == null)
                        {
                            phpdocTypes = PhpDocTypes.All;
                        }
                        else
                        {
                            phpdocTypes = (PhpDocTypes)Enum.Parse(typeof(PhpDocTypes), value);
                        }
                        break;

                    case "modulename":
                        var unquotedModuleName = RemoveQuotesAndSlashes(value);
                        if (string.IsNullOrEmpty(unquotedModuleName))
                        {
                            //AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), "modulename");
                            //continue;
                            throw new ArgumentException("modulename"); // TODO: ErrorCode
                        }
                        else
                        {
                            moduleName = unquotedModuleName;
                        }

                        continue;

                    case "runtimemetadataversion":
                        unquoted = RemoveQuotesAndSlashes(value);
                        if (string.IsNullOrEmpty(unquoted))
                        {
                            //AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<text>", name);
                            //continue;
                            throw new ArgumentException("runtimemetadataversion"); // TODO: ErrorCode
                        }

                        runtimeMetadataVersion = unquoted;
                        continue;

                    default:
                        break;
                }
            }

            GetCompilationAndModuleNames(diagnostics, outputKind, sourceFiles, sourceFiles.Count != 0, /*moduleAssemblyName*/null, ref outputFileName, ref moduleName, out compilationName);

            // XML Documentation path
            if (documentationPath != null)
            {
                if (documentationPath.Length == 0)
                {
                    // default xmldoc file name
                    documentationPath = compilationName + ".xml";
                }

                // resolve path
                documentationPath = PathUtilities.CombinePossiblyRelativeAndRelativePaths(outputDirectory, documentationPath);
            }

            var parseOptions = new PhpParseOptions
            (
                //languageVersion: languageVersion,
                //preprocessorSymbols: defines.ToImmutableAndFree(),
                documentationMode: DocumentationMode.Diagnose, // always diagnose
                kind: SourceCodeKind.Regular//,
                                            //features: parsedFeatures
            );

            var scriptParseOptions = parseOptions.WithKind(SourceCodeKind.Script);

            //// We want to report diagnostics with source suppression in the error log file.
            //// However, these diagnostics won't be reported on the command line.
            //var reportSuppressedDiagnostics = errorLogPath != null;

            var options = new PhpCompilationOptions
            (
                outputKind: outputKind,
                baseDirectory: baseDirectory,
                sdkDirectory: sdkDirectoryOpt,
                moduleName: moduleName,
                mainTypeName: mainTypeName,
                scriptClassName: WellKnownMemberNames.DefaultScriptClassName,
                phpdocTypes: phpdocTypes,
                //usings: usings,
                optimizationLevel: optimize ? OptimizationLevel.Release : OptimizationLevel.Debug,
                checkOverflow: false, // checkOverflow,
                                      //deterministic: deterministic,
                concurrentBuild: concurrentBuild,
                                        //cryptoKeyContainer: keyContainerSetting,
                                        //cryptoKeyFile: keyFileSetting,
                                        //delaySign: delaySignSetting,
                platform: Platform.AnyCpu // platform,
                                          //generalDiagnosticOption: generalDiagnosticOption,
                                          //warningLevel: warningLevel,
                                          //specificDiagnosticOptions: diagnosticOptions,
                                          //reportSuppressedDiagnostics: reportSuppressedDiagnostics,
                                          //publicSign: publicSign
            );

            if (debugPlus)
            {
                options = options.WithDebugPlusMode(debugPlus);
            }

            var emitOptions = new EmitOptions
            (
                metadataOnly: false,
                debugInformationFormat: debugInformationFormat,
                //pdbFilePath: null, // to be determined later
                //outputNameOverride: null, // to be determined later
                //baseAddress: baseAddress,
                //highEntropyVirtualAddressSpace: highEntropyVA,
                //fileAlignment: fileAlignment,
                //subsystemVersion: subsystemVersion,
                runtimeMetadataVersion: runtimeMetadataVersion
            );

            return new PhpCommandLineArguments()
            {
                // TODO: parsed arguments
                IsScriptRunner = IsScriptRunner,
                //InteractiveMode = interactiveMode || IsScriptRunner && sourceFiles.Count == 0,
                BaseDirectory = baseDirectory,
                //PathMap = pathMap,
                Errors = ImmutableArray<Diagnostic>.Empty,
                Utf8Output = true,
                CompilationName = compilationName,
                OutputFileName = outputFileName,
                PdbPath = pdbPath,
                EmitPdb = emitPdb,
                OutputDirectory = outputDirectory,
                DocumentationPath = documentationPath,
                //ErrorLogPath = errorLogPath,
                //AppConfigPath = appConfigPath,
                SourceFiles = sourceFiles.AsImmutable(),
                Encoding = Encoding.UTF8,
                ChecksumAlgorithm = SourceHashAlgorithm.Sha1, // checksumAlgorithm,
                MetadataReferences = metadataReferences.AsImmutable(),
                AnalyzerReferences = analyzers.AsImmutable(),
                AdditionalFiles = additionalFiles.AsImmutable(),
                ReferencePaths = referencePaths.AsImmutable(),
                SourcePaths = ImmutableArray<string>.Empty, //sourcePaths.AsImmutable(),
                //KeyFileSearchPaths = keyFileSearchPaths.AsImmutable(),
                //Win32ResourceFile = win32ResourceFile,
                //Win32Icon = win32IconFile,
                //Win32Manifest = win32ManifestFile,
                //NoWin32Manifest = noWin32Manifest,
                DisplayLogo = displayLogo,
                DisplayHelp = displayHelp,
                ManifestResources = managedResources.AsImmutable(),
                CompilationOptions = options,
                ParseOptions = IsScriptRunner ? scriptParseOptions : parseOptions,
                EmitOptions = emitOptions,
                //ScriptArguments = scriptArgs.AsImmutableOrEmpty(),
                //TouchedFilesPath = touchedFilesPath,
                //PrintFullPaths = printFullPaths,
                //ShouldIncludeErrorEndLocation = errorEndLocation,
                //PreferredUILang = preferredUILang,
                //SqmSessionGuid = sqmSessionGuid,
                //ReportAnalyzer = reportAnalyzer
            };
        }

        private void GetCompilationAndModuleNames(
            List<Diagnostic> diagnostics,
            OutputKind outputKind,
            List<CommandLineSourceFile> sourceFiles,
            bool sourceFilesSpecified,
            string moduleAssemblyName,
            ref string outputFileName,
            ref string moduleName,
            out string compilationName)
        {
            // simple name
            string simpleName = null;
            if (outputFileName != null)
            {
                simpleName = PathUtilities.GetFileName(outputFileName, false);
            }
            else if (sourceFiles.Count != 0)
            {
                simpleName = PathUtilities.GetFileName(sourceFiles[0].Path, false);
            }
            else
            {
                throw new ArgumentException("No source files specified.");  // TODO: ErrorCode
            }

            // assembly name
            compilationName = simpleName;

            if (moduleName == null)
            {
                moduleName = simpleName;
            }

            // file name
            if (outputFileName == null)
            {
                outputFileName = simpleName + outputKind.GetDefaultExtension();
            }
        }

        private static OutputKind ParseTarget(string value, IList<Diagnostic> diagnostics)
        {
            switch (value.ToLowerInvariant())
            {
                case "exe":
                    return OutputKind.ConsoleApplication;

                case "winexe":
                    return OutputKind.WindowsApplication;

                case "library":
                    return OutputKind.DynamicallyLinkedLibrary;

                case "module":
                    return OutputKind.NetModule;

                case "appcontainerexe":
                    return OutputKind.WindowsRuntimeApplication;

                case "winmdobj":
                    return OutputKind.WindowsRuntimeMetadata;

                default:
                    //AddDiagnostic(diagnostics, ErrorCode.FTL_InvalidTarget);
                    //return OutputKind.ConsoleApplication;
                    throw new ArgumentException("value");
            }
        }

        internal override void GenerateErrorForNoFilesFoundInRecurse(string path, IList<Diagnostic> errors)
        {
            // nothing
        }

        //private static void AddDiagnostic(IList<Diagnostic> diagnostics, ErrorCode errorCode, params object[] arguments)
        //{
        //    diagnostics.Add(Diagnostic.Create(Errors.MessageProvider.Instance, (int)errorCode, arguments));
        //}

        private IEnumerable<CommandLineReference> ParseAssemblyReferences(string arg, string value, IList<Diagnostic> diagnostics, bool embedInteropTypes)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("value");   // TODO: ErrorCode

            //if (value == null)
            //{
            //    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), arg);
            //    yield break;
            //}
            //else if (value.Length == 0)
            //{
            //    AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, arg);
            //    yield break;
            //}

            int eqlOrQuote = value.IndexOfAny(new[] { '"', '=' });

            string alias;
            if (eqlOrQuote >= 0 && value[eqlOrQuote] == '=')
            {
                alias = value.Substring(0, eqlOrQuote);
                value = value.Substring(eqlOrQuote + 1);

                //if (!SyntaxFacts.IsValidIdentifier(alias))
                //{
                //    AddDiagnostic(diagnostics, ErrorCode.ERR_BadExternIdentifier, alias);
                //    yield break;
                //}
            }
            else
            {
                alias = null;
            }

            List<string> paths = ParseSeparatedPaths(value).Where((path) => !string.IsNullOrWhiteSpace(path)).ToList();
            if (alias != null)
            {
                //if (paths.Count > 1)
                //{
                //    AddDiagnostic(diagnostics, ErrorCode.ERR_OneAliasPerReference, value);
                //    yield break;
                //}

                //if (paths.Count == 0)
                //{
                //    AddDiagnostic(diagnostics, ErrorCode.ERR_AliasMissingFile, alias);
                //    yield break;
                //}
                throw new NotSupportedException();  // TODO: ErrorCode
            }

            foreach (string path in paths)
            {
                var aliases = (alias != null) ? ImmutableArray.Create(alias) : ImmutableArray<string>.Empty;

                var properties = new MetadataReferenceProperties(MetadataImageKind.Assembly, aliases, embedInteropTypes);
                yield return new CommandLineReference(path, properties);
            }
        }
    }
}
