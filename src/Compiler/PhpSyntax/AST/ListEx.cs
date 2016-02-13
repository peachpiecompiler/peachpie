using System;
using System.Collections.Generic;
using System.Diagnostics;
using Pchp.Syntax.Parsers;

namespace Pchp.Syntax.AST
{
	/// <summary>
	/// Represents a <c>list</c> construct.
	/// </summary>
	public sealed class ListEx : Expression
	{
        public override Operations Operation { get { return Operations.List; } }

		/// <summary>
        /// Elements of this list are VarLikeConstructUse, ListEx and null.
        /// Null represents empty expression - for example next piece of code is ok: 
        /// list(, $value) = each ($arr)
        /// </summary>
        public List<Expression>/*!*/LValues { get; private set; }
        /// <summary>Array being assigned</summary>
        public Expression RValue { get; internal set; }

        public ListEx(Text.Span p, List<Expression>/*!*/ lvalues, Expression rvalue)
            : base(p)
        {
            Debug.Assert(lvalues != null /*&& rvalue != null*/);    // rvalue can be determined during runtime in case of list in list.
            Debug.Assert(lvalues.TrueForAll(delegate(Expression lvalue)
            {
                return lvalue == null || lvalue is VarLikeConstructUse || lvalue is ListEx;
            }));

            this.LValues = lvalues;
            this.RValue = rvalue;
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitListEx(this);
        }
	}
}
