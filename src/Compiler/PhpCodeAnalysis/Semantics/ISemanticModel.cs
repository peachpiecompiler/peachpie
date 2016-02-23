using Microsoft.CodeAnalysis;
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
        // TODO: source file, constant, local variable

        /// <summary>
        /// Gets type symbol by its name in current context.
        /// Can be <c>null</c> if type cannot be found.
        /// </summary>
        INamedTypeSymbol GetClass(QualifiedName name);

        /// <summary>
        /// Get global function symbol by its name in current context.
        /// Can be <c>null</c> if function could not be found.
        /// </summary>
        ISemanticFunction GetFunction(QualifiedName name);

        /// <summary>
        /// Gets value determining whether <paramref name="qname"/> type can be assigned from <paramref name="from"/>.
        /// </summary>
        /// <remarks>Gets <c>true</c>, if <paramref name="qname"/> is equal to or is a base type of <paramref name="from"/>.</remarks>
        bool IsAssignableFrom(QualifiedName qname, INamedTypeSymbol from);
    }
}
