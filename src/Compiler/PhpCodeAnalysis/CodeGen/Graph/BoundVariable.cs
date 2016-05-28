using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
        internal IPlace _place;

        internal override void EmitInit(CodeGenerator cg)
        {
            if (_symbol is SourceReturnSymbol)  // TODO: remove SourceReturnSymbol
            {
                return;
            }

            if (cg.HasUnoptimizedLocals)
            {
                return;
            }

            // declare variable in global scope
            var il = cg.Builder;
            var def = il.LocalSlotManager.DeclareLocal(
                    (Cci.ITypeReference)_symbol.Type, _symbol as ILocalSymbolInternal,
                    this.Name, SynthesizedLocalKind.UserDefined,
                    LocalDebugId.None, 0, LocalSlotConstraints.None, false, ImmutableArray<TypedConstant>.Empty, false);

            _place = new LocalPlace(def);
            il.AddLocalToScope(def);

            //
            if (_symbol is SynthesizedLocalSymbol)
                return;

            // Initialize local variable with void.
            // This is mandatory since even assignments reads the target value to assign properly to PhpAlias.

            // TODO: Once analysis tells us, the target cannot be alias, this step won't be necessary.

            // TODO: only if the local will be used uninitialized

            if (_place.TypeOpt == cg.CoreTypes.PhpValue)
            {
                _place.EmitStorePrepare(cg.Builder);
                cg.Emit_PhpValue_Void();
                _place.EmitStore(cg.Builder);
            }
            else if (_place.TypeOpt == cg.CoreTypes.PhpNumber)
            {
                // <place> = PhpNumber(0)
                _place.EmitStorePrepare(cg.Builder);
                cg.Builder.EmitOpCode(ILOpCode.Ldsfld);
                cg.EmitSymbolToken(cg.CoreMethods.PhpNumber.Default, null);
                _place.EmitStore(cg.Builder);
            }
            else if (_place.TypeOpt == cg.CoreTypes.PhpAlias)
            {
                _place.EmitStorePrepare(cg.Builder);
                cg.Emit_PhpValue_Void();
                cg.Emit_PhpValue_MakeAlias();
                _place.EmitStore(cg.Builder);
            }
        }

        internal override IBoundReference BindPlace(ILBuilder il, BoundAccess access, TypeRefMask thint)
        {
            if (_place == null)
            {
                // unoptimized locals
                return new BoundIndirectVariablePlace(new BoundLiteral(this.Name), access);
            }
            else
            {
                return new BoundLocalPlace(_place, access, thint);
            }
        }

        internal override IPlace Place(ILBuilder il) => LocalPlace(il);

        private IPlace LocalPlace(ILBuilder il) => _place;
    }

    partial class BoundStaticLocal
    {
        /// <summary>
        /// Place of the local variable containing the holder instance.
        /// </summary>
        internal IPlace _holderPlace;

        internal SynthesizedStaticLocHolder _holder;

        internal override void EmitInit(CodeGenerator cg)
        {
            // variable holder class
            _holder = cg.Factory.DeclareStaticLocalHolder(this.Name, (TypeSymbol)this.Variable.Type);

            // local with its instance
            var symbol = new SynthesizedLocalSymbol(cg.Routine, this.Name, _holder);
            var loc = cg.Builder.LocalSlotManager.DeclareLocal(_holder, symbol, symbol.Name, SynthesizedLocalKind.OptimizerTemp, LocalDebugId.None, 0, LocalSlotConstraints.None, false, ImmutableArray<TypedConstant>.Empty, false);

            _holderPlace = new LocalPlace(loc);

            // place = holder.value
            _place = new FieldPlace(_holderPlace, _holder.ValueField);

            if (cg.HasUnoptimizedLocals)
            {
                // TODO reference to <locals>
            }
        }

        internal override IBoundReference BindPlace(ILBuilder il, BoundAccess access, TypeRefMask thint)
        {
            return new BoundLocalPlace(_place, access, thint);
        }

        internal override IPlace Place(ILBuilder il) => _place;
    }

    partial class BoundParameter
    {
        /// <summary>
        /// In case routine uses array of locals (unoptimized locals).
        /// </summary>
        bool _isUnoptimized;

        /// <summary>
        /// When parameter should be copied or its CLR type does not fit into its runtime type.
        /// E.g. foo(int $i){ $i = "Hello"; }
        /// </summary>
        BoundLocal _lazyLocal;

        internal override void EmitInit(CodeGenerator cg)
        {
            // TODO: ? if (cg.HasUnoptimizedLocals && $this) <locals>["this"] = ...

            var srcparam = _symbol as SourceParameterSymbol;
            if (srcparam != null)
            {
                var srcplace = new ParamPlace(_symbol);
                var routine = srcparam.Routine;

                if (cg.HasUnoptimizedLocals)
                {
                    Debug.Assert(cg.LocalsPlaceOpt != null);

                    // copy parameter to <locals>[Name]

                    // <locals>[name] = value
                    cg.LocalsPlaceOpt.EmitLoad(cg.Builder); // <locals>
                    cg.EmitIntStringKey(new BoundLiteral(this.Name));   // [key]
                    cg.EmitConvertToPhpValue(srcplace.EmitLoad(cg.Builder), 0); // value
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.SetItemValue_IntStringKey_PhpValue);

                    //
                    _isUnoptimized = true;
                }
                else
                {
                    // TODO: copy parameter by value in case of PhpValue, Array, PhpString

                    // create local variable in case of parameter type is not enough for its use within routine
                    if (_symbol.Type != cg.CoreTypes.PhpValue && _symbol.Type != cg.CoreTypes.PhpAlias)
                    {
                        var tmask = routine.ControlFlowGraph.GetParamTypeMask(srcparam);
                        var clrtype = cg.DeclaringCompilation.GetTypeFromTypeRef(routine, tmask);
                        if (clrtype != _symbol.Type)    // Assert: only if clrtype is not covered by _symbol.Type
                        {
                            // TODO: performance warning

                            _lazyLocal = new BoundLocal(new SynthesizedLocalSymbol(routine, srcparam.Name, clrtype));
                            _lazyLocal.EmitInit(cg);
                            var localplace = _lazyLocal.Place(cg.Builder);

                            // <local> = <param>
                            localplace.EmitStorePrepare(cg.Builder);
                            cg.EmitConvert(srcplace.EmitLoad(cg.Builder), 0, clrtype);
                            localplace.EmitStore(cg.Builder);
                        }
                    }
                }
            }
        }

        internal override IBoundReference BindPlace(ILBuilder il, BoundAccess access, TypeRefMask thint)
        {
            if (_isUnoptimized)
            {
                return new BoundIndirectVariablePlace(new BoundLiteral(this.Name), access);
            }
            else
            {
                //
                return (_lazyLocal != null)
                    ? _lazyLocal.BindPlace(il, access, thint)
                    : new BoundLocalPlace(Place(il), access, thint);
            }
        }

        internal override IPlace Place(ILBuilder il)
        {
            if (_isUnoptimized)
                return null;

            var place = (_lazyLocal != null) ? _lazyLocal.Place(il) : new ParamPlace(_symbol);

            if (this.VariableKind == VariableKind.ThisParameter)
            {
                place = new ReadOnlyPlace(place);
            }

            return place;
        }
    }

    partial class BoundGlobalVariable
    {
        internal override void EmitInit(CodeGenerator cg)
        {
            if (!_name.IsAutoGlobal)
            {
                // TODO: create shadow local in case we can
            }
        }

        internal override IBoundReference BindPlace(ILBuilder il, BoundAccess access, TypeRefMask thint)
        {
            // IBoundReference of $_GLOBALS[<name>]

            if (_name.IsAutoGlobal)
            {
                return new BoundSuperglobalPlace(_name, access);
            }
            else
            {
                // <variables>[<name>]
                return new BoundGlobalPlace(new BoundLiteral(_name.Value), access);
            }
        }

        internal override IPlace Place(ILBuilder il)
        {
            // TODO: place of superglobal variable

            return null;
        }
    }
}
