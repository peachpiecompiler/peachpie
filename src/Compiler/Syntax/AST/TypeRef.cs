using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;

using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	#region TypeRef

	/// <summary>
	/// Represents a use of a class name.
	/// </summary>
	public abstract class TypeRef : LangElement
	{
        /// <summary>
        /// Immutable empty list of <see cref="TypeRef"/>.
        /// </summary>
		internal static readonly List<TypeRef>/*!*/ EmptyList = new List<TypeRef>();

        /// <summary>
        /// List of generic parameters.
        /// </summary>
        public List<TypeRef>/*!*/ GenericParams
        {
            get
            {
                return this.Properties[GenericParamsPropertyKey] as List<TypeRef> ?? TypeRef.EmptyList;
            }
            private set
            {
                if (value != null && value.Count > 0)
                    this.Properties[GenericParamsPropertyKey] = value;
                else
                    this.Properties.RemoveProperty(GenericParamsPropertyKey);
            }
        }
        
        /// <summary>
        /// Key to property collection to get/store generic parameters list.
        /// </summary>
        private const string GenericParamsPropertyKey = "GenericParams";

        public GenericQualifiedName GenericQualifiedName
        {
            get
            {
                return new GenericQualifiedName(this.QualifiedName, ToStaticTypeRefs(this.GenericParams, null, null));
            }
        }

        internal abstract QualifiedName QualifiedName { get; }

        public TypeRef(Text.Span span, List<TypeRef> genericParams)
			: base(span)
		{
			this.GenericParams = genericParams;
		}

        /// <summary>
		/// Gets the static type reference or <B>null</B> if the reference cannot be resolved at compile time.
		/// </summary>
		internal abstract object ToStaticTypeRef(ErrorSink errors, SourceUnit sourceUnit);

		internal static object[]/*!!*/ ToStaticTypeRefs(List<TypeRef>/*!*/ typeRefs, ErrorSink errors, SourceUnit sourceUnit)
		{
            if (typeRefs == null || typeRefs.Count == 0)
                return ArrayUtils.EmptyObjects;

			object[] result = new object[typeRefs.Count];

			for (int i = 0; i < typeRefs.Count; i++)
			{
                if ((result[i] = typeRefs[i].ToStaticTypeRef(errors, sourceUnit)) == null)
				{
					if (errors != null)
                        errors.Add(Errors.GenericParameterMustBeType, sourceUnit, typeRefs[i].Span);

					result[i] = new PrimitiveTypeName(QualifiedName.Object);
				}
			}

			return result;
		}
	}

	#endregion

	#region PrimitiveTypeRef

	/// <summary>
	/// Primitive type reference.
	/// </summary>
	public sealed class PrimitiveTypeRef : TypeRef
	{
        private PrimitiveTypeName typeName;

		public PrimitiveTypeRef(Text.Span span, PrimitiveTypeName name)
			: base(span, null)
		{
            this.typeName = name;
		}

		internal override object ToStaticTypeRef(ErrorSink errors, SourceUnit sourceUnit)
		{
			return typeName;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitPrimitiveTypeRef(this);
        }

        internal override QualifiedName QualifiedName
        {
            get { return this.typeName.QualifiedName; }
        }
	}

	#endregion

	#region DirectTypeRef

	/// <summary>
	/// Direct use of class name.
	/// </summary>
    public sealed class DirectTypeRef : TypeRef
	{
		public QualifiedName ClassName { get { return className; } }
		private QualifiedName className;

		internal override QualifiedName QualifiedName
        {
            get { return this.ClassName; }
        }

        internal override object ToStaticTypeRef(ErrorSink/*!*/ errors, SourceUnit/*!*/ sourceUnit)
		{
			return new GenericQualifiedName(className, TypeRef.ToStaticTypeRefs(GenericParams, errors, sourceUnit));
		}

        public DirectTypeRef(Text.Span span, QualifiedName className, List<TypeRef>/*!*/ genericParams)
			: base(span, genericParams)
		{
			this.className = className;
		}

        internal static DirectTypeRef/*!*/FromGenericQualifiedName(Text.Span span, GenericQualifiedName genericQualifiedName)
        {
            List<TypeRef> genericParams;

            if (genericQualifiedName.IsGeneric)
            {
                genericParams = new List<TypeRef>(genericQualifiedName.GenericParams.Length);
                foreach (var obj in genericQualifiedName.GenericParams)
                {
                    TypeRef objtype;
                    if (obj is GenericQualifiedName) objtype = FromGenericQualifiedName(Text.Span.Invalid, (GenericQualifiedName)obj);
                    else if (obj is PrimitiveTypeName) objtype = new PrimitiveTypeRef(Text.Span.Invalid, (PrimitiveTypeName)obj);
                    else objtype = new PrimitiveTypeRef(Text.Span.Invalid, new PrimitiveTypeName(QualifiedName.Object));

                    genericParams.Add(objtype);
                }
            }
            else
            {
                //if (genericQualifiedName.QualifiedName.IsPrimitiveTypeName)
                //    return new PrimitiveTypeRef(position, new PrimitiveTypeName(genericQualifiedName.QualifiedName));

                genericParams = TypeRef.EmptyList;
            }

            return new DirectTypeRef(span, genericQualifiedName.QualifiedName, genericParams.ToList());
        }

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitDirectTypeRef(this);
        }

        public override bool Equals(object obj)
        {
            var other = obj as DirectTypeRef;
            if (other == null)
                return false;

            return this.QualifiedName.Equals(other.QualifiedName);
        }

        public override int GetHashCode()
        {
            return this.QualifiedName.GetHashCode();
        }
	}

	#endregion

	#region IndirectTypeRef

	/// <summary>
	/// Indirect use of class name (through variable).
	/// </summary>
    public sealed class IndirectTypeRef : TypeRef
	{
		/// <summary>
        /// <see cref="VariableUse"/> which value in runtime contains the name of the type.
        /// </summary>
        public VariableUse/*!*/ ClassNameVar { get { return this.classNameVar; } }
        private readonly VariableUse/*!*/ classNameVar;

        internal override QualifiedName QualifiedName
        {
            get { return new QualifiedName(Name.EmptyBaseName); }
        }

		public IndirectTypeRef(Text.Span span, VariableUse/*!*/ classNameVar, List<TypeRef>/*!*/ genericParams)
			: base(span, genericParams)
		{
			Debug.Assert(classNameVar != null && genericParams != null);

			this.classNameVar = classNameVar;
		}

        internal override object ToStaticTypeRef(ErrorSink errors, SourceUnit sourceUnit)
		{
			return null;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIndirectTypeRef(this);
        }
	}

	#endregion
}
