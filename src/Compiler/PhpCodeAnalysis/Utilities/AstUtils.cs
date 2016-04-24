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
