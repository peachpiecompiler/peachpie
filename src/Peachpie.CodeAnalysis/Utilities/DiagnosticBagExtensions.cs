using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Pchp.CodeAnalysis.Errors;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis
{
    internal static class DiagnosticBagExtensions
    {
        public static void Add(
            this DiagnosticBag diagnostics,
            SourceRoutineSymbol routine,
            LangElement syntax,
            ErrorCode code,
            params object[] args)
        {
            // TODO: Reuse the existing one instead
            var tree = routine.ContainingFile.SyntaxTree;
            var span = syntax.Span;
            var location = new SourceLocation(tree, span.ToTextSpan());
            diagnostics.Add(location, code, args);
        }

        public static void Add(this DiagnosticBag diagnostics, Location location, ErrorCode code, params object[] args)
        {
            var diag = MessageProvider.Instance.CreateDiagnostic(code, location, args);
            diagnostics.Add(diag);
        }
    }
}
