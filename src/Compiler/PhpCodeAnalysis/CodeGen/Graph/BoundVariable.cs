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
        internal abstract IBoundReference BindPlace(ILBuilder il, BoundAccess access);

        internal abstract IPlace Place(ILBuilder il);
    }

    partial class BoundLocal
    {
        LocalPlace _place;

        internal override IBoundReference BindPlace(ILBuilder il, BoundAccess access)
        {
            return new BoundLocalPlace(Place(il), access);
        }

        internal override IPlace Place(ILBuilder il) => LocalPlace(il);

        private LocalPlace LocalPlace(ILBuilder il)
        {
            if (_place == null)
                _place = new LocalPlace(il.LocalSlotManager.DeclareLocal((Cci.ITypeReference)_symbol.Type, _symbol as ILocalSymbolInternal, this.Name, SynthesizedLocalKind.UserDefined, LocalDebugId.None, 0, LocalSlotConstraints.None, false, ImmutableArray<TypedConstant>.Empty, false));

            return _place;
        }
    }

    partial class BoundParameter
    {
        internal override IBoundReference BindPlace(ILBuilder il, BoundAccess access)
        {
            return new BoundLocalPlace(Place(il), access);
        }

        internal override IPlace Place(ILBuilder il)
        {
            return new ParamPlace(this.Parameter);
        }
    }

    partial class BoundGlobalVariable
    {
        internal override IBoundReference BindPlace(ILBuilder il, BoundAccess access)
        {
            throw new NotImplementedException();
        }

        internal override IPlace Place(ILBuilder il)
        {
            throw new NotImplementedException();
        }
    }
}
