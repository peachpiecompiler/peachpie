using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    internal interface IWithSynthesized
    {
        /// <summary>
        /// Gets or initializes static constructor symbol.
        /// </summary>
        /// <returns></returns>
        MethodSymbol GetOrCreateStaticCtorSymbol();

        /// <summary>
        /// Creates synthesized field.
        /// </summary>
        SynthesizedFieldSymbol CreateSynthesizedField(TypeSymbol type, string name, Accessibility accessibility, bool isstatic);
    }
}
