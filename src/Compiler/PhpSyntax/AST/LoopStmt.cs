using System;
using System.Collections.Generic;
using Pchp.Syntax;
using System.Diagnostics;
using Pchp.Syntax.Parsers;

namespace Pchp.Syntax.AST
{
	#region WhileStmt

	/// <summary>
	/// Represents a while-loop statement.
	/// </summary>
    public sealed class WhileStmt : Statement
	{
		public enum Type { While, Do };

        /// <summary>Type of statement</summary>
        public Type LoopType { get { return type; } }
        private Type type;

		/// <summary>
		/// Condition or a <B>null</B> reference for unbounded loop.
		/// </summary>
        public Expression CondExpr { get { return condExpr; } internal set { condExpr = value; } }
        private Expression condExpr;

        /// <summary>Body of loop</summary>
        public Statement/*!*/ Body { get { return body; } internal set { body = value; } }
        private Statement/*!*/ body;

		public WhileStmt(Text.Span span, Type type, Expression/*!*/ condExpr, Statement/*!*/ body)
            : base(span)
		{
			Debug.Assert(condExpr != null && body != null);

			this.type = type;
			this.condExpr = condExpr;
			this.body = body;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitWhileStmt(this);
        }
	}

	#endregion

	#region ForStmt

	/// <summary>
	/// Represents a for-loop statement.
	/// </summary>
    public sealed class ForStmt : Statement
	{
		private readonly List<Expression>/*!*/ initExList;
		private readonly List<Expression>/*!*/ condExList;
		private readonly List<Expression>/*!*/ actionExList;
		private Statement/*!*/ body;

        /// <summary>List of expressions used for initialization</summary>
        public List<Expression> /*!*/ InitExList { get { return initExList; } }
        /// <summary>List of expressions used as condition</summary>
        public List<Expression> /*!*/ CondExList { get { return condExList; } }
        /// <summary>List of expressions used to incrent iterator</summary>
        public List<Expression> /*!*/ ActionExList { get { return actionExList; } }
        /// <summary>Body of statement</summary>
        public Statement/*!*/ Body { get { return body; } internal set { body = value; } }

        public ForStmt(Text.Span p, List<Expression>/*!*/ initExList, List<Expression>/*!*/ condExList,
		  List<Expression>/*!*/ actionExList, Statement/*!*/ body)
			: base(p)
		{
			Debug.Assert(initExList != null && condExList != null && actionExList != null && body != null);

			this.initExList = initExList;
			this.condExList = condExList;
			this.actionExList = actionExList;
			this.body = body;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitForStmt(this);
        }
	}
	#endregion

	#region ForeachStmt

	/// <summary>
	/// Represents a foreach-loop statement.
	/// </summary>
    public sealed class ForeachVar : AstNode
	{
		/// <summary>
		/// Whether the variable is aliased.
		/// </summary>
        public bool Alias { get { return alias; } set { alias = value; } }
        private bool alias;
		
		/// <summary>
		/// The variable itself. Can be <c>null</c> reference if <see cref="ListEx"/> is represented instead.
		/// </summary>
        public VariableUse Variable { get { return this.expr as VariableUse; } }

        /// <summary>
        /// PHP list expression. Can be <c>null</c> reference if <see cref="VariableUse"/> is represented instead.
        /// </summary>
        public ListEx List { get { return this.expr as ListEx; } }

        /// <summary>
        /// Inner expression representing <see cref="Variable"/> or <see cref="List"/>.
        /// </summary>
        internal Expression/*!*/Expression { get { return expr; } }
        private readonly Expression/*!*/expr;

        /// <summary>
        /// Position of foreach variable.
        /// </summary>
        internal Text.Span Span { get { return expr.Span; } }

		public ForeachVar(VariableUse variable, bool alias)
		{
			this.expr = variable;
			this.alias = alias;
		}

        /// <summary>
        /// Initializes instance of <see cref="ForeachVar"/> representing PHP list expression.
        /// </summary>
        /// <param name="list"></param>
        public ForeachVar(ListEx/*!*/list)
        {
            Debug.Assert(list != null);
            Debug.Assert(list.RValue == null);

            this.expr = list;
            this.alias = false;
        }
	}

	/// <summary>
	/// Represents a foreach statement.
	/// </summary>
    public class ForeachStmt : Statement
	{
		private Expression/*!*/ enumeree;
        /// <summary>Array to enumerate through</summary>
        public Expression /*!*/Enumeree { get { return enumeree; } }
		private ForeachVar keyVariable;
        /// <summary>Variable to store key in (can be null)</summary>
        public ForeachVar KeyVariable { get { return keyVariable; } }
		private ForeachVar/*!*/ valueVariable;
        /// <summary>Variable to store value in</summary>
        public ForeachVar /*!*/ ValueVariable { get { return valueVariable; } }
		private Statement/*!*/ body;
        /// <summary>Body - statement in loop</summary>
        public Statement/*!*/ Body { get { return body; } internal set { body = value; } }

		public ForeachStmt(Text.Span span, Expression/*!*/ enumeree, ForeachVar key, ForeachVar/*!*/ value,
		  Statement/*!*/ body)
            : base(span)
		{
			Debug.Assert(enumeree != null && value != null && body != null);

			this.enumeree = enumeree;
			this.keyVariable = key;
			this.valueVariable = value;
			this.body = body;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitForeachStmt(this);
        }
	}

	#endregion
}
