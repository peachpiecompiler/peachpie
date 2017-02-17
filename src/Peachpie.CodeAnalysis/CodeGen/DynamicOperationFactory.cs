using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.CodeGen
{
    internal class DynamicOperationFactory
    {
        public class CallSiteData
        {
            /// <summary>
            /// CallSite_T.Target method.
            /// </summary>
            public FieldSymbol Target => _target;
            SubstitutedFieldSymbol _target;

            /// <summary>
            /// CallSite_T field.
            /// </summary>
            public IPlace Place => new FieldPlace(null, _fld);
            SynthesizedFieldSymbol _fld;

            /// <summary>
            /// Gets CallSite.Create method.
            /// </summary>
            public MethodSymbol CallSite_Create => _callsite_create;
            MethodSymbol _callsite_create;

            public void Construct(NamedTypeSymbol functype, Action<CodeGenerator> binder_builder)
            {
                var callsitetype = _factory.CallSite_T.Construct(functype);

                // TODO: check if it wasn't constructed already

                _target.SetContainingType((SubstitutedNamedTypeSymbol)callsitetype);
                _fld.SetFieldType(callsitetype);
                _callsite_create = (MethodSymbol)_factory.CallSite_T_Create.SymbolAsMember(callsitetype);

                // create callsite

                // static .cctor {
                var cctor = _factory.CctorBuilder;

                // fld = CallSite<T>.Create( <BINDER> )
                var fldPlace = this.Place;
                fldPlace.EmitStorePrepare(cctor);

                var cg = _factory._cg;
                var cctor_cg = new CodeGenerator(cctor, cg.Module, cg.Diagnostics, cg.DeclaringCompilation.Options.OptimizationLevel, false, _factory._container, null, null, cg.Routine);
                binder_builder(cctor_cg);
                cctor.EmitCall(_factory._cg.Module, _factory._cg.Diagnostics, ILOpCode.Call, this.CallSite_Create);

                fldPlace.EmitStore(cctor);

                // }
            }

            public void EmitLoadTarget(ILBuilder il)
            {
                this.Place.EmitLoad(il);
                il.EmitOpCode(ILOpCode.Ldfld);
                il.EmitSymbolToken(_factory._cg.Module, _factory._cg.Diagnostics, _target, null);
            }

            readonly DynamicOperationFactory _factory;

            internal CallSiteData(DynamicOperationFactory factory, string fldname = null)
            {
                _factory = factory;

                _fld = factory.CreateCallSiteField(fldname ?? string.Empty);

                // AsMember // we'll change containing type later once we know, important to have Substitued symbol before calling it
                _target = new SubstitutedFieldSymbol(factory.CallSite_T, factory.CallSite_T_Target, _fld.MetadataName);
            }
        }

        readonly PhpCompilation _compilation;
        readonly NamedTypeSymbol _container;
        readonly CodeGenerator _cg;

        NamedTypeSymbol _callsitetype;
        NamedTypeSymbol _callsitetype_generic;
        MethodSymbol _callsite_generic_create;
        FieldSymbol _callsite_generic_target;

        public NamedTypeSymbol CallSite => _callsitetype ?? (_callsitetype = _compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_CallSite));
        public NamedTypeSymbol CallSite_T => _callsitetype_generic ?? (_callsitetype_generic = _compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_CallSite_T));
        public MethodSymbol CallSite_T_Create => _callsite_generic_create ?? (_callsite_generic_create = (MethodSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_CallSite_T__Create));
        public FieldSymbol CallSite_T_Target => _callsite_generic_target ?? (_callsite_generic_target = (FieldSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_CallSite_T__Target));

        public CallSiteData StartCallSite(string fldname) => new CallSiteData(this, fldname);

        /// <summary>
        /// Static constructor IL builder for dynamic sites in current context.
        /// </summary>
        public ILBuilder CctorBuilder => _cg.Module.GetStaticCtorBuilder(_container);

        public SynthesizedFieldSymbol CreateCallSiteField(string namehint) => _cg.Module.SynthesizedManager
            .GetOrCreateSynthesizedField(
                _container, CallSite, namehint, Accessibility.Private, true, true,
                autoincrement: true);

        public DynamicOperationFactory(CodeGenerator cg, NamedTypeSymbol container)
        {
            Contract.ThrowIfNull(cg);
            Contract.ThrowIfNull(container);

            _cg = cg;
            _compilation = cg.DeclaringCompilation;
            _container = container;
        }

        internal NamedTypeSymbol GetCallSiteDelegateType(
            TypeSymbol loweredReceiver,
            RefKind receiverRefKind,
            ImmutableArray<TypeSymbol> loweredArguments,
            ImmutableArray<RefKind> refKinds,
            TypeSymbol loweredRight,
            TypeSymbol resultType)
        {
            Debug.Assert(refKinds.IsDefaultOrEmpty || refKinds.Length == loweredArguments.Length);

            var callSiteType = this.CallSite;
            if (callSiteType.IsErrorType())
            {
                return null;
            }

            var delegateSignature = MakeCallSiteDelegateSignature(callSiteType, loweredReceiver, loweredArguments, loweredRight, resultType);
            bool returnsVoid = resultType.SpecialType == SpecialType.System_Void;
            bool hasByRefs = receiverRefKind != RefKind.None || !refKinds.IsDefaultOrEmpty;

            if (!hasByRefs)
            {
                var wkDelegateType = returnsVoid ?
                    WellKnownTypes.GetWellKnownActionDelegate(invokeArgumentCount: delegateSignature.Length) :
                    WellKnownTypes.GetWellKnownFunctionDelegate(invokeArgumentCount: delegateSignature.Length - 1);

                if (wkDelegateType != WellKnownType.Unknown)
                {
                    var delegateType = _compilation.GetWellKnownType(wkDelegateType);
                    if (delegateType != null && !delegateType.IsErrorType())
                    {
                        return delegateType.Construct(delegateSignature);
                    }
                }
            }

            BitVector byRefs;
            if (hasByRefs)
            {
                byRefs = BitVector.Create(1 + (loweredReceiver != null ? 1 : 0) + loweredArguments.Length + (loweredRight != null ? 1 : 0));

                int j = 1;
                if (loweredReceiver != null)
                {
                    byRefs[j++] = receiverRefKind != RefKind.None;
                }

                if (!refKinds.IsDefault)
                {
                    for (int i = 0; i < refKinds.Length; i++, j++)
                    {
                        if (refKinds[i] != RefKind.None)
                        {
                            byRefs[j] = true;
                        }
                    }
                }
            }
            else
            {
                byRefs = default(BitVector);
            }

            int parameterCount = delegateSignature.Length - (returnsVoid ? 0 : 1);

            return _compilation.AnonymousTypeManager.SynthesizeDelegate(parameterCount, byRefs, returnsVoid).Construct(delegateSignature);
        }

        internal TypeSymbol[] MakeCallSiteDelegateSignature(TypeSymbol callSiteType, TypeSymbol receiver, ImmutableArray<TypeSymbol> arguments, TypeSymbol right, TypeSymbol resultType)
        {
            var systemObjectType = (TypeSymbol)_compilation.GetSpecialType(SpecialType.System_Object);
            var result = new TypeSymbol[1 + (receiver != null ? 1 : 0) + arguments.Length + (right != null ? 1 : 0) + (resultType.SpecialType == SpecialType.System_Void ? 0 : 1)];
            int j = 0;

            // CallSite:
            result[j++] = callSiteType;

            // receiver:
            if (receiver != null)
            {
                result[j++] = receiver;
            }

            // argument types:
            for (int i = 0; i < arguments.Length; i++)
            {
                result[j++] = arguments[i];
            }

            // right hand side of an assignment:
            if (right != null)
            {
                result[j++] = right;
            }

            // return type:
            if (j < result.Length)
            {
                result[j++] = resultType;
            }

            return result;
        }

        internal SynthesizedStaticLocHolder DeclareStaticLocalHolder(string locName, TypeSymbol locType)
        {
            var holder = new SynthesizedStaticLocHolder(_cg.Routine, locName, locType);

            _cg.Module.SynthesizedManager.AddNestedType(_container, holder);

            return holder;
        }
    }
}
