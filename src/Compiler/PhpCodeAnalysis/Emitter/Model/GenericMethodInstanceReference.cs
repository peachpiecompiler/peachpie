using System.Collections.Generic;
using Microsoft.CodeAnalysis.Emit;
using Cci = Microsoft.Cci;
using Pchp.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.Emit
{
    /// <summary>
    /// Represents a reference to a generic method instantiation, closed over type parameters,
    /// e.g. MyNamespace.Class.Method{T}()
    /// </summary>
    internal sealed class GenericMethodInstanceReference : MethodReference, Cci.IGenericMethodInstanceReference
    {
        public GenericMethodInstanceReference(MethodSymbol underlyingMethod)
            : base(underlyingMethod)
        {
        }

        public override void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.IGenericMethodInstanceReference)this);
        }

        IEnumerable<Cci.ITypeReference> Cci.IGenericMethodInstanceReference.GetGenericArguments(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            foreach (var arg in UnderlyingMethod.TypeArguments)
            {
                yield return moduleBeingBuilt.Translate(arg, syntaxNodeOpt: context.SyntaxNodeOpt, diagnostics: context.Diagnostics);
            }
        }

        Cci.IMethodReference Cci.IGenericMethodInstanceReference.GetGenericMethod(EmitContext context)
        {
            // NoPia method might come through here.
            return ((PEModuleBuilder)context.Module).Translate(
                UnderlyingMethod.OriginalDefinition,
                syntaxNodeOpt: context.SyntaxNodeOpt,
                diagnostics: context.Diagnostics,
                needDeclaration: true);
        }

        public override Cci.IGenericMethodInstanceReference AsGenericMethodInstanceReference
        {
            get
            {
                return this;
            }
        }
    }
}
