using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Synthesized static constructor.
    /// </summary>
    internal sealed partial class SynthesizedCctorSymbol : SynthesizedMethodSymbol
    {
        public SynthesizedCctorSymbol(Cci.ITypeDefinition container, ModuleSymbol module)
            :base(container, module, WellKnownMemberNames.StaticConstructorName, true, false, module.DeclaringCompilation.CoreTypes.Void)
        {
            SetParameters(ImmutableArray<ParameterSymbol>.Empty);
        }

        public override MethodKind MethodKind => MethodKind.StaticConstructor;

        public override Accessibility DeclaredAccessibility => Accessibility.Private;

        internal override bool HasSpecialName => true;

        public override bool HidesBaseMethodsByName => true;
    }
}
