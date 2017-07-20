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

namespace Pchp.CodeAnalysis.Symbols
{
    partial class SourceRoutineSymbol
    {
        /// <summary>
        /// Gets place referring to <c>Pchp.Core.Context</c> object.
        /// </summary>
        internal virtual IPlace GetContextPlace()
        {
            var ps = ImplicitParameters;
            if (ps.Count != 0 && SpecialParameterSymbol.IsContextParameter(ps[0]))
            {
                return new ParamPlace(ps[0]);  // <ctx>
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets place of <c>this</c> value in CLR.
        /// This is not necessary the same as PHP <c>$this</c> variable.
        /// </summary>
        internal virtual IPlace GetThisPlace()
        {
            var thisParameter = this.ThisParameter;
            return (thisParameter != null)
                ? new ReadOnlyPlace(new ParamPlace(thisParameter))
                : null;
        }

        /// <summary>
        /// Gets place of PHP <c>$this</c> variable.
        /// </summary>
        public virtual IPlace PhpThisVariablePlace
        {
            get
            {
                var thisPlace = GetThisPlace();
                if (thisPlace != null)
                {
                    if (this.IsGeneratorMethod())
                    {
                        // $this ~ arg1
                        return new ArgPlace(thisPlace.TypeOpt, 1);
                    }
                    else
                    {
                        return thisPlace;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Creates ghost stubs,
        /// i.e. methods with a different signature calling this routine to comply with CLR standards.
        /// </summary>
        internal virtual void SynthesizeStubs(PEModuleBuilder module, DiagnosticBag diagnostic)
        {
            SynthesizeOverloadsWithOptionalParameters(module, diagnostic);
        }

        /// <summary>
        /// Synthesizes method overloads in case there are optional parameters which explicit default value cannot be resolved as a <see cref="ConstantValue"/>.
        /// </summary>
        /// <remarks>
        /// foo($a = [], $b = [1, 2, 3])
        /// + foo() => foo([], [1, 2, 3)
        /// + foo($a) => foo($a, [1, 2, 3])
        /// </remarks>
        protected void SynthesizeOverloadsWithOptionalParameters(PEModuleBuilder module, DiagnosticBag diagnostic)
        {
            var ps = this.Parameters;
            for (int i = 0; i < ps.Length; i++)
            {
                var p = ps[i] as SourceParameterSymbol;
                if (p != null && p.Initializer != null && p.ExplicitDefaultConstantValue == null)   // => ConstantValue couldn't be resolved for optional parameter
                {
                    if (this.ContainingType.IsInterface)
                    {
                        // TODO: we can't build instance method in an interface, generate static extension method ?
                        Debug.WriteLine($"we've lost parameter explicit default value {this.ContainingType.Name}::{this.RoutineName}, parameter ${p.Name}");
                    }
                    else
                    {
                        // create ghost stub foo(p0, .. pi-1) => foo(p0, .. , pN)
                        CreateGhostOverload(module, diagnostic, i);
                    }
                }
            }
        }

        void CreateGhostOverload(PEModuleBuilder module, DiagnosticBag diagnostic, int pcount)
        {
            Debug.Assert(this.Parameters.Length > pcount);
            GhostMethodBuilder.CreateGhostOverload(this, this.ContainingType, module, diagnostic, this.ReturnType, this.Parameters.Take(pcount), null);
        }

        public virtual void Generate(CodeGenerator cg)
        {
            if (!this.IsGeneratorMethod())
            {
                //Proceed with normal method generation
                cg.GenerateScope(this.ControlFlowGraph.Start, int.MaxValue);
            }
            else
            {
                var genSymbol = new SourceGeneratorSymbol(this);
                var il = cg.Builder;

                /* Template:
                 * return BuildGenerator( <ctx>, this, new PhpArray(){ p1, p2, ... }, new GeneratorStateMachineDelegate((IntPtr)<genSymbol>) )
                 */

                cg.EmitLoadContext(); // ctx for generator
                cg.EmitThisOrNull();  // @this for generator

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

                // create generator object via Operators factory method
                cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.BuildGenerator_Context_Object_PhpArray_PhpArray_GeneratorStateMachineDelegate);

                // Convert to return type (Generator or PhpValue, depends on analysis)
                cg.EmitConvert(cg.CoreTypes.Generator, 0, this.ReturnType);
                il.EmitRet(false);

                // Generate SM method. Must be generated after EmitInit of parameters (it sets their _isUnoptimized field).
                CreateStateMachineNextMethod(cg, genSymbol);
            }
        }

        private void InitializeParametersForGeneratorMethod(CodeGenerator cg, Microsoft.CodeAnalysis.CodeGen.ILBuilder il, Microsoft.CodeAnalysis.CodeGen.LocalDefinition generatorsLocals)
        {
            // Emit init of unoptimized BoundParameters using separate CodeGenerator that has locals place pointing to our generator's locals array
            using (var localsArrayCg = new CodeGenerator(
                il, cg.Module, cg.Diagnostics,
                cg.DeclaringCompilation.Options.OptimizationLevel,
                cg.EmitPdbSequencePoints,
                this.ContainingType,
                contextPlace: null,
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
        public override IPlace PhpThisVariablePlace => base.PhpThisVariablePlace;

        internal override void SynthesizeStubs(PEModuleBuilder module, DiagnosticBag diagnostic)
        {
            // <Main>'0
            this.SynthesizeMainMethodWrapper(module, diagnostic);

            //
            base.SynthesizeStubs(module, diagnostic);
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
            }
        }
    }

    partial class SourceMethodSymbol
    {
        internal override IPlace GetContextPlace()
        {
            if (!IsStatic && this.ThisParameter != null)
            {
                // <this>.<ctx> in instance methods
                var t = (SourceTypeSymbol)this.ContainingType;

                var ctx_field = t.ContextStore;
                if (ctx_field != null)  // might be null in interfaces
                {
                    return new FieldPlace(GetThisPlace(), ctx_field);
                }
                else
                {
                    Debug.Assert(t.IsInterface);
                    return null;
                }
            }

            //
            return base.GetContextPlace();
        }

        internal override void SynthesizeStubs(PEModuleBuilder module, DiagnosticBag diagnostic)
        {
            // empty body for static abstract
            if (this.ControlFlowGraph == null && this.IsStatic)
            {
                SynthesizeEmptyBody(module, diagnostic);
            }

            //
            base.SynthesizeStubs(module, diagnostic);
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
        internal void EmitInit(Emit.PEModuleBuilder module)
        {
            var cctor = module.GetStaticCtorBuilder(_file);
            var field = new FieldPlace(null, this.EnsureRoutineInfoField(module));

            // {RoutineInfoField} = RoutineInfo.CreateUserRoutine(name, handle)
            field.EmitStorePrepare(cctor);

            cctor.EmitStringConstant(this.QualifiedName.ToString());
            cctor.EmitLoadToken(module, DiagnosticBag.GetInstance(), this, null);
            cctor.EmitCall(module, DiagnosticBag.GetInstance(), ILOpCode.Call, module.Compilation.CoreMethods.Reflection.CreateUserRoutine_string_RuntimeMethodHandle);

            field.EmitStore(cctor);
        }
    }

    partial class SourceLambdaSymbol
    {
        internal void EmitInit(Emit.PEModuleBuilder module)
        {
            var cctor = module.GetStaticCtorBuilder(_container);
            var field = new FieldPlace(null, this.EnsureRoutineInfoField(module));

            // {RoutineInfoField} = RoutineInfo.CreateUserRoutine(name, handle)
            field.EmitStorePrepare(cctor);

            cctor.EmitStringConstant(this.MetadataName);
            cctor.EmitLoadToken(module, DiagnosticBag.GetInstance(), this, null);
            cctor.EmitCall(module, DiagnosticBag.GetInstance(), ILOpCode.Call, module.Compilation.CoreMethods.Reflection.CreateUserRoutine_string_RuntimeMethodHandle);

            field.EmitStore(cctor);
        }
    }

    partial class SourceGeneratorSymbol
    {
        internal void EmitInit(Emit.PEModuleBuilder module)
        {
            //Don't  need any initial emit
        }
    }
};