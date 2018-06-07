using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Synthesized method representing implementation of used trait method inside a containing class.
    /// </summary>
    sealed class SynthesizedTraitMethodSymbol : SynthesizedMethodSymbol
    {
        public SynthesizedTraitMethodSymbol(TypeSymbol containingType, string name, MethodSymbol traitmethod, Accessibility accessibility, bool isfinal = true)
            : base(containingType, name, traitmethod.IsStatic, !traitmethod.IsStatic && !containingType.IsTraitType(), null, accessibility, isfinal)
        {
            _parameters = default(ImmutableArray<ParameterSymbol>);

            this.ForwardedCall = traitmethod;
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_parameters.IsDefault)
                {
                    // clone parameters:
                    var srcparams = ForwardedCall.Parameters;
                    var ps = new List<ParameterSymbol>(srcparams.Length);

                    foreach (var p in srcparams)
                    {
                        ps.Add(SynthesizedParameterSymbol.Create(this, p));
                    }

                    _parameters = ps.AsImmutableOrEmpty();
                }

                //
                return _parameters;
            }
        }
    }
}
