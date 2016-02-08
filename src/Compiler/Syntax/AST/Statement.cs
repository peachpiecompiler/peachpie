using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
    #region Statement

    /// <summary>
    /// Abstract base class representing all statements elements of PHP source file.
    /// </summary>
    public abstract class Statement : LangElement
    {
        protected Statement(Text.Span span)
            : base(span)
        {
        }

        /// <summary>
        /// Whether the statement is a declaration statement (class, function, namespace, const).
        /// </summary>
        internal virtual bool IsDeclaration { get { return false; } }

        internal virtual bool SkipInPureGlobalCode() { return false; }
    }

    #endregion

    #region BlockStmt

    /// <summary>
    /// Block statement.
    /// </summary>
    public sealed class BlockStmt : Statement
    {
        private readonly Statement[]/*!*/_statements;
        /// <summary>Statements in block</summary>
        public Statement[]/*!*/ Statements { get { return _statements; } }

        public BlockStmt(Text.Span span, IList<Statement>/*!*/body)
            : base(span)
        {
            Debug.Assert(body != null);
            _statements = body.AsArray();
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitBlockStmt(this);
        }
    }

    #endregion

    #region ExpressionStmt

    /// <summary>
    /// Expression statement.
    /// </summary>
    public sealed class ExpressionStmt : Statement
    {
        /// <summary>Expression that repesents this statement</summary>
        public Expression/*!*/ Expression { get { return expression; } internal set { expression = value; } }
        private Expression/*!*/ expression;

        public ExpressionStmt(Text.Span span, Expression/*!*/ expression)
            : base(span)
        {
            Debug.Assert(expression != null);
            this.expression = expression;
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitExpressionStmt(this);
        }
    }

    #endregion

    #region EmptyStmt

    /// <summary>
    /// Empty statement.
    /// </summary>
    public sealed class EmptyStmt : Statement
    {
        public static readonly EmptyStmt Unreachable = new EmptyStmt(Text.Span.Invalid);
        public static readonly EmptyStmt Skipped = new EmptyStmt(Text.Span.Invalid);
        public static readonly EmptyStmt PartialMergeResiduum = new EmptyStmt(Text.Span.Invalid);

        internal override bool SkipInPureGlobalCode()
        {
            return true;
        }

        public EmptyStmt(Text.Span p) : base(p) { }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitEmptyStmt(this);
        }
    }

    #endregion

    #region PHPDocStmt

    /// <summary>
    /// Empty statement containing PHPDoc block.
    /// </summary>
    public sealed class PHPDocStmt : Statement
    {
        public PHPDocBlock/*!*/PHPDoc { get { return _phpdoc; } }
        private readonly PHPDocBlock _phpdoc;

        internal override bool SkipInPureGlobalCode() { return true; }

        public PHPDocStmt(PHPDocBlock/*!*/phpdoc) : base(phpdoc.Span)
        {
            Debug.Assert(phpdoc != null);
            _phpdoc = phpdoc;
        }

        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitPHPDocStmt(this);
        }
    }

    #endregion

    #region UnsetStmt

    /// <summary>
    /// Represents an <c>unset</c> statement.
    /// </summary>
    public sealed class UnsetStmt : Statement
    {
        /// <summary>List of variables to be unset</summary>
        public List<VariableUse> /*!*/VarList { get { return varList; } }
        private readonly List<VariableUse>/*!*/ varList;

        public UnsetStmt(Text.Span p, List<VariableUse>/*!*/ varList)
            : base(p)
        {
            Debug.Assert(varList != null);
            this.varList = varList;
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitUnsetStmt(this);
        }
    }

    #endregion

    #region GlobalStmt

    /// <summary>
    /// Represents a <c>global</c> statement.
    /// </summary>
    public sealed class GlobalStmt : Statement
    {
        public List<SimpleVarUse>/*!*/ VarList { get { return varList; } }
        private List<SimpleVarUse>/*!*/ varList;

        public GlobalStmt(Text.Span p, List<SimpleVarUse>/*!*/ varList)
            : base(p)
        {
            Debug.Assert(varList != null);
            this.varList = varList;
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitGlobalStmt(this);
        }
    }

    #endregion

    #region StaticStmt

    /// <summary>
    /// Represents a <c>static</c> statement.
    /// </summary>
    public sealed class StaticStmt : Statement
    {
        /// <summary>List of static variables</summary>
        public List<StaticVarDecl>/*!*/ StVarList { get { return stVarList; } }
        private List<StaticVarDecl>/*!*/ stVarList;

        public StaticStmt(Text.Span p, List<StaticVarDecl>/*!*/ stVarList)
            : base(p)
        {
            Debug.Assert(stVarList != null);
            this.stVarList = stVarList;
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitStaticStmt(this);
        }
    }

    /// <summary>
    /// Helper class. No error or warning can be caused by declaring variable as static.
    /// </summary>
    /// <remarks>
    /// Even this is ok:
    /// 
    /// function f()
    ///	{
    ///   global $a;
    ///   static $a = 1;
    /// }
    /// 
    /// That's why we dont'need to know Position => is not child of LangElement
    /// </remarks>
    public class StaticVarDecl : LangElement
    {
        /// <summary>Static variable being declared</summary>
        public DirectVarUse /*!*/ Variable { get { return variable; } }
        private DirectVarUse/*!*/ variable;
        
        /// <summary>Expression used to initialize static variable</summary>
        public Expression Initializer { get { return initializer; } internal set { initializer = value; } }
        private Expression initializer;
        
        public StaticVarDecl(Text.Span span, DirectVarUse/*!*/ variable, Expression initializer)
            : base(span)
        {
            Debug.Assert(variable != null);

            this.variable = variable;
            this.initializer = initializer;
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitStaticVarDecl(this);
        }
    }

    #endregion

    #region DeclareStmt

    public sealed class DeclareStmt : Statement
    {
        /// <summary>
        /// Inner statement.
        /// </summary>
        public Statement Statement { get { return this.stmt; } }
        private readonly Statement/*!*/stmt;

        public DeclareStmt(Text.Span p, Statement statement)
            : base(p)
        {
            this.stmt = statement;
        }

        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitDeclareStmt(this);
        }
    }

    #endregion
}
