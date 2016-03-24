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
        protected readonly SourceNamedTypeSymbol _type;

        ImmutableArray<ParameterSymbol> _lazyParameters;

        public SynthesizedCtorSymbol(SourceNamedTypeSymbol/*!*/type)
        {
            Contract.ThrowIfNull(type);
            _type = type;
        }

        public sealed override Symbol ContainingSymbol => _type;

        public sealed override INamedTypeSymbol ContainingType => _type;

        public override bool HidesBaseMethodsByName => false;

        internal override bool HasSpecialName => true;

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public sealed override bool IsAbstract => false;

        public sealed override bool IsExtern => false;

        public sealed override bool IsOverride => false;

        public sealed override bool IsSealed => false;

        public override bool IsStatic => false;

        public sealed override bool IsVirtual => false;

        public sealed override MethodKind MethodKind => MethodKind.Constructor;

        public sealed override string Name => IsStatic ? WellKnownMemberNames.StaticConstructorName : WellKnownMemberNames.InstanceConstructorName;

        public sealed override bool ReturnsVoid => true;

        public sealed override TypeSymbol ReturnType => _type.DeclaringCompilation.CoreTypes.Void;

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => false;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_lazyParameters.IsDefault)
                {
                    var ps = new List<ParameterSymbol>(1);

                    // Context <ctx>
                    ps.Add(new SpecialParameterSymbol(this, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, ps.Count));

                    //
                    _lazyParameters = ps.AsImmutable();
                }

                return _lazyParameters;
            }
        }
    }
}
