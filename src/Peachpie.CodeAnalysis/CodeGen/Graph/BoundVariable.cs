using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Text;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Symbols;
using Peachpie.CodeAnalysis.Utilities;
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
            if (VariableKind == VariableKind.LocalTemporalVariable || cg.HasUnoptimizedLocals)
            {
                // temporal variables must be indirect
                return;
            }

            // declare variable in global scope
            var il = cg.Builder;
            var def = il.LocalSlotManager.DeclareLocal(
                    (Cci.ITypeReference)_symbol.Type, _symbol as ILocalSymbolInternal,
                    this.Name, SynthesizedLocalKind.UserDefined,
                    LocalDebugId.None, 0, LocalSlotConstraints.None, ImmutableArray<bool>.Empty, ImmutableArray<string>.Empty, false);

            _place = new LocalPlace(def);
            il.AddLocalToScope(def);

            //
            if (_symbol is SynthesizedLocalSymbol)
            {
                return;
            }

            // Initialize local variable with void.
            // This is mandatory since even assignments reads the target value to assign properly to PhpAlias.

            // TODO: Once analysis tells us, the target cannot be alias, this step won't be necessary.

            // TODO: only if the local will be used uninitialized

            cg.EmitInitializePlace(_place);
        }

        internal override IBoundReference BindPlace(ILBuilder il, BoundAccess access, TypeRefMask thint)
        {
            if (VariableKind == VariableKind.LocalTemporalVariable)
            {
                // Temporal variables must be indirect
                return new BoundIndirectTemporalVariablePlace(new BoundLiteral(this.Name), access);
            }

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

        /// <summary>
        /// Creates local bound to a place.
        /// </summary>
        internal static BoundLocal CreateFromPlace(IPlace place)
        {
            Contract.ThrowIfNull(place);
            return new BoundLocal(null) { _place = place };
        }
    }

    partial class BoundIndirectLocal
    {
        internal override IBoundReference BindPlace(ILBuilder il, BoundAccess access, TypeRefMask thint)
        {
            return new BoundIndirectVariablePlace(_nameExpr, access);
        }

        internal override IPlace Place(ILBuilder il) => null;
    }

    partial class BoundParameter
    {
        #region IParameterSource, IParameterTarget

        /// <summary>
        /// Describes the parameter source place.
        /// </summary>
        interface IParameterSource
        {
            void EmitTypeCheck(CodeGenerator cg, SourceParameterSymbol srcp);

            /// <summary>Inplace copies the parameter.</summary>
            void EmitPass(CodeGenerator cg);

            /// <summary>Loads copied parameter value.</summary>
            TypeSymbol EmitLoad(CodeGenerator cg);
        }

        /// <summary>
        /// Describes the local variable target slot.
        /// </summary>
        interface IParameterTarget
        {
            void StorePrepare(CodeGenerator cg);
            void Store(CodeGenerator cg, TypeSymbol valuetype);
        }

        /// <summary>
        /// Parameter or local is real CLR value on stack.
        /// </summary>
        sealed class DirectParameter : IParameterSource, IParameterTarget
        {
            readonly IPlace _place;
            readonly bool _isparams;
            readonly bool _byref;
            readonly bool _notNull;

            public DirectParameter(IPlace place, bool isparams, bool byref, bool notNull)
            {
                Debug.Assert(place != null);
                _place = place;
                _isparams = isparams;
                _byref = byref;
                _notNull = notNull;
            }

            /// <summary>Loads copied parameter value.</summary>
            public TypeSymbol EmitLoad(CodeGenerator cg)
            {
                if (_isparams)
                {
                    // converts params -> PhpArray
                    Debug.Assert(_place.TypeOpt.IsSZArray());
                    return cg.ArrayToPhpArray(_place, deepcopy: true);
                }
                else
                {
                    // load parameter & dereference PhpValue
                    TypeSymbol t;
                    if (_place.TypeOpt == cg.CoreTypes.PhpValue)
                    {
                        // p.GetValue() : PhpValue
                        _place.EmitLoadAddress(cg.Builder);
                        t = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.GetValue);
                    }
                    else
                    {
                        // p
                        t = _place.EmitLoad(cg.Builder);
                    }

                    // make copy of given value
                    return cg.EmitDeepCopy(t, nullcheck: !_notNull);
                }
            }

            public void EmitPass(CodeGenerator cg)
            {
                // inplace copies the parameter

                if (_place.TypeOpt == cg.CoreTypes.PhpValue)
                {
                    // dereference & copy
                    // (ref <param>).PassValue()
                    _place.EmitLoadAddress(cg.Builder);
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.PassValue);
                }
                else if (cg.IsCopiable(_place.TypeOpt))
                {
                    _place.EmitStorePrepare(cg.Builder);

                    // copy
                    // <param> = DeepCopy(<param>)
                    cg.EmitDeepCopy(_place.EmitLoad(cg.Builder), nullcheck: !_notNull);

                    _place.EmitStore(cg.Builder);
                }
            }

            public void EmitTypeCheck(CodeGenerator cg, SourceParameterSymbol srcp)
            {
                BoundParameter.EmitTypeCheck(cg, _place, srcp);
            }

            public void Store(CodeGenerator cg, TypeSymbol valuetype)
            {
                cg.EmitConvert(valuetype, 0, _place.TypeOpt);
                _place.EmitStore(cg.Builder);
            }

            public void StorePrepare(CodeGenerator cg)
            {
                _place.EmitStorePrepare(cg.Builder); // nop
            }
        }

        /// <summary>
        /// Parameter is fake and is stored in {varargs} array.
        /// </summary>
        sealed class IndirectParameterSource : IParameterSource
        {
            readonly IPlace _varargsplace;
            readonly int _index;
            bool _isparams => _p.IsParams;
            bool _byref => _p.Syntax.PassedByRef;

            readonly SourceParameterSymbol _p;

            public IndirectParameterSource(SourceParameterSymbol p, ParameterSymbol varargparam)
            {
                Debug.Assert(p.IsFake);
                Debug.Assert(varargparam.Type.IsSZArray());

                _p = p;
                _varargsplace = new ParamPlace(varargparam);
                _index = p.Ordinal - varargparam.Ordinal;
                Debug.Assert(_index >= 0);
            }

            public TypeSymbol EmitLoad(CodeGenerator cg)
            {
                if (_isparams)
                {
                    // PhpArray( {varargs[index..] )
                    return cg.ArrayToPhpArray(_varargsplace, startindex: _index, deepcopy: true);
                }
                else
                {
                    var il = cg.Builder;

                    var lbl_default = new object();
                    var lbl_end = new object();

                    var element_type = ((ArrayTypeSymbol)_varargsplace.TypeOpt).ElementType;

                    // Template: _index < {vargags}.Length ? {varargs[_index]} : DEFAULT

                    // _index < {varargs}.Length
                    il.EmitIntConstant(_index);
                    _varargsplace.EmitLoad(il);
                    cg.EmitArrayLength();
                    il.EmitBranch(ILOpCode.Bge, lbl_default);

                    // LOAD varargs[index]
                    _varargsplace.EmitLoad(il);
                    il.EmitIntConstant(_index);
                    il.EmitOpCode(ILOpCode.Ldelem);
                    cg.EmitSymbolToken(element_type, null);
                    cg.EmitConvertToPhpValue(cg.EmitDeepCopy(element_type, nullcheck: false), 0);
                    il.EmitBranch(ILOpCode.Br, lbl_end);

                    // DEFAULT
                    il.MarkLabel(lbl_default);

                    if (_p.Initializer != null)
                    {
                        using (var tmpcg = new CodeGenerator(cg, _p.Routine))
                        {
                            tmpcg.EmitConvertToPhpValue(_p.Initializer);
                        }
                    }
                    else
                    {
                        cg.Emit_PhpValue_Null();
                    }

                    //
                    il.MarkLabel(lbl_end);

                    //
                    return cg.CoreTypes.PhpValue;
                }
            }

            public void EmitPass(CodeGenerator cg) => throw ExceptionUtilities.Unreachable;

            public void EmitTypeCheck(CodeGenerator cg, SourceParameterSymbol srcp)
            {
                // throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Local variables are unoptimized, parameter must be stored in {locals} array.
        /// </summary>
        sealed class IndirectLocalTarget : IParameterTarget
        {
            readonly string _localname;

            public IndirectLocalTarget(string localname)
            {
                _localname = localname;
            }

            public void StorePrepare(CodeGenerator cg)
            {
                Debug.Assert(cg.LocalsPlaceOpt != null);

                // LOAD <locals>, <name>
                cg.LocalsPlaceOpt.EmitLoad(cg.Builder);             // <locals>
                cg.EmitIntStringKey(_localname);  // [key]
            }

            public void Store(CodeGenerator cg, TypeSymbol valuetype)
            {
                // Template: {PhpArray}.Add({name}, {value})
                cg.EmitConvertToPhpValue(valuetype, 0);
                cg.EmitPop(cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.Add_IntStringKey_PhpValue));  // TODO: Append() without duplicity check
            }
        }

        #endregion

        /// <summary>
        /// In case routine uses array of locals (unoptimized locals).
        /// </summary>
        bool _isUnoptimized;

        /// <summary>
        /// When parameter should be copied or its CLR type does not fit into its runtime type.
        /// E.g. foo(int $i){ $i = "Hello"; }
        /// </summary>
        IPlace _lazyplace;

        static void EmitTypeCheck(CodeGenerator cg, IPlace valueplace, SourceParameterSymbol srcparam)
        {
            // TODO: check callable, iterable, type if not resolved in ct

            // check NotNull
            if (srcparam.IsNotNull)
            {
                if (valueplace.TypeOpt.IsReferenceType && valueplace.TypeOpt != cg.CoreTypes.PhpAlias)
                {
                    cg.EmitSequencePoint(srcparam.Syntax);

                    // Template: if (<param> == null) { PhpException.ArgumentNullError(param_name); }
                    var lbl_notnull = new object();
                    cg.EmitNotNull(valueplace);
                    cg.Builder.EmitBranch(ILOpCode.Brtrue_s, lbl_notnull);

                    // PhpException.ArgumentNullError(param_name);
                    // Consider: just Debug.Assert(<param> != null) for private methods
                    cg.Builder.EmitStringConstant(srcparam.Name);
                    cg.EmitPop(cg.EmitCall(ILOpCode.Call, cg.CoreTypes.PhpException.Method("ArgumentNullError", cg.CoreTypes.String)));

                    //
                    cg.Builder.EmitOpCode(ILOpCode.Nop);

                    cg.Builder.MarkLabel(lbl_notnull);
                }
            }
        }

        internal override void EmitInit(CodeGenerator cg)
        {
            var srcparam = _symbol as SourceParameterSymbol;
            if (srcparam == null)
            {
                // an implicit parameter,
                // nothing to initialize
                return;
            }

            //
            // source: real parameter OR fake parameter: IParameterSource { TypeCheck, Pass, Load }
            // target: optimized locals OR unoptimized locals: IParameterTarget { StorePrepare, Store }
            //

            var source = srcparam.IsFake
                ? (IParameterSource)new IndirectParameterSource(srcparam, srcparam.Routine.GetParamsParameter())
                : (IParameterSource)new DirectParameter(new ParamPlace(srcparam), srcparam.IsParams, byref: srcparam.Syntax.PassedByRef, notNull: srcparam.IsNotNull);

            if (cg.HasUnoptimizedLocals == false) // usual case - optimized locals
            {
                // TODO: cleanup
                var tmask = srcparam.Routine.ControlFlowGraph.GetLocalTypeMask(srcparam.Name);
                var clrtype = cg.DeclaringCompilation.GetTypeFromTypeRef(srcparam.Routine, tmask);

                // target local must differ from source parameter ?
                if (srcparam.IsFake || (srcparam.Type != cg.CoreTypes.PhpValue && srcparam.Type != cg.CoreTypes.PhpAlias && srcparam.Type != clrtype))
                {
                    var loc = cg.Builder.LocalSlotManager.DeclareLocal(
                        (Cci.ITypeReference)clrtype, new SynthesizedLocalSymbol(srcparam.Routine, srcparam.Name, clrtype), srcparam.Name,
                        SynthesizedLocalKind.UserDefined, LocalDebugId.None, 0, LocalSlotConstraints.None, ImmutableArray<bool>.Empty, ImmutableArray<string>.Empty, false);
                    _lazyplace = new LocalPlace(loc);
                    cg.Builder.AddLocalToScope(loc);
                }
            }

            var target = cg.HasUnoptimizedLocals
                ? (IParameterTarget)new IndirectLocalTarget(srcparam.Name)
                : (_lazyplace != null)
                    ? new DirectParameter(_lazyplace, srcparam.IsParams, byref: srcparam.Syntax.PassedByRef, notNull: srcparam.IsNotNull/*not important*/)
                    : (DirectParameter)source;

            // 1. TypeCheck
            source.EmitTypeCheck(cg, srcparam);

            if (source == target)
            {
                // 2a. (source == target): Pass (inplace copy)
                source.EmitPass(cg);
            }
            else
            {
                // 2b. (source != target): StorePrepare -> Load&Copy -> Store
                target.StorePrepare(cg);
                var loaded = source.EmitLoad(cg);
                target.Store(cg, loaded);
            }

            _isUnoptimized = cg.HasUnoptimizedLocals;

            if (_lazyplace != null)
            {
                // TODO: perf warning
            }

            // TODO: ? if (cg.HasUnoptimizedLocals && $this) <locals>["this"] = ...
        }

        internal override IBoundReference BindPlace(ILBuilder il, BoundAccess access, TypeRefMask thint)
        {
            if (_lazyplace != null)
            {
                return new BoundLocalPlace(_lazyplace, access, thint);
            }

            if (_isUnoptimized)
            {
                return new BoundIndirectVariablePlace(new BoundLiteral(this.Name), access);
            }
            else
            {
                //
                return new BoundLocalPlace(Place(il), access, thint);
            }
        }

        internal override IPlace Place(ILBuilder il)
        {
            if (_lazyplace != null)
            {
                return _lazyplace;
            }

            if (_isUnoptimized)
            {
                return null;
            }

            IPlace place = new ParamPlace(_symbol);

            if (this.VariableKind == VariableKind.ThisParameter)
            {
                place = new ReadOnlyPlace(place);
            }

            return place;
        }
    }

    partial class BoundThisParameter
    {
        internal override IBoundReference BindPlace(ILBuilder il, BoundAccess access, TypeRefMask thint)
        {
            return new BoundLocalPlace(Place(il), access, thint);
        }

        internal override IPlace Place(ILBuilder il)
        {
            // Get place of PHP $this variable in the routine.
            // This may vary in different symbols like global code, generator sm method, etc.

            return _routine.GetPhpThisVariablePlace();
        }
    }

    partial class BoundSuperGlobalVariable
    {
        internal override void EmitInit(CodeGenerator cg)
        {
            Debug.Assert(_name.IsAutoGlobal);
        }

        internal override IBoundReference BindPlace(ILBuilder il, BoundAccess access, TypeRefMask thint)
        {
            Debug.Assert(_name.IsAutoGlobal);

            return new BoundSuperglobalPlace(_name, access);
        }

        internal override IPlace Place(ILBuilder il)
        {
            // TODO: place of superglobal variable

            return null;
        }
    }
}
