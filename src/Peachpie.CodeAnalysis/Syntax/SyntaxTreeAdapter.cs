using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using System.Threading;
using Devsense.PHP.Syntax;
using System.Collections.Immutable;
using Pchp.CodeAnalysis.Errors;
using Devsense.PHP.Errors;

namespace Pchp.CodeAnalysis
{
    /// <summary>
    /// Adapter providing <see cref="SyntaxTree"/> from <see cref="SourceUnit"/>.
    /// </summary>
    public class SyntaxTreeAdapter : SyntaxTree
    {
        readonly SourceUnit _source;

        internal SyntaxTreeAdapter(SourceUnit source, IEnumerable<ParserDiagnosticStub> diagnosticStubs)
        {
            Contract.ThrowIfNull(source);

            _source = source;
            Diagnostics = diagnosticStubs.Select(ConvertStubToDiagnostic).ToImmutableArray();
        }

        public ImmutableArray<Diagnostic> Diagnostics { get; }

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

        /// <summary>
        /// Processes errors from PHP parser transforming them into Roslyn diagnostics using
        /// <see cref="ParserMessageProvider"/>.
        /// </summary>
        private Diagnostic ConvertStubToDiagnostic(ParserDiagnosticStub stub)
        {
            var location = new SourceLocation(
                this,
                new TextSpan(stub.Span.Start, stub.Span.Length));

            var diagnostic = ParserMessageProvider.Instance.CreateDiagnostic(
                stub.Info.Severity == ErrorSeverity.WarningAsError,
                stub.Info.Id,
                location,
                stub.Args);

            return diagnostic;
        }
    }
}
