using Microsoft.CodeAnalysis.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics;
using Devsense.PHP.Syntax.Ast;

namespace Pchp.CodeAnalysis.Semantics
{
    /// <summary>
    /// Base class representing a statement semantic.
    /// </summary>
    public abstract partial class BoundStatement : IPhpStatement
    {
        public virtual bool IsInvalid => false;

        public abstract OperationKind Kind { get; }

        public SyntaxNode Syntax => null;

        public LangElement PhpSyntax { get; set; }

        public abstract void Accept(OperationVisitor visitor);

        public abstract TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);

        public abstract void Accept(PhpOperationVisitor visitor);
    }

    public sealed partial class BoundEmptyStatement : BoundStatement
    {
        public override OperationKind Kind => OperationKind.EmptyStatement;

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitEmptyStatement(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitEmptyStatement(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitEmptyStatement(this);
    }

    /// <summary>
    /// Represents an expression statement.
    /// </summary>
    public sealed partial class BoundExpressionStatement : BoundStatement, IExpressionStatement
    {
        /// <summary>
        /// Expression of the statement.
        /// </summary>
        IExpression IExpressionStatement.Expression => Expression;

        /// <summary>
        /// Expression of the statement.
        /// </summary>
        public BoundExpression Expression { get; private set; }

        public override OperationKind Kind => OperationKind.ExpressionStatement;

        public BoundExpressionStatement(BoundExpression/*!*/expression)
        {
            Debug.Assert(expression != null);
            this.Expression = expression;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitExpressionStatement(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitExpressionStatement(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitExpressionStatement(this);
    }

    /// <summary>
    /// return <c>optional</c>;
    /// </summary>
    public sealed partial class BoundReturnStatement : BoundStatement, IReturnStatement
    {
        public override OperationKind Kind => OperationKind.ReturnStatement;

        IExpression IReturnStatement.Returned => Returned;

        public BoundExpression Returned { get; private set; }

        public BoundReturnStatement(BoundExpression returned)
        {
            this.Returned = returned;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitReturnStatement(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitReturnStatement(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitReturn(this);
    }

    /// <summary>
    /// throw <c>Thrown</c>;
    /// </summary>
    public sealed partial class BoundThrowStatement : BoundStatement, IThrowStatement
    {
        public override OperationKind Kind => OperationKind.ThrowStatement;

        internal BoundExpression Thrown { get; set; }

        IExpression IThrowStatement.Thrown => this.Thrown;

        public BoundThrowStatement(BoundExpression thrown)
            :base()
        {
            Debug.Assert(thrown != null);
            this.Thrown = thrown;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitThrowStatement(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitThrowStatement(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitThrow(this);
    }

    /// <summary>
    /// Conditionally declared functions.
    /// </summary>
    public sealed partial class BoundFunctionDeclStatement : BoundStatement // TODO: ILocalFunctionStatement
    {
        public override OperationKind Kind => OperationKind.LocalFunctionStatement;

        internal FunctionDecl FunctionDecl => (FunctionDecl)PhpSyntax;

        internal Symbols.SourceFunctionSymbol Function => _function;
        readonly Symbols.SourceFunctionSymbol _function;

        internal BoundFunctionDeclStatement(Symbols.SourceFunctionSymbol function)
        {
            Contract.ThrowIfNull(function);

            _function = function;
            this.PhpSyntax = (FunctionDecl)function.Syntax;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitInvalidStatement(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitInvalidStatement(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitFunctionDeclaration(this);
    }
    
    /// <summary>
    /// Conditionally declared class.
    /// </summary>
    public sealed partial class BoundTypeDeclStatement : BoundStatement
    {
        public override OperationKind Kind => OperationKind.LocalFunctionStatement;

        internal TypeDecl TypeDecl => (TypeDecl)PhpSyntax;

        internal Symbols.SourceTypeSymbol Type => _type;
        readonly Symbols.SourceTypeSymbol _type;

        internal BoundTypeDeclStatement(Symbols.SourceTypeSymbol type)
        {
            Contract.ThrowIfNull(type);

            _type = type;
            this.PhpSyntax = type.Syntax;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitInvalidStatement(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitInvalidStatement(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitTypeDeclaration(this);
    }

    public sealed partial class BoundGlobalVariableStatement : BoundStatement, IVariableDeclarationStatement
    {
        public override OperationKind Kind => OperationKind.VariableDeclarationStatement;

        ImmutableArray<IVariable> IVariableDeclarationStatement.Variables { get { throw new InvalidOperationException(); } }   // global variable does not have a symbol (yet?)

        public ImmutableArray<BoundGlobalVariable> Variables => _variables;

        readonly ImmutableArray<BoundGlobalVariable> _variables;

        public BoundGlobalVariableStatement(ImmutableArray<BoundGlobalVariable> variables)
        {
            _variables = variables;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitVariableDeclarationStatement(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitVariableDeclarationStatement(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitGlobalStatement(this);
    }

    public sealed partial class BoundStaticVariableStatement : BoundStatement, IVariableDeclarationStatement
    {
        public override OperationKind Kind => OperationKind.VariableDeclarationStatement;

        public ImmutableArray<IVariable> Variables => StaticCast<IVariable>.From(_variables);

        readonly ImmutableArray<BoundStaticLocal> _variables;

        public BoundStaticVariableStatement(ImmutableArray<BoundStaticLocal> variables)
        {
            _variables = variables;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitVariableDeclarationStatement(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitVariableDeclarationStatement(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitStaticStatement(this);
    }

    public partial class BoundUnset : BoundStatement
    {
        public override OperationKind Kind => OperationKind.None;

        /// <summary>
        /// Reference to be unset.
        /// </summary>
        public ImmutableArray<BoundReferenceExpression> VarReferences { get; set; }

        public BoundUnset(ImmutableArray<BoundReferenceExpression> vars)
        {
            this.VarReferences = vars;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.DefaultVisit(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.DefaultVisit(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitUnset(this);
    }
}
