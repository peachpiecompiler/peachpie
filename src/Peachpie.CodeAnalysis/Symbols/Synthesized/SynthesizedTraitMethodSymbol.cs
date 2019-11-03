using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Cci;
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
            _parameters = default;  // as uninitialized

            this.ForwardedCall = traitmethod;
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_parameters.IsDefault)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _parameters, SynthesizedParameterSymbol.Create(this, ForwardedCall.Parameters));
                }

                //
                return _parameters;
            }
        }
    }
}
