using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Cci;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    internal class SynthesizedPropertySymbol : PropertySymbol
    {
        readonly NamedTypeSymbol _containing;
        readonly Accessibility _accessibility;
        readonly MethodSymbol _setMethod, _getMethod;
        readonly string _name;
        readonly bool _isStatic;
        readonly TypeSymbol _type;

        public SynthesizedPropertySymbol(NamedTypeSymbol containing, string name, bool isStatic, TypeSymbol type, Accessibility accessibility, MethodSymbol getter, MethodSymbol setter)
        {
            _containing = containing;
            _name = name;
            _accessibility = accessibility;
            _setMethod = setter;
            _getMethod = getter;
            _type = type;
            _isStatic = isStatic;
        }

        public override Symbol ContainingSymbol => _type;

        public override Accessibility DeclaredAccessibility => _accessibility;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations
        {
            get
            {
                return ImmutableArray<PropertySymbol>.Empty;
            }
        }

        public override MethodSymbol GetMethod => _getMethod;

        public override MethodSymbol SetMethod => _setMethod;

        public override bool IsAbstract => false;

        public override bool IsExtern => false;

        public override bool IsIndexer => false;

        public override bool IsOverride => false;

        public override bool IsSealed => false;

        public override bool IsStatic => _isStatic;

        public override bool IsVirtual => !IsStatic && (!IsSealed || IsOverride);

        public override string Name => _name;

        public override ImmutableArray<ParameterSymbol> Parameters { get; } = ImmutableArray<ParameterSymbol>.Empty;

        public override TypeSymbol Type => _type;

        public override ImmutableArray<CustomModifier> TypeCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        internal override CallingConvention CallingConvention => CallingConvention.Default;

        internal override bool HasSpecialName => false;

        internal override bool MustCallMethodsDirectly => false;

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;
    }
}
