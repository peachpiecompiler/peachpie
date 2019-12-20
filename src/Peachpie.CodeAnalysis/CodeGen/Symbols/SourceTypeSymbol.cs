using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static Devsense.PHP.Syntax.Name;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Emit;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.Symbols
{
    partial class SourceTypeSymbol
    {
        internal void SynthesizeInit(Emit.PEModuleBuilder module, DiagnosticBag diagnostics)
        {
            // .cctor
            EmitFieldsCctor(module);

            // __statics.Init
            ((SynthesizedStaticFieldsHolder)this.StaticsContainer)?.EmitCtors(module);

            // IPhpCallable { Invoke, ToPhpValue }
            EmitPhpCallable(module, diagnostics);

            // IDisposable { Dispose }
            EmitDisposable(module, diagnostics);

            // .ctor
            EmitPhpCtors(this.InstanceConstructors, module, diagnostics);

            // System.ToString
            EmitToString(module);

            // trait methods
            EmitTraitImplementations(module);
        }

        void EmitFieldsCctor(Emit.PEModuleBuilder module)
        {
            // list app static fields
            var sflds = GetMembers().OfType<IPhpPropertySymbol>()
                .Where(f => f.FieldKind == PhpPropertyKind.AppStaticField)
                .ToList();

            if (sflds.Count != 0)
            {
                // emit initialization of app static fields
                // note, their initializers do not have Context available, since they are not bound to a Context

                var cctor = module.GetStaticCtorBuilder(this);
                lock (cctor)
                {
                    using (var cg = new CodeGenerator(cctor, module, DiagnosticBag.GetInstance(), module.Compilation.Options.OptimizationLevel, false, this, null, thisPlace: null)
                    {
                        CallerType = this,
                        ContainingFile = this.ContainingFile,
                    })
                    {
                        foreach (var f in sflds)
                        {
                            Debug.Assert(f.RequiresContext == false);
                            Debug.Assert(f.ContainingStaticsHolder == null);

                            f.EmitInit(cg);
                        }
                    }
                }
            }
        }

        void EmitPhpCallable(Emit.PEModuleBuilder module, DiagnosticBag diagnostics)
        {
            var iphpcallable = DeclaringCompilation.CoreTypes.IPhpCallable;

            var __invoke = TryGetMagicInvoke();
            if (__invoke == null ||
                IsAlreadyImplemented(__invoke) ||
                IsAlreadyImplemented(iphpcallable))
            {
                // already implemented in a base class
                return;
            }

            //
            // IPhpCallable.Invoke(Context <ctx>, PhpVaue[] arguments)
            //
            var invoke = new SynthesizedMethodSymbol(this, iphpcallable.FullName + ".Invoke", false, true, DeclaringCompilation.CoreTypes.PhpValue, isfinal: true)
            {
                ExplicitOverride = (MethodSymbol)iphpcallable.Symbol.GetMembers("Invoke").Single(),
                ForwardedCall = __invoke,
            };
            invoke.SetParameters(
                new SpecialParameterSymbol(invoke, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, 0),
                new SynthesizedParameterSymbol(invoke, ArrayTypeSymbol.CreateSZArray(ContainingAssembly, DeclaringCompilation.CoreTypes.PhpValue.Symbol), 1, RefKind.None, name: "arguments", isParams: true));

            module.SetMethodBody(invoke, MethodGenerator.GenerateMethodBody(module, invoke, il =>
            {
                var cg = new CodeGenerator(il, module, diagnostics, module.Compilation.Options.OptimizationLevel, false, this, new ParamPlace(invoke.Parameters[0]), new ArgPlace(this, 0))
                {
                    CallerType = this,
                };

                cg.EmitRet(cg.EmitForwardCall(__invoke, invoke, callvirt: true));

            }, null, diagnostics, false));

            module.SynthesizedManager.AddMethod(this, invoke);

            //
            // IPhpCallable.ToPhpValue()
            //
            var tophpvalue = new SynthesizedMethodSymbol(this, iphpcallable.FullName + ".ToPhpValue", false, true, DeclaringCompilation.CoreTypes.PhpValue, isfinal: true)
            {
                ExplicitOverride = (MethodSymbol)iphpcallable.Symbol.GetMembers("ToPhpValue").Single(),
            };

            //
            module.SetMethodBody(tophpvalue, MethodGenerator.GenerateMethodBody(module, tophpvalue, il =>
            {
                var thisPlace = new ArgPlace(this, 0);
                var ctxPlace = new FieldPlace(thisPlace, this.ContextStore, module);
                var cg = new CodeGenerator(il, module, diagnostics, module.Compilation.Options.OptimizationLevel, false, this, ctxPlace, thisPlace);

                // return PhpValue.FromClass(this)
                cg.EmitThis();
                cg.EmitRet(cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.FromClass_Object));

            }, null, diagnostics, false));

            module.SynthesizedManager.AddMethod(this, tophpvalue);
        }

        bool IsAlreadyImplemented(MethodSymbol method)
        {
            Debug.Assert(method != null);

            if (method.OverriddenMethod != null && method.OverriddenMethod.ContainingType.TypeKind != TypeKind.Interface)
            {
                return true;
            }

            for (var b = this.BaseType; b != null; b = b.BaseType)
            {
                foreach (var m in b.GetMembersByPhpName(method.RoutineName).OfType<MethodSymbol>())
                {
                    if (m.IsStatic == method.IsStatic)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        bool IsAlreadyImplemented(NamedTypeSymbol iface)
        {
            Debug.Assert(iface != null && iface.IsInterface);

            for (var b = this.BaseType; b != null; b = b.BaseType)
            {
                if (b.Interfaces.Contains(iface))
                {
                    return true;
                }
            }

            return false;
        }

        void EmitDisposable(Emit.PEModuleBuilder module, DiagnosticBag diagnostics)
        {
            var idisposable = DeclaringCompilation.GetSpecialType(SpecialType.System_IDisposable);

            var __destruct = TryGetDestruct();
            if (__destruct == null ||
                IsAlreadyImplemented(__destruct) ||
                IsAlreadyImplemented(idisposable))
            {
                // already implemented in a base class
                return;
            }

            //
            // IDisposable.Dispose()
            //
            var dispose = new SynthesizedMethodSymbol(this, idisposable.GetFullName() + ".Dispose", false, true, DeclaringCompilation.CoreTypes.Void, isfinal: true)
            {
                ExplicitOverride = (MethodSymbol)DeclaringCompilation.GetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose),
                ForwardedCall = __destruct,
            };

            module.SetMethodBody(dispose, MethodGenerator.GenerateMethodBody(module, dispose, il =>
            {
                var thisPlace = new ArgPlace(this, 0);
                var ctxPlace = new FieldPlace(thisPlace, this.ContextStore, module);
                var cg = new CodeGenerator(il, module, diagnostics, module.Compilation.Options.OptimizationLevel, false, this, ctxPlace, thisPlace)
                {
                    CallerType = this,
                };

                // private bool <>b_disposed;
                var disposedField = cg.Module.SynthesizedManager.GetOrCreateSynthesizedField(this, DeclaringCompilation.CoreTypes.Boolean, WellKnownPchpNames.SynthesizedDisposedFieldName, Accessibility.Private, false, false, false);
                var disposedPlace = new FieldPlace(thisPlace, disposedField, cg.Module);

                // if (<>b_disposed) return;
                var lblContinue = new object();
                disposedPlace.EmitLoad(cg.Builder);
                cg.Builder.EmitBranch(ILOpCode.Brfalse, lblContinue);
                cg.EmitRet(DeclaringCompilation.CoreTypes.Void);
                cg.Builder.MarkLabel(lblContinue);

                // <>b_disposed = true;
                disposedPlace.EmitStorePrepare(cg.Builder);
                cg.Builder.EmitBoolConstant(true);
                disposedPlace.EmitStore(cg.Builder);

                // __destruct()
                cg.EmitPop(cg.EmitForwardCall(__destruct, dispose, callvirt: true));

                // .ret
                cg.EmitRet(DeclaringCompilation.GetSpecialType(SpecialType.System_Void));

            }, null, diagnostics, false));

            module.SynthesizedManager.AddMethod(this, dispose);

            ////
            //// Finalize()
            ////
            //var finalize = new SynthesizedFinalizeSymbol(this)
            //{
            //    ForwardedCall = dispose,
            //};

            //Debug.Assert(finalize.OverriddenMethod != null);

            //module.SetMethodBody(finalize, MethodGenerator.GenerateMethodBody(module, finalize, il =>
            //{
            //    var thisPlace = new ArgPlace(this, 0);
            //    var ctxPlace = new FieldPlace(thisPlace, this.ContextStore, module);
            //    var cg = new CodeGenerator(il, module, diagnostics, module.Compilation.Options.OptimizationLevel, false, this, ctxPlace, thisPlace)
            //    {
            //        CallerType = this,
            //    };

            //    // 
            //    cg.Builder.OpenLocalScope(ScopeType.TryCatchFinally);
            //    // try {
            //    cg.Builder.OpenLocalScope(ScopeType.Try);

            //    // Dispose()
            //    cg.EmitPop(cg.EmitForwardCall(dispose, finalize, callvirt: false));
            //    //if (cg.EmitPdbSequencePoints) cg.Builder.EmitOpCode(ILOpCode.Nop);

            //    // }
            //    cg.Builder.CloseLocalScope();
            //    // finally {
            //    cg.Builder.OpenLocalScope(ScopeType.Finally);

            //    // base.Finalize()
            //    thisPlace.EmitLoad(cg.Builder);
            //    cg.EmitCall(ILOpCode.Call, finalize.ExplicitOverride);
            //    //if (cg.EmitPdbSequencePoints) cg.Builder.EmitOpCode(ILOpCode.Nop);

            //    // }
            //    cg.Builder.CloseLocalScope();
            //    cg.Builder.CloseLocalScope();

            //    // .ret
            //    cg.EmitRet(DeclaringCompilation.GetSpecialType(SpecialType.System_Void));

            //}, null, diagnostics, false));

            //module.SynthesizedManager.AddMethod(this, finalize);
        }

        void EmitPhpCtors(ImmutableArray<MethodSymbol> instancectors, Emit.PEModuleBuilder module, DiagnosticBag diagnostics)
        {
            foreach (SynthesizedPhpCtorSymbol ctor in instancectors)
            {
                module.SetMethodBody(ctor, MethodGenerator.GenerateMethodBody(module, ctor, il =>
                {
                    if (ctor is SynthesizedParameterlessPhpCtorSymbol)
                    {
                        EmitParameterlessCtor(ctor, il, module, diagnostics);
                        return;
                    }

                    Debug.Assert(SpecialParameterSymbol.IsContextParameter(ctor.Parameters[0]));

                    var cg = new CodeGenerator(il, module, diagnostics, module.Compilation.Options.OptimizationLevel, false, this, new ParamPlace(ctor.Parameters[0]), new ArgPlace(this, 0))
                    {
                        CallerType = this,
                        ContainingFile = ContainingFile,
                    };

                    Debug.Assert(ctor.BaseCtor != null);

                    // base..ctor or this..ctor
                    cg.EmitPop(cg.EmitForwardCall(ctor.BaseCtor, ctor));

                    if (ctor.PhpConstructor == null)
                    {
                        // initialize <ctx> field, if field is declared within this type
                        var ctxField = this.ContextStore;
                        if (ctxField != null && object.ReferenceEquals((object)ctxField.ContainingType, this))
                        {
                            var ctxFieldPlace = new FieldPlace(cg.ThisPlaceOpt, ctxField, module);

                            // Debug.Assert(<ctx> != null)
                            cg.EmitDebugAssertNotNull(cg.ContextPlaceOpt, "Context cannot be null.");

                            // <this>.<ctx> = <ctx>
                            ctxFieldPlace.EmitStorePrepare(il);
                            cg.EmitLoadContext();
                            ctxFieldPlace.EmitStore(il);
                        }

                        // trait specific:
                        if (ctor is SynthesizedPhpTraitCtorSymbol tctor)
                        {
                            EmitTraitCtorInit(cg, tctor);
                        }

                        // trait instances:
                        foreach (var t in this.TraitUses)
                        {
                            EmitTraitInstanceInit(cg, ctor, t);
                        }

                        // initialize instance fields:
                        foreach (var f in this.GetMembers().OfType<IPhpPropertySymbol>().Where(f => f.FieldKind == PhpPropertyKind.InstanceField))
                        {
                            Debug.Assert(f.ContainingStaticsHolder == null);
                            f.EmitInit(cg);
                        }
                    }
                    else
                    {
                        Debug.Assert(ctor.BaseCtor.ContainingType == this);

                        // this.__construct
                        cg.EmitPop(cg.EmitForwardCall(ctor.PhpConstructor, ctor));
                    }

                    // ret
                    Debug.Assert(ctor.ReturnsVoid);
                    cg.EmitRet(ctor.ReturnType);

                }, null, diagnostics, false));
            }
        }

        void EmitParameterlessCtor(SynthesizedPhpCtorSymbol ctor, ILBuilder il, PEModuleBuilder module, DiagnosticBag diagnostics)
        {
            Debug.Assert(ctor.BaseCtor != null);
            Debug.Assert(ctor.PhpConstructor == null);

            // Pchp.Core.Utilities.ContextExtensions.CurrentContext
            var extensions = (NamedTypeSymbol)module.Compilation.GetTypeByMetadataName("Pchp.Core.Utilities.ContextExtensions");
            if (extensions.IsErrorTypeOrNull()) throw new InvalidOperationException("Class Pchp.Core.Utilities.ContextExtensions cannot be resolved.");

            var ctxfield = extensions.LookupMember<PropertySymbol>("CurrentContext");
            if (ctxfield == null) throw new InvalidOperationException("Property ContextExtensions.DefaultContext cannot be resolved.");

            Debug.Assert(ctxfield.GetMethod.IsStatic);

            //
            var cg = new CodeGenerator(il, module, diagnostics, module.Compilation.Options.OptimizationLevel, false, this, new PropertyPlace(null, ctxfield), new ArgPlace(this, 0))
            {
                CallerType = this,
                ContainingFile = ContainingFile,
            };

            // this( ContextExtensions.CurrentContext )
            cg.EmitPop(cg.EmitForwardCall(ctor.BaseCtor, ctor));

            // ret
            Debug.Assert(ctor.ReturnsVoid);
            cg.EmitRet(ctor.ReturnType);
        }

        static void EmitTraitCtorInit(CodeGenerator cg, SynthesizedPhpTraitCtorSymbol tctor)
        {
            var il = cg.Builder;

            // this.<>this = @this
            var thisFieldPlace = new FieldPlace(cg.ThisPlaceOpt, tctor.ContainingType.RealThisField, module: cg.Module);
            thisFieldPlace.EmitStorePrepare(il);
            tctor.ThisParameter.EmitLoad(il);
            thisFieldPlace.EmitStore(il);
        }

        void EmitTraitInstanceInit(CodeGenerator cg, SynthesizedPhpCtorSymbol ctor, TraitUse t)
        {
            // Template: this.<>trait_T = new T(ctx, this, self)

            // PLACE: this.<>trait_T
            var instancePlace = new FieldPlace(cg.ThisPlaceOpt, t.TraitInstanceField, module: cg.Module);

            // .ctor(Context, object @this, RuntimeTypeHandle self)
            var tctor = t.Symbol.InstanceConstructors[0];
            Debug.Assert(tctor.ParameterCount == 2);
            Debug.Assert(tctor.Parameters[0].Type == cg.CoreTypes.Context);
            //Debug.Assert(tctor.Parameters[1].Type == TSelfParameter);

            // using trait in trait?
            var ctort = ctor as SynthesizedPhpTraitCtorSymbol;

            //
            instancePlace.EmitStorePrepare(cg.Builder);
            // Context:
            cg.EmitLoadContext();
            // this:
            if (ctort != null) ctort.ThisParameter.EmitLoad(cg.Builder);    // this is passed from caller
            else cg.EmitThis();
            // new T<TSelf>(...)
            cg.EmitCall(ILOpCode.Newobj, tctor);

            instancePlace.EmitStore(cg.Builder);
        }

        void EmitToString(Emit.PEModuleBuilder module)
        {
            if (this.IsInterface || this.IsTrait)
            {
                return;
            }

            var __tostring = this.GetMembersByPhpName(SpecialMethodNames.Tostring.Value).OfType<MethodSymbol>().FirstOrDefault();
            if (__tostring != null)    // implement ToString if: there is __toString() function
            {
                // lookup base string ToString()
                var overriden = this.LookupMember<MethodSymbol>(
                    WellKnownMemberNames.ObjectToString,
                    m => OverrideHelper.SignaturesMatch(m, (MethodSymbol)DeclaringCompilation.GetSpecialTypeMember(SpecialMember.System_Object__ToString)));

                Debug.Assert(overriden != null);

                if (overriden == null || overriden.IsSealed || overriden.ContainingType == this)
                {
                    // cannot be overriden
                    return;
                }

                // public sealed override string ToString()
                var tostring = new SynthesizedMethodSymbol(this, WellKnownMemberNames.ObjectToString, false, true, DeclaringCompilation.CoreTypes.String, Accessibility.Public, isfinal: false, phphidden: true)
                {
                    ExplicitOverride = overriden,
                    ForwardedCall = __tostring,
                };

                module.SetMethodBody(tostring, MethodGenerator.GenerateMethodBody(module, tostring, il =>
                {
                    var thisPlace = new ArgPlace(this, 0);
                    var cg = new CodeGenerator(il, module, DiagnosticBag.GetInstance(), module.Compilation.Options.OptimizationLevel, false, this, new FieldPlace(thisPlace, this.ContextStore, module), thisPlace);

                    if (__tostring != null)
                    {
                        // __tostring().ToString()
                        cg.EmitConvert(cg.EmitForwardCall(__tostring, tostring, callvirt: true), 0, tostring.ReturnType);
                    }
                    else
                    {
                        // PhpException.ObjectToStringNotSupported(this)
                        cg.EmitThis();
                        cg.EmitPop(cg.EmitCall(ILOpCode.Call, cg.CoreTypes.PhpException.Method("ObjectToStringNotSupported", cg.CoreTypes.Object)));

                        // return ""
                        cg.Builder.EmitStringConstant(string.Empty);
                    }
                    cg.EmitRet(tostring.ReturnType);

                }, null, DiagnosticBag.GetInstance(), false));
                module.SynthesizedManager.AddMethod(this, tostring);
            }
        }

        void EmitTraitImplementations(Emit.PEModuleBuilder module)
        {
            foreach (var t in TraitUses)
            {
                foreach (var m in t.GetMembers().OfType<SynthesizedMethodSymbol>())
                {
                    Debug.Assert(m.ForwardedCall != null);

                    module.SetMethodBody(m, MethodGenerator.GenerateMethodBody(module, m, il =>
                    {
                        IPlace thisPlace = null;
                        IPlace traitInstancePlace = null;
                        IPlace ctxPlace;

                        if (m.IsStatic)
                        {
                            // Template: return TRAIT.method(...)
                            Debug.Assert(SpecialParameterSymbol.IsContextParameter(m.Parameters[0]));
                            ctxPlace = new ParamPlace(m.Parameters[0]);
                        }
                        else
                        {
                            // Template: return this.<>trait.method(...)
                            thisPlace = new ArgPlace(this, 0);  // this
                            ctxPlace = new FieldPlace(thisPlace, this.ContextStore, module);    // this.<ctx>
                            traitInstancePlace = new FieldPlace(thisPlace, t.TraitInstanceField, module); // this.<>trait
                        }

                        using (var cg = new CodeGenerator(il, module, DiagnosticBag.GetInstance(), module.Compilation.Options.OptimizationLevel, false, this, ctxPlace, thisPlace)
                        {
                            CallerType = this,
                        })
                        {
                            var forwarded_type = cg.EmitForwardCall(m.ForwardedCall, m, thisPlaceExplicit: traitInstancePlace);
                            var target_type = m.ReturnType;

                            cg.EmitConvert(forwarded_type, 0, target_type); // always (forwarded_type === target_type)
                            cg.EmitRet(target_type);
                        }

                    }, null, DiagnosticBag.GetInstance(), false));
                    module.SynthesizedManager.AddMethod(this, m);

                    // NOTE: following is not needed anymore:
                    //// ghost stubs: // ... resolve this already in SourceTypeSymbol.GetMembers(), now it does not get overloaded properly
                    //var ps = m.Parameters;
                    //for (int i = 0; i < ps.Length; i++)
                    //{
                    //    if (ps[i].HasUnmappedDefaultValue())  // => ConstantValue couldn't be resolved for optional parameter
                    //    {
                    //        // create ghost stub foo(p0, .. pi-1) => foo(p0, .. , pN)
                    //        GhostMethodBuilder.CreateGhostOverload(m, module, DiagnosticBag.GetInstance(), i);
                    //    }
                    //}
                }
            }
        }

        /// <summary>
        /// Collects methods that has to be overriden and matches with this declaration.
        /// Missing overrides are reported, needed ghost stubs are synthesized.
        /// </summary>
        public void FinalizeMethodTable(Emit.PEModuleBuilder module, DiagnosticBag diagnostics)
        {
            // creates ghost stubs for overrides that do not match the signature

            foreach (var info in this.ResolveOverrides(diagnostics))
            {
                // note: unresolved abstracts already reported by ResolveOverrides

                // is ghost stub needed?

                if (ReferenceEquals(info.OverrideCandidate?.ContainingType, this) ||    // candidate not matching exactly the signature in this type
                    info.ImplementsInterface)                                           // explicitly implement the interface
                {
                    if (info.HasOverride)
                    {
                        if (info.ImplementsInterface && info.Override != null && info.Override.IsVirtual)
                        {
                            // create explicit override only if the interface method is implemented with a class method that does not implement the interface
                            /*
                             * interface I { foo }  // METHOD
                             * class X { foo }      // OVERRIDE
                             * class Y : X, I { explicit I.foo override }   // GHOST
                             */
                            if (info.Override.ContainingType.ImplementsInterface(info.Method.ContainingType))
                            {
                                /* => X implements I */
                                // explicit method override is not needed
                                continue;
                            }
                        }

                        /* 
                         * class A {
                         *    TReturn1 foo(A, B);
                         * }
                         * class B : A {
                         *    TReturn2 foo(A2, B2);
                         *    
                         *    // SYNTHESIZED GHOST:
                         *    override TReturn foo(A, B){ return (TReturn)foo((A2)A, (B2)B);
                         * }
                         */

                        // override method with a ghost that calls the override
                        (info.Override ?? info.OverrideCandidate).CreateGhostOverload(
                            this, module, diagnostics,
                            info.Method.ReturnType, info.Method.Parameters,
                            phphidden: true, explicitOverride: info.Method);
                    }
                    else
                    {
                        // synthesize abstract method implementing unresolved interface member
                        if (info.IsUnresolvedAbstract && info.ImplementsInterface && this.IsAbstract && !this.IsInterface)
                        {
                            Debug.Fail("Unreachable; The method should be synthesized in 'ResolveOverrides'.");
                        }
                    }
                }

                if (info.Method is SynthesizedMethodSymbol && info.Method.ContainingType == this && info.Method.IsAbstract)
                {
                    module.SynthesizedManager.AddMethod(this, info.Method);
                }

                if (info.Override is SynthesizedMethodSymbol sm && sm.ContainingType == this)
                {
                    if (sm.ExplicitOverride == null)
                    {
                        // setup synthesized methods explicit override as resolved
                        sm.ExplicitOverride = info.Method;
                    }
                }
            }

            // synthesized field accessors:
            foreach (var srcf in GetMembers().OfType<SourceFieldSymbol>())
            {
                var paccessor = srcf.FieldAccessorProperty;
                if (paccessor != null)
                {
                    GenerateFieldAccessorProperty(module, diagnostics, srcf, paccessor);
                }
            }
        }

        void GenerateFieldAccessorProperty(Emit.PEModuleBuilder module, DiagnosticBag diagnostics, SourceFieldSymbol srcf, PropertySymbol paccessor)
        {
            //
            module.SynthesizedManager.AddProperty(this, paccessor);

            //
            var get_body = MethodGenerator.GenerateMethodBody(module, paccessor.GetMethod, (il) =>
            {
                // Template: return field;
                var place = new FieldPlace(new ArgPlace(this, 0), srcf.OverridenDefinition, module);
                place.EmitLoad(il);
                il.EmitRet(false);
            }, null, diagnostics, false);

            module.SetMethodBody(paccessor.GetMethod, get_body);
            module.SynthesizedManager.AddMethod(this, paccessor.GetMethod);

            //
            var set_body = MethodGenerator.GenerateMethodBody(module, paccessor.SetMethod, (il) =>
            {
                // Template: field = value;
                var place = new FieldPlace(new ArgPlace(this, 0), srcf.OverridenDefinition, module);
                place.EmitStorePrepare(il);
                new ArgPlace(this, 1).EmitLoad(il);
                place.EmitStore(il);
                il.EmitRet(true);
            }, null, diagnostics, false);

            module.SetMethodBody(paccessor.SetMethod, set_body);
            module.SynthesizedManager.AddMethod(this, paccessor.SetMethod);
        }
    }
}
