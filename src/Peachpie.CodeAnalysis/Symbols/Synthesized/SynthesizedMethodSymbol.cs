using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;

namespace Pchp.CodeAnalysis.Symbols
{
    class SynthesizedMethodSymbol : MethodSymbol
    {
        readonly TypeSymbol _type;
        readonly bool _static, _virtual, _final, _abstract, _phphidden;
        readonly string _name;
        TypeSymbol _return;
        readonly Accessibility _accessibility;
        protected ImmutableArray<ParameterSymbol> _parameters;

        /// <summary>
        /// Optional.
        /// Gats actual method that will be called by this one.
        /// For informational purposes.
        /// </summary>
        public MethodSymbol ForwardedCall { get; set; }

        internal MethodSymbol ExplicitOverride { get; set; }

        public override IMethodSymbol OverriddenMethod => ExplicitOverride;

        public SynthesizedMethodSymbol(TypeSymbol containingType, string name, bool isstatic, bool isvirtual, TypeSymbol returnType, Accessibility accessibility = Accessibility.Private, bool isfinal = true, bool isabstract = false, bool phphidden = false, params ParameterSymbol[] ps)
        {
            _type = containingType;
            _name = name;
            _static = isstatic;
            _virtual = isvirtual && !isstatic;
            _abstract = isvirtual && isabstract && !isfinal;
            _return = returnType;
            _accessibility = accessibility;
            _final = isfinal && isvirtual && !isstatic;
            _phphidden = phphidden;

            SetParameters(ps);
        }

        internal void SetParameters(params ParameterSymbol[] ps)
        {
            _parameters = ps.AsImmutable();
        }

        public override ImmutableArray<AttributeData> GetAttributes()
        {
            if (_phphidden)
            {
                return ImmutableArray.Create<AttributeData>(
                    // [PhpHiddenAttribute]
                    new SynthesizedAttributeData(
                        DeclaringCompilation.CoreTypes.PhpHiddenAttribute.Ctor().Symbol,
                        ImmutableArray<TypedConstant>.Empty,
                        ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty));
            }

            return ImmutableArray<AttributeData>.Empty;
        }

        public override Symbol ContainingSymbol => _type;

        internal override IModuleSymbol ContainingModule => _type.ContainingModule;

        public override Accessibility DeclaredAccessibility => _accessibility;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsAbstract => _abstract;

        public override bool IsExtern => false;

        public override bool IsOverride => OverriddenMethod != null;

        public override bool IsSealed => _final;

        public override bool IsStatic => _static;

        public override bool IsVirtual => _virtual;

        public override bool IsImplicitlyDeclared => true;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations =>
            IsExplicitInterfaceImplementation ? ImmutableArray.Create(ExplicitOverride) : ImmutableArray<MethodSymbol>.Empty;

        internal override bool IsExplicitInterfaceImplementation => ExplicitOverride != null && ExplicitOverride.ContainingType.IsInterface;

        public override MethodKind MethodKind => MethodKind.Ordinary;

        public override string Name => _name;

        public override ImmutableArray<ParameterSymbol> Parameters => _parameters;

        public override bool ReturnsVoid => ReturnType.SpecialType == SpecialType.System_Void;

        public override RefKind RefKind => RefKind.None;

        public override TypeSymbol ReturnType => _return ?? ForwardedCall?.ReturnType ?? throw new InvalidOperationException();

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        public override bool HidesBaseMethodsByName => !IsExplicitInterfaceImplementation && true;

        /// <summary>
        /// virtual = IsVirtual AND NewSlot 
        /// override = IsVirtual AND !NewSlot
        /// </summary>
        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => IsVirtual && !IsOverride;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => IsVirtual;
    }
}
