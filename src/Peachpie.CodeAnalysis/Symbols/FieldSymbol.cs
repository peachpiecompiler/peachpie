using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Semantics;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Pchp.CodeAnalysis.FlowAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a field in a class, struct or enum
    /// </summary>
    internal abstract partial class FieldSymbol : Symbol, IFieldSymbol, IPhpValue
    {
        internal FieldSymbol()
        {
        }

        /// <summary>
        /// Optional. Gets the initializer.
        /// </summary>
        public virtual BoundExpression Initializer => null;

        /// <summary>
        /// Value indicating the field has [NotNull] metadata.
        /// </summary>
        public virtual bool HasNotNull => false;

        /// <summary>
        /// The original definition of this symbol. If this symbol is constructed from another
        /// symbol by type substitution then OriginalDefinition gets the original symbol as it was defined in
        /// source or metadata.
        /// </summary>
        public new virtual FieldSymbol OriginalDefinition => this;

        protected override sealed Symbol OriginalSymbolDefinition => this.OriginalDefinition;

        /// <summary>
        /// Gets the type of this field.
        /// </summary>
        public TypeSymbol Type
        {
            get
            {
                return GetFieldType(ConsList<FieldSymbol>.Empty);
            }
        }

        internal abstract TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound);

        /// <summary>
        /// Gets the list of custom modifiers, if any, associated with the field.
        /// </summary>
        public abstract ImmutableArray<CustomModifier> CustomModifiers { get; }

        /// <summary>
        /// If this field serves as a backing variable for an automatically generated
        /// property or a field-like event, returns that 
        /// property/event. Otherwise returns null.
        /// Note, the set of possible associated symbols might be expanded in the future to 
        /// reflect changes in the languages.
        /// </summary>
        public abstract Symbol AssociatedSymbol { get; }

        /// <summary>
        /// Returns true if this field was declared as "readonly". 
        /// </summary>
        public abstract bool IsReadOnly { get; }

        /// <summary>
        /// Returns true if this field was declared as "volatile". 
        /// </summary>
        public abstract bool IsVolatile { get; }

        /// <summary>
        /// Returns true if this field was declared as "fixed".
        /// Note that for a fixed-size buffer declaration, this.Type will be a pointer type, of which
        /// the pointed-to type will be the declared element type of the fixed-size buffer.
        /// </summary>
        public virtual bool IsFixed { get { return false; } }

        /// <summary>
        /// If IsFixed is true, the value between brackets in the fixed-size-buffer declaration.
        /// If IsFixed is false FixedSize is 0.
        /// Note that for fixed-a size buffer declaration, this.Type will be a pointer type, of which
        /// the pointed-to type will be the declared element type of the fixed-size buffer.
        /// </summary>
        public virtual int FixedSize { get { return 0; } }

        /// <summary>
        /// If this.IsFixed is true, returns the underlying implementation type for the
        /// fixed-size buffer when emitted.  Otherwise returns null.
        /// </summary>
        internal virtual NamedTypeSymbol FixedImplementationType(PEModuleBuilder emitModule)
        {
            return null;
        }

        /// <summary>
        /// Returns true when field is a backing field for a captured frame pointer (typically "this").
        /// </summary>
        internal virtual bool IsCapturedFrame { get { return false; } }

        /// <summary>
        /// Returns true if this field was declared as "const" (i.e. is a constant declaration).
        /// Also returns true for an enum member.
        /// </summary>
        public abstract bool IsConst { get; }

        // Gets a value indicating whether this instance is metadata constant. A constant field is considered to be 
        // metadata constant unless they are of type decimal, because decimals are not regarded as constant by the CLR.
        public bool IsMetadataConstant
        {
            get
            {
                var isconst = this.IsConst && (this.Type.SpecialType != SpecialType.System_Decimal);
                Debug.Assert(!isconst || IsStatic, "Literal field must be Static.");
                return isconst;
            }
        }

        /// <summary>
        /// Returns false if the field wasn't declared as "const", or constant value was omitted or erroneous.
        /// True otherwise.
        /// </summary>
        public virtual bool HasConstantValue
        {
            get
            {
                if (!IsConst)
                {
                    return false;
                }

                ConstantValue constantValue = GetConstantValue(earlyDecodingWellKnownAttributes: false);
                return constantValue != null && !constantValue.IsBad; //can be null in error scenarios
            }
        }

        /// <summary>
        /// If IsConst returns true, then returns the constant value of the field or enum member. If IsConst returns
        /// false, then returns null.
        /// </summary>
        public virtual object ConstantValue
        {
            get
            {
                if (!IsConst)
                {
                    return null;
                }

                ConstantValue constantValue = GetConstantValue(earlyDecodingWellKnownAttributes: false);
                return constantValue == null ? null : constantValue.Value; //can be null in error scenarios
            }
        }

        internal abstract ConstantValue GetConstantValue(bool earlyDecodingWellKnownAttributes);

        /// <summary>
        /// Gets the kind of this symbol.
        /// </summary>
        public sealed override SymbolKind Kind
        {
            get
            {
                return SymbolKind.Field;
            }
        }

        /// <summary>
        /// If this field represents a tuple element, returns a corresponding default element
        ///  field. Otherwise returns null.
        /// </summary>
        public virtual IFieldSymbol CorrespondingTupleField => null;

        /// <summary>
        /// Returns false because field can't be abstract.
        /// </summary>
        public sealed override bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns false because field can't be defined externally.
        /// </summary>
        public sealed override bool IsExtern
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns false because field can't be overridden.
        /// </summary>
        public sealed override bool IsOverride
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns false because field can't be sealed.
        /// </summary>
        public sealed override bool IsSealed
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns false because field can't be virtual.
        /// </summary>
        public sealed override bool IsVirtual
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// True if this symbol has a special name (metadata flag SpecialName is set).
        /// </summary>
        internal abstract bool HasSpecialName { get; }

        /// <summary>
        /// True if this symbol has a runtime-special name (metadata flag RuntimeSpecialName is set).
        /// </summary>
        internal abstract bool HasRuntimeSpecialName { get; }

        /// <summary>
        /// True if this field is not serialized (metadata flag NotSerialized is set).
        /// </summary>
        internal abstract bool IsNotSerialized { get; }

        /// <summary>
        /// Describes how the field is marshalled when passed to native code.
        /// Null if no specific marshalling information is available for the field.
        /// </summary>
        /// <remarks>PE symbols don't provide this information and always return null.</remarks>
        internal abstract MarshalPseudoCustomAttributeData MarshallingInformation { get; }

        /// <summary>
        /// Returns the marshalling type of this field, or 0 if marshalling information isn't available.
        /// </summary>
        /// <remarks>
        /// By default this information is extracted from <see cref="MarshallingInformation"/> if available. 
        /// Since the compiler does only need to know the marshalling type of symbols that aren't emitted 
        /// PE symbols just decode the type from metadata and don't provide full marshalling information.
        /// </remarks>
        internal virtual UnmanagedType MarshallingType
        {
            get
            {
                var info = MarshallingInformation;
                return info != null ? info.UnmanagedType : 0;
            }
        }

        /// <summary>
        /// Offset assigned to the field when the containing type is laid out by the VM.
        /// Null if unspecified.
        /// </summary>
        internal abstract int? TypeLayoutOffset { get; }

        internal FieldSymbol AsMember(NamedTypeSymbol newOwner)
        {
            Debug.Assert(this.IsDefinition);
            Debug.Assert(ReferenceEquals(newOwner.OriginalDefinition, this.ContainingSymbol.OriginalDefinition));
            return (newOwner == this.ContainingSymbol) ? this : new SubstitutedFieldSymbol(newOwner as SubstitutedNamedTypeSymbol, this);
        }

        #region IFieldSymbol Members

        ISymbol IFieldSymbol.AssociatedSymbol
        {
            get
            {
                return this.AssociatedSymbol;
            }
        }

        ITypeSymbol IFieldSymbol.Type
        {
            get
            {
                return this.Type;
            }
        }

        ImmutableArray<CustomModifier> IFieldSymbol.CustomModifiers
        {
            get { return this.CustomModifiers; }
        }

        IFieldSymbol IFieldSymbol.OriginalDefinition
        {
            get { return this.OriginalDefinition; }
        }

        #endregion

        #region ISymbol Members

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitField(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitField(this);
        }

        #endregion
    }
}
