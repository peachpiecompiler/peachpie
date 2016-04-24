using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.FlowAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
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
            if (_symbol is Symbols.SourceReturnSymbol)  // TODO: remove SourceReturnSymbol
                return;

            // declare variable in global scope
            var il = cg.Builder;
            var def = il.LocalSlotManager.DeclareLocal(
                    (Cci.ITypeReference)_symbol.Type, _symbol as ILocalSymbolInternal,
                    this.Name, SynthesizedLocalKind.UserDefined,
                    LocalDebugId.None, 0, LocalSlotConstraints.None, false, ImmutableArray<TypedConstant>.Empty, false);

            _place = new LocalPlace(def);
            il.AddLocalToScope(def);

            // Initialize local variable with void.
            // This is mandatory since even assignments reads the target value to assign properly to PhpAlias.
            
            // TODO: Once analysis tells us, the target cannot be alias, this step won't be necessary.

            // TODO: only if the local will be used uninitialized

            if (_place.Type == cg.CoreTypes.PhpValue)
            {
                _place.EmitStorePrepare(cg.Builder);
                cg.Emit_PhpValue_Void();
                _place.EmitStore(cg.Builder);
            }
            else if (_place.Type == cg.CoreTypes.PhpAlias)
            {
                _place.EmitStorePrepare(cg.Builder);
                cg.Emit_PhpValue_Void();
                cg.Emit_PhpValue_MakeAlias();
                _place.EmitStore(cg.Builder);
            }
        }

        internal override IBoundReference BindPlace(ILBuilder il, BoundAccess access, TypeRefMask thint)
        {
            return new BoundLocalPlace(Place(il), access, thint);
        }

        internal override IPlace Place(ILBuilder il) => LocalPlace(il);

        private LocalPlace LocalPlace(ILBuilder il) => _place;
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
