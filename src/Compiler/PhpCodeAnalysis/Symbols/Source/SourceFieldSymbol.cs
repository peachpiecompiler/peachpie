using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;
using Pchp.Syntax;

namespace Pchp.CodeAnalysis.Symbols
{
    internal class SourceFieldSymbol : FieldSymbol//, IAttributeTargetSymbol
    {
        readonly SourceNamedTypeSymbol _type;
        readonly Syntax.AST.FieldDecl _syntax;
        readonly PhpMemberAttributes _modifiers;

        public SourceFieldSymbol(SourceNamedTypeSymbol type, Syntax.AST.FieldDecl syntax, PhpMemberAttributes modifiers)
        {
            Contract.ThrowIfNull(type);
            Contract.ThrowIfNull(syntax);

            _type = type;
            _syntax = syntax;
            _modifiers = modifiers;
        }

        public override string Name => _syntax.Name.Value;

        public override Symbol AssociatedSymbol => null;

        public override Symbol ContainingSymbol => _type;

        internal override PhpCompilation DeclaringCompilation => _type.DeclaringCompilation;

        public override ImmutableArray<CustomModifier> CustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public override Accessibility DeclaredAccessibility => _modifiers.GetAccessibility();

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsConst => false;

        public override bool IsReadOnly => false;

        public override bool IsStatic => _modifiers.IsStatic();

        public override bool IsVolatile => false;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override bool HasRuntimeSpecialName => false;

        internal override bool HasSpecialName => false;

        internal override bool IsNotSerialized => false;

        internal override MarshalPseudoCustomAttributeData MarshallingInformation => null;

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        internal override int? TypeLayoutOffset => null;

        internal override ConstantValue GetConstantValue(bool earlyDecodingWellKnownAttributes)
        {
            throw new ArgumentException();
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return (TypeSymbol)DeclaringCompilation.GetSpecialType(SpecialType.System_Object);  // TODO: analysed PHP type
        }
    }
}
