using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.CodeGen;
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
        // TODO: initialization at routine begin

        /// <summary>
        /// Gets <see cref="IBoundReference"/> providing load and store operations.
        /// </summary>
        internal abstract IBoundReference BindPlace(ILBuilder il);

        /// <summary>
        /// Gets <see cref="IPlace"/> providing load and store operations.
        /// </summary>
        internal abstract IPlace Place(ILBuilder il);
    }

    partial class BoundLocal
    {
        BoundLocalPlace _place;

        internal override IBoundReference BindPlace(ILBuilder il)
        {
            if (_place == null)
            {
                _place = new BoundLocalPlace(il.LocalSlotManager.DeclareLocal((Cci.ITypeReference)_symbol.Type, _symbol as ILocalSymbolInternal, this.Name, SynthesizedLocalKind.UserDefined, LocalDebugId.None, 0, LocalSlotConstraints.None, false, ImmutableArray<TypedConstant>.Empty, false));
            }

            return _place;
        }

        internal override IPlace Place(ILBuilder il) => (IPlace)BindPlace(il);
    }

    partial class BoundParameter
    {
        internal override IBoundReference BindPlace(ILBuilder il)
        {
            return new BoundParamPlace(this.Parameter);
        }

        internal override IPlace Place(ILBuilder il) => (IPlace)BindPlace(il);
    }

    partial class BoundGlobalVariable
    {
        internal override IBoundReference BindPlace(ILBuilder il)
        {
            throw new NotImplementedException();
        }

        internal override IPlace Place(ILBuilder il)
        {
            throw new NotImplementedException();
        }
    }
}
