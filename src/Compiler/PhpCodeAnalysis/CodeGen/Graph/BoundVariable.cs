using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.FlowAnalysis;
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
        /// <summary>
        /// Emits initialization of the variable if needed.
        /// Called from within <see cref="Graph.StartBlock"/>.
        /// </summary>
        internal virtual void EmitInit(CodeGenerator cg) { }

        /// <summary>
        /// Gets <see cref="IBoundReference"/> providing load and store operations.
        /// </summary>
        internal abstract IBoundReference BindPlace(ILBuilder il, BoundAccess access, TypeRefMask thint);

        /// <summary>
        /// Gets <see cref="IPlace"/> of the variable.
        /// </summary>
        internal abstract IPlace Place(ILBuilder il);
    }

    partial class BoundLocal
    {
        LocalPlace _place;

        internal override void EmitInit(CodeGenerator cg)
        {
            // TODO: in case of PhpValue, PhpAlias, PhpArray - init local with default value if it is used uninitialized later
        }

        internal override IBoundReference BindPlace(ILBuilder il, BoundAccess access, TypeRefMask thint)
        {
            return new BoundLocalPlace(Place(il), access, thint);
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
        internal override void EmitInit(CodeGenerator cg)
        {
            // TODO: copy parameter by value in case of PhpValue, Array, PhpString
            // TODO: create local variable in case of parameter type is not enough for its use within routine
        }

        internal override IBoundReference BindPlace(ILBuilder il, BoundAccess access, TypeRefMask thint)
        {
            return new BoundLocalPlace(Place(il), access, thint);
        }

        internal override IPlace Place(ILBuilder il)
        {
            return new ParamPlace(this.Parameter);
        }
    }

    partial class BoundGlobalVariable
    {
        internal override void EmitInit(CodeGenerator cg)
        {
            // TODO: copy to local var in case we can
        }

        internal override IBoundReference BindPlace(ILBuilder il, BoundAccess access, TypeRefMask thint)
        {
            throw new NotImplementedException();
        }

        internal override IPlace Place(ILBuilder il)
        {
            throw new NotImplementedException();
        }
    }
}
