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
    partial class NamedTypeSymbol
    {
        /// <summary>
        /// Gets special <c>_statics</c> nested class holding static fields bound to context.
        /// </summary>
        /// <returns></returns>
        internal TypeSymbol TryGetStatics() => (TypeSymbol)(this as IPhpTypeSymbol)?.StaticsContainer;

        /// <summary>
        /// Emits load of statics holder.
        /// </summary>
        internal TypeSymbol EmitLoadStatics(CodeGenerator cg)
        {
            var statics = TryGetStatics();

            if (statics != null && statics.GetMembers().OfType<IFieldSymbol>().Any())
            {
                // Template: <ctx>.GetStatics<_statics>()
                cg.EmitLoadContext();
                return cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.Context.GetStatic_T.Symbol.Construct(statics))
                    .Expect(statics);
            }

            return null;
        }
    }

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
        }

        void EmitFieldsCctor(Emit.PEModuleBuilder module)
        {
            var sflds = GetMembers().OfType<SourceFieldSymbol>().Where(f => !f.IsConst && f.IsStatic && !f.RequiresHolder).ToList();
            if (sflds.Count != 0)
            {
                // emit initialization of app static fields
                // note, their initializers do not have Context available, since they are not bound to a Context

                var cctor = module.GetStaticCtorBuilder(this);
                var cg = new CodeGenerator(cctor, module, DiagnosticBag.GetInstance(), module.Compilation.Options.OptimizationLevel, false, this, null, null);

                foreach (var f in sflds)
                {
                    f.EmitInit(cg);
                }
            }
        }

        void EmitPhpCallable(Emit.PEModuleBuilder module, DiagnosticBag diagnostics)
        {
            var __invoke = TryGetMagicInvoke();
            if (__invoke == null || __invoke.OverriddenMethod != null)
            {
                return;
            }

            //
            // IPhpCallable.Invoke(Context, PhpVaue[])
            //
            var invoke = new SynthesizedMethodSymbol(this, "IPhpCallable.Invoke", false, true, DeclaringCompilation.CoreTypes.PhpValue, isfinal: false)
            {
                ExplicitOverride = (MethodSymbol)DeclaringCompilation.CoreTypes.IPhpCallable.Symbol.GetMembers("Invoke").Single(),
            };
            invoke.SetParameters(
                new SpecialParameterSymbol(invoke, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, 0),
                new SynthesizedParameterSymbol(invoke, ArrayTypeSymbol.CreateSZArray(ContainingAssembly, DeclaringCompilation.CoreTypes.PhpValue.Symbol), 1, RefKind.None, "arguments"));

            module.SetMethodBody(invoke, MethodGenerator.GenerateMethodBody(module, invoke, il =>
            {
                var cg = new CodeGenerator(il, module, diagnostics, module.Compilation.Options.OptimizationLevel, false, this, new ParamPlace(invoke.Parameters[0]), new ArgPlace(this, 0));

                var argsplace = new ParamPlace(invoke.Parameters[1]);
                var args_element = ((ArrayTypeSymbol)argsplace.TypeOpt).ElementType;
                var ps = __invoke.Parameters;

                // Template: this.__invoke(args[0], args[1], ...)

                cg.EmitThis();

                for (int i = 0; i < ps.Length; i++)
                {
                    var p = ps[i];

                    // LOAD args[i]
                    // Template: (i < args.Length) ? (T)args[i] : default(T)

                    var lbldefault = new NamedLabel("args_default");
                    var lblend = new NamedLabel("args_end");

                    cg.Builder.EmitIntConstant(i);  // <i>
                    argsplace.EmitLoad(cg.Builder); // <args>
                    cg.EmitArrayLength();           // .Length
                    cg.Builder.EmitBranch(ILOpCode.Bge, lbldefault);

                    // (T)args[i]
                    if (p.IsImplicitlyDeclared)
                    {
                        throw new NotImplementedException();
                    }
                    else if (p.Type == cg.CoreTypes.PhpAlias)
                    {
                        // args[i].EnsureAlias()
                        argsplace.EmitLoad(cg.Builder); // <args>
                        cg.Builder.EmitIntConstant(i);  // <i>
                        cg.Builder.EmitOpCode(ILOpCode.Ldelema);    // ref args[i]
                        cg.EmitSymbolToken(args_element, null);
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.EnsureAlias);
                    }
                    else
                    {
                        // (T)args[i]
                        argsplace.EmitLoad(cg.Builder); // <args>
                        cg.Builder.EmitIntConstant(i);  // <i>
                        cg.Builder.EmitOpCode(ILOpCode.Ldelem); // args[i]
                        cg.EmitSymbolToken(args_element, null);
                        cg.EmitConvert(args_element, 0, p.Type);
                    }

                    cg.Builder.EmitBranch(ILOpCode.Br, lblend);

                    // default(T)
                    cg.Builder.MarkLabel(lbldefault);
                    cg.EmitParameterDefaultValue(p);

                    //
                    cg.Builder.MarkLabel(lblend);
                }

                cg.EmitCall(ILOpCode.Callvirt, __invoke);
                cg.EmitRet(invoke.ReturnType);

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
                var cg = new CodeGenerator(il, module, diagnostics, module.Compilation.Options.OptimizationLevel, false, this, new FieldPlace(thisPlace, this.ContextStore), thisPlace);

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
                    cg.EmitPop(cg.EmitThisCall(ctor.BaseCtor, ctor));

                    if (ctor.PhpConstructor == null)
                    {
                        // initialize <ctx> field, if field is declared within this type
                        var ctxField = this.ContextStore;
                        if (ctxField != null && object.ReferenceEquals((object)ctxField.ContainingType, this))
                        {
                            var ctxFieldPlace = new FieldPlace(cg.ThisPlaceOpt, ctxField);

                            // Debug.Assert(<ctx> != null)
                            cg.EmitDebugAssertNotNull(cg.ContextPlaceOpt, "Context cannot be null.");

                            // <this>.<ctx> = <ctx>
                            ctxFieldPlace.EmitStorePrepare(il);
                            cg.EmitLoadContext();
                            ctxFieldPlace.EmitStore(il);
                        }

                        // initialize class fields
                        foreach (var fld in this.EnsureMembers().OfType<SourceFieldSymbol>().Where(fld => !fld.RequiresHolder && !fld.IsStatic && !fld.IsConst))
                        {
                            fld.EmitInit(cg);
                        }
                    }
                    else
                    {
                        Debug.Assert(ctor.BaseCtor.ContainingType == this);

                        // this.__construct
                        cg.EmitPop(cg.EmitThisCall(ctor.PhpConstructor, ctor));
                    }

                    // ret
                    Debug.Assert(ctor.ReturnsVoid);
                    cg.EmitRet(ctor.ReturnType);

                }, null, diagnostics, false));
            }
        }

        void EmitToString(Emit.PEModuleBuilder module)
        {
            if (this.IsInterface)
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
                };

                module.SetMethodBody(tostring, MethodGenerator.GenerateMethodBody(module, tostring, il =>
                {
                    var thisPlace = new ArgPlace(this, 0);
                    var cg = new CodeGenerator(il, module, DiagnosticBag.GetInstance(), module.Compilation.Options.OptimizationLevel, false, this, new FieldPlace(thisPlace, this.ContextStore), thisPlace);

                    if (__tostring != null)
                    {
                        // __tostring().ToString()
                        cg.EmitConvert(cg.EmitThisCall(__tostring, tostring), 0, tostring.ReturnType);
                    }
                    else
                    {
                        // TODO: Throw object_could_not_be_converted
                        // return ""
                        cg.Builder.EmitStringConstant(string.Empty);
                    }
                    cg.EmitRet(tostring.ReturnType);

                }, null, DiagnosticBag.GetInstance(), false));
                module.SynthesizedManager.AddMethod(this, tostring);
            }
        }
    }
}
