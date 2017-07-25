using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Symbols;
using Pchp.CodeAnalysis.CodeGen;

namespace Pchp.CodeAnalysis.CodeGen
{
    internal static class GhostMethodBuilder
    {
        /// <summary>
        /// Creates ghost stub that calls method.
        /// </summary>
        public static void CreateGhostOverload(this MethodSymbol method, NamedTypeSymbol containingtype, PEModuleBuilder module, DiagnosticBag diagnostic,
            TypeSymbol ghostreturn, IEnumerable<ParameterSymbol> ghostparams,
            MethodSymbol explicitOverride = null)
        {
            string prefix = (explicitOverride != null && explicitOverride.ContainingType.IsInterface)
                ? (explicitOverride.ContainingType.GetFullName() + ".")   // explicit interface override
                : null;

            var ghost = new SynthesizedMethodSymbol(
                containingtype, prefix + method.Name, method.IsStatic, explicitOverride != null, ghostreturn, method.DeclaredAccessibility)
            {
                ExplicitOverride = explicitOverride,
            };

            ghost.SetParameters(ghostparams.Select(p =>
                new SynthesizedParameterSymbol(ghost, p.Type, p.Ordinal, p.RefKind, p.Name, p.IsParams)).ToArray());

            // save method symbol to module
            module.SynthesizedManager.AddMethod(containingtype, ghost);

            // generate method body
            GenerateGhostBody(module, diagnostic, method, ghost);
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
                        ? new FieldPlace(thisPlace, sourcetype.ContextStore)
                        : (IPlace)new ArgPlace(module.Compilation.CoreTypes.Context, 0);

                    var cg = new CodeGenerator(il, module, diagnostic, module.Compilation.Options.OptimizationLevel, false, containingtype, ctxPlace, thisPlace);

                    // return (T){routine}(p0, ..., pN);
                    cg.EmitConvert(cg.EmitForwardCall(method, ghost), 0, ghost.ReturnType);
                    cg.EmitRet(ghost.ReturnType);
                },
                null, diagnostic, false);

            module.SetMethodBody(ghost, body);
        }
    }
}
