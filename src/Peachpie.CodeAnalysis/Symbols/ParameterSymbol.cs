using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Pchp.CodeAnalysis.Semantics;
using Peachpie.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.Symbols
{
    internal abstract partial class ParameterSymbol : Symbol, IParameterSymbol, IPhpValue
    {
        public virtual ImmutableArray<CustomModifier> CustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public virtual ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public override SymbolKind Kind => SymbolKind.Parameter;

        /// <summary>
        /// Optional. Gets the initializer.
        /// </summary>
        public virtual BoundExpression Initializer
        {
            get
            {
                // MAYBE: if (DefaultValueField != null) BoundFieldRef.CreateStaticField(DefaultValueField)
                var cvalue = ExplicitDefaultConstantValue;
                return cvalue != null ? new BoundLiteral(cvalue.Value) : null;
            }
        }

        /// <summary>
        /// In case there is a default value that cannot be represented by <see cref="ConstantValue"/>,
        /// this gets a static readonly field containing the value.
        /// </summary>
        /// <remarks>
        /// In PHP it is possible to set parameter's default value which cannot be represented using <see cref="ConstantValue"/>.
        /// In such case, the value is set to this runtime field and read if needed.
        /// </remarks>
        public virtual FieldSymbol DefaultValueField => null;

        public virtual bool IsOptional => false;

        public virtual bool IsParams => false;

        public virtual bool IsThis => false;

        public override bool IsStatic => false;

        public override bool IsVirtual => false;

        public override bool IsOverride => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => true;

        public override bool IsExtern => false;

        public virtual bool HasNotNull => false;

        public virtual bool IsPhpRw => false;

        /// <summary>
        /// Gets the ordinal position of the parameter. The first parameter has ordinal zero.
        /// The "'this' parameter has ordinal -1.
        /// </summary>
        public abstract int Ordinal { get; }

        public virtual RefKind RefKind => RefKind.None;

        ITypeSymbol IParameterSymbol.Type => Type;

        internal virtual TypeSymbol Type
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// The original definition of this symbol. If this symbol is constructed from another
        /// symbol by type substitution then OriginalDefinition gets the original symbol as it was defined in
        /// source or metadata.
        /// </summary>
        protected override Symbol OriginalSymbolDefinition => this;

        IParameterSymbol IParameterSymbol.OriginalDefinition => (IParameterSymbol)OriginalSymbolDefinition;

        /// <summary>
        /// Get this accessibility that was declared on this symbol. For symbols that do not have
        /// accessibility declared on them, returns NotApplicable.
        /// </summary>
        public override Accessibility DeclaredAccessibility => Accessibility.NotApplicable;

        /// <summary>
        /// Returns the default value constant of the parameter, 
        /// or null if the parameter doesn't have a default value or 
        /// the parameter type is a struct and the default value of the parameter
        /// is the default value of the struct type or of type parameter type which is 
        /// not known to be a referenced type.
        /// </summary>
        /// <remarks>
        /// This is used for emitting.  It does not reflect the language semantics
        /// (i.e. even non-optional parameters can have default values).
        /// </remarks>
        internal abstract ConstantValue ExplicitDefaultConstantValue { get; }

        /// <summary>
        /// Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        /// This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        /// </summary>
        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        internal virtual ImportValueAttributeData ImportValueAttributeData => default;

        /// <summary>
        /// Helper method that checks whether this parameter can be passed to anothers method parameter.
        /// </summary>
        internal bool CanBePassedTo(ParameterSymbol another)
        {
            return another != null && this.Type.IsEqualToOrDerivedFrom(another.Type);
        }
    }
}
