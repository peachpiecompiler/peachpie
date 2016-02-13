using System;
using System.Diagnostics;

using Pchp.Syntax;
using Pchp.Syntax.Parsers;

namespace Pchp.Syntax.AST
{
	/// <summary>
	/// Unary expression.
	/// </summary>
    public sealed class UnaryEx : Expression
    {
        #region Fields & Properties

        public override Operations Operation { get { return operation; } }
		private Operations operation;

		/// <summary>Expression the operator is applied on</summary>
        public Expression /*!*/ Expr { get { return expr; } internal set { expr = value; } }
        private Expression/*!*/ expr;

        #endregion

        #region Construction

        public UnaryEx(Text.Span span, Operations operation, Expression/*!*/ expr)
			: base(span)
		{
			Debug.Assert(expr != null);
			this.operation = operation;
			this.expr = expr;
		}

		public UnaryEx(Operations operation, Expression/*!*/ expr)
			: this(Text.Span.Invalid, operation, expr)
		{
		}

		#endregion

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitUnaryEx(this);
        }
	}
}