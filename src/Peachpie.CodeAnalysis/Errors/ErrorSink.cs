using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Devsense.PHP.Errors;
using Devsense.PHP.Syntax;
using Devsense.PHP.Text;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Pchp.CodeAnalysis.Errors
{
    /// <summary>
    /// Stores errors from PHP parser.
    /// </summary>
    internal class ErrorSink : IErrorSink<Span>
    {
        private PhpSyntaxTree _syntaxTree;
        private List<Diagnostic> _diagnostics;

        public ErrorSink(PhpSyntaxTree syntaxTree)
        {
            _syntaxTree = syntaxTree;
        }

        // Save an allocation if no errors were found
        public ImmutableArray<Diagnostic> Diagnostics =>
            _diagnostics?.ToImmutableArray() ?? ImmutableArray<Diagnostic>.Empty;

        public void Error(Span span, ErrorInfo info, params string[] argsOpt)
        {
            ParserMessageProvider.Instance.RegisterError(info);

            var location = new SourceLocation(_syntaxTree, span.ToTextSpan());

            var diagnostic = ParserMessageProvider.Instance.CreateDiagnostic(
                info.Severity == ErrorSeverity.WarningAsError,
                info.Id,
                location,
                argsOpt);

            if (_diagnostics == null)
            {
                _diagnostics = new List<Diagnostic>();
            }

            _diagnostics.Add(diagnostic);
        }
    }
}
