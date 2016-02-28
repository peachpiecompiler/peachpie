using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis.Semantics
{
    partial class BoundVariable
    {
        // TODO: wrap to IPlace

        internal void Load(ILBuilder il) => il.EmitLoad(this.LocalOrParameter(il));

        internal void Store(ILBuilder il)
        {
            var lp = this.LocalOrParameter(il);
            if (lp.Local != null)
                il.EmitLocalStore(lp.Local);
            else
                il.EmitStoreArgumentOpcode(lp.ParameterIndex);
        }

        internal abstract LocalOrParameter LocalOrParameter(ILBuilder il);
    }

    partial class BoundLocal
    {
        LocalDefinition _def;

        internal override LocalOrParameter LocalOrParameter(ILBuilder il)
        {
            var def = _def;
            if (def == null)
            {
                _def = def = il.LocalSlotManager.DeclareLocal((Cci.ITypeReference)_symbol.Type, _symbol as ILocalSymbolInternal, this.Name, SynthesizedLocalKind.UserDefined, LocalDebugId.None, 0, LocalSlotConstraints.None, false, ImmutableArray<TypedConstant>.Empty, false);
            }

            return def;
        }
    }

    partial class BoundParameter
    {
        internal override LocalOrParameter LocalOrParameter(ILBuilder il) => this.Parameter.Ordinal;
    }
}
