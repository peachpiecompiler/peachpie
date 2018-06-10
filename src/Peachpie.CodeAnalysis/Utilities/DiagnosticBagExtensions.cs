using System.Diagnostics;
using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Pchp.CodeAnalysis.Errors;
using Pchp.CodeAnalysis.Symbols;

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
            Debug.Assert(syntax != null);
            Add(diagnostics, routine, syntax.Span.ToTextSpan(), code, args);
        }

        public static void Add(
            this DiagnosticBag diagnostics,
            SourceRoutineSymbol routine,
            TextSpan span,
            ErrorCode code,
            params object[] args)
        {
            var tree = routine.ContainingFile.SyntaxTree;
            var location = new SourceLocation(tree, span);
            diagnostics.Add(location, code, args);
        }

        public static void Add(this DiagnosticBag diagnostics, Location location, ErrorCode code, params object[] args)
        {
            var diag = MessageProvider.Instance.CreateDiagnostic(code, location, args);
            diagnostics.Add(diag);
        }

        public static Diagnostic ParserDiagnostic(SourceRoutineSymbol routine, Devsense.PHP.Text.Span span, Devsense.PHP.Errors.ErrorInfo info, params string[] argsOpt)
        {
            return ParserDiagnostic(routine.ContainingFile.SyntaxTree, span, info, argsOpt);
        }

        public static Diagnostic ParserDiagnostic(SyntaxTree tree, Devsense.PHP.Text.Span span, Devsense.PHP.Errors.ErrorInfo info, params string[] argsOpt)
        {
            ParserMessageProvider.Instance.RegisterError(info);

            return ParserMessageProvider.Instance.CreateDiagnostic(
                info.Severity == Devsense.PHP.Errors.ErrorSeverity.WarningAsError,
                info.Id,
                new SourceLocation(tree, span.ToTextSpan()),
                argsOpt);
        }
    }
}
