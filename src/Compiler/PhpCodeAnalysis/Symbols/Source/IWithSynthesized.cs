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
        SynthesizedFieldSymbol GetOrCreateSynthesizedField(TypeSymbol type, string name, Accessibility accessibility, bool isstatic);

        /// <summary>
        /// Adds a type member to the class.
        /// </summary>
        /// <param name="nestedType">Type to be added as nested type.</param>
        void AddTypeMember(NamedTypeSymbol nestedType);
    }
}
