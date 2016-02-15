using Microsoft.CodeAnalysis;
using Pchp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    internal interface ISymbolTables
    {
        // TODO: source files, constants, global variables

        INamedTypeSymbol GetType(QualifiedName name);

        IEnumerable<INamedTypeSymbol> GetTypes();

        IMethodSymbol GetFunction(QualifiedName name);

        IEnumerable<IMethodSymbol> GetFunctions();
    }
}
