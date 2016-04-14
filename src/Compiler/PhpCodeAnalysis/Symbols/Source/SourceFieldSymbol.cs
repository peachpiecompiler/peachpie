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
using Pchp.CodeAnalysis.FlowAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    internal class SourceFieldSymbol : FieldSymbol//, IAttributeTargetSymbol
    {
        readonly SourceNamedTypeSymbol _type;
        readonly Syntax.AST.FieldDecl _syntax;
        readonly PhpMemberAttributes _modifiers;
        readonly PHPDocBlock _phpdoc;

        public SourceFieldSymbol(SourceNamedTypeSymbol type, Syntax.AST.FieldDecl syntax, PhpMemberAttributes modifiers, PHPDocBlock phpdoc)
        {
            Contract.ThrowIfNull(type);
            Contract.ThrowIfNull(syntax);

            _type = type;
            _syntax = syntax;
            _modifiers = modifiers;
            _phpdoc = phpdoc;
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
            var vartag = _phpdoc.GetElement<PHPDocBlock.VarTag>();
            if (vartag != null && vartag.TypeNamesArray.Length != 0)
            {
                var typectx = TypeRefFactory.CreateTypeRefContext(_type.Syntax);
                var tmask = PHPDoc.GetTypeMask(typectx, vartag.TypeNamesArray);
                var t = DeclaringCompilation.GetTypeFromTypeRef(typectx, tmask);
                return t;
            }

            // TODO: analysed PHP type

            return DeclaringCompilation.CoreTypes.PhpValue;
        }

        public override TypeRefMask GetResultType(TypeRefContext ctx)
        {
            return base.GetResultType(ctx); // convert typemask from CLR type
        }
    }
}
