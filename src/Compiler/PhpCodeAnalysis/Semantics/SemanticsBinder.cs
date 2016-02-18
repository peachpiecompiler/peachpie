using Microsoft.CodeAnalysis.Semantics;
using Pchp.Syntax.AST;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics
{
    /// <summary>
    /// Binds syntax nodes to semantic nodes (CFG).
    /// </summary>
    internal static class SemanticsBinder
    {
        public static IEnumerable<IStatement> BindStatements(IList<Statement>/*!*/statements)
        {
            Debug.Assert(statements != null);

            for (int i = 0; i < statements.Count; i++)
                yield return BindStatement(statements[i]);
        }

        public static IStatement BindStatement(Statement/*!*/stmt)
        {
            Debug.Assert(stmt != null);

            if (stmt is EchoStmt) return new BoundExpressionStatement(new BoundEchoStatement(/*((EchoStmt)stmt).Parameters*/));

            return new BoundEmptyStatement();   // TODO
        }
    }
}
