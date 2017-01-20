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
        private List<ParserDiagnosticStub> _diagnosticStubs;

        public ErrorSink()
        {
        }

        // Save an allocation if no errors were found
        public IEnumerable<ParserDiagnosticStub> DiagnosticStubs =>
            (IEnumerable<ParserDiagnosticStub>)_diagnosticStubs ?? ImmutableArray<ParserDiagnosticStub>.Empty;

        public void Error(Span span, ErrorInfo info, params string[] argsOpt)
        {
            ParserMessageProvider.Instance.RegisterError(info);

            var diagnosticStub = new ParserDiagnosticStub()
            {
                Span = span,
                Info = info,
                Args = argsOpt
            };

            if (_diagnosticStubs == null)
            {
                _diagnosticStubs = new List<ParserDiagnosticStub>();
            }

            _diagnosticStubs.Add(diagnosticStub);
        }
    }
}
