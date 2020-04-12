using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Pchp.CodeAnalysis.Symbols
{
    class SynthesizedFieldSymbol : FieldSymbol
    {
        [Flags]
        enum PackedFlags : byte
        {
            //
            // 000|r|s|aaa
            //

            /// <summary>
            /// See <see cref="Accessibility"/>.
            /// </summary>
            AccessibilityMask = 0x7, // aaa

            IsStaticBit = 1 << 3,    // s
            IsReadOnlyBit = 1 << 4,  // r
        }

        readonly NamedTypeSymbol _containing;
        TypeSymbol _type;
        readonly string _name;
        readonly PackedFlags _flags;
        readonly ConstantValue _const;

        public SynthesizedFieldSymbol(
            NamedTypeSymbol containing,
            TypeSymbol type,
            string name,
            Accessibility accessibility,
            bool isStatic = false,
            bool isReadOnly = false)
        {
            _containing = containing;
            _name = name;
            _type = type;

            //

            _flags =
                ((PackedFlags)accessibility) |
                (isStatic ? PackedFlags.IsStaticBit : 0) |
                (isReadOnly ? PackedFlags.IsReadOnlyBit : 0)
                ;

            Debug.Assert(DeclaredAccessibility == accessibility);
        }

        public SynthesizedFieldSymbol(
            NamedTypeSymbol containing,
            TypeSymbol type,
            string name,
            Accessibility accessibility,
            ConstantValue constant)
            : this(containing, type, name, accessibility, isStatic: true, isReadOnly: false)
        {
            _const = constant;
        }

        public override Symbol AssociatedSymbol => null;

        public override Symbol ContainingSymbol => _containing;

        public override ImmutableArray<CustomModifier> CustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public override Accessibility DeclaredAccessibility => (Accessibility)(_flags & PackedFlags.AccessibilityMask);

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override bool IsConst => _const != null;

        public override bool IsReadOnly => (_flags & PackedFlags.IsReadOnlyBit) != 0;   // .initonly

        public override bool IsStatic => (_flags & PackedFlags.IsStaticBit) != 0;

        public override bool IsVolatile => false;

        public override bool IsImplicitlyDeclared => true;

        public override string Name => _name;

        internal override bool HasRuntimeSpecialName => false;

        internal override bool HasSpecialName => false;

        internal override bool IsNotSerialized => false;

        internal override MarshalPseudoCustomAttributeData MarshallingInformation => null;

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        internal override int? TypeLayoutOffset => null;

        internal override ConstantValue GetConstantValue(bool earlyDecodingWellKnownAttributes) => _const;

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound) => _type;

        internal void SetFieldType(TypeSymbol type)
        {
            _type = type;
        }
    }
}
