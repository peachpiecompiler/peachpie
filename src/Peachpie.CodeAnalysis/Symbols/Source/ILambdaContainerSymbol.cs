using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// A type container holding lambda declarations.
    /// TODO: LambdaSymbolManager
    /// </summary>
    internal interface ILambdaContainerSymbol
    {
        /// <summary>
        /// Adds declared lambda into this container.
        /// </summary>
        /// <param name="routine"></param>
        void AddLambda(SourceLambdaSymbol routine);

        /// <summary>
        /// Gets lambda functions declared within this container.
        /// </summary>
        IEnumerable<SourceLambdaSymbol> Lambdas { get; }

        /// <summary>
        /// Resolves lambda symbol for given syntax node.
        /// </summary>
        SourceLambdaSymbol ResolveLambdaSymbol(Devsense.PHP.Syntax.Ast.LambdaFunctionExpr expr);
    }
}
