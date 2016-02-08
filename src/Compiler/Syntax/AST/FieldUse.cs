using System;
using System.IO;
using System.Diagnostics;
using System.Reflection.Emit;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
    #region StaticFieldUse

    /// <summary>
    /// Base class for static field uses.
    /// </summary>
    public abstract class StaticFieldUse : VariableUse
    {
        /// <summary>Name of type which's field is being accessed</summary>
        public GenericQualifiedName TypeName { get { return typeRef.GenericQualifiedName; } }

        /// <summary>Position of <see cref="TypeName"/>.</summary>
        public Text.Span TypeNameSpan { get { return this.typeRef.Span; } }

        /// <summary>Position of the field name.</summary>
        public Text.Span NameSpan { get; private set; }

        public TypeRef TypeRef { get { return typeRef; } }
        protected TypeRef typeRef;

        public StaticFieldUse(Text.Span span, Text.Span nameSpan, GenericQualifiedName typeName, Text.Span typeNamePosition)
            : this(span, nameSpan, DirectTypeRef.FromGenericQualifiedName(typeNamePosition, typeName))
        {
        }

        public StaticFieldUse(Text.Span span, Text.Span nameSpan, TypeRef typeRef)
            : base(span)
        {
            Debug.Assert(typeRef != null);

            this.typeRef = typeRef;
            this.NameSpan = nameSpan;
        }
    }

    #endregion

    #region DirectStFldUse

    /// <summary>
    /// Direct static field uses (a static field accessed by field identifier).
    /// </summary>
    public sealed class DirectStFldUse : StaticFieldUse
    {
        public override Operations Operation { get { return Operations.DirectStaticFieldUse; } }

        private VariableName propertyName;
        /// <summary>Name of static field beign accessed</summary>
        public VariableName PropertyName { get { return propertyName; } }

        public DirectStFldUse(Text.Span span, TypeRef typeRef, VariableName propertyName, Text.Span propertyNamePosition)
            : base(span, propertyNamePosition, typeRef)
        {
            this.propertyName = propertyName;
        }

        public DirectStFldUse(Text.Span span, GenericQualifiedName qualifiedName, Text.Span qualifiedNameSpan, VariableName propertyName, Text.Span propertyNameSpan)
            : this(span, DirectTypeRef.FromGenericQualifiedName(qualifiedNameSpan, qualifiedName), propertyName, propertyNameSpan)
        {
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitDirectStFldUse(this);
        }
    }

    #endregion

    #region IndirectStFldUse

    /// <summary>
    /// Indirect static field used - a static field accessed by run-time evaluated name.
    /// </summary>
    public sealed class IndirectStFldUse : StaticFieldUse
    {
        public override Operations Operation { get { return Operations.IndirectStaticFieldUse; } }

        /// <summary>Expression that produces name of the field</summary>
        public Expression/*!*/ FieldNameExpr { get { return fieldNameExpr; } internal set { fieldNameExpr = value; } }
        private Expression/*!*/ fieldNameExpr;
        
        public IndirectStFldUse(Text.Span span, TypeRef typeRef, Expression/*!*/ fieldNameExpr)
            : base(span, fieldNameExpr.Span, typeRef)
        {
            this.fieldNameExpr = fieldNameExpr;
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIndirectStFldUse(this);
        }
    }

    #endregion
}
