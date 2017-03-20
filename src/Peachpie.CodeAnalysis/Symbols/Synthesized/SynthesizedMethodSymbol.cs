using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        readonly bool _static, _virtual, _final;
        readonly string _name;
        TypeSymbol _return;
        readonly Accessibility _accessibility;
        protected ImmutableArray<ParameterSymbol> _parameters;

        internal MethodSymbol ExplicitOverride { get; set; }

        public override IMethodSymbol OverriddenMethod => ExplicitOverride;

        public SynthesizedMethodSymbol(TypeSymbol containingType, string name, bool isstatic, bool isvirtual, TypeSymbol returnType, Accessibility accessibility = Accessibility.Private, bool isfinal = true, params ParameterSymbol[] ps)
        {
            _type = containingType;
            _name = name;
            _static = isstatic;
            _virtual = isvirtual;
            _return = returnType;
            _accessibility = accessibility;
            _final = isfinal && isvirtual && !isstatic;

            SetParameters(ps);
        }

        internal void SetParameters(params ParameterSymbol[] ps)
        {
            _parameters = ps.AsImmutable();
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

        public override bool IsAbstract => false;

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
        
        public override bool ReturnsVoid => _return.SpecialType == SpecialType.System_Void;

        public override TypeSymbol ReturnType => _return;

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
