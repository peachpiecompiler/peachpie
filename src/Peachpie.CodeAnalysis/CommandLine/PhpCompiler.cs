using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Devsense.PHP.Syntax;
using Devsense.PHP.Errors;
using Devsense.PHP.Text;
using Pchp.CodeAnalysis.Errors;
using Devsense.PHP.Syntax.Ast;
using System.Reflection;
using Pchp.CodeAnalysis.Utilities;
using Microsoft.CodeAnalysis.Collections;

namespace Pchp.CodeAnalysis.CommandLine
{
    /// <summary>
    /// Implementation of <c>pchp.exe</c>.
    /// </summary>
    internal class PhpCompiler : CommonCompiler
    {
        internal const string ResponseFileName = "php.rsp";

        private readonly DiagnosticFormatter _diagnosticFormatter = new DiagnosticFormatter();
        private readonly string _tempDirectory;

        protected internal new PhpCommandLineArguments Arguments { get { return (PhpCommandLineArguments)base.Arguments; } }

        public PhpCompiler(CommandLineParser parser, string responseFile, string[] args, BuildPaths buildPaths, string additionalReferenceDirectories, IAnalyzerAssemblyLoader analyzerLoader)
            : base(parser, responseFile, args, buildPaths, additionalReferenceDirectories, analyzerLoader)
        {
            _tempDirectory = buildPaths.TempDirectory;
        }

        public override DiagnosticFormatter DiagnosticFormatter => _diagnosticFormatter;

        internal override Type Type => typeof(PhpCompiler);

        public override Compilation CreateCompilation(TextWriter consoleOutput, TouchedFileLogger touchedFilesLogger, ErrorLogger errorLogger)
        {
            bool hadErrors = false;
            var sourceFiles = Arguments.SourceFiles;
            var trees = new PhpSyntaxTree[sourceFiles.Length];

            using (Arguments.CompilationOptions.Observers.StartMetric("parse"))
            {
                // PARSE

                var parseOptions = Arguments.ParseOptions;

                // We compute script parse options once so we don't have to do it repeatedly in
                // case there are many script files.
                var scriptParseOptions = parseOptions.WithKind(SourceCodeKind.Script);

                if (Arguments.CompilationOptions.ConcurrentBuild)
                {
                    Parallel.For(0, sourceFiles.Length, new Action<int>(i =>
                    {
                        //NOTE: order of trees is important!!
                        trees[i] = ParseFile(consoleOutput, parseOptions, scriptParseOptions, ref hadErrors, sourceFiles[i], errorLogger);
                    }));
                }
                else
                {
                    for (int i = 0; i < sourceFiles.Length; i++)
                    {
                        //NOTE: order of trees is important!!
                        trees[i] = ParseFile(consoleOutput, parseOptions, scriptParseOptions, ref hadErrors, sourceFiles[i], errorLogger);
                    }
                }

                // END PARSE
            }

            // If errors had been reported in ParseFile, while trying to read files, then we should simply exit.
            if (hadErrors)
            {
                return null;
            }

            var diagnostics = new List<DiagnosticInfo>();

            var assemblyIdentityComparer = DesktopAssemblyIdentityComparer.Default;

            var xmlFileResolver = new LoggingXmlFileResolver(Arguments.BaseDirectory, touchedFilesLogger);
            var sourceFileResolver = new LoggingSourceFileResolver(ImmutableArray<string>.Empty, Arguments.BaseDirectory, Arguments.PathMap, touchedFilesLogger);

            MetadataReferenceResolver referenceDirectiveResolver;
            var resolvedReferences = ResolveMetadataReferences(diagnostics, touchedFilesLogger, out referenceDirectiveResolver);
            if (ReportErrors(diagnostics, consoleOutput, errorLogger))
            {
                return null;
            }

            var referenceResolver = GetCommandLineMetadataReferenceResolver(touchedFilesLogger);
            var loggingFileSystem = new LoggingStrongNameFileSystem(touchedFilesLogger);
            var strongNameProvider = Arguments.GetStrongNameProvider(loggingFileSystem, _tempDirectory);

            var compilation = PhpCompilation.Create(
                Arguments.CompilationName,
                trees.WhereNotNull(),
                resolvedReferences,
                Arguments.CompilationOptions.
                    WithMetadataReferenceResolver(referenceResolver).
                    WithAssemblyIdentityComparer(assemblyIdentityComparer).
                    WithStrongNameProvider(strongNameProvider).
                    WithXmlReferenceResolver(xmlFileResolver).
                    WithSourceReferenceResolver(sourceFileResolver)
                    );

            return compilation;
        }

        private PhpSyntaxTree ParseFile(
            TextWriter consoleOutput,
            PhpParseOptions parseOptions,
            PhpParseOptions scriptParseOptions,
            ref bool hadErrors,
            CommandLineSourceFile file,
            ErrorLogger errorLogger)
        {
            var diagnosticInfos = new List<DiagnosticInfo>();
            var content = TryReadFileContent(file, diagnosticInfos);

            if (diagnosticInfos.Count != 0)
            {
                ReportErrors(diagnosticInfos, consoleOutput, errorLogger);
                hadErrors = true;
            }

            PhpSyntaxTree result = null;

            if (content != null)
            {
                result = PhpSyntaxTree.ParseCode(content.ToString(), parseOptions, scriptParseOptions, file.Path);
            }

            if (result != null && result.Diagnostics.HasAnyErrors())
            {
                ReportErrors(result.Diagnostics, consoleOutput, errorLogger);
                hadErrors = true;
            }

            return result;
        }

        public override void PrintHelp(TextWriter consoleOutput)
        {
            consoleOutput.WriteLine(PhpResources.IDS_Help);
        }

        public override void PrintLogo(TextWriter consoleOutput)
        {
            // {ToolName} version {ProductVersion}
            consoleOutput.WriteLine(PhpResources.IDS_Logo, GetToolName(), GetAssemblyFileVersion());
        }

        public override void PrintLangVersions(TextWriter consoleOutput)
        {
            consoleOutput.WriteLine(PhpResources.IDS_LangVersions);
            foreach (var version in PhpSyntaxTree.SupportedLanguageVersions)
            {
                consoleOutput.WriteLine(version.ToString(2));
            }
            consoleOutput.WriteLine();
        }

        internal static string GetVersion() => typeof(PhpCompiler).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        internal new string GetAssemblyFileVersion() => GetVersion();

        protected override ImmutableArray<DiagnosticAnalyzer> ResolveAnalyzersFromArguments(List<DiagnosticInfo> diagnostics, CommonMessageProvider messageProvider)
        {
            return Arguments.ResolveAnalyzersFromArguments(Constants.PhpLanguageName, diagnostics, messageProvider, AssemblyLoader);
        }

        protected override bool TryGetCompilerDiagnosticCode(string diagnosticId, out uint code)
        {
            return TryGetCompilerDiagnosticCode(diagnosticId, "PHP", out code);
        }

        internal override string GetToolName()
        {
            return PhpResources.IDS_ToolName;
        }

        internal override bool SuppressDefaultResponseFile(IEnumerable<string> args)
        {
            return args.Any(arg => new[] { "/noconfig", "-noconfig" }.Contains(arg.ToLowerInvariant()));
        }

        protected override void ResolveEmbeddedFilesFromExternalSourceDirectives(SyntaxTree tree, SourceReferenceResolver resolver, OrderedSet<string> embeddedFiles, DiagnosticBag diagnostics)
        {
            // We don't use any source mapping directives in PHP
        }
    }
}
