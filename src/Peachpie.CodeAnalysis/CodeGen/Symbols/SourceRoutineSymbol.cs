using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.CodeAnalysis.Emit;
using System.Reflection.Metadata;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;
using System.Collections.Immutable;

namespace Pchp.CodeAnalysis.Symbols
{
    partial class SourceRoutineSymbol
    {
        /// <summary>
        /// Gets place referring to <c>Pchp.Core.Context</c> object.
        /// </summary>
        internal virtual IPlace GetContextPlace(PEModuleBuilder module)
        {
            var ps = ImplicitParameters;
            if (ps.Length != 0 && SpecialParameterSymbol.IsContextParameter(ps[0]))
            {
                return new ParamPlace(ps[0]);  // <ctx>
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets place of <c>this</c> parameter in CLR corresponding to <c>current class instance</c>.
        /// </summary>
        internal virtual IPlace GetThisPlace()
        {
            return this.HasThis
                ? new ArgPlace(ContainingType, 0)
                : null;
        }

        internal virtual IPlace GetPhpThisVariablePlaceWithoutGenerator(PEModuleBuilder module = null)
        {
            var thisPlace = GetThisPlace();
            if (thisPlace != null)
            {
                if (this.ContainingType.IsTraitType())
                {
                    // $this ~ this.<>this
                    return new FieldPlace(thisPlace, ((SourceTraitTypeSymbol)this.ContainingType).RealThisField, module);
                }
            }

            //
            return thisPlace;
        }

        /// <summary>
        /// Gets place of PHP <c>$this</c> variable.
        /// </summary>
        public IPlace GetPhpThisVariablePlace(PEModuleBuilder module = null)
        {
            var thisPlace = GetPhpThisVariablePlaceWithoutGenerator(module);

            if (this.IsGeneratorMethod())
            {
                // $this ~ arg1
                return thisPlace != null
                    ? new ArgPlace(thisPlace.Type, 1)
                    : null;
            }

            return thisPlace;
        }

        /// <summary>
        /// Creates ghost stubs,
        /// i.e. methods with a different signature calling this routine to comply with CLR standards.
        /// </summary>
        /// <returns>List of additional overloads.</returns>
        internal virtual IList<MethodSymbol> SynthesizeStubs(PEModuleBuilder module, DiagnosticBag diagnostic)
        {
            //
            EmitParametersDefaultValue(module, diagnostic);

            // TODO: resolve this already in SourceTypeSymbol.GetMembers(), now it does not get overloaded properly
            return SynthesizeOverloadsWithOptionalParameters(module, diagnostic);
        }

        /// <summary>
        /// Synthesizes method overloads in case there are optional parameters which explicit default value cannot be resolved as a <see cref="ConstantValue"/>.
        /// </summary>
        /// <remarks>
        /// foo($a = [], $b = [1, 2, 3])
        /// + foo() => foo([], [1, 2, 3)
        /// + foo($a) => foo($a, [1, 2, 3])
        /// </remarks>
        protected IList<MethodSymbol> SynthesizeOverloadsWithOptionalParameters(PEModuleBuilder module, DiagnosticBag diagnostic)
        {
            List<MethodSymbol> list = null;

            var implparams = ImplicitParameters;
            var srcparams = SourceParameters;
            var implicitVarArgs = VarargsParam;

            for (int i = 0; i <= srcparams.Length; i++) // how many to be copied from {srcparams}
            {
                var isfake = /*srcparams[i - 1].IsFake*/ implicitVarArgs != null && i > 0 && srcparams[i - 1].Ordinal >= implicitVarArgs.Ordinal; // parameter was replaced with [params]
                var hasdefault = false; // i < srcparams.Length && srcparams[i].HasUnmappedDefaultValue();  // ConstantValue couldn't be resolved for optional parameter

                if (isfake || hasdefault)
                {
                    if (this.ContainingType.IsInterface)
                    {
                        // we can't build instance method in an interface
                        // CONSIDER: generate static extension method ?
                    }
                    else
                    {
                        // ghostparams := [...implparams, ...srcparams{0..i-1}]
                        var ghostparams = ImmutableArray.CreateBuilder<ParameterSymbol>(implparams.Length + i);
                        ghostparams.AddRange(implparams);
                        ghostparams.AddRange(srcparams, i);

                        // create ghost stub foo(p0, .. pi-1) => foo(p0, .. , pN)
                        var ghost = GhostMethodBuilder.CreateGhostOverload(
                            this, ContainingType, module, diagnostic,
                            ReturnType, ghostparams.MoveToImmutable());

                        //
                        if (list == null)
                        {
                            list = new List<MethodSymbol>();
                        }

                        list.Add(ghost);
                    }
                }
            }

            return list ?? (IList<MethodSymbol>)Array.Empty<MethodSymbol>();
        }

        /// <summary>
        /// Emits initializers of all parameter's non-standard default values (such as PhpArray)
        /// within the type's static .cctor
        /// </summary>
        private void EmitParametersDefaultValue(PEModuleBuilder module, DiagnosticBag diagnostics)
        {
            foreach (var p in this.SourceParameters)
            {
                var field = p.DefaultValueField;
                if (field is SynthesizedFieldSymbol)
                {
                    Debug.Assert(p.Initializer != null);

                    module.SynthesizedManager.AddField(field.ContainingType, field);

                    // .cctor() {

                    var cctor = module.GetStaticCtorBuilder(field.ContainingType);
                    lock (cctor)
                    {
                        SynthesizedMethodSymbol func = null;

                        if (field.Type.Is_Func_Context_PhpValue())  // Func<Context, PhpValue>
                        {
                            // private static PhpValue func(Context) => INITIALIZER()
                            func = new SynthesizedMethodSymbol(field.ContainingType, field.Name + "Func", isstatic: true, isvirtual: false, DeclaringCompilation.CoreTypes.PhpValue, isfinal: true);
                            func.SetParameters(new SynthesizedParameterSymbol(func, DeclaringCompilation.CoreTypes.Context, 0, RefKind.None, name: SpecialParameterSymbol.ContextName));

                            //
                            module.SetMethodBody(func, MethodGenerator.GenerateMethodBody(module, func, il =>
                            {
                                var ctxPlace = new ArgPlace(DeclaringCompilation.CoreTypes.Context, 0);
                                var cg = new CodeGenerator(il, module, diagnostics, module.Compilation.Options.OptimizationLevel, false, field.ContainingType, ctxPlace, null)
                                {
                                    CallerType = ContainingType,
                                    ContainingFile = ContainingFile,
                                    IsInCachedArrayExpression = true,   // do not cache array initializers twice
                                };

                                // return {Initializer}
                                cg.EmitConvert(p.Initializer, func.ReturnType);
                                cg.EmitRet(func.ReturnType);

                            }, null, diagnostics, false));

                            module.SynthesizedManager.AddMethod(func.ContainingType, func);
                        }

                        using (var cg = new CodeGenerator(cctor, module, diagnostics, module.Compilation.Options.OptimizationLevel, false, field.ContainingType,
                                contextPlace: null,
                                thisPlace: null)
                        {
                            CallerType = ContainingType,
                            ContainingFile = ContainingFile,
                            IsInCachedArrayExpression = true,   // do not cache array initializers twice
                        })
                        {
                            var fldplace = new FieldPlace(null, field, module);
                            fldplace.EmitStorePrepare(cg.Builder);
                            if (func == null)
                            {
                                // {field} = {Initializer};
                                cg.EmitConvert(p.Initializer, field.Type);
                            }
                            else
                            {
                                MethodSymbol funcsymbol = func;

                                // bind func in case it is generic
                                if (func.ContainingType is SourceTraitTypeSymbol st)
                                {
                                    funcsymbol = func.AsMember(st.Construct(st.TypeArguments));
                                }

                                // Func<,>(object @object, IntPtr method)
                                var func_ctor = ((NamedTypeSymbol)field.Type).InstanceConstructors.Single(m =>
                                    m.ParameterCount == 2 &&
                                    m.Parameters[0].Type.SpecialType == SpecialType.System_Object &&
                                    m.Parameters[1].Type.SpecialType == SpecialType.System_IntPtr
                                );

                                // {field} = new Func<Context, PhpValue>( {func} )
                                cg.Builder.EmitNullConstant(); // null
                                cg.EmitOpCode(ILOpCode.Ldftn); // method
                                cg.EmitSymbolToken(funcsymbol, null);
                                cg.EmitCall(ILOpCode.Newobj, func_ctor);
                            }
                            fldplace.EmitStore(cg.Builder);
                        }
                    }
                }
            }
        }

        public virtual void Generate(CodeGenerator cg)
        {
            if (this.IsGeneratorMethod())
            {
                // generate method as a state machine (SM) in a separate synthesized method
                // this routine returns instance of new SM:
                GenerateGeneratorMethod(cg);
            }
            else
            {
                // normal method generation:
                cg.GenerateScope(this.ControlFlowGraph.Start, int.MaxValue);
            }
        }

        private void GenerateGeneratorMethod(CodeGenerator cg)
        {
            Debug.Assert(this.IsGeneratorMethod());

            var genSymbol = new SourceGeneratorSymbol(this);
            //var genConstructed = (genSymbol.ContainingType is SourceTraitTypeSymbol st)
            //    ? genSymbol.AsMember(st.Construct(st.TypeArguments))
            //    : genSymbol;

            var il = cg.Builder;
            var lambda = this as SourceLambdaSymbol;

            /* Template:
             * return BuildGenerator( <ctx>, new PhpArray(){ p1, p2, ... }, new GeneratorStateMachineDelegate((IntPtr)<genSymbol>), (RuntimeMethodHandle)this )
             */

            cg.EmitLoadContext(); // ctx for generator

            // new PhpArray for generator's locals
            cg.EmitCall(ILOpCode.Newobj, cg.CoreMethods.Ctors.PhpArray);

            var generatorsLocals = cg.GetTemporaryLocal(cg.CoreTypes.PhpArray);
            cg.Builder.EmitLocalStore(generatorsLocals);

            // initialize parameters (set their _isOptimized and copy them to locals array)
            InitializeParametersForGeneratorMethod(cg, il, generatorsLocals);
            cg.Builder.EmitLoad(generatorsLocals);
            cg.ReturnTemporaryLocal(generatorsLocals);

            // new PhpArray for generator's synthesizedLocals
            cg.EmitCall(ILOpCode.Newobj, cg.CoreMethods.Ctors.PhpArray);

            // new GeneratorStateMachineDelegate(<genSymbol>) delegate for generator
            cg.Builder.EmitNullConstant(); // null
            cg.EmitOpCode(ILOpCode.Ldftn); // method
            cg.EmitSymbolToken(genSymbol, null);

            cg.EmitCall(ILOpCode.Newobj, cg.CoreTypes.GeneratorStateMachineDelegate.Ctor(cg.CoreTypes.Object, cg.CoreTypes.IntPtr)); // GeneratorStateMachineDelegate(object @object, IntPtr method)

            // handleof(this)
            cg.EmitLoadToken(this, null);

            // create generator object via Operators factory method
            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.BuildGenerator_Context_PhpArray_PhpArray_GeneratorStateMachineDelegate_RuntimeMethodHandle);

            // .SetGeneratorThis( object ) : Generator
            if (!this.IsStatic || (lambda != null && lambda.UseThis))
            {
                GetPhpThisVariablePlaceWithoutGenerator(cg.Module).EmitLoad(cg.Builder);
                cg.EmitCastClass(cg.DeclaringCompilation.GetSpecialType(SpecialType.System_Object));
                cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.SetGeneratorThis_Generator_Object)
                    .Expect(cg.CoreTypes.Generator);
            }

            // .SetGeneratorLazyStatic( PhpTypeInfo ) : Generator
            if ((this.Flags & RoutineFlags.UsesLateStatic) != 0 && this.IsStatic)
            {
                cg.EmitLoadStaticPhpTypeInfo();
                cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.SetGeneratorLazyStatic_Generator_PhpTypeInfo)
                    .Expect(cg.CoreTypes.Generator);
            }

            // .SetGeneratorDynamicScope( scope ) : Generator
            if (lambda != null)
            {
                lambda.GetCallerTypePlace().EmitLoad(cg.Builder); // RuntimeTypeContext
                cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.SetGeneratorDynamicScope_Generator_RuntimeTypeHandle)
                    .Expect(cg.CoreTypes.Generator);
            }

            // Convert to return type (Generator or PhpValue, depends on analysis)
            cg.EmitConvert(cg.CoreTypes.Generator, 0, this.ReturnType);
            il.EmitRet(false);

            // Generate SM method. Must be generated after EmitInit of parameters (it sets their _isUnoptimized field).
            CreateStateMachineNextMethod(cg, genSymbol);
        }

        private void InitializeParametersForGeneratorMethod(CodeGenerator cg, Microsoft.CodeAnalysis.CodeGen.ILBuilder il, Microsoft.CodeAnalysis.CodeGen.LocalDefinition generatorsLocals)
        {
            // Emit init of unoptimized BoundParameters using separate CodeGenerator that has locals place pointing to our generator's locals array
            using (var localsArrayCg = new CodeGenerator(
                il, cg.Module, cg.Diagnostics,
                cg.DeclaringCompilation.Options.OptimizationLevel,
                cg.EmitPdbSequencePoints,
                this.ContainingType,
                contextPlace: cg.ContextPlaceOpt,
                thisPlace: null,
                routine: this, // needed to support static variables (they need enclosing routine while binding)
                locals: new LocalPlace(generatorsLocals),
                localsInitialized: false
                    ))
            {
                // EmitInit (for UnoptimizedLocals) copies arguments to locals array, does nothing for normal variables, handles local statics, global variables ...
                LocalsTable.Variables.ForEach(v => v.EmitInit(localsArrayCg));
            }
        }

        private void CreateStateMachineNextMethod(CodeGenerator cg, SourceGeneratorSymbol genSymbol)
        {
            cg.Module.SynthesizedManager.AddMethod(ContainingType, genSymbol); // save method symbol to module

            // generate generator's next method body
            var genMethodBody = MethodGenerator.GenerateMethodBody(cg.Module, genSymbol, (_il) =>
            {
                GenerateStateMachinesNextMethod(cg, _il, genSymbol);
            }
            , null, cg.Diagnostics, cg.EmitPdbSequencePoints);

            cg.Module.SetMethodBody(genSymbol, genMethodBody);
        }

        //Initialized a new CodeGenerator for generation of SourceGeneratorSymbol (state machine's next method)
        private void GenerateStateMachinesNextMethod(CodeGenerator cg, Microsoft.CodeAnalysis.CodeGen.ILBuilder _il, SourceGeneratorSymbol genSymbol)
        {
            // TODO: get correct ThisPlace, ReturnType etc. resolution & binding out of the box without GN_SGS hacks
            // using SourceGeneratorSymbol

            //Refactor parameters references to proper fields
            using (var stateMachineNextCg = new CodeGenerator(
                _il, cg.Module, cg.Diagnostics,
                cg.DeclaringCompilation.Options.OptimizationLevel,
                cg.EmitPdbSequencePoints,
                this.ContainingType,
                contextPlace: new ParamPlace(genSymbol.ContextParameter),
                thisPlace: new ParamPlace(genSymbol.ThisParameter),
                routine: this,
                locals: new ParamPlace(genSymbol.LocalsParameter),
                localsInitialized: true,
                tempLocals: new ParamPlace(genSymbol.TmpLocalsParameter)
                    )
            {
                GeneratorStateMachineMethod = genSymbol,    // Pass SourceGeneratorSymbol to CG for additional yield and StartBlock emit 
            })
            {
                stateMachineNextCg.GenerateScope(this.ControlFlowGraph.Start, int.MaxValue);
            }
        }
    }

    partial class SourceGlobalMethodSymbol
    {
        /// <summary>
        /// Real main method with <c>MainDelegate</c> signature.
        /// The method is generated lazily in order to provide method compatible with MainDelegate.
        /// <see cref="SourceGlobalMethodSymbol"/> may have (usually have) a different return type.
        /// </summary>
        internal SynthesizedMethodSymbol _mainMethod0;

        internal override IPlace GetThisPlace() => new ParamPlace(ThisParameter);

        internal override IList<MethodSymbol> SynthesizeStubs(PEModuleBuilder module, DiagnosticBag diagnostic)
        {
            //base.SynthesizeStubs(module, diagnostic);          // ghosts (always empty)

            // <Main>'0
            this.SynthesizeMainMethodWrapper(module, diagnostic);

            // no overloads synthesized for global code
            return Array.Empty<MethodSymbol>();
        }

        /// <summary>
        /// Main method wrapper in case it does not return PhpValue.
        /// </summary>
        void SynthesizeMainMethodWrapper(PEModuleBuilder module, DiagnosticBag diagnostics)
        {
            if (this.ReturnType != DeclaringCompilation.CoreTypes.PhpValue)
            {
                // PhpValue <Main>`0(parameters)
                var wrapper = new SynthesizedMethodSymbol(
                    this.ContainingFile, WellKnownPchpNames.GlobalRoutineName + "`0", true, false,
                    DeclaringCompilation.CoreTypes.PhpValue, Accessibility.Public);

                wrapper.SetParameters(this.Parameters.Select(p =>
                    new SynthesizedParameterSymbol(wrapper, p.Type, p.Ordinal, p.RefKind, p.Name)).ToArray());

                // save method symbol to module
                module.SynthesizedManager.AddMethod(this.ContainingFile, wrapper);

                // generate method body
                module.CreateMainMethodWrapper(wrapper, this, diagnostics);

                //
                _mainMethod0 = wrapper;
            }
        }
    }

    partial class SourceMethodSymbol
    {
        internal override IPlace GetContextPlace(PEModuleBuilder module)
        {
            var thisplace = GetThisPlace();
            if (thisplace != null)
            {
                // <this>.<ctx> in instance methods
                var t = (SourceTypeSymbol)this.ContainingType;

                var ctx_field = t.ContextStore;
                if (ctx_field != null)  // might be null in interfaces
                {
                    return new FieldPlace(thisplace, ctx_field, module);
                }
                else
                {
                    Debug.Assert(t.IsInterface);
                    return null;
                }
            }

            //
            return base.GetContextPlace(module);
        }

        internal override IList<MethodSymbol> SynthesizeStubs(PEModuleBuilder module, DiagnosticBag diagnostic)
        {
            // empty body for static abstract
            if (this.ControlFlowGraph == null && this.IsStatic)
            {
                SynthesizeEmptyBody(module, diagnostic);
            }

            //
            return base.SynthesizeStubs(module, diagnostic);
        }

        void SynthesizeEmptyBody(PEModuleBuilder module, DiagnosticBag diagnostic)
        {
            Debug.Assert(this.ControlFlowGraph == null);
            Debug.Assert(this.IsAbstract == false);

            module.SetMethodBody(this, MethodGenerator.GenerateMethodBody(module, this, (il) =>
            {
                var cg = new CodeGenerator(this, il, module, diagnostic, module.Compilation.Options.OptimizationLevel, false);

                // Template: return default(T)
                cg.EmitRetDefault();
            }, null, diagnostic, false));
        }
    }

    partial class SourceFunctionSymbol
    {
        internal override IList<MethodSymbol> SynthesizeStubs(PEModuleBuilder module, DiagnosticBag diagnostic)
        {
            var overloads = base.SynthesizeStubs(module, diagnostic);

            // synthesize RoutineInfo:
            var cctor = module.GetStaticCtorBuilder(_file);
            lock (cctor)
            {
                using (var cg = new CodeGenerator(
                        cctor, module, diagnostic, PhpOptimizationLevel.Release, false, this.ContainingType,
                        contextPlace: null, thisPlace: null, routine: this))
                {
                    var field = new FieldPlace(null, this.EnsureRoutineInfoField(module), module);

                    // {RoutineInfoField} = RoutineInfo.CreateUserRoutine(name, handle, overloads[])
                    field.EmitStorePrepare(cctor);

                    cctor.EmitStringConstant(this.QualifiedName.ToString());
                    cctor.EmitLoadToken(module, DiagnosticBag.GetInstance(), this, null);
                    cg.Emit_NewArray(cg.CoreTypes.RuntimeMethodHandle, overloads.AsImmutable(), m => cg.EmitLoadToken(m, null));

                    cctor.EmitCall(module, DiagnosticBag.GetInstance(), ILOpCode.Call, cg.CoreMethods.Reflection.CreateUserRoutine_string_RuntimeMethodHandle_RuntimeMethodHandleArr);

                    field.EmitStore(cctor);
                }
            }

            //
            return overloads;
        }
    }

    partial class SourceLambdaSymbol
    {
        internal override IPlace GetContextPlace(PEModuleBuilder module)
        {
            // Template: Operators.Context(<closure>)
            return new OperatorPlace(DeclaringCompilation.CoreMethods.Operators.Context_Closure, new ParamPlace(ClosureParameter));
        }

        internal override IPlace GetThisPlace()
        {
            if (UseThis)
            {
                // Template: Operators.Context(<closure>)
                return new OperatorPlace(DeclaringCompilation.CoreMethods.Operators.This_Closure, new ParamPlace(ClosureParameter));
            }

            return base.GetThisPlace();
        }

        internal override IPlace GetPhpThisVariablePlaceWithoutGenerator(PEModuleBuilder module = null)
        {
            return GetThisPlace();
        }

        internal IPlace GetCallerTypePlace()
        {
            // Template: Operators.Scope(<closure>)
            return new OperatorPlace(DeclaringCompilation.CoreMethods.Operators.Scope_Closure, new ParamPlace(ClosureParameter));
        }

        internal override IList<MethodSymbol> SynthesizeStubs(PEModuleBuilder module, DiagnosticBag diagnostic)
        {
            var overloads = base.SynthesizeStubs(module, diagnostic);

            // synthesize RoutineInfo:
            var cctor = module.GetStaticCtorBuilder(_container);
            lock (cctor)
            {
                var field = new FieldPlace(null, this.EnsureRoutineInfoField(module), module);

                var ct = module.Compilation.CoreTypes;

                // {RoutineInfoField} = new PhpAnonymousRoutineInfo(name, handle)
                field.EmitStorePrepare(cctor);

                cctor.EmitStringConstant(this.MetadataName);
                cctor.EmitLoadToken(module, DiagnosticBag.GetInstance(), this, null);
                cctor.EmitCall(module, DiagnosticBag.GetInstance(), ILOpCode.Call, ct.Operators.Method("AnonymousRoutine", ct.String, ct.RuntimeMethodHandle));

                field.EmitStore(cctor);
            }

            //
            return overloads;
        }
    }

    partial class SourceGeneratorSymbol
    {
        internal void EmitInit(PEModuleBuilder module)
        {
            // Don't need any initial emit
        }
    }
};