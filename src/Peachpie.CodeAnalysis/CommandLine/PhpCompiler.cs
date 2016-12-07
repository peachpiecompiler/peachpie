using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Devsense.PHP.Syntax;
using Devsense.PHP.Errors;
using Devsense.PHP.Text;
using Pchp.CodeAnalysis.Errors;
using Devsense.PHP.Syntax.Ast;

namespace Pchp.CodeAnalysis.CommandLine
{
    /// <summary>
    /// Implementation of <c>pchp.exe</c>.
    /// </summary>
    internal class PhpCompiler : CommonCompiler
    {
        internal const string ResponseFileName = "php.rsp";

        private readonly DiagnosticFormatter _diagnosticFormatter = new DiagnosticFormatter();

        protected internal new PhpCommandLineArguments Arguments { get { return (PhpCommandLineArguments)base.Arguments; } }

        public PhpCompiler(CommandLineParser parser, string responseFile, string[] args, string clientDirectory, string baseDirectory, string sdkDirectory, string additionalReferenceDirectories, IAnalyzerAssemblyLoader analyzerLoader)
            :base(parser, responseFile, args, clientDirectory, baseDirectory, sdkDirectory, additionalReferenceDirectories, analyzerLoader)
        {

        }

        public override DiagnosticFormatter DiagnosticFormatter => _diagnosticFormatter;

        public override Compilation CreateCompilation(TextWriter consoleOutput, TouchedFileLogger touchedFilesLogger, ErrorLogger errorLogger)
        {
            var parseOptions = Arguments.ParseOptions;

            // We compute script parse options once so we don't have to do it repeatedly in
            // case there are many script files.
            var scriptParseOptions = parseOptions.WithKind(SourceCodeKind.Script);

            bool hadErrors = false;

            var sourceFiles = Arguments.SourceFiles;
            var trees = new SourceUnit[sourceFiles.Length];

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
            var strongNameProvider = new LoggingStrongNameProvider(Arguments.KeyFileSearchPaths, touchedFilesLogger);

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

        private SourceUnit ParseFile(
            TextWriter consoleOutput,
            PhpParseOptions parseOptions,
            PhpParseOptions scriptParseOptions,
            ref bool hadErrors,
            CommandLineSourceFile file,
            ErrorLogger errorLogger)
        {
            var diagnosticInfos = new List<DiagnosticInfo>();
            var content = ReadFileContent(file, diagnosticInfos);

            if (diagnosticInfos.Count != 0)
            {
                ReportErrors(diagnosticInfos, consoleOutput, errorLogger);
                hadErrors = true;
            }

            var diagnostics = new List<Diagnostic>();
            SourceUnit result = null;

            if (content != null)
            {
                result = ParseFile(consoleOutput, parseOptions, scriptParseOptions, content, file, diagnostics);
            }

            if (diagnostics.Count != 0)
            {
                ReportErrors(diagnostics, consoleOutput, errorLogger);
                hadErrors = true;
                diagnostics.Clear();
            }

            return result;
        }

        class ErrorSink : IErrorSink<Span>
        {
            readonly List<Diagnostic> _diagnostics;
            private SourceUnit _sourceUnit;
            private SyntaxTree _lazySyntaxTree;

            public ErrorSink(List<Diagnostic> diagnostics, SourceUnit sourceUnit)
            {
                Contract.ThrowIfNull(diagnostics);
                _diagnostics = diagnostics;
                _sourceUnit = sourceUnit;
            }

            private SyntaxTree LazySyntaxTree
            {
                get
                {
                    if (_lazySyntaxTree == null)
                    {
                        _lazySyntaxTree = new SyntaxTreeAdapter(_sourceUnit);
                    }
                    return _lazySyntaxTree;
                }
            }

            public void Error(Span span, ErrorInfo info, params string[] argsOpt)
            {
                var location = new SourceLocation(
                    LazySyntaxTree, 
                    new Microsoft.CodeAnalysis.Text.TextSpan(span.Start, span.Length));
                ParserMessageProvider.Instance.RegisterError(info);
                var diagnostic = ParserMessageProvider.Instance.CreateDiagnostic(
                    info.Severity == ErrorSeverity.WarningAsError, info.Id, location, argsOpt);
                _diagnostics.Add(diagnostic);
            }
        }

        private static SourceUnit ParseFile(
            TextWriter consoleOutput,
            PhpParseOptions parseOptions,
            PhpParseOptions scriptParseOptions,
            SourceText content,
            CommandLineSourceFile file,
            List<Diagnostic> diagnostics)
        {
            // TODO: new parser implementation based on Roslyn

            // TODO: file.IsScript ? scriptParseOptions : parseOptions
            var unit = new CodeSourceUnit(content.ToString(), file.Path, Encoding.UTF8);
            var errorSink = new ErrorSink(diagnostics, unit);
            unit.Parse(new BasicNodesFactory(unit), errorSink);

            return unit;
        }

        public override void PrintHelp(TextWriter consoleOutput)
        {
            consoleOutput.WriteLine(PhpResources.IDS_Help);
        }

        public override void PrintLogo(TextWriter consoleOutput)
        {
            consoleOutput.WriteLine(PhpResources.IDS_Logo);
        }

        protected override void CompilerSpecificSqm(IVsSqmMulti sqm, uint sqmSession)
        {
            // nothing, implement SQM if needed
        }

        protected override uint GetSqmAppID()
        {
            // nothing, implement SQM if needed
            return 0;
        }

        protected override ImmutableArray<DiagnosticAnalyzer> ResolveAnalyzersFromArguments(List<DiagnosticInfo> diagnostics, CommonMessageProvider messageProvider, TouchedFileLogger touchedFiles)
        {
            return Arguments.ResolveAnalyzersFromArguments(Constants.PhpLanguageName, diagnostics, messageProvider, touchedFiles, AnalyzerLoader);
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
    }
}
