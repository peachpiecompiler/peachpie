using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.CommandLine
{
    internal sealed class PhpCommandLineParser : CommandLineParser
    {
        public static PhpCommandLineParser Default { get; } = new PhpCommandLineParser();

        protected override string RegularFileExtension { get; } = Constants.ScriptFileExtension;
        protected override string ScriptFileExtension { get; } = Constants.ScriptFileExtension;

        internal PhpCommandLineParser()
            : base(Errors.MessageProvider.Instance, false)
        {
        }

        internal override CommandLineArguments CommonParse(IEnumerable<string> args, string baseDirectory, string sdkDirectoryOpt, string additionalReferenceDirectories)
        {
            var sourceFiles = new List<CommandLineSourceFile>();
            var metadataReferences = new List<CommandLineReference>();
            var analyzers = new List<CommandLineAnalyzerReference>();
            var additionalFiles = new List<CommandLineSourceFile>();
            var managedResources = new List<ResourceDescription>();
            string outputFileName = null;
            string moduleName = null;
            string compilationName = null;
            var outputKind = OutputKind.DynamicallyLinkedLibrary;

            // DEBUG
            sourceFiles.Add(new CommandLineSourceFile("test.php", false));
            metadataReferences.Add(new CommandLineReference(@"C:\Windows\Microsoft.NET\assembly\GAC_64\mscorlib\v4.0_4.0.0.0__b77a5c561934e089\mscorlib.dll", new MetadataReferenceProperties(MetadataImageKind.Assembly)));
            metadataReferences.Add(new CommandLineReference(@"C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Collections\v4.0_4.0.0.0__b03f5f7f11d50a3a\System.Collections.dll", new MetadataReferenceProperties(MetadataImageKind.Assembly)));
            compilationName = "test";
            outputFileName = moduleName = "test" + outputKind.GetDefaultExtension();
            // 

            var parseOptions = new PhpParseOptions
            (
                //languageVersion: languageVersion,
                //preprocessorSymbols: defines.ToImmutableAndFree(),
                //documentationMode: parseDocumentationComments ? DocumentationMode.Diagnose : DocumentationMode.None,
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
                moduleName: moduleName,
                //mainTypeName: mainTypeName,
                scriptClassName: WellKnownMemberNames.DefaultScriptClassName,
                //usings: usings,
                optimizationLevel: OptimizationLevel.Debug, //optimize ? OptimizationLevel.Release : OptimizationLevel.Debug,
                //checkOverflow: checkOverflow,
                //allowUnsafe: allowUnsafe,
                //deterministic: deterministic,
                concurrentBuild: false, //concurrentBuild,  // TODO: true in Release
                //cryptoKeyContainer: keyContainerSetting,
                //cryptoKeyFile: keyFileSetting,
                //delaySign: delaySignSetting,
                platform: Platform.AnyCpu //, // platform,
                //generalDiagnosticOption: generalDiagnosticOption,
                //warningLevel: warningLevel,
                //specificDiagnosticOptions: diagnosticOptions,
                //reportSuppressedDiagnostics: reportSuppressedDiagnostics,
                //publicSign: publicSign
            );

            //if (debugPlus)
            //{
            //    options = options.WithDebugPlusMode(debugPlus);
            //}

            var emitOptions = new EmitOptions
            (
                //metadataOnly: false,
                //debugInformationFormat: debugInformationFormat,
                //pdbFilePath: null, // to be determined later
                //outputNameOverride: null, // to be determined later
                //baseAddress: baseAddress,
                //highEntropyVirtualAddressSpace: highEntropyVA,
                //fileAlignment: fileAlignment,
                //subsystemVersion: subsystemVersion,
                runtimeMetadataVersion: ".NET 4.0"
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
                //PdbPath = pdbPath,
                //EmitPdb = emitPdb,
                OutputDirectory = baseDirectory,     // TODO: out dir
                //DocumentationPath = documentationPath,
                //ErrorLogPath = errorLogPath,
                //AppConfigPath = appConfigPath,
                SourceFiles = sourceFiles.AsImmutable(),
                Encoding = Encoding.UTF8,
                ChecksumAlgorithm = SourceHashAlgorithm.Sha1, // checksumAlgorithm,
                MetadataReferences = metadataReferences.AsImmutable(),
                AnalyzerReferences = analyzers.AsImmutable(),
                AdditionalFiles = additionalFiles.AsImmutable(),
                ReferencePaths = ImmutableArray<string>.Empty, //referencePaths,
                SourcePaths = ImmutableArray<string>.Empty, //sourcePaths.AsImmutable(),
                //KeyFileSearchPaths = keyFileSearchPaths.AsImmutable(),
                //Win32ResourceFile = win32ResourceFile,
                //Win32Icon = win32IconFile,
                //Win32Manifest = win32ManifestFile,
                //NoWin32Manifest = noWin32Manifest,
                //DisplayLogo = displayLogo,
                //DisplayHelp = displayHelp,
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

        internal override void GenerateErrorForNoFilesFoundInRecurse(string path, IList<Diagnostic> errors)
        {
            // nothing
        }
    }
}
