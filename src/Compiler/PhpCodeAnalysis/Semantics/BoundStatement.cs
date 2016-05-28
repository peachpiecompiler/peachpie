using Microsoft.CodeAnalysis.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Pchp.Syntax.AST;
using System.Diagnostics;

namespace Pchp.CodeAnalysis.Semantics
{
    /// <summary>
    /// Base class representing a statement semantic.
    /// </summary>
    public abstract partial class BoundStatement : IStatement
    {
        public virtual bool IsInvalid => false;

        public abstract OperationKind Kind { get; }

        public SyntaxNode Syntax => null;

        internal LangElement PhpSyntax { get; set; }

        public abstract void Accept(OperationVisitor visitor);

        public abstract TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);
    }

    public sealed partial class BoundEmptyStatement : BoundStatement
    {
        public override OperationKind Kind => OperationKind.EmptyStatement;

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitEmptyStatement(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitEmptyStatement(this, argument);
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
    }
}
