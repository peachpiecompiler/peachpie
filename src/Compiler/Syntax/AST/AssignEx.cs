using System;
using System.Reflection.Emit;
using System.Diagnostics;
using Pchp.Syntax.Parsers;

namespace Pchp.Syntax.AST
{
	/// <summary>
	/// Base class for assignment expressions (by-value and by-ref).
	/// </summary>
	public abstract class AssignEx : Expression
	{
		internal override bool AllowsPassByReference { get { return true; } }

		internal VariableUse lvalue;
        /// <summary>Target of assignment</summary>
        public VariableUse LValue { get { return lvalue; } }

		protected AssignEx(Text.Span p) : base(p) { }
	}

	#region ValueAssignEx

	/// <summary>
	/// By-value assignment expression with possibly associated operation.
	/// </summary>
	/// <remarks>
	/// Implements PHP operators: <c>=  +=  -=  *=  /=  %=  .= =.  &amp;=  |=  ^=  &lt;&lt;=  &gt;&gt;=</c>.
	/// </remarks>
	public sealed class ValueAssignEx : AssignEx
	{
        public override Operations Operation { get { return operation; } }
		internal Operations operation;

		internal Expression/*!*/ rvalue;
        /// <summary>Expression being assigned</summary>
        public Expression/*!*/RValue { get { return rvalue; } }

		public ValueAssignEx(Text.Span span, Operations operation, VariableUse/*!*/ lvalue, Expression/*!*/ rvalue)
			: base(span)
		{
			this.lvalue = lvalue;
			this.rvalue = rvalue;
			this.operation = operation;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitValueAssignEx(this);
        }
	}

	#endregion

	#region RefAssignEx

	/// <summary>
	/// By-reference assignment expression (<c>&amp;=</c> PHP operator).
	/// </summary>
	public sealed class RefAssignEx : AssignEx
	{
        public override Operations Operation { get { return Operations.AssignRef; } }

		/// <summary>Expression being assigned</summary>
        public Expression/*!*/RValue { get { return rvalue; } }
        internal Expression/*!*/ rvalue;
        
		public RefAssignEx(Text.Span span, VariableUse/*!*/ lvalue, Expression/*!*/ rvalue)
			: base(span)
		{
			Debug.Assert(rvalue is VarLikeConstructUse || rvalue is NewEx);
			this.lvalue = lvalue;
			this.rvalue = rvalue;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitRefAssignEx(this);
        }
	}

	#endregion
}
