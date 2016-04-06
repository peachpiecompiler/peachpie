using Microsoft.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Symbols;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis.Emit
{
    /// <summary>
    /// Represents a reference to a generic type instantiation that is nested in a non-generic type.
    /// e.g. A.B{int}
    /// </summary>
    internal sealed class GenericNestedTypeInstanceReference : GenericTypeInstanceReference, Cci.INestedTypeReference
    {
        public GenericNestedTypeInstanceReference(NamedTypeSymbol underlyingNamedType)
            : base(underlyingNamedType)
        {
        }

        Cci.ITypeReference Cci.ITypeMemberReference.GetContainingType(EmitContext context)
        {
            return ((PEModuleBuilder)context.Module).Translate(UnderlyingNamedType.ContainingType, syntaxNodeOpt: context.SyntaxNodeOpt, diagnostics: context.Diagnostics);
        }

        public override Cci.IGenericTypeInstanceReference AsGenericTypeInstanceReference
        {
            get { return this; }
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
            get { return null; }
        }
    }
}
