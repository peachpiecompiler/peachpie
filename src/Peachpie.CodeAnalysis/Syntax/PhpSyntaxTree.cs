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

namespace Pchp.CodeAnalysis
{
    /// <summary>
    /// Adapter providing <see cref="SyntaxTree"/> from <see cref="SourceUnit"/> and storing parse diagnostics.
    /// </summary>
    public class PhpSyntaxTree : SyntaxTree
    {
        readonly SourceUnit _source;

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

        private PhpSyntaxTree(SourceUnit source)
        {
            Contract.ThrowIfNull(source);

            _source = source;
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
                    }
                }

                throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(languageVersion);
            }
            else
            {
                // latest
                languageVersion = new Version(7, 1);
                return LanguageFeatures.Basic;
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
            string content,
            PhpParseOptions parseOptions,
            PhpParseOptions scriptParseOptions,
            string fname)
        {
            // TODO: new parser implementation based on Roslyn

            // TODO: file.IsScript ? scriptParseOptions : parseOptions
            var unit = new CodeSourceUnit(
                content, fname, Encoding.UTF8,
                (parseOptions.Kind == SourceCodeKind.Regular) ? Lexer.LexicalStates.INITIAL : Lexer.LexicalStates.ST_IN_SCRIPTING, GetLanguageFeatures(parseOptions));

            var result = new PhpSyntaxTree(unit);

            var errorSink = new ErrorSink(result);
            var factory = new NodesFactory(unit);

            //
            unit.Parse(factory, errorSink);

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
                var fullSpan = new Devsense.PHP.Text.Span(0, unit.Code.Length);
                result.Root = new GlobalCode(fullSpan, ImmutableArray<Statement>.Empty, unit);
            }

            //
            return result;
        }

        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }

        public override Encoding Encoding => Encoding.UTF8;

        public override string FilePath => _source.FilePath;

        public override bool HasCompilationUnitRoot
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int Length => _source.LineBreaks.TextLength;

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
            throw new NotImplementedException();
        }

        public override FileLinePositionSpan GetMappedLineSpan(TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            // We do not use anything like C# #line directive in PHP
            return GetLineSpan(span, cancellationToken);
        }

        public override SyntaxReference GetReference(SyntaxNode node)
        {
            throw new NotImplementedException();
        }

        public override SourceText GetText(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
