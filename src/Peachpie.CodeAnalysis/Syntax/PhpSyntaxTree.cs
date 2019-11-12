using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Pchp.CodeAnalysis.Errors;
using Peachpie.CodeAnalysis.Syntax;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis
{
    /// <summary>
    /// Adapter providing <see cref="SyntaxTree"/> from <see cref="SourceUnit"/> and storing parse diagnostics.
    /// </summary>
    public class PhpSyntaxTree : SyntaxTree
    {
        readonly PhpSourceUnit _source;

        /// <summary>
        /// Gets constructed lambda nodes.
        /// </summary>
        public ImmutableArray<LambdaFunctionExpr> Lambdas { get; private set; }

        /// <summary>
        /// Gets constructed type declaration nodes.
        /// </summary>
        public ImmutableArray<TypeDecl> Types { get; private set; }

        /// <summary>
        /// Gets constructed function declaration nodes.
        /// </summary>
        public ImmutableArray<FunctionDecl> Functions { get; private set; }

        /// <summary>
        /// Gets constructed global code (ast root).
        /// </summary>
        public GlobalCode Root { get; private set; }

        /// <summary>
        /// Gets constructed yield extpressions.
        /// </summary>
        public ImmutableArray<LangElement> YieldNodes { get; private set; }

        /// <summary>In case of Phar entry, gets or set the PHAR stub.</summary>
        public PhpSyntaxTree PharStubFile { get; set; }

        /// <summary>
        /// Gets value indicating the file is a PHAR entry.
        /// </summary>
        public bool IsPharEntry => PharStubFile != null;

        public static ImmutableArray<Version> SupportedLanguageVersions { get; } = new Version[]
        {
            new Version(5, 4),
            new Version(5, 5),
            new Version(5, 6),
            new Version(7, 0),
            new Version(7, 1),
            new Version(7, 2),
        }.ToImmutableArray();

        private PhpSyntaxTree(PhpSourceUnit source)
        {
            _source = source ?? throw ExceptionUtilities.ArgumentNull();
        }

        internal override bool SupportsLocations => true;

        static LanguageFeatures DefaultLanguageVersion(ref Version languageVersion)
        {
            languageVersion = new Version(7, 3);
            return LanguageFeatures.Php73Set;
        }

        internal static LanguageFeatures ParseLanguageVersion(ref Version languageVersion)
        {
            if (languageVersion != null)
            {
                if (languageVersion.Major == 5)
                {
                    switch (languageVersion.Minor)
                    {
                        case 4: return LanguageFeatures.Php54Set;
                        case 5: return LanguageFeatures.Php55Set;
                        case 6: return LanguageFeatures.Php56Set;
                    }
                }
                else if (languageVersion.Major == 7)
                {
                    switch (languageVersion.Minor)
                    {
                        case 0: return LanguageFeatures.Php70Set;
                        case 1: return LanguageFeatures.Php71Set;
                        case 2: return LanguageFeatures.Php72Set;
                        case 3: return LanguageFeatures.Php73Set;
                        case 4: return LanguageFeatures.Php74Set;
                    }
                }

                throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(languageVersion);
            }
            else
            {
                // latest
                return DefaultLanguageVersion(ref languageVersion);
            }
        }

        static LanguageFeatures GetLanguageFeatures(PhpParseOptions options)
        {
            var version = options.LanguageVersion;
            var features = ParseLanguageVersion(ref version);

            //
            if (options.AllowShortOpenTags)
            {
                features |= LanguageFeatures.ShortOpenTags;
            }

            //
            return features;
        }

        public static PhpSyntaxTree ParseCode(
            SourceText sourceText,
            PhpParseOptions parseOptions,
            PhpParseOptions scriptParseOptions,
            string fname)
        {
            if (fname == null)
            {
                throw ExceptionUtilities.ArgumentNull(nameof(fname));
            }

            // TODO: new parser implementation based on Roslyn

            // TODO: file.IsScript ? scriptParseOptions : parseOptions
            var unit = new PhpSourceUnit(fname, sourceText, encoding: sourceText.Encoding ?? Encoding.UTF8);

            var result = new PhpSyntaxTree(unit);

            var errorSink = new ErrorSink(result);
            var factory = new NodesFactory(unit, parseOptions.Defines);

            //
            try
            {
                unit.Parse(factory, errorSink,
                    features: GetLanguageFeatures(parseOptions),
                    state: (parseOptions.Kind == SourceCodeKind.Regular) ? Lexer.LexicalStates.INITIAL : Lexer.LexicalStates.ST_IN_SCRIPTING);
            }
            finally
            {
                unit.Close();
            }

            //
            result.Diagnostics = errorSink.Diagnostics;

            result.Lambdas = factory.Lambdas.AsImmutableSafe();
            result.Types = factory.Types.AsImmutableSafe();
            result.Functions = factory.Functions.AsImmutableSafe();
            result.YieldNodes = factory.YieldNodes.AsImmutableSafe();

            if (factory.Root != null)
            {
                result.Root = factory.Root;
            }
            else
            {
                // Parser leaves factory.Root to null in the case of syntax errors -> create a proxy syntax node
                var fullSpan = new Devsense.PHP.Text.Span(0, sourceText.Length);
                result.Root = new GlobalCode(fullSpan, ImmutableArray<Statement>.Empty, unit);
            }

            //
            return result;
        }

        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }

        public override Encoding Encoding => _source.Encoding ?? Encoding.UTF8;

        public override string FilePath => _source.FilePath;

        public override bool HasCompilationUnitRoot
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int Length => _source.SourceText.Length;

        protected override ParseOptions OptionsCore
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal SourceUnit Source => _source;

        public override IList<TextSpan> GetChangedSpans(SyntaxTree syntaxTree)
        {
            throw new NotImplementedException();
        }

        public override IList<TextChange> GetChanges(SyntaxTree oldTree)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Diagnostic> GetDiagnostics(SyntaxTrivia trivia)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Diagnostic> GetDiagnostics(SyntaxNodeOrToken nodeOrToken)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Diagnostic> GetDiagnostics(SyntaxToken token)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Diagnostic> GetDiagnostics(SyntaxNode node)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Diagnostic> GetDiagnostics(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Diagnostics;
        }

        public override FileLinePositionSpan GetLineSpan(TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            return new FileLinePositionSpan(_source.FilePath, _source.LinePosition(span.Start), _source.LinePosition(span.End));
        }

        public override Location GetLocation(TextSpan span)
        {
            return new SourceLocation(this, span);
        }

        public override FileLinePositionSpan GetMappedLineSpan(TextSpan span, CancellationToken cancellationToken = default)
        {
            // We do not use anything like C# #line directive in PHP
            return GetLineSpan(span, cancellationToken);
        }

        public override SyntaxReference GetReference(SyntaxNode node)
        {
            throw new NotImplementedException();
        }

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return _source.SourceText;
        }

        public override bool HasHiddenRegions()
        {
            throw new NotImplementedException();
        }

        public override bool IsEquivalentTo(SyntaxTree tree, bool topLevel = false)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetText(out SourceText text)
        {
            text = default;
            return false;
        }

        public override SyntaxTree WithChangedText(SourceText newText)
        {
            throw new NotImplementedException();
        }

        public override SyntaxTree WithFilePath(string path)
        {
            throw new NotImplementedException();
        }

        public override SyntaxTree WithRootAndOptions(SyntaxNode root, ParseOptions options)
        {
            throw new NotImplementedException();
        }

        protected override Task<SyntaxNode> GetRootAsyncCore(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override SyntaxNode GetRootCore(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override bool TryGetRootCore(out SyntaxNode root)
        {
            throw new NotImplementedException();
        }
    }
}
