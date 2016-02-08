using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using PHP.Core;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	/// <summary>
	/// Represents an if-statement.
	/// </summary>
	public sealed class IfStmt : Statement
	{
		/// <summary>
		/// List of conditions including the if-conditions and the final else.
		/// </summary>
		private List<ConditionalStmt>/*!!*/ conditions;
        public List<ConditionalStmt>/*!!*/ Conditions { get { return conditions; } internal set { conditions = value; } }

		public IfStmt(Text.Span span, List<ConditionalStmt>/*!!*/ conditions)
			: base(span)
		{
			Debug.Assert(conditions != null && conditions.Count > 0);
			Debug.Assert(conditions.All((x) => x != null));
			this.conditions = conditions;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIfStmt(this);
        }
	}

	public sealed class ConditionalStmt : AstNode
	{
		/// <summary>
		/// Condition or a <B>null</B> reference for the case of "else" branch.
		/// </summary>
		public Expression Condition { get { return condition; } internal set { condition = value; } }
		private Expression condition;

		public Statement/*!*/ Statement { get { return statement; } internal set { statement = value; } }
		private Statement/*!*/ statement;

        /// <summary>
        /// Beginning of <see cref="ConditionalStmt"/>.
        /// </summary>
        public readonly Text.Span Span;

        public ConditionalStmt(Text.Span span, Expression condition, Statement/*!*/ statement)
		{
            this.Span = span;
			this.condition = condition;
			this.statement = statement;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        internal void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitConditionalStmt(this);
        }
	}
}
