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
    public abstract class BoundStatement : IStatement
    {
        public virtual bool IsInvalid => false;

        public abstract OperationKind Kind { get; }

        public SyntaxNode Syntax => null;

        public abstract void Accept(OperationVisitor visitor);

        public abstract TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);
    }

    public sealed class BoundEmptyStatement : BoundStatement
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
    public sealed class BoundExpressionStatement : BoundStatement, IExpressionStatement
    {
        /// <summary>
        /// Expression of the statement.
        /// </summary>
        public IExpression Expression { get; private set; }

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
}
