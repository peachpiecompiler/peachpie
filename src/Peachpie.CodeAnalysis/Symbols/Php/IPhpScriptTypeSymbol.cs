using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    public interface IPhpScriptTypeSymbol : INamedTypeSymbol
    {
        /// <summary>
        /// Gets method symbol representing the script entry point.
        /// The method's signature corresponds to <c>runtime:Context.MainDelegate</c> (Context ctx, PhpArray locals, object @this, RuntimeTypeHandle self).
        /// </summary>
        IMethodSymbol MainMethod { get; }

        /// <summary>
        /// Script's relative path to the application root.
        /// </summary>
        string RelativeFilePath { get; }
    }
}
