using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;

using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	#region NewEx

	/// <summary>
	/// <c>new</c> expression.
	/// </summary>
    public sealed class NewEx : VarLikeConstructUse
	{
        public override Operations Operation { get { return Operations.New; } }

		internal override bool AllowsPassByReference { get { return true; } }

		private TypeRef/*!*/ classNameRef;
		private CallSignature callSignature;
		/// <summary>Type of class being instantiated</summary>
        public TypeRef /*!*/ ClassNameRef { get { return classNameRef; } }
        /// <summary>Call signature of constructor</summary>
        public CallSignature CallSignature { get { return callSignature; } }

		public NewEx(Text.Span span, TypeRef/*!*/ classNameRef, List<ActualParam>/*!*/ parameters)
            : base(span)
		{
			Debug.Assert(classNameRef != null && parameters != null);
			this.classNameRef = classNameRef;
			this.callSignature = new CallSignature(parameters, TypeRef.EmptyList);
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitNewEx(this);
        }
	}

	#endregion

	#region InstanceOfEx

	/// <summary>
	/// <c>instanceof</c> expression.
	/// </summary>
    public sealed class InstanceOfEx : Expression
	{
        public override Operations Operation { get { return Operations.InstanceOf; } }

		private Expression/*!*/ expression;
        /// <summary>Expression being tested</summary>
        public Expression /*!*/ Expression { get { return expression; } internal set { expression = value; } }
        private TypeRef/*!*/ classNameRef;
        /// <summary>Type to test if <see cref="Expression"/> is of</summary>
        public TypeRef/*!*/ ClassNameRef { get { return classNameRef; } }
		
		public InstanceOfEx(Text.Span span, Expression/*!*/ expression, TypeRef/*!*/ classNameRef)
            : base(span)
		{
			Debug.Assert(expression != null && classNameRef != null);

			this.expression = expression;
			this.classNameRef = classNameRef;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitInstanceOfEx(this);
        }
	}

	#endregion

	#region TypeOfEx

	/// <summary>
	/// <c>typeof</c> expression.
	/// </summary>
    public sealed class TypeOfEx : Expression
	{
        public override Operations Operation { get { return Operations.TypeOf; } }

		public TypeRef/*!*/ ClassNameRef { get { return classNameRef; } }
		private TypeRef/*!*/ classNameRef;

		public TypeOfEx(Text.Span span, TypeRef/*!*/ classNameRef)
            : base(span)
		{
			Debug.Assert(classNameRef != null);

			this.classNameRef = classNameRef;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitTypeOfEx(this);
        }
	}

	#endregion	
}
