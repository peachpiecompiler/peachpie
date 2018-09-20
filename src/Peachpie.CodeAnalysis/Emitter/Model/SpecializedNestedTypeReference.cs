using Microsoft.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Symbols;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis.Emit
{
    /// <summary>
    /// Represents a reference to a type nested in an instantiation of a generic type.
    /// e.g. 
    /// A{int}.B
    /// A.B{int}.C.D
    /// </summary>
    internal class SpecializedNestedTypeReference : NamedTypeReference, Cci.ISpecializedNestedTypeReference
    {
        public SpecializedNestedTypeReference(NamedTypeSymbol underlyingNamedType)
            : base(underlyingNamedType)
        {
        }

        Cci.INestedTypeReference Cci.ISpecializedNestedTypeReference.GetUnspecializedVersion(EmitContext context)
        {
            System.Diagnostics.Debug.Assert(UnderlyingNamedType.OriginalDefinition.IsDefinition);
            return this.UnderlyingNamedType.OriginalDefinition;
        }

        public override void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.ISpecializedNestedTypeReference)this);
        }

        Cci.ITypeReference Cci.ITypeMemberReference.GetContainingType(EmitContext context)
        {
            return ((PEModuleBuilder)context.Module).Translate(UnderlyingNamedType.ContainingType, context.SyntaxNodeOpt, context.Diagnostics);
        }

        public override Cci.IGenericTypeInstanceReference AsGenericTypeInstanceReference
        {
            get { return null; }
        }

        public override Cci.INamespaceTypeReference AsNamespaceTypeReference
        {
            get { return null; }
        }

        public override Cci.INestedTypeReference AsNestedTypeReference
        {
            get { return this; }
        }

        public override Cci.ISpecializedNestedTypeReference AsSpecializedNestedTypeReference
        {
            get { return this; }
        }
    }
}
