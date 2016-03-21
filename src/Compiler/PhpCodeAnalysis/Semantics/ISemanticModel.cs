using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols;
using Pchp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents PHP semantics.
    /// Used to query semantic questions about the compilation in specific context.
    /// </summary>
    /// <remarks>Use <see cref="SemanticModel"/> once we implement <see cref="SyntaxTree"/>.</remarks>
    internal interface ISemanticModel
    {
        /// <summary>
        /// Gets next semantics in the chain. Can be <c>null</c>.
        /// </summary>
        ISemanticModel Next { get; }

        // TODO: constant, variable

        /// <summary>
        /// Gets value indicating whether the parameter won't be a part of analysis since it gets a special meaning during emit.
        /// </summary>
        /// <param name="p">A method call parameter.</param>
        /// <returns>True if parameter will not be bound to an argument and won't be analysed.</returns>
        bool IsSpecialParameter(ParameterSymbol p);

        /// <summary>
        /// Gets a file by its path relative to current context.
        /// </summary>
        SourceFileSymbol GetFile(string relativePath);

        /// <summary>
        /// Gets type symbol by its name in current context.
        /// Can be <c>null</c> if type cannot be found.
        /// </summary>
        INamedTypeSymbol GetType(QualifiedName name);

        /// <summary>
        /// Get global function symbol by its name in current context.
        /// Can be <c>null</c> if function could not be found.
        /// </summary>
        IEnumerable<ISemanticFunction> ResolveFunction(QualifiedName name);

        /// <summary>
        /// Gets value determining whether <paramref name="qname"/> type can be assigned from <paramref name="from"/>.
        /// </summary>
        /// <remarks>Gets <c>true</c>, if <paramref name="qname"/> is equal to or is a base type of <paramref name="from"/>.</remarks>
        bool IsAssignableFrom(QualifiedName qname, INamedTypeSymbol from);
    }
}
