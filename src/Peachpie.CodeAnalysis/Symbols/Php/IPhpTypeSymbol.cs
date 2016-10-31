using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// A symbol representing PHP type in CLR.
    /// </summary>
    interface IPhpTypeSymbol : INamedTypeSymbol
    {
        /// <summary>
        /// Gets fully qualified name of the class.
        /// </summary>
        QualifiedName FullName { get; }

        #region Model

        /// <summary>
        /// Optional.
        /// A field holding a reference to current runtime context.
        /// Is of type <see cref="Pchp.Core.Context"/>.
        /// </summary>
        FieldSymbol ContextStore { get; }

        /// <summary>
        /// Optional.
        /// A field holding array of the class runtime fields.
        /// Is of type <see cref="Pchp.Core.PhpArray"/>.
        /// </summary>
        FieldSymbol RuntimeFieldsStore { get; }

        /// <summary>
        /// Optional.
        /// A method <c>.phpnew</c> that ensures the initialization of the class without calling the base type constructor.
        /// </summary>
        MethodSymbol InitializeInstanceMethod { get; }

        /// <summary>
        /// Optional.
        /// A nested class <c>__statics</c> containing class static fields and constants which are bound to runtime context.
        /// </summary>
        NamedTypeSymbol StaticsContainer { get; }

        #endregion
    }
}
