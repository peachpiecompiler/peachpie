using System;
using System.Diagnostics;

using Pchp.Syntax.Parsers;

namespace Pchp.Syntax.AST
{
	/// <summary>
	/// Binary expression.
	/// </summary>
	public sealed class BinaryEx : Expression
    {
        #region Fields & Properties

        public Expression/*!*/ LeftExpr { get { return leftExpr; } internal set { leftExpr = value; } }
		private Expression/*!*/ leftExpr;

        public Expression/*!*/ RightExpr { get { return rightExpr; } internal set { rightExpr = value; } }
		private Expression/*!*/ rightExpr;

        public override Operations Operation { get { return operation; } }
		private Operations operation;

        #endregion

        #region Construction

        public BinaryEx(Text.Span span, Operations operation, Expression/*!*/ leftExpr, Expression/*!*/ rightExpr)
			: base(span)
		{
			Debug.Assert(leftExpr != null && rightExpr != null);
			this.operation = operation;
			this.leftExpr = leftExpr;
			this.rightExpr = rightExpr;
		}

		#endregion

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitBinaryEx(this);
        }
	}
}