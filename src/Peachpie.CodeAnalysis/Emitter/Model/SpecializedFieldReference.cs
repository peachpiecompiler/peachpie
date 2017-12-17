using System.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Pchp.CodeAnalysis.Symbols;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis.Emit
{
    /// <summary>
    /// Represents a reference to a field of a generic type instantiation.
    /// e.g.
    /// A{int}.Field
    /// A{int}.B{string}.C.Field
    /// </summary>
    internal sealed class SpecializedFieldReference : TypeMemberReference, Cci.ISpecializedFieldReference
    {
        private readonly FieldSymbol _underlyingField;

        public SpecializedFieldReference(FieldSymbol underlyingField)
        {
            Debug.Assert((object)underlyingField != null);

            _underlyingField = underlyingField;
        }

        protected override Symbol UnderlyingSymbol
        {
            get
            {
                return _underlyingField;
            }
        }

        public override void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.ISpecializedFieldReference)this);
        }

        Cci.IFieldReference Cci.ISpecializedFieldReference.UnspecializedVersion
        {
            get
            {
                Debug.Assert(_underlyingField.OriginalDefinition.IsDefinition);
                return _underlyingField.OriginalDefinition;
            }
        }

        Cci.ISpecializedFieldReference Cci.IFieldReference.AsSpecializedFieldReference
        {
            get
            {
                return this;
            }
        }

        Cci.ITypeReference Cci.IFieldReference.GetType(EmitContext context)
        {
            var customModifiers = _underlyingField.CustomModifiers;
            var type = ((PEModuleBuilder)context.Module).Translate(_underlyingField.Type, syntaxNodeOpt: /*(CSharpSyntaxNode)context.SyntaxNodeOpt*/null, diagnostics: context.Diagnostics);

            if (customModifiers.Length == 0)
            {
                return type;
            }
            else
            {
                return new Cci.ModifiedTypeReference(type, customModifiers.As<Cci.ICustomModifier>());
            }
        }

        public override Cci.ITypeReference GetContainingType(EmitContext context)
        {
            return base.GetContainingType(context);
        }

        Cci.IFieldDefinition Cci.IFieldReference.GetResolvedField(EmitContext context)
        {
            return null;
        }

        bool Cci.IFieldReference.IsContextualNamedEntity
        {
            get
            {
                return false;
            }
        }
    }
}
