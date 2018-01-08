using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents an anonymous type template's type parameter.
    /// </summary>
    internal sealed class AnonymousTypeParameterSymbol : TypeParameterSymbol
    {
        readonly Symbol _container;
        readonly int _ordinal;
        readonly string _name;

        readonly bool _hasReferenceTypeConstraint;

        public AnonymousTypeParameterSymbol(Symbol container, int ordinal, string name,
            bool hasReferenceTypeConstraint = false)
        {
            Debug.Assert((object)container != null);
            Debug.Assert(!string.IsNullOrEmpty(name));

            _container = container;
            _ordinal = ordinal;
            _name = name;

            _hasReferenceTypeConstraint = hasReferenceTypeConstraint;
        }

        public override TypeParameterKind TypeParameterKind
        {
            get
            {
                return TypeParameterKind.Type;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return ImmutableArray<Location>.Empty; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override int Ordinal
        {
            get { return _ordinal; }
        }

        public override string Name
        {
            get { return _name; }
        }

        public override bool HasConstructorConstraint
        {
            get { return false; }
        }

        public override bool HasReferenceTypeConstraint
        {
            get { return _hasReferenceTypeConstraint; }
        }

        public override bool HasValueTypeConstraint
        {
            get { return false; }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        public override VarianceKind Variance
        {
            get { return VarianceKind.None; }
        }

        internal override void EnsureAllConstraintsAreResolved()
        {
        }

        internal override ImmutableArray<TypeSymbol> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
        {
            return ImmutableArray<TypeSymbol>.Empty;
        }

        public override Symbol ContainingSymbol
        {
            get { return _container; }
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress)
        {
            return null;
        }

        internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress)
        {
            return null;
        }
    }
}