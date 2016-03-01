using Microsoft.CodeAnalysis.Semantics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AST = Pchp.Syntax.AST;

namespace Pchp.CodeAnalysis.Semantics
{
    /// <summary>
    /// Binds syntax nodes (<see cref="AST.LangElement"/>) to semantic nodes (<see cref="IOperation"/>).
    /// </summary>
    internal class SemanticsBinder
    {
        #region Construction

	    public SemanticsBinder(/*PhpCompilation compilation, AST.GlobalCode ast, bool ignoreAccessibility*/)
        {
        }

        #endregion

        #region Helpers

        public IEnumerable<BoundStatement> BindStatements(IEnumerable<AST.Statement> statements)
        {
            return statements.Select(BindStatement);
        }

        public ImmutableArray<BoundExpression> BindExpressions(IEnumerable<AST.Expression> expressions)
        {
            return expressions.Select(BindExpression).ToImmutableArray();
        }

        BoundExpression BindExpression(AST.Expression expr) => BindExpression(expr, AccessType.Read);

        #endregion

        public BoundStatement BindStatement(AST.Statement stmt)
        {
            Debug.Assert(stmt != null);

            if (stmt is AST.EchoStmt) return new BoundExpressionStatement(new BoundEcho(BindExpressions(((AST.EchoStmt)stmt).Parameters)));
            if (stmt is AST.ExpressionStmt) return new BoundExpressionStatement(BindExpression(((AST.ExpressionStmt)stmt).Expression, AccessType.None));

              throw new NotImplementedException(stmt.GetType().FullName);
        }

        public BoundExpression BindExpression(AST.Expression expr, AccessType access = AccessType.Read)
        {
            Debug.Assert(expr != null);

            if (expr is AST.Literal) return BindLiteral((AST.Literal)expr).WithAccess(access);
            if (expr is AST.VarLikeConstructUse) return BindVarLikeConstructUse((AST.VarLikeConstructUse)expr, access);
            if (expr is AST.BinaryEx) return BindBinaryEx((AST.BinaryEx)expr).WithAccess(access);

            throw new NotImplementedException(expr.GetType().FullName);
        }

        BoundExpression BindVarLikeConstructUse(AST.VarLikeConstructUse expr, AccessType access)
        {
            if (expr is AST.DirectVarUse) return BindDirectVarUse((AST.DirectVarUse)expr, access);

            throw new NotImplementedException(expr.GetType().FullName);
        }

        BoundExpression BindDirectVarUse(AST.DirectVarUse expr, AccessType access)
        {
            if (expr.IsMemberOf == null)
            {
                
            }

            throw new NotImplementedException();
        }

        BoundExpression BindBinaryEx(AST.BinaryEx expr)
        {
            return new BoundBinaryEx(
                BindExpression(expr.LeftExpr, AccessType.Read),
                BindExpression(expr.RightExpr, AccessType.Read),
                BindBinaryOperationKind(expr.Operation));
        }

        static BinaryOperationKind BindBinaryOperationKind(AST.Operations op)
        {
            switch (op)
            {
                // logical op
                case AST.Operations.NotEqual: return BinaryOperationKind.OperatorNotEquals;
                case AST.Operations.Equal: return BinaryOperationKind.OperatorEquals;
                //case AST.Operations.NotIdentical: 
                //case AST.Operations.Identical:
                case AST.Operations.LessThan: return BinaryOperationKind.OperatorLessThan;
                case AST.Operations.LessThanOrEqual: return BinaryOperationKind.OperatorLessThanOrEqual;
                case AST.Operations.GreaterThan: return BinaryOperationKind.OperatorGreaterThan;
                case AST.Operations.GreaterThanOrEqual: return BinaryOperationKind.OperatorGreaterThanOrEqual;
                case AST.Operations.Concat: return BinaryOperationKind.StringConcatenation; // .

                case AST.Operations.Or: return BinaryOperationKind.OperatorConditionalOr;
                case AST.Operations.And: return BinaryOperationKind.OperatorConditionalAnd;
                //case AST.Operations.Xor: return BinaryOperationKind.OperatorConditionalExclusiveOr;
                case AST.Operations.BitOr: return BinaryOperationKind.OperatorOr;
                case AST.Operations.BitAnd: return BinaryOperationKind.OperatorAnd;
                case AST.Operations.BitXor: return BinaryOperationKind.OperatorExclusiveOr;

                // arithmetic
                case AST.Operations.Add: return BinaryOperationKind.OperatorAdd;
                case AST.Operations.Sub: return BinaryOperationKind.OperatorSubtract;
                case AST.Operations.Mul: return BinaryOperationKind.OperatorMultiply;
                case AST.Operations.Div: return BinaryOperationKind.OperatorDivide;
                case AST.Operations.Mod: return BinaryOperationKind.OperatorRemainder;  // %

                // shift
                case AST.Operations.ShiftLeft: return BinaryOperationKind.OperatorLeftShift;    // <<
                case AST.Operations.ShiftRight: return BinaryOperationKind.OperatorRightShift;  // >>

                //
                default:
                    throw new NotImplementedException(op.ToString());
            }
        }

        static BoundExpression BindLiteral(AST.Literal expr)
        {
            if (expr is AST.IntLiteral) return new BoundLiteral(((AST.IntLiteral)expr).Value);
            if (expr is AST.LongIntLiteral) return new BoundLiteral(((AST.LongIntLiteral)expr).Value);
            if (expr is AST.StringLiteral) return new BoundLiteral(((AST.StringLiteral)expr).Value);
            if (expr is AST.DoubleLiteral) return new BoundLiteral(((AST.DoubleLiteral)expr).Value);
            if (expr is AST.BoolLiteral) return new BoundLiteral(((AST.BoolLiteral)expr).Value);
            if (expr is AST.NullLiteral) return new BoundLiteral(null);
            if (expr is AST.BinaryStringLiteral) return new BoundLiteral(((AST.BinaryStringLiteral)expr).Value);

            throw new NotImplementedException();
        }
    }
}
