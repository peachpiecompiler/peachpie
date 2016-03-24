using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    internal class SynthesizedCtorSymbol : MethodSymbol
    {
        readonly SourceNamedTypeSymbol _type;

        public SynthesizedCtorSymbol(SourceNamedTypeSymbol/*!*/type)
        {
            Contract.ThrowIfNull(type);
            _type = type;
        }

        public override bool HidesBaseMethodsByName => false;

        internal override bool HasSpecialName => true;

        public override Symbol ContainingSymbol => _type;

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsAbstract => false;

        public override bool IsExtern => false;

        public override bool IsOverride => false;

        public override bool IsSealed => false;

        public override bool IsStatic => false;

        public override bool IsVirtual => false;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override MethodKind MethodKind => MethodKind.Constructor;

        public override string Name => WellKnownMemberNames.InstanceConstructorName;

        public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;    // TODO: (Context)

        public override bool ReturnsVoid => true;

        public override TypeSymbol ReturnType => _type.DeclaringCompilation.CoreTypes.Void;

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => false;
    }
}
