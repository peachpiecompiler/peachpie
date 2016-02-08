using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	#region FormalTypeParam

	public sealed class FormalTypeParam : LangElement
	{
		public Name Name { get { return name; } }
		private readonly Name name;

		/// <summary>
		/// Either <see cref="PrimitiveTypeName"/>, <see cref="GenericQualifiedName"/>, or <B>null</B>.
		/// </summary>
		public object DefaultType { get { return defaultType; } }
		private readonly object defaultType;

        /// <summary>
        /// Gets collection of CLR attributes annotating this statement.
        /// </summary>
        public CustomAttributes Attributes
        {
            get { return this.GetCustomAttributes(); }
            set { this.SetCustomAttributes(value); }
        }

		/// <summary>
        /// Singleton instance of an empty <see cref="List&lt;FormalTypeParam&gt;"/>.
        /// </summary>
        public static readonly List<FormalTypeParam>/*!*/EmptyList = new List<FormalTypeParam>();

		#region Construction

		public FormalTypeParam(Text.Span span, Name name, object defaultType, List<CustomAttribute> attributes)
            : base(span)
		{
            Debug.Assert(defaultType == null || defaultType is PrimitiveTypeName || defaultType is GenericQualifiedName);

			this.name = name;
			this.defaultType = defaultType;

			if (attributes != null && attributes.Count != 0)
                this.Attributes = new CustomAttributes(attributes);
		}

		#endregion

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitFormalTypeParam(this);
        }
	}

	#endregion

	#region TypeSignature

	public struct TypeSignature
	{
		internal FormalTypeParam[]/*!!*/ TypeParams { get { return typeParams; } }
		private readonly FormalTypeParam[]/*!!*/ typeParams;

		#region Construction

		public TypeSignature(IList<FormalTypeParam>/*!!*/ typeParams)
		{
			Debug.Assert(typeParams != null);
			this.typeParams = typeParams.AsArray();
		}

		#endregion
	}

	#endregion

	#region TypeDecl

	/// <summary>
	/// Represents a class or an interface declaration.
	/// </summary>
    public sealed class TypeDecl : Statement, IHasSourceUnit, IDeclarationElement
	{
		#region Properties

		internal override bool IsDeclaration { get { return true; } }

		/// <summary>
		/// Name of the class.
		/// </summary>
		public Name Name { get { return name; } }
		private readonly Name name;

        /// <summary>
        /// Position of <see cref="Name"/> in the source code.
        /// </summary>
        public Text.Span NamePosition { get; private set; }

		/// <summary>
		/// Namespace where the class is declared in.
		/// </summary>
		public NamespaceDecl Namespace { get { return ns; } }
		private readonly NamespaceDecl ns;

        /// <summary>
		/// Name of the base class.
		/// </summary>
		private readonly GenericQualifiedName? baseClassName;
        /// <summary>Name of the base class.</summary>
        public GenericQualifiedName? BaseClassName { get { return baseClassName; } }

        /// <summary>Position of <see cref="BaseClassName"/>.</summary>
        public Text.Span BaseClassNamePosition { get; private set; }

        public PhpMemberAttributes MemberAttributes { get; private set; }

		/// <summary>Implemented interface name indices. </summary>
        public GenericQualifiedName[]/*!!*/ ImplementsList { get; private set; }

        /// <summary>Positions of <see cref="ImplementsList"/> elements.</summary>
        public Text.Span[]/*!!*/ImplementsListPosition { get; private set; }

		/// <summary>
		/// Type parameters.
		/// </summary>
        public TypeSignature TypeSignature { get { return typeSignature; } }
        internal readonly TypeSignature typeSignature;

		/// <summary>
		/// Member declarations. Partial classes merged to the aggregate has this field <B>null</B>ed.
		/// </summary>
        public List<TypeMemberDecl> Members { get { return members; } internal set { members = value; } }
		private List<TypeMemberDecl> members;

		/// <summary>
        /// Gets collection of CLR attributes annotating this statement.
        /// </summary>
        public CustomAttributes Attributes
        {
            get { return this.GetCustomAttributes(); }
            set { this.SetCustomAttributes(value); }
        }

		/// <summary>
		/// Position spanning over the entire declaration including the attributes.
		/// Used for transformation to an eval and for VS integration.
		/// </summary>
        public Text.Span EntireDeclarationSpan { get { return entireDeclarationSpan; } }
        private Text.Span entireDeclarationSpan;

        public int DeclarationBodyPosition { get { return declarationBodyPosition; } }
        private int declarationBodyPosition;

        private int headingEndPosition;
        public int HeadingEndPosition { get { return headingEndPosition; } }

        /// <summary>Indicates if type was decorated with partial keyword (Pure mode only).</summary>
        public bool PartialKeyword { get { return partialKeyword; } }
        /// <summary>Contains value of the <see cref="PartialKeyword"/> property</summary>
        private bool partialKeyword;

        internal Scope Scope { get; private set; }

        /// <summary>
        /// Gets value indicating whether the declaration is conditional.
        /// </summary>
        public bool IsConditional { get; private set; }

        public SourceUnit SourceUnit { get; private set; }
        
		#endregion

		#region Construction

		public TypeDecl(SourceUnit/*!*/ sourceUnit,
            Text.Span span, Text.Span entireDeclarationPosition, int headingEndPosition, int declarationBodyPosition,
            bool isConditional, Scope scope, PhpMemberAttributes memberAttributes, bool isPartial, Name className, Text.Span classNamePosition,
            NamespaceDecl ns, List<FormalTypeParam>/*!*/ genericParams, Tuple<GenericQualifiedName, Text.Span> baseClassName,
            List<Tuple<GenericQualifiedName, Text.Span>>/*!*/ implementsList, List<TypeMemberDecl>/*!*/ elements,
			List<CustomAttribute> attributes)
            : base(span)
		{
			Debug.Assert(genericParams != null && implementsList != null && elements != null);
            Debug.Assert((memberAttributes & PhpMemberAttributes.Trait) == 0 || (memberAttributes & PhpMemberAttributes.Interface) == 0, "Interface cannot be a trait");

			this.name = className;
            this.NamePosition = classNamePosition;
			this.ns = ns;
			this.typeSignature = new TypeSignature(genericParams);
            if (baseClassName != null)
            {
                this.baseClassName = baseClassName.Item1;
                this.BaseClassNamePosition = baseClassName.Item2;
            }
            this.MemberAttributes = memberAttributes;
            this.Scope = scope;
            this.SourceUnit = sourceUnit;
            this.IsConditional = isConditional;
            if (implementsList == null || implementsList.Count == 0)
            {
                this.ImplementsList = EmptyArray<GenericQualifiedName>.Instance;
                this.ImplementsListPosition = EmptyArray<Text.Span>.Instance;
            }
            else
            {
                this.ImplementsList = implementsList.Select(x => x.Item1).ToArray();
                this.ImplementsListPosition = implementsList.Select(x => x.Item2).ToArray();
            }
            this.members = elements;
            this.members.TrimExcess();

			if (attributes != null && attributes.Count != 0)
                this.Attributes = new CustomAttributes(attributes);
			this.entireDeclarationSpan = entireDeclarationPosition;
            this.headingEndPosition = headingEndPosition;
			this.declarationBodyPosition = declarationBodyPosition;
            this.partialKeyword = isPartial;
		}

        #endregion

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitTypeDecl(this);
        }

        /// <summary>
        /// <see cref="PHPDocBlock"/> instance or <c>null</c> reference.
        /// </summary>
        public PHPDocBlock PHPDoc
        {
            get { return this.GetPHPDoc(); }
            set { this.SetPHPDoc(value); }
        }
    }

	#endregion

	#region TypeMemberDecl

	/// <summary>
	/// Represents a member declaration.
	/// </summary>
    public abstract class TypeMemberDecl : LangElement, IDeclarationElement
	{
        public PhpMemberAttributes Modifiers { get { return modifiers; } }
		protected PhpMemberAttributes modifiers;

        /// <summary>
        /// Gets extent of the entire declaration.
        /// </summary>
        public abstract Text.Span EntireDeclarationSpan { get; }

        /// <summary>
        /// Gets collection of CLR attributes annotating this statement.
        /// </summary>
        public CustomAttributes Attributes
        {
            get { return this.GetCustomAttributes(); }
            set { this.SetCustomAttributes(value); }
        }

        protected TypeMemberDecl(Text.Span span, List<CustomAttribute> attributes)
            : base(span)
		{
            if (attributes != null && attributes.Count != 0)
			    this.Attributes = new CustomAttributes(attributes);
		}
	}

	#endregion

	#region Methods

	/// <summary>
	/// Represents a method declaration.
	/// </summary>
    public sealed class MethodDecl : TypeMemberDecl
	{
		/// <summary>
		/// Name of the method.
		/// </summary>
		public Name Name { get { return name; } }
		private readonly Name name;

		public Signature Signature { get { return signature; } }
		private readonly Signature signature;

		public TypeSignature TypeSignature { get { return typeSignature; } }
		private readonly TypeSignature typeSignature;

        public Statement[] Body { get { return body; } internal set { body = value; } }
        private Statement[] body;

        public ActualParam[] BaseCtorParams { get { return baseCtorParams; } internal set { baseCtorParams = value; } }
		private ActualParam[] baseCtorParams;

        public override Text.Span EntireDeclarationSpan { get { return entireDeclarationSpan; } }
        private Text.Span entireDeclarationSpan;

        public int HeadingEndPosition { get { return headingEndPosition; } }
        private int headingEndPosition;

        public int DeclarationBodyPosition { get { return declarationBodyPosition; } }
        private int declarationBodyPosition;

		#region Construction

        public MethodDecl(Text.Span span, Text.Span entireDeclarationPosition, int headingEndPosition, int declarationBodyPosition, 
			string name, bool aliasReturn, IList<FormalParam>/*!*/ formalParams, IList<FormalTypeParam>/*!*/ genericParams, 
			IList<Statement> body, PhpMemberAttributes modifiers, IList<ActualParam> baseCtorParams, 
			List<CustomAttribute> attributes)
            : base(span, attributes)
        {
            Debug.Assert(genericParams != null && formalParams != null);

            this.modifiers = modifiers;
            this.name = new Name(name);
            this.signature = new Signature(aliasReturn, formalParams);
            this.typeSignature = new TypeSignature(genericParams);
            this.body = (body != null) ? body.AsArray() : null;
            this.baseCtorParams = (baseCtorParams != null) ? baseCtorParams.AsArray() : null;
            this.entireDeclarationSpan = entireDeclarationPosition;
            this.headingEndPosition = headingEndPosition;
            this.declarationBodyPosition = declarationBodyPosition;
        }

		#endregion

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitMethodDecl(this);
        }

        /// <summary>
        /// <see cref="PHPDocBlock"/> instance or <c>null</c> reference.
        /// </summary>
        public PHPDocBlock PHPDoc
        {
            get { return this.GetPHPDoc(); }
            set { this.SetPHPDoc(value); }
        }
    }

	#endregion

	#region Fields

	/// <summary>
	/// Represents a field multi-declaration.
	/// </summary>
	/// <remarks>
	/// Is derived from LangElement because we need position to report field_in_interface error.
	/// Else we would have to test ClassType in every FieldDecl and not only in FildDeclList
	/// </remarks>
	public sealed class FieldDeclList : TypeMemberDecl
	{
		private readonly List<FieldDecl>/*!*/ fields;
        /// <summary>List of fields in this list</summary>
        public List<FieldDecl> Fields/*!*/ { get { return fields; } }

        public override Text.Span EntireDeclarationSpan { get { return this.Span; } }

		public FieldDeclList(Text.Span span, PhpMemberAttributes modifiers, List<FieldDecl>/*!*/ fields,
			List<CustomAttribute> attributes)
            : base(span, attributes)
		{
			Debug.Assert(fields != null);

			this.modifiers = modifiers;
			this.fields = fields;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitFieldDeclList(this);
        }

        /// <summary>
        /// <see cref="PHPDocBlock"/> instance or <c>null</c> reference.
        /// </summary>
        public PHPDocBlock PHPDoc
        {
            get { return this.GetPHPDoc(); }
            set { this.SetPHPDoc(value); }
        }
	}

	/// <summary>
	/// Represents a field declaration.
	/// </summary>
	public sealed class FieldDecl : LangElement
	{
		/// <summary>
		/// Gets a name of the field.
		/// </summary>
		public VariableName Name { get { return name; } }
		private VariableName name;

		/// <summary>
		/// Initial value of the field represented by compile time evaluated expression.
		/// After analysis represented by Literal or ConstantUse or ArrayEx with constant parameters.
		/// Can be null.
		/// </summary>
		private Expression initializer;
        /// <summary>
        /// Initial value of the field represented by compile time evaluated expression.
        /// After analysis represented by Literal or ConstantUse or ArrayEx with constant parameters.
        /// Can be null.
        /// </summary>
        public Expression Initializer { get { return initializer; } internal set { initializer = value; } }
		
		/// <summary>
		/// Determines whether the field has an initializer.
		/// </summary>
		public bool HasInitVal { get { return initializer != null; } }

		public FieldDecl(Text.Span span, string/*!*/ name, Expression initVal)
            : base(span)
		{
			this.name = new VariableName(name);
			this.initializer = initVal;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitFieldDecl(this);
        }
	}

	#endregion

	#region Class constants

	/// <summary>
	/// Represents a class constant declaration.
	/// </summary>
	public sealed class ConstDeclList : TypeMemberDecl
	{
		/// <summary>List of constants in this list</summary>
        public List<ClassConstantDecl>/*!*/ Constants { get { return constants; } }
        private readonly List<ClassConstantDecl>/*!*/ constants;

        public override Text.Span EntireDeclarationSpan { get { return this.Span; } }
        
		public ConstDeclList(Text.Span span, List<ClassConstantDecl>/*!*/ constants, List<CustomAttribute> attributes)
            : base(span, attributes)
		{
			Debug.Assert(constants != null);

			this.constants = constants;

			//class constants never have modifiers
			modifiers = PhpMemberAttributes.Public;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitConstDeclList(this);
        }

        /// <summary>
        /// <see cref="PHPDocBlock"/> instance or <c>null</c> reference.
        /// </summary>
        public PHPDocBlock PHPDoc
        {
            get { return this.GetPHPDoc(); }
            set { this.SetPHPDoc(value); }
        }
	}

	public sealed class ClassConstantDecl : ConstantDecl
	{
        public ClassConstantDecl(Text.Span span, string/*!*/ name, Expression/*!*/ initializer)
            : base(span, name, initializer)
		{
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitClassConstantDecl(this);
        }
	}

	#endregion

    #region Traits

    /// <summary>
    /// Represents class traits usage.
    /// </summary>
    public sealed class TraitsUse : TypeMemberDecl
    {
        #region TraitAdaptation, TraitAdaptationPrecedence, TraitAdaptationAlias

        public abstract class TraitAdaptation : LangElement
        {
            /// <summary>
            /// Name of existing trait member. Its qualified name is optional.
            /// </summary>
            public Tuple<QualifiedName?, Name> TraitMemberName { get; private set; }

            public TraitAdaptation(Text.Span span, Tuple<QualifiedName?, Name> traitMemberName)
                : base(span)
            {
                this.TraitMemberName = traitMemberName;                
            }
        }

        /// <summary>
        /// Trait usage adaptation specifying a member which will be preferred over specified ambiguities.
        /// </summary>
        public sealed class TraitAdaptationPrecedence : TraitAdaptation
        {
            /// <summary>
            /// List of types which member <see cref="TraitAdaptation.TraitMemberName"/>.<c>Item2</c> will be ignored.
            /// </summary>
            public List<QualifiedName>/*!*/IgnoredTypes { get; private set; }

            public TraitAdaptationPrecedence(Text.Span span, Tuple<QualifiedName?, Name> traitMemberName, List<QualifiedName>/*!*/ignoredTypes)
                : base(span, traitMemberName)
            {
                this.IgnoredTypes = ignoredTypes;
            }

            public override void VisitMe(TreeVisitor visitor)
            {
                visitor.VisitTraitAdaptationPrecedence(this);
            }
        }

        /// <summary>
        /// Trait usage adaptation which aliases a trait member.
        /// </summary>
        public sealed class TraitAdaptationAlias : TraitAdaptation
        {
            /// <summary>
            /// Optionally new member visibility attributes.
            /// </summary>
            public PhpMemberAttributes? NewModifier { get; private set; }

            /// <summary>
            /// Optionally new member name. Can be <c>null</c>.
            /// </summary>
            public string NewName { get; private set; }

            public TraitAdaptationAlias(Text.Span span, Tuple<QualifiedName?, Name>/*!*/oldname, string newname, PhpMemberAttributes? newmodifier)
                : base(span, oldname)
            {
                if (oldname == null)
                    throw new ArgumentNullException("oldname");

                this.NewName = newname;
                this.NewModifier = newmodifier;
            }

            public override void VisitMe(TreeVisitor visitor)
            {
                visitor.VisitTraitAdaptationAlias(this);
            }
        }

        #endregion

        /// <summary>
        /// List of trait types to be used.
        /// </summary>
        public List<QualifiedName>/*!*/TraitsList { get { return traitsList; } }
        private readonly List<QualifiedName>/*!*/traitsList;

        /// <summary>
        /// List of trait adaptations modifying names of trait members. Can be <c>null</c> reference.
        /// </summary>
        public List<TraitAdaptation> TraitAdaptationList { get { return traitAdaptationList; } }
        private readonly List<TraitAdaptation> traitAdaptationList;

        /// <summary>
        /// Gets a value indicating whether there is a block (even empty) of trait adaptations.
        /// </summary>
        public bool HasTraitAdaptationBlock { get { return this.traitAdaptationList != null; } }

        /// <summary>
        /// Position where traits list ends.
        /// </summary>
        public int HeadingEndPosition { get { return headingEndPosition; } }
        private readonly int headingEndPosition;

        public override Text.Span EntireDeclarationSpan { get { return this.Span; } }

        public TraitsUse(Text.Span span, int headingEndPosition, List<QualifiedName>/*!*/traitsList, List<TraitAdaptation> traitAdaptationList)
            : base(span, null)
        {
            if (traitsList == null)
                throw new ArgumentNullException("traitsList");

            this.traitsList = traitsList;
            this.traitAdaptationList = traitAdaptationList;
            this.headingEndPosition = headingEndPosition;
        }

        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitTraitsUse(this);
        }
    }

    #endregion
}
