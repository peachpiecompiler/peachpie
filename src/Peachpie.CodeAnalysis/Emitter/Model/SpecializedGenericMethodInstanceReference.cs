using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Symbols;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis.Emit
{
    /// <summary>
    /// Represents a generic method of a generic type instantiation, closed over type parameters.
    /// e.g. 
    /// A{T}.M{S}()
    /// A.B{T}.C.M{S}()
    /// </summary>
    internal sealed class SpecializedGenericMethodInstanceReference : SpecializedMethodReference, Cci.IGenericMethodInstanceReference
    {
        private readonly SpecializedMethodReference _genericMethod;

        public SpecializedGenericMethodInstanceReference(MethodSymbol underlyingMethod)
            : base(underlyingMethod)
        {
            Debug.Assert(PEModuleBuilder.IsGenericType(underlyingMethod.ContainingType) && underlyingMethod.ContainingType.IsDefinition);
            _genericMethod = new SpecializedMethodReference(underlyingMethod);
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
            return _genericMethod;
        }

        public override Cci.IGenericMethodInstanceReference AsGenericMethodInstanceReference
        {
            get
            {
                return this;
            }
        }

        public override void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.IGenericMethodInstanceReference)this);
        }
    }
}
