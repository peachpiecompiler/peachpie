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

namespace Pchp.CodeAnalysis.Symbols
{
    partial class SourceTypeSymbol
    {
        internal void EmitInit(Emit.PEModuleBuilder module, DiagnosticBag diagnostics)
        {
            // .cctor
            EmitFieldsCctor(module);

            // __statics.Init
            ((SynthesizedStaticFieldsHolder)this.StaticsContainer)?.EmitCtors(module);

            // IPhpCallable { Invoke, ToPhpValue }
            EmitPhpCallable(module, diagnostics);

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
                var cg = new CodeGenerator(cctor, module, DiagnosticBag.GetInstance(), module.Compilation.Options.OptimizationLevel, false, this, null, null)
                {
                    CallerType = this,
                };

                foreach (var f in sflds)
                {
                    Debug.Assert(f.RequiresContext == false);
                    Debug.Assert(f.ContainingStaticsHolder == null);

                    f.EmitInit(cg);
                }
            }
        }

        void EmitPhpCallable(Emit.PEModuleBuilder module, DiagnosticBag diagnostics)
        {
            var __invoke = TryGetMagicInvoke();
            if (__invoke == null || (__invoke.OverriddenMethod != null && __invoke.OverriddenMethod.ContainingType.TypeKind != TypeKind.Interface))
            {
                return;
            }

            //
            // IPhpCallable.Invoke(Context <ctx>, PhpVaue[] arguments)
            //
            var invoke = new SynthesizedMethodSymbol(this, "IPhpCallable.Invoke", false, true, DeclaringCompilation.CoreTypes.PhpValue, isfinal: false)
            {
                ExplicitOverride = (MethodSymbol)DeclaringCompilation.CoreTypes.IPhpCallable.Symbol.GetMembers("Invoke").Single(),
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
            var tophpvalue = new SynthesizedMethodSymbol(this, "IPhpCallable.ToPhpValue", false, true, DeclaringCompilation.CoreTypes.PhpValue, isfinal: false)
            {
                ExplicitOverride = (MethodSymbol)DeclaringCompilation.CoreTypes.IPhpCallable.Symbol.GetMembers("ToPhpValue").Single(),
            };

            //
            module.SetMethodBody(tophpvalue, MethodGenerator.GenerateMethodBody(module, tophpvalue, il =>
            {
                var thisPlace = new ArgPlace(this, 0);
                var cg = new CodeGenerator(il, module, diagnostics, module.Compilation.Options.OptimizationLevel, false, this, new FieldPlace(thisPlace, this.ContextStore, module), thisPlace);

                // return PhpValue.FromClass(this)
                cg.EmitThis();
                cg.EmitRet(cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.FromClass_Object));

            }, null, diagnostics, false));

            module.SynthesizedManager.AddMethod(this, tophpvalue);
        }

        void EmitPhpCtors(ImmutableArray<MethodSymbol> instancectors, Emit.PEModuleBuilder module, DiagnosticBag diagnostics)
        {
            foreach (SynthesizedPhpCtorSymbol ctor in instancectors)
            {
                module.SetMethodBody(ctor, MethodGenerator.GenerateMethodBody(module, ctor, il =>
                {
                    Debug.Assert(SpecialParameterSymbol.IsContextParameter(ctor.Parameters[0]));

                    var cg = new CodeGenerator(il, module, diagnostics, module.Compilation.Options.OptimizationLevel, false, this, new ParamPlace(ctor.Parameters[0]), new ArgPlace(this, 0));

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

            var __tostring = this.GetMembers(SpecialMethodNames.Tostring.Value, true).OfType<MethodSymbol>().FirstOrDefault();
            if (__tostring != null || this.Syntax.BaseClass == null)    // implement ToString if: there is __tostring() function or ToString is not overriden yet
            {
                // public override string ToString()
                // Note, there might be two ToString methods with same parameters only differing by their return type, CLR allows that
                var tostring = new SynthesizedMethodSymbol(this, "ToString", false, true, DeclaringCompilation.CoreTypes.String, Accessibility.Public, isfinal: false)
                {
                    ExplicitOverride = (MethodSymbol)DeclaringCompilation.GetSpecialTypeMember(SpecialMember.System_Object__ToString),
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
                        if (info.ImplementsInterface && info.Override != null)
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
                            info.Method.ReturnType, info.Method.Parameters, info.Method);
                    }
                    else
                    {
                        // synthesize abstract method implementing unresolved interface member
                        if (info.IsUnresolvedAbstract && info.ImplementsInterface && this.IsAbstract && !this.IsInterface)
                        {
                            var method = info.Method;

                            Debug.Assert(!method.IsStatic);
                            Debug.Assert(method.DeclaredAccessibility != Accessibility.Private);
                            Debug.Assert(method.ContainingType.IsInterface);

                            // Template: abstract function {name}({parameters})
                            var ghost = new SynthesizedMethodSymbol(this, method.RoutineName,
                                isstatic: false, isvirtual: true, isabstract: true, isfinal: false,
                                returnType: method.ReturnType,
                                accessibility: method.DeclaredAccessibility);

                            ghost.SetParameters(method.Parameters.Select(p => SynthesizedParameterSymbol.Create(ghost, p)).ToArray());

                            module.SynthesizedManager.AddMethod(this, ghost);
                        }
                    }
                }

                // setup synthesized methods explicit override as resolved
                if (info.Override is SynthesizedMethodSymbol sm && sm.ExplicitOverride == null && sm.ContainingType == this)
                {
                    sm.ExplicitOverride = info.Method;
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
