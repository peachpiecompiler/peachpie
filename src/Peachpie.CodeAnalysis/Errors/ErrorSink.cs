using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Devsense.PHP.Errors;
using Devsense.PHP.Syntax;
using Devsense.PHP.Text;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Errors
{
    internal class ErrorSink : IErrorSink<Span>
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
}
