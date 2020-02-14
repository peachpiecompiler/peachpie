using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Symbols;
using Pchp.CodeAnalysis.CodeGen;
using System.Diagnostics;
using System.Collections.Immutable;

namespace Pchp.CodeAnalysis.CodeGen
{
    internal static class GhostMethodBuilder
    {
        public static MethodSymbol CreateGhostOverload(MethodSymbol original, PEModuleBuilder module, DiagnosticBag diagnostic, int pcount)
        {
            Debug.Assert(original.Parameters.Length > pcount);
            return GhostMethodBuilder.CreateGhostOverload(
                original, original.ContainingType, module, diagnostic,
                ghostreturn: original.ReturnType, ghostparams: ImmutableArray.Create(original.Parameters, 0, pcount), explicitOverride: null);
        }

        /// <summary>
        /// Creates ghost stub that calls method.
        /// </summary>
        public static MethodSymbol CreateGhostOverload(this MethodSymbol method, NamedTypeSymbol containingtype, PEModuleBuilder module, DiagnosticBag diagnostic,
            TypeSymbol ghostreturn, ImmutableArray<ParameterSymbol> ghostparams,
            bool phphidden = false,
            MethodSymbol explicitOverride = null)
        {
            //string prefix = null;
            string name = explicitOverride?.MetadataName ?? method.MetadataName;

            //if (explicitOverride != null && explicitOverride.ContainingType.IsInterface)
            //{
            //    // it is nice to generate an explicit override that is not publically visible
            //    // TODO: this causes issues when we already generated abstract override or if there are more abstract/interface methods that will be implemented by this ghost
            //    prefix = explicitOverride.ContainingType.GetFullName() + ".";   // explicit interface override
            //}

            var ghost = new SynthesizedMethodSymbol(
                containingtype, /*prefix +*/ name, method.IsStatic, explicitOverride != null, ghostreturn, method.DeclaredAccessibility, isfinal: false, phphidden: phphidden)
            {
                ExplicitOverride = explicitOverride,
                ForwardedCall = method,
            };

            ghost.SetParameters(SynthesizedParameterSymbol.Create(ghost, ghostparams));

            // save method symbol to module
            module.SynthesizedManager.AddMethod(containingtype, ghost);

            // generate method body
            GenerateGhostBody(module, diagnostic, method, ghost);

            //
            return ghost;
        }

        /// <summary>
        /// Generates ghost method body that calls <c>this</c> method.
        /// </summary>
        static void GenerateGhostBody(PEModuleBuilder module, DiagnosticBag diagnostic, MethodSymbol method, SynthesizedMethodSymbol ghost)
        {
            var containingtype = ghost.ContainingType;

            var body = MethodGenerator.GenerateMethodBody(module, ghost,
                (il) =>
                {
                    // $this
                    var thisPlace = ghost.HasThis ? new ArgPlace(containingtype, 0) : null;

                    // Context
                    var ctxPlace = thisPlace != null && ghost.ContainingType is SourceTypeSymbol sourcetype
                        ? (sourcetype.ContextStore != null ? new FieldPlace(thisPlace, sourcetype.ContextStore, module) : null)
                        : (IPlace)new ArgPlace(module.Compilation.CoreTypes.Context, 0);

                    // .callvirt
                    bool callvirt = ghost.ExplicitOverride != null && ghost.ExplicitOverride.ContainingType.IsInterface;  // implementing interface, otherwise we should be able to call specific method impl. non-virtually via ghost

                    var cg = new CodeGenerator(il, module, diagnostic, module.Compilation.Options.OptimizationLevel, false, containingtype, ctxPlace, thisPlace)
                    {
                        DebugRoutine = ghost,
                    };

                    // return (T){routine}(p0, ..., pN);
                    cg.EmitConvert(cg.EmitForwardCall(method, ghost, callvirt: callvirt), 0, ghost.ReturnType);
                    cg.EmitRet(ghost.ReturnType);
                },
                null, diagnostic, false);

            module.SetMethodBody(ghost, body);
        }
    }
}
