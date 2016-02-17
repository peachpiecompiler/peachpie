using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.Syntax.AST;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a PHP class method.
    /// </summary>
    internal sealed class SourceMethodSymbol : MethodSymbol
    {
        readonly SourceNamedTypeSymbol _type;
        readonly MethodDecl/*!*/_syntax;
        readonly ImmutableArray<ParameterSymbol> _params;

        public SourceMethodSymbol(SourceNamedTypeSymbol/*!*/type, MethodDecl/*!*/syntax)
        {
            Contract.ThrowIfNull(type);
            Contract.ThrowIfNull(syntax);

            _type = type;
            _syntax = syntax;
            _params = BuildParameters().ToImmutableArray();
        }

        IEnumerable<ParameterSymbol> BuildParameters()
        {
            int index = 0;

            foreach (var p in _syntax.Signature.FormalParams)
            {
                yield return new SourceParameterSymbol(this, p, index++);
            }
        }

        public override string Name => _syntax.Name.Value;

        public override Symbol ContainingSymbol => _type;

        public override Accessibility DeclaredAccessibility => _syntax.Modifiers.GetAccessibility();

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsAbstract => _syntax.Modifiers.IsAbstract();

        public override bool IsExtern => false;

        public override bool IsOverride
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsSealed => _syntax.Modifiers.IsSealed();

        public override bool IsStatic => _syntax.Modifiers.IsStatic();

        public override bool IsVirtual => !IsSealed && !_type.IsSealed && !IsStatic;

        public override SymbolKind Kind => SymbolKind.Method;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override MethodKind MethodKind
        {
            get
            {
                // TODO: ctor, dtor, props, magic, ...

                return MethodKind.Ordinary;
            }
        }

        public override ImmutableArray<IParameterSymbol> Parameters => StaticCast<IParameterSymbol>.From(_params);

        public override bool ReturnsVoid
        {
            get
            {
                return false; // throw new NotImplementedException();
            }
        }

        public override ITypeSymbol ReturnType
        {
            get
            {
                return this.DeclaringCompilation.GetSpecialType(SpecialType.System_Object);
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;   // TODO: from PHPDoc

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => false;
    }
}
