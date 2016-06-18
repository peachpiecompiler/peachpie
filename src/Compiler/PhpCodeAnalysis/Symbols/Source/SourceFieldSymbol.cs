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
using Pchp.CodeAnalysis.Semantics;

namespace Pchp.CodeAnalysis.Symbols
{
    internal partial class SourceFieldSymbol : FieldSymbol//, IAttributeTargetSymbol
    {
        readonly SourceNamedTypeSymbol _type;
        readonly string _name;
        readonly PhpMemberAttributes _modifiers;
        readonly PHPDocBlock _phpdoc;
        readonly BoundExpression _initializerOpt;

        public BoundExpression Initializer => _initializerOpt;

        public SourceFieldSymbol(SourceNamedTypeSymbol type, string name, PhpMemberAttributes modifiers, PHPDocBlock phpdoc, BoundExpression initializer = null)
        {
            Contract.ThrowIfNull(type);
            Contract.ThrowIfNull(name);

            _type = type;
            _name = name;
            _modifiers = modifiers;
            _phpdoc = phpdoc;
            _initializerOpt = initializer;
        }

        public override string Name => _name;

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
            var vartag = _phpdoc?.GetElement<PHPDocBlock.VarTag>();
            if (vartag != null && vartag.TypeNamesArray.Length != 0)
            {
                var typectx = TypeRefFactory.CreateTypeRefContext(_type);
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

    internal class SourceConstSymbol : SourceFieldSymbol
    {
        readonly Syntax.AST.Expression _value;
        ConstantValue _resolvedValue;

        public SourceConstSymbol(SourceNamedTypeSymbol type, string name, PHPDocBlock phpdoc, Syntax.AST.Expression value)
            : base(type, name, PhpMemberAttributes.Public | PhpMemberAttributes.Static, phpdoc)
        {
            _value = value;
        }

        public override bool IsConst => true;

        public override bool IsReadOnly => false;

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            var cvalue = GetConstantValue(false);
            if (cvalue.IsNull)
                return this.DeclaringCompilation.GetSpecialType(SpecialType.System_Object);

            return this.DeclaringCompilation.GetSpecialType(cvalue.SpecialType);
        }

        internal override ConstantValue GetConstantValue(bool earlyDecodingWellKnownAttributes)
        {
            return _resolvedValue ?? (_resolvedValue = SemanticsBinder.TryGetConstantValue(this.DeclaringCompilation, _value));
        }
    }

    internal class SourceRuntimeConstantSymbol : SourceFieldSymbol
    {
        public SourceRuntimeConstantSymbol(SourceNamedTypeSymbol type, string name, PHPDocBlock phpdoc, BoundExpression initializer = null)
            : base(type, name, PhpMemberAttributes.Public, phpdoc, initializer)
        {

        }

        public override bool IsReadOnly => true;
    }
}
