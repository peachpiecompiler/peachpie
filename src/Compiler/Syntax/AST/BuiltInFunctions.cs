using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using PHP.Core.Parsers;

namespace PHP.Core.AST
{
    #region IncludingEx

    /// <summary>
	/// Inclusion expression (include, require, synthetic auto-inclusion nodes).
	/// </summary>
	public sealed class IncludingEx : Expression
	{
        public override Operations Operation { get { return Operations.Inclusion; } }

		/// <summary>
		/// An argument of the inclusion.
		/// </summary>
        public Expression/*!*/ Target { get { return fileNameEx; } set { fileNameEx = value; } }
		private Expression/*!*/ fileNameEx;

		/// <summary>
		/// A type of an inclusion (include, include-once, ...).
		/// </summary>
		public InclusionTypes InclusionType { get { return inclusionType; } }
		private InclusionTypes inclusionType;

		/// <summary>
		/// Whether the inclusion is conditional.
		/// </summary>
		public bool IsConditional { get { return isConditional; } }
		private bool isConditional;

		public Scope Scope { get { return scope; } }
		private Scope scope;

		public SourceUnit/*!*/ SourceUnit { get { return sourceUnit; } }
		private SourceUnit/*!*/ sourceUnit;

		public IncludingEx(SourceUnit/*!*/ sourceUnit, Scope scope, bool isConditional, Text.Span span,
			InclusionTypes inclusionType, Expression/*!*/ fileName)
            : base(span)
		{
			Debug.Assert(fileName != null);

			this.inclusionType = inclusionType;
			this.fileNameEx = fileName;
			this.scope = scope;
			this.isConditional = isConditional;
			this.sourceUnit = sourceUnit;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIncludingEx(this);
        }
	}

	#endregion

	#region IssetEx

	/// <summary>
	/// Represents <c>isset</c> construct.
	/// </summary>
	public sealed class IssetEx : Expression
	{
        public override Operations Operation { get { return Operations.Isset; } }

		private readonly List<VariableUse>/*!*/ varList;
        /// <summary>List of variables to test</summary>
        public List<VariableUse>/*!*/ VarList { get { return varList; } }

		public IssetEx(Text.Span span, List<VariableUse>/*!*/ varList)
			: base(span)
		{
			Debug.Assert(varList != null && varList.Count > 0);
			this.varList = varList;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIssetEx(this);
        }
	}

	#endregion

	#region EmptyEx

	/// <summary>
	/// Represents <c>empty</c> construct.
	/// </summary>
	public sealed class EmptyEx : Expression
	{
        public override Operations Operation { get { return Operations.Empty; } }

        /// <summary>
        /// Expression to be checked for emptiness.
        /// </summary>
        public Expression/*!*/Expression { get { return this.expression; } set { this.expression = value; } }
        private Expression/*!*/expression;
        
        public EmptyEx(Text.Span p, Expression expression)
			: base(p)
		{
            if (expression == null)
                throw new ArgumentNullException("expression");

            this.expression = expression;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitEmptyEx(this);
        }
	}

	#endregion

	#region EvalEx, AssertEx

	/// <summary>
	/// Represents <c>eval</c> construct.
	/// </summary>
	public sealed class EvalEx : Expression
	{
        public override Operations Operation { get { return Operations.Eval; } }

		/// <summary>Expression containing source code to be evaluated.</summary>
        public Expression /*!*/ Code { get { return code; } set { code = value; } }

        /// <summary>
        /// Expression containing source code to be evaluated.
        /// </summary>
        private Expression/*!*/ code;
        
		#region Construction

		/// <summary>
		/// Creates a node representing an eval or assert constructs.
		/// </summary>
        /// <param name="span">Position.</param>
		/// <param name="code">Source code expression.</param>
		public EvalEx(Text.Span span, Expression/*!*/ code)
            : base(span)
		{
            this.code = code;
		}

		#endregion

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitEvalEx(this);
        }
	}

    /// <summary>
    /// Meta language element used for assert() function call analysis.
    /// </summary>
    internal sealed class AssertEx : Expression
    {
        public override Operations Operation { get { return Operations.Eval; } }

        /// <summary>Expression containing source code to be evaluated.</summary>
        public Expression /*!*/ CodeEx { get; internal set; }

        ///// <summary>Description para,eter.</summary>
        //public Expression DescriptionEx { get; internal set; }

        public AssertEx(Text.Span span, CallSignature callsignature)
            : base(span)
        {
            Debug.Assert(callsignature.Parameters.Any());
            Debug.Assert(callsignature.GenericParams.Empty());

            this.CodeEx = callsignature.Parameters[0].Expression;
            //this.DescriptionEx = description;
        }

        public override void VisitMe(TreeVisitor visitor)
        {
            // note: should not be used
            visitor.VisitElement(this.CodeEx);
            //visitor.VisitElement(this.DescriptionEx);
        }
    }

	#endregion

	#region ExitEx

	/// <summary>
	/// Represents <c>exit</c> expression.
	/// </summary>
	public sealed class ExitEx : Expression
	{
        public override Operations Operation { get { return Operations.Exit; } }

		/// <summary>Die (exit) expression. Can be null.</summary>
        public Expression ResulExpr { get { return resultExpr; } set { resultExpr = value; } }
        private Expression resultExpr; //can be null
        
		public ExitEx(Text.Span span, Expression resultExpr)
            : base(span)
		{
			this.resultExpr = resultExpr;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitExitEx(this);
        }
	}

	#endregion
}
