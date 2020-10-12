using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Symbols;
using System.Diagnostics;

namespace Peachpie.CodeAnalysis.Utilities
{
    static class ExceptionUtilities
    {
        public static string GuessSourceLocation(CodeGenerator cg, IPhpOperation op = null)
            => GuessSourceLocation(cg.Builder, op, cg.Routine, cg.DebugRoutine);

        /// <summary>
        /// According to current <see cref="ILBuilder.SeqPointsOpt"/>, gets file name and position in the source code currently being emitted.
        /// </summary>
        public static string GuessSourceLocation(ILBuilder il, IPhpOperation op = null, SourceRoutineSymbol routine = null, MethodSymbol debugroutine = null)
        {
            var syntax = op?.PhpSyntax;
            if (syntax != null)
            {
                // get location from AST
                var unit = syntax.ContainingSourceUnit;
                unit.GetLineColumnFromPosition(syntax.Span.Start, out int line, out int col);
                return $"{unit.FilePath}({line + 1}, {col + 1})";
            }
            else if (il.SeqPointsOpt != null && il.SeqPointsOpt.Count != 0)
            {
                // get location from last sequence point
                var pt = il.SeqPointsOpt.Last();
                ((PhpSyntaxTree)pt.SyntaxTree).Source.GetLineColumnFromPosition(pt.Span.Start, out int line, out int col);
                return $"{pt.SyntaxTree.FilePath}({line + 1}, {col + 1})";
            }
            else if (routine != null)
            {
                return $"{routine.ContainingFile.SyntaxTree.FilePath} in '{routine.RoutineName}'";
            }
            else if (debugroutine != null)
            {
                var method = $"{debugroutine.ContainingType.GetFullName()}::{debugroutine.RoutineName}";

                if (debugroutine.ContainingType is SourceTypeSymbol srctype)
                {
                    return $"{srctype.ContainingFile.SyntaxTree.FilePath} in {method}";
                }

                return method;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets <see cref="System.NotImplementedException"/> with aproximate location of the error.
        /// </summary>
        public static NotImplementedException NotImplementedException(this CodeGenerator cg, string message = null, IPhpOperation op = null)
        {
            return NotImplementedException(cg.Builder, message, op: op, routine: cg.Routine, debugroutine: cg.DebugRoutine);
        }

        /// <summary>
        /// Gets <see cref="System.NotImplementedException"/> with aproximate location of the error.
        /// </summary>
        public static NotImplementedException NotImplementedException(ILBuilder il, string message = null, IPhpOperation op = null, SourceRoutineSymbol routine = null, MethodSymbol debugroutine = null)
        {
            var location = GuessSourceLocation(il, op, routine, debugroutine) ?? "<unknown>";

            //
            return new NotImplementedException($"{message} not implemented at {location}");
        }

        public static ArgumentNullException ArgumentNull(string argName)
        {
            return new ArgumentNullException(argName);
        }

        public static ArgumentNullException ArgumentNull()
        {
            return new ArgumentNullException();
        }

        public static InvalidOperationException UnexpectedValue(object o)
        {
            string output = string.Format("Unexpected value '{0}' of type '{1}'", o, (o != null) ? o.GetType().FullName : "<unknown>");
            Debug.Assert(false, output);

            // We do not throw from here because we don't want all Watson reports to be bucketed to this call.
            return new InvalidOperationException(output);
        }

        internal static InvalidOperationException Unreachable
        {
            get { return new InvalidOperationException("This program location is thought to be unreachable."); }
        }
    }
}
