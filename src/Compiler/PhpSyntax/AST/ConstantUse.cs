using System;
using System.Reflection.Emit;
using System.Diagnostics;

using Pchp.Syntax.Parsers;

namespace Pchp.Syntax.AST
{
	#region ConstantUse

	/// <summary>
	/// Base class for constant uses.
	/// </summary>
	public abstract class ConstantUse : Expression
	{
		public ConstantUse(Text.Span span)
			: base(span)
		{
		}
	}

	#endregion

	#region GlobalConstUse

	/// <summary>
	/// Global constant use (constants defined by <c>define</c> function).
	/// </summary>
    public sealed class GlobalConstUse : ConstantUse
	{
        public override Operations Operation { get { return Operations.GlobalConstUse; } }

		public QualifiedName Name { get { return name; } }
		private QualifiedName name;

        /// <summary>
        /// Name used when the <see cref="Name"/> is not found. Used when reading global constant in a namespace context.
        /// </summary>
        internal QualifiedName? FallbackName { get { return fallbackName; } }
        private QualifiedName? fallbackName;

		public GlobalConstUse(Text.Span span, QualifiedName name, QualifiedName? fallbackName)
			: base(span)
		{
			this.name = name;
            this.fallbackName = fallbackName;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitGlobalConstUse(this);
        }
	}

	#endregion

	#region ClassConstUse

	/// <summary>
	/// Class constant use.
	/// </summary>
    public class ClassConstUse : ConstantUse
	{
		public override Operations Operation { get { return Operations.ClassConstUse; } }

        /// <summary>
        /// Class name. May have an empty <see cref="Name"/> if the class is referenced indirectly.
        /// </summary>
        public GenericQualifiedName ClassName { get { return this.typeRef.GenericQualifiedName; } }

        /// <summary>
        /// Class type reference.
        /// </summary>
        public TypeRef/*!*/TypeRef { get { return this.typeRef; } }
        private readonly TypeRef/*!*/typeRef;
        
		public VariableName Name { get { return name; } }
		private readonly VariableName name;

        /// <summary>
        /// Position of <see cref="Name"/> part of the constant use.
        /// </summary>
        public Text.Span NamePosition { get; private set; }

        public ClassConstUse(Text.Span span, GenericQualifiedName className, Text.Span classNamePosition, string/*!*/ name, Text.Span namePosition)
            : this(span, DirectTypeRef.FromGenericQualifiedName(classNamePosition, className), name, namePosition)
		{
		}

        public ClassConstUse(Text.Span span, TypeRef/*!*/typeRef, string/*!*/ name, Text.Span namePosition)
            : base(span)
        {
            Debug.Assert(typeRef != null);
            Debug.Assert(!string.IsNullOrEmpty(name));

            this.typeRef = typeRef;
			this.name = new VariableName(name);
            this.NamePosition = namePosition;
        }

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitClassConstUse(this);
        }
	}

    /// <summary>
    /// Pseudo class constant use.
    /// </summary>
    public sealed class PseudoClassConstUse : ClassConstUse
    {
        /// <summary>
        /// Possible types of pseudo class constant.
        /// </summary>
        public enum Types
        {
            Class
        }

        /// <summary>Type of pseudoconstant</summary>
        public Types Type { get { return consttype; } }
        private Types consttype;

        public PseudoClassConstUse(Text.Span span, GenericQualifiedName className, Text.Span classNamePosition, Types type, Text.Span namePosition)
            : this(span, DirectTypeRef.FromGenericQualifiedName(classNamePosition, className), type, namePosition)
		{
		}

        public PseudoClassConstUse(Text.Span span, TypeRef/*!*/typeRef, Types type, Text.Span namePosition)
            : base(span, typeRef, type.ToString().ToLowerInvariant(), namePosition)
        {
            this.consttype = type;
        }

        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitPseudoClassConstUse(this);
        }
    }

	#endregion

    #region PseudoConstUse

    /// <summary>
	/// Pseudo-constant use (PHP keywords: __LINE__, __FILE__, __DIR__, __FUNCTION__, __METHOD__, __CLASS__, __TRAIT__, __NAMESPACE__)
	/// </summary>
    public sealed class PseudoConstUse : Expression
	{
        public override Operations Operation { get { return Operations.PseudoConstUse; } }

		public enum Types { Line, File, Class, Trait, Function, Method, Namespace, Dir }

		private Types type;
        /// <summary>Type of pseudoconstant</summary>
        public Types Type { get { return type; } }

		public PseudoConstUse(Text.Span span, Types type)
			: base(span)
		{
			this.type = type;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitPseudoConstUse(this);
        }
	}

	#endregion
}
