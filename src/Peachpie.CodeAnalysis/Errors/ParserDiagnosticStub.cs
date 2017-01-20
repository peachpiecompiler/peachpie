using Devsense.PHP.Errors;
using Devsense.PHP.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Errors
{
    /// <summary>
    /// Stores the parser errors, which are eventually turned into diagnostics in <see cref="SyntaxTreeAdapter"/>.
    /// </summary>
    internal struct ParserDiagnosticStub
    {
        public Span Span;
        public ErrorInfo Info;
        public object[] Args;
    }
}
