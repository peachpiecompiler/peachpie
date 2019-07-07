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
        readonly PhpSyntaxTree _syntaxTree;

        List<Diagnostic> _diagnostics;

        public ErrorSink(PhpSyntaxTree syntaxTree)
        {
            _syntaxTree = syntaxTree;
        }

        public ImmutableArray<Diagnostic> Diagnostics => _diagnostics != null ? _diagnostics.ToImmutableArray() : ImmutableArray<Diagnostic>.Empty; // Save an allocation if no errors were found

        public void Error(Span span, ErrorInfo info, params string[] argsOpt)
        {
            if (info == FatalErrors.ParentAccessedInParentlessClass)
            {
                // ignore PHP2070: we'll handle it more precisely, also we might recover from it
                // otherwise we'll report it again in our diagnostics
                return;
            }

            if (_diagnostics == null)
            {
                _diagnostics = new List<Diagnostic>();
            }

            _diagnostics.Add(DiagnosticBagExtensions.ParserDiagnostic(_syntaxTree, span, info, argsOpt));
        }
    }
}
