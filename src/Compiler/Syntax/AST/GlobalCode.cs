using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Pchp.Syntax;
using Pchp.Syntax.Parsers;
using Pchp.Syntax.Text;

namespace Pchp.Syntax.AST
{
    #region GlobalCode

    /// <summary>
    /// Represents a container for global statements.
    /// </summary>
    /// <remarks>
    /// PHP source file can contain global code definition which is represented in AST 
    /// by GlobalCode node. Finally, it is emitted into Main() method of concrete PHPPage 
    /// class. The sample code below illustrates a part of PHP global code
    /// </remarks>
    public sealed class GlobalCode : AstNode, IHasSourceUnit, IDeclarationElement
    {
        /// <summary>
        /// Array of nodes representing statements in PHP global code
        /// </summary>
        public Statement[]/*!*/ Statements { get { return statements; } internal set { statements = value; } }
        private Statement[]/*!*/ statements;

        /// <summary>
        /// Represented source unit.
        /// </summary>
        public SourceUnit/*!*/ SourceUnit { get { return sourceUnit; } }
        private readonly SourceUnit/*!*/ sourceUnit;

        public Span EntireDeclarationSpan { get { return new Text.Span(0, sourceUnit.LineBreaks.TextLength); } }

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the GlobalCode class.
        /// </summary>
        public GlobalCode(IList<Statement>/*!*/ statements, SourceUnit/*!*/ sourceUnit)
        {
            Debug.Assert(statements != null && sourceUnit != null);

            this.sourceUnit = sourceUnit;
            this.statements = statements.AsArray();
        }

        #endregion

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitGlobalCode(this);
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

    #region NamespaceDecl

    public sealed class NamespaceDecl : Statement, IDeclarationElement
    {
        internal override bool IsDeclaration { get { return true; } }

        /// <summary>
        /// Whether the namespace was declared using PHP simple syntax.
        /// </summary>
        public readonly bool IsSimpleSyntax;

        public QualifiedName QualifiedName { get { return this.qualifiedName; } }
        private QualifiedName qualifiedName;

        public Span EntireDeclarationSpan { get { return this.Span; } }

        /// <summary>
        /// Naming context defining aliases.
        /// </summary>
        public NamingContext/*!*/ Naming { get { return this.naming; } }
        private readonly NamingContext naming;

        public bool IsAnonymous { get { return this.isAnonymous; } }
        private readonly bool isAnonymous;

        public List<Statement>/*!*/ Statements
        {
            get { return this.statements; }
            internal /* friend Parser */ set { this.statements = value; }
        }
        private List<Statement>/*!*/ statements;

        #region Construction

        public NamespaceDecl(Text.Span p)
            : base(p)
        {
            this.isAnonymous = true;
            this.qualifiedName = new QualifiedName(Name.EmptyBaseName, Name.EmptyNames);
            this.IsSimpleSyntax = false;
            this.naming = new NamingContext(null, null);
        }

        public NamespaceDecl(Text.Span p, List<string>/*!*/ names, bool simpleSyntax)
            : base(p)
        {
            this.isAnonymous = false;
            this.qualifiedName = new QualifiedName(names, false, true);
            this.IsSimpleSyntax = simpleSyntax;
            this.naming = new NamingContext(this.qualifiedName, null);
        }

        /// <summary>
        /// Finish parsing of namespace, complete its position.
        /// </summary>
        /// <param name="p"></param>
        internal void UpdatePosition(Text.Span p)
        {
            this.Span = p;
        }

        #endregion

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitNamespaceDecl(this);
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

    #region GlobalConstDeclList, GlobalConstantDecl

    public sealed class GlobalConstDeclList : Statement, IDeclarationElement
    {
        /// <summary>
        /// Gets collection of CLR attributes annotating this statement.
        /// </summary>
        public CustomAttributes Attributes
        {
            get { return this.GetCustomAttributes(); }
            set { this.SetCustomAttributes(value); }
        }

        public List<GlobalConstantDecl>/*!*/ Constants { get { return constants; } }
        private readonly List<GlobalConstantDecl>/*!*/ constants;

        public Text.Span EntireDeclarationSpan
        {
            get { return this.Span; }
        }

        public GlobalConstDeclList(Text.Span span, List<GlobalConstantDecl>/*!*/ constants, List<CustomAttribute> attributes)
            : base(span)
        {
            Debug.Assert(constants != null);

            this.constants = constants;
            if (attributes != null && attributes.Count != 0)
                this.Attributes = new CustomAttributes(attributes);
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitGlobalConstDeclList(this);
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

    public sealed class GlobalConstantDecl : ConstantDecl
    {
        /// <summary>
        /// Namespace.
        /// </summary>
        public NamespaceDecl Namespace { get { return ns; } }
        private NamespaceDecl ns;

        /// <summary>
        /// Gets value indicating whether this global constant is declared conditionally.
        /// </summary>
        public bool IsConditional { get; private set; }

        /// <summary>
        /// Scope.
        /// </summary>
        internal Scope Scope { get; private set; }

        /// <summary>
        /// Source unit.
        /// </summary>
        internal SourceUnit SourceUnit { get; private set; }

        public GlobalConstantDecl(SourceUnit/*!*/ sourceUnit, Text.Span span, bool isConditional, Scope scope,
            string/*!*/ name, NamespaceDecl ns, Expression/*!*/ initializer)
            : base(span, name, initializer)
        {
            this.ns = ns;
            this.IsConditional = IsConditional;
            this.Scope = scope;
            this.SourceUnit = sourceUnit;
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitGlobalConstantDecl(this);
        }
    }

    #endregion

}
