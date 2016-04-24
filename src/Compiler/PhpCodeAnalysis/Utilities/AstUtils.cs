using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.Syntax.AST;
using Microsoft.CodeAnalysis.Text;

namespace Pchp.CodeAnalysis
{
    internal static class AstUtils
    {
        /// <summary>
        /// Fixes <see cref="ItemUse"/> so it propagates correctly through our visitor.
        /// </summary>
        /// <remarks><c>IsMemberOf</c> will be set on Array, not ItemUse itself.</remarks>
        public static void PatchItemUse(ItemUse item)
        {
            if (item.IsMemberOf != null)
            {
                item.Array.IsMemberOf = item.IsMemberOf;
                item.IsMemberOf = null;
            }
        }

        /// <summary>
        /// Determines whether method has <c>$this</c> variable.
        /// </summary>
        public static bool HasThisVariable(MethodDecl method)
        {
            return method != null && (method.Modifiers & Syntax.PhpMemberAttributes.Static) == 0;
        }

        public static Syntax.Text.Span BodySpanOrInvalid(this AstNode routine)
        {
            if (routine is FunctionDecl)
            {
                var node = (FunctionDecl)routine;
                return Syntax.Text.Span.FromBounds(node.DeclarationBodyPosition, node.EntireDeclarationSpan.End);
            }
            if (routine is MethodDecl)
            {
                var node = (MethodDecl)routine;
                return Syntax.Text.Span.FromBounds(node.DeclarationBodyPosition, node.EntireDeclarationSpan.End);
            }
            else
            {
                return Syntax.Text.Span.Invalid;
            }
        }

        /// <summary>
        /// Gets <see cref="Microsoft.CodeAnalysis.Text.LinePosition"/> from source position.
        /// </summary>
        public static LinePosition LinePosition(this Syntax.Text.ILineBreaks lines, int pos)
        {
            int line, col;
            lines.GetLineColumnFromPosition(pos, out line, out col);

            return new LinePosition(line, col);
        }
    }
}
