using Microsoft.CodeAnalysis.Text;
using Pchp.CodeAnalysis.Symbols;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis.Emit
{
    /// <summary>
    /// Represents a method of a generic type instantiation.
    /// e.g. 
    /// A{int}.M()
    /// A.B{int}.C.M()
    /// </summary>
    internal class SpecializedMethodReference : MethodReference, Cci.ISpecializedMethodReference
    {
        public SpecializedMethodReference(MethodSymbol underlyingMethod)
            : base(underlyingMethod)
        {
        }

        public override void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.ISpecializedMethodReference)this);
        }

        Cci.IMethodReference Cci.ISpecializedMethodReference.UnspecializedVersion
        {
            get
            {
                return UnderlyingMethod.OriginalDefinition;
            }
        }

        public override Cci.ISpecializedMethodReference AsSpecializedMethodReference
        {
            get
            {
                return this;
            }
        }
    }
}
