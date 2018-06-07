using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    internal sealed class ConstructedMethodSymbol : SubstitutedMethodSymbol
    {
        private readonly ImmutableArray<TypeSymbol> _typeArguments;

        internal ConstructedMethodSymbol(MethodSymbol constructedFrom, ImmutableArray<TypeSymbol> typeArguments)
            : base(containingSymbol: constructedFrom.ContainingType,
                   map: new TypeMap(constructedFrom.ContainingType, ((MethodSymbol)constructedFrom.OriginalDefinition).TypeParameters, typeArguments.SelectAsArray(TypeMap.TypeSymbolAsTypeWithModifiers)),
                   originalDefinition: (MethodSymbol)constructedFrom.OriginalDefinition,
                   constructedFrom: constructedFrom)
        {
            _typeArguments = typeArguments;
        }

        public override ImmutableArray<TypeSymbol> TypeArguments
        {
            get
            {
                return _typeArguments;
            }
        }
    }
}
