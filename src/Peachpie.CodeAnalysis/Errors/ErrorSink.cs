using System.Collections.Generic;
using System.Collections.Immutable;
using Devsense.PHP.Errors;
using Devsense.PHP.Text;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Errors
{
    /// <summary>
    /// Stores errors from PHP parser.
    /// </summary>
    internal class ErrorSink : IErrorSink<Span>
    {
        readonly PhpSyntaxTree _syntaxTree;

        List<Diagnostic> _diagnostics;

        public ErrorSink(PhpSyntaxTree syntaxTree)
        {
            _syntaxTree = syntaxTree;
        }

        public ImmutableArray<Diagnostic> Diagnostics => _diagnostics != null ? _diagnostics.ToImmutableArray() : ImmutableArray<Diagnostic>.Empty; // Save an allocation if no errors were found

        public void Error(Span span, ErrorInfo info, params string[] argsOpt)
        {
            if (_diagnostics == null)
            {
                _diagnostics = new List<Diagnostic>();
            }

            _diagnostics.Add(DiagnosticBagExtensions.ParserDiagnostic(_syntaxTree, span, info, argsOpt));
        }
    }
}
