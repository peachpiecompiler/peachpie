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
        /// <summary>
        /// Gets <see cref="System.NotImplementedException"/> with aproximate location of the error.
        /// </summary>
        public static Exception NotImplementedException(this CodeGenerator cg, string message = null, IPhpOperation op = null)
        {
            return NotImplementedException(cg.Builder, message, op: op, routine: cg.Routine);
        }

        /// <summary>
        /// Gets <see cref="System.NotImplementedException"/> with aproximate location of the error.
        /// </summary>
        public static Exception NotImplementedException(ILBuilder il, string message = null, IPhpOperation op = null, SourceRoutineSymbol routine = null)
        {
            string location = null;

            var syntax = op?.PhpSyntax;
            if (syntax != null)
            {
                // get location from AST
                var unit = syntax.ContainingSourceUnit;
                unit.GetLineColumnFromPosition(syntax.Span.Start, out int line, out int col);
                location = $"{unit.FilePath}({line}, {col})";
            }
            else if (il.SeqPointsOpt != null && il.SeqPointsOpt.Count != 0)
            {
                // get location from last sequence point
                var pt = il.SeqPointsOpt.Last();
                ((PhpSyntaxTree)pt.SyntaxTree).Source.GetLineColumnFromPosition(pt.Span.Start, out int line, out int col);
                location = $"{pt.SyntaxTree.FilePath}({line}, {col})";
            }
            else if (routine != null)
            {
                location = $"{routine.ContainingFile.SyntaxTree.FilePath} in '{routine.RoutineName}'";
            }
            else
            {
                location = "<unknown>";
            }

            //
            return new NotImplementedException($"{message} not implemented at {location}");
        }

        public static Exception UnexpectedValue(object o)
        {
            string output = string.Format("Unexpected value '{0}' of type '{1}'", o, (o != null) ? o.GetType().FullName : "<unknown>");
            Debug.Assert(false, output);

            // We do not throw from here because we don't want all Watson reports to be bucketed to this call.
            return new InvalidOperationException(output);
        }

        internal static Exception Unreachable
        {
            get { return new InvalidOperationException("This program location is thought to be unreachable."); }
        }
    }
}
