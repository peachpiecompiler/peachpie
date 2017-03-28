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

        internal virtual IPlace GetThisPlace()
        {
            var thisParameter = this.ThisParameter;
            return (thisParameter != null)
                ? new ReadOnlyPlace(new ParamPlace(thisParameter))
                : null;
        }

        /// <summary>
        /// Creates ghost stubs,
        /// i.e. methods with a different signature calling this routine to comply with CLR standards.
        /// </summary>
        internal virtual void SynthesizeGhostStubs(PEModuleBuilder module, DiagnosticBag diagnostic)
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

            CreateGhostOverload(module, diagnostic, this.ReturnType, this.Parameters.Take(pcount), null);
        }

        /// <summary>
        /// Creates ghost stub that calls current method.
        /// </summary>
        protected void CreateGhostOverload(PEModuleBuilder module, DiagnosticBag diagnostic,
            TypeSymbol ghostreturn, IEnumerable<ParameterSymbol> ghostparams,
            MethodSymbol explicitOverride = null)
        {
            var ghost = new SynthesizedMethodSymbol(
                this.ContainingType, this.Name, this.IsStatic, explicitOverride != null, ghostreturn, this.DeclaredAccessibility)
            {
                ExplicitOverride = explicitOverride,
            };

            ghost.SetParameters(ghostparams.Select(p =>
                new SynthesizedParameterSymbol(ghost, p.Type, p.Ordinal, p.RefKind, p.Name)).ToArray());

            // save method symbol to module
            module.SynthesizedManager.AddMethod(this.ContainingType, ghost);

            // generate method body
            GenerateGhostBody(module, diagnostic, ghost);
        }

        /// <summary>
        /// Generates ghost method body that calls <c>this</c> method.
        /// </summary>
        protected void GenerateGhostBody(PEModuleBuilder module, DiagnosticBag diagnostic, SynthesizedMethodSymbol ghost)
        {
            var body = MethodGenerator.GenerateMethodBody(module, ghost,
                (il) =>
                {
                    var cg = new CodeGenerator(il, module, diagnostic, module.Compilation.Options.OptimizationLevel, false, this.ContainingType, this.GetContextPlace(), this.GetThisPlace());

                    // return (T){routine}(p0, ..., pN);
                    cg.EmitConvert(cg.EmitThisCall(this, ghost), 0, ghost.ReturnType);
                    cg.EmitRet(ghost.ReturnType);
                },
                null, diagnostic, false);

            module.SetMethodBody(ghost, body);
        }
    }

    partial class SourceGlobalMethodSymbol
    {
        internal override void SynthesizeGhostStubs(PEModuleBuilder module, DiagnosticBag diagnostic)
        {
            // <Main>'0
            this.SynthesizeMainMethodWrapper(module, diagnostic);

            //
            base.SynthesizeGhostStubs(module, diagnostic);
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

        internal override void SynthesizeGhostStubs(PEModuleBuilder module, DiagnosticBag diagnostic)
        {
            base.SynthesizeGhostStubs(module, diagnostic);

            // not matching override has to have a ghost stub
            var overriden = (MethodSymbol)this.OverriddenMethod; // OverrideHelper.ResolveOverride(this);
            if (overriden != null && !this.SignaturesMatch(overriden))
            {
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

                if (string.Equals(overriden.RoutineName, "ToString", StringComparison.OrdinalIgnoreCase) &&
                    this.ContainingType.GetMembers(Devsense.PHP.Syntax.Name.SpecialMethodNames.Tostring.Value, true).OfType<MethodSymbol>().Any())
                {
                    /* Exception: __toString() already implements System.Object.ToString(),
                     * do not create ghost override for ToString then.
                     */
                    return;
                }

                CreateGhostOverload(module, diagnostic, overriden.ReturnType, overriden.Parameters, overriden);
            }

            // empty body for static abstract
            if (this.ControlFlowGraph == null && this.IsStatic)
            {
                SynthesizeEmptyBody(module, diagnostic);
            }
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
};