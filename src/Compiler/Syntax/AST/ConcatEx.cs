using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;

using PHP.Core.Parsers;

namespace PHP.Core.AST
{
    /// <summary>
    /// Represents a concatenation expression (dot PHP operator).
    /// </summary>
    public sealed class ConcatEx : Expression
    {
        public override Operations Operation { get { return Operations.ConcatN; } }

        public Expression[]/*!*/ Expressions { get { return this.expressions; } internal set { this.expressions = value; } }
        private Expression[]/*!*/ expressions;

        /// <summary>
        /// Initialize the ConcatEx AST node and optimize the subtree if possible. Look for child expressions and chain possible concatenations. This prevents StackOverflowException in case of huge concatenation expressions.
        /// </summary>
        /// <param name="span"></param>
        /// <param name="expressions">List of expressions to concatenate.</param>
        /// <remarks>This method tries to propagate child concatenations and chain them.</remarks>
        public ConcatEx(Text.Span span, IList<Expression>/*!*/ expressions)
            : base(span)
        {
            Debug.Assert(expressions != null);
            this.expressions = expressions.AsArray();
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitConcatEx(this);
        }
    }
}

