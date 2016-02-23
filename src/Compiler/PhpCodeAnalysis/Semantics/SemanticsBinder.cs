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
    /// Binds syntax nodes to semantic nodes.
    /// </summary>
    internal static class SemanticsBinder
    {
        public static IEnumerable<BoundStatement> BindStatements(IEnumerable<AST.Statement> statements)
        {
            return statements.Select(BindStatement);
        }

        public static BoundStatement BindStatement(AST.Statement stmt)
        {
            Debug.Assert(stmt != null);

            if (stmt is AST.EchoStmt) return new BoundExpressionStatement(new BoundEcho(BindExpressions(((AST.EchoStmt)stmt).Parameters)));

            return new BoundEmptyStatement();   // TODO
        }

        public static ImmutableArray<BoundExpression> BindExpressions(IEnumerable<Syntax.AST.Expression> expressions)
        {
            return expressions.Select(BindExpression).ToImmutableArray();
        }

        public static BoundExpression BindExpression(AST.Expression expr)
        {
            Debug.Assert(expr != null);

            if (expr is AST.Literal)
            {
                if (expr is AST.IntLiteral) return new BoundLiteral(((AST.IntLiteral)expr).Value);
                if (expr is AST.LongIntLiteral) return new BoundLiteral(((AST.LongIntLiteral)expr).Value);
                if (expr is AST.StringLiteral) return new BoundLiteral(((AST.StringLiteral)expr).Value);
                if (expr is AST.DoubleLiteral) return new BoundLiteral(((AST.DoubleLiteral)expr).Value);
                if (expr is AST.BoolLiteral) return new BoundLiteral(((AST.BoolLiteral)expr).Value);
                if (expr is AST.NullLiteral) return new BoundLiteral(null);
                if (expr is AST.BinaryStringLiteral) return new BoundLiteral(((AST.BinaryStringLiteral)expr).Value);
            }

            throw new NotImplementedException();
        }
    }
}
