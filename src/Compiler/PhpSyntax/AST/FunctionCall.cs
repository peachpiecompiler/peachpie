using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Collections;

using Pchp.Syntax.Parsers;

namespace Pchp.Syntax.AST
{
	#region FunctionCall

	public abstract class FunctionCall : VarLikeConstructUse
	{
		protected CallSignature callSignature;
        /// <summary>GetUserEntryPoint calling signature</summary>
        public CallSignature CallSignature { get { return callSignature; } internal set { callSignature = value; } }

		/// <summary>
        /// Position of called function name in source code.
        /// </summary>
        public Text.Span NameSpan { get; protected set; }

        public FunctionCall(Text.Span span, Text.Span nameSpan, IList<ActualParam> parameters, IList<TypeRef> genericParams)
			: base(span)
		{
			Debug.Assert(parameters != null);

			this.callSignature = new CallSignature(parameters, genericParams);
            this.NameSpan = nameSpan;
		}
	}

	#endregion

	#region DirectFcnCall

    public sealed class DirectFcnCall : FunctionCall
	{
        public override Operations Operation { get { return Operations.DirectCall; } }

        /// <summary>
		/// Simple name for methods.
		/// </summary>
		private QualifiedName qualifiedName;
        private QualifiedName? fallbackQualifiedName;
        /// <summary>Simple name for methods.</summary>
        public QualifiedName QualifiedName { get { return qualifiedName; } }
        public QualifiedName? FallbackQualifiedName { get { return fallbackQualifiedName; } }

        public DirectFcnCall(Text.Span span,
            QualifiedName qualifiedName, QualifiedName? fallbackQualifiedName, Text.Span qualifiedNameSpan,
            IList<ActualParam> parameters, IList<TypeRef> genericParams)
            : base(span, qualifiedNameSpan, parameters, genericParams)
		{
            this.qualifiedName = qualifiedName;
            this.fallbackQualifiedName = fallbackQualifiedName;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitDirectFcnCall(this);
        }
	}

	#endregion

	#region IndirectFcnCall

    public sealed class IndirectFcnCall : FunctionCall
	{
        public override Operations Operation { get { return Operations.IndirectCall; } }

		public Expression/*!*/ NameExpr { get { return nameExpr; } }
		internal Expression/*!*/ nameExpr;

		public IndirectFcnCall(Text.Span p, Expression/*!*/ nameExpr, IList<ActualParam> parameters, IList<TypeRef> genericParams)
            : base(p, nameExpr.Span, parameters, genericParams)
		{
			this.nameExpr = nameExpr;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIndirectFcnCall(this);
        }
	}

	#endregion

	#region StaticMtdCall

	public abstract class StaticMtdCall : FunctionCall
	{
        public GenericQualifiedName ClassName { get { return typeRef.GenericQualifiedName; } }
        protected readonly TypeRef/*!*/typeRef;

        /// <summary>
        /// Position of <see cref="ClassName"/> in source code.
        /// </summary>
        public Text.Span ClassNamePosition { get { return this.typeRef.Span; } }

        public TypeRef/*!*/ TypeRef { get { return this.typeRef; } }

        public StaticMtdCall(Text.Span span, Text.Span methodNamePosition, GenericQualifiedName className, Text.Span classNamePosition, IList<ActualParam> parameters, IList<TypeRef> genericParams)
            : this(span, methodNamePosition, DirectTypeRef.FromGenericQualifiedName(classNamePosition, className), parameters, genericParams)
		{	
		}

        public StaticMtdCall(Text.Span span, Text.Span methodNamePosition, TypeRef typeRef, IList<ActualParam> parameters, IList<TypeRef> genericParams)
            : base(span, methodNamePosition, parameters, genericParams)
        {
            Debug.Assert(typeRef != null);

            this.typeRef = typeRef;
        }
	}

	#endregion

	#region DirectStMtdCall

	public sealed class DirectStMtdCall : StaticMtdCall
	{
        public override Operations Operation { get { return Operations.DirectStaticCall; } }

		private Name methodName;
        public Name MethodName { get { return methodName; } }
		
		public DirectStMtdCall(Text.Span span, ClassConstUse/*!*/ classConstant,
            IList<ActualParam>/*!*/ parameters, IList<TypeRef>/*!*/ genericParams)
			: base(span, classConstant.NamePosition, classConstant.TypeRef, parameters, genericParams)
		{
			this.methodName = new Name(classConstant.Name.Value);
		}

        public DirectStMtdCall(Text.Span span, GenericQualifiedName className, Text.Span classNamePosition,
            Name methodName, Text.Span methodNamePosition, IList<ActualParam> parameters, IList<TypeRef> genericParams)
			: base(span, methodNamePosition, className, classNamePosition, parameters, genericParams)
		{
			this.methodName = methodName;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitDirectStMtdCall(this);
        }
	}

	#endregion

	#region IndirectStMtdCall

    public sealed class IndirectStMtdCall : StaticMtdCall
	{
        public override Operations Operation { get { return Operations.IndirectStaticCall; } }

		private CompoundVarUse/*!*/ methodNameVar;
        /// <summary>Expression that represents name of method</summary>
        public CompoundVarUse/*!*/ MethodNameVar { get { return methodNameVar; } }

		public IndirectStMtdCall(Text.Span span,
                                 GenericQualifiedName className, Text.Span classNamePosition, CompoundVarUse/*!*/ mtdNameVar,
	                             IList<ActualParam> parameters, IList<TypeRef> genericParams)
            : base(span, mtdNameVar.Span, className, classNamePosition, parameters, genericParams)
		{
			this.methodNameVar = mtdNameVar;
		}

        public IndirectStMtdCall(Text.Span span,
                                 TypeRef/*!*/typeRef, CompoundVarUse/*!*/ mtdNameVar,
                                 IList<ActualParam> parameters, IList<TypeRef> genericParams)
            : base(span, mtdNameVar.Span, typeRef, parameters, genericParams)
        {
            this.methodNameVar = mtdNameVar;
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIndirectStMtdCall(this);
        }
	}

	#endregion
}
