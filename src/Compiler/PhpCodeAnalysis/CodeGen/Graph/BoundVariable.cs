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
        /// Gets <see cref="IPlace"/> providing load and store operations.
        /// </summary>
        internal abstract IPlace GetPlace(ILBuilder il);
    }

    partial class BoundLocal
    {
        LocalPlace _place;

        internal override IPlace GetPlace(ILBuilder il)
        {
            if (_place == null)
            {
                _place = new LocalPlace(il.LocalSlotManager.DeclareLocal((Cci.ITypeReference)_symbol.Type, _symbol as ILocalSymbolInternal, this.Name, SynthesizedLocalKind.UserDefined, LocalDebugId.None, 0, LocalSlotConstraints.None, false, ImmutableArray<TypedConstant>.Empty, false));
            }

            return _place;
        }
    }

    partial class BoundParameter
    {
        internal override IPlace GetPlace(ILBuilder il)
        {
            return new ParamPlace(this.Parameter);
        }
    }

    partial class BoundGlobalVariable
    {
        internal override IPlace GetPlace(ILBuilder il)
        {
            throw new NotImplementedException();
        }
    }

    partial class BoundFieldRef
    {
        internal override IPlace GetPlace(CodeGenerator il)
        {
            throw new NotImplementedException();
        }
    }
}
