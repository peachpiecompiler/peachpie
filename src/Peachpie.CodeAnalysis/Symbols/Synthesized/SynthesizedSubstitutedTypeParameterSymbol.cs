namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// A type parameter for a synthesized class or method.
    /// </summary>
    internal sealed class SynthesizedSubstitutedTypeParameterSymbol : SubstitutedTypeParameterSymbol
    {
        public SynthesizedSubstitutedTypeParameterSymbol(Symbol owner, TypeMap map, TypeParameterSymbol substitutedFrom)
            : base(owner, map, substitutedFrom)
        {
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }
    }
}
