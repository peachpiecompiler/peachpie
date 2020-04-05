using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Globalization;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using Pchp.CodeAnalysis.CodeGen;

namespace Pchp.CodeAnalysis.Symbols
{
    internal sealed class SubstitutedFieldSymbol : FieldSymbol, IPhpPropertySymbol
    {
        #region IPhpPropertySymbol

        PhpPropertyKind IPhpPropertySymbol.FieldKind => ((IPhpPropertySymbol)OriginalDefinition).FieldKind;

        TypeSymbol IPhpPropertySymbol.ContainingStaticsHolder
        {
            get
            {
                if (_containingType.IsStaticsContainer())
                {
                    return _containingType;
                }

                if (PhpFieldSymbolExtension.IsInStaticsHolder(_originalDefinition))
                {
                    return _containingType.TryGetStaticsHolder();
                }

                return null;
            }
        }

        bool IPhpPropertySymbol.RequiresContext => ((IPhpPropertySymbol)OriginalDefinition).RequiresContext;

        TypeSymbol IPhpPropertySymbol.DeclaringType => throw new NotImplementedException();

        void IPhpPropertySymbol.EmitInit(CodeGenerator cg) { throw new NotSupportedException(); }

        public override bool HasNotNull => OriginalDefinition.HasNotNull;

        #endregion

        NamedTypeSymbol _containingType;
        readonly FieldSymbol _originalDefinition;
        readonly object _token;

        private TypeSymbol _lazyType;

        internal SubstitutedFieldSymbol(NamedTypeSymbol containingType, FieldSymbol substitutedFrom)
            : this(containingType, substitutedFrom, containingType)
        {
        }

        internal SubstitutedFieldSymbol(NamedTypeSymbol containingType, FieldSymbol substitutedFrom, object token)
        {
            _containingType = containingType;
            _originalDefinition = substitutedFrom.OriginalDefinition as FieldSymbol;
            _token = token ?? _containingType;
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            if ((object)_lazyType == null)
            {
                Interlocked.CompareExchange(ref _lazyType, _containingType.TypeSubstitution.SubstituteType(_originalDefinition.GetFieldType(fieldsBeingBound)).Type, null);
            }

            return _lazyType;
        }

        public override string Name
        {
            get
            {
                return _originalDefinition.Name;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _originalDefinition.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        internal override bool HasSpecialName
        {
            get
            {
                return _originalDefinition.HasSpecialName;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return _originalDefinition.HasRuntimeSpecialName;
            }
        }

        internal override bool IsNotSerialized
        {
            get
            {
                return _originalDefinition.IsNotSerialized;
            }
        }

        internal override int? TypeLayoutOffset
        {
            get
            {
                return _originalDefinition.TypeLayoutOffset;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _containingType;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return _containingType;
            }
        }

        internal void SetContainingType(SubstitutedNamedTypeSymbol type)
        {
            Debug.Assert(_lazyType == null);

            _lazyType = null;
            _containingType = type;
        }

        public override FieldSymbol OriginalDefinition
        {
            get
            {
                return _originalDefinition.OriginalDefinition;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _originalDefinition.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _originalDefinition.DeclaringSyntaxReferences;
            }
        }

        public override ImmutableArray<AttributeData> GetAttributes()
        {
            return _originalDefinition.GetAttributes();
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                Symbol underlying = _originalDefinition.AssociatedSymbol;

                if ((object)underlying == null)
                {
                    return null;
                }

                return underlying.SymbolAsMember(ContainingType);
            }
        }

        public override bool IsStatic
        {
            get
            {
                return _originalDefinition.IsStatic;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return _originalDefinition.IsReadOnly;
            }
        }

        public override bool IsConst
        {
            get
            {
                return _originalDefinition.IsConst;
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return _originalDefinition.ObsoleteAttributeData;
            }
        }

        public override object ConstantValue
        {
            get
            {
                return _originalDefinition.ConstantValue;
            }
        }

        internal override ConstantValue GetConstantValue(bool earlyDecodingWellKnownAttributes)
        {
            return _originalDefinition.GetConstantValue(earlyDecodingWellKnownAttributes);
        }

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get
            {
                return _originalDefinition.MarshallingInformation;
            }
        }

        public override bool IsVolatile
        {
            get
            {
                return _originalDefinition.IsVolatile;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return _originalDefinition.IsImplicitlyDeclared;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return _originalDefinition.DeclaredAccessibility;
            }
        }

        public override ImmutableArray<CustomModifier> CustomModifiers
        {
            get
            {
                return _containingType.TypeSubstitution.SubstituteCustomModifiers(_originalDefinition.Type, _originalDefinition.CustomModifiers);
            }
        }

        //internal override NamedTypeSymbol FixedImplementationType(PEModuleBuilder emitModule)
        //{
        //    // This occurs rarely, if ever.  The scenario would be a generic struct
        //    // containing a fixed-size buffer.  Given the rarity there would be little
        //    // benefit to "optimizing" the performance of this by caching the
        //    // translated implementation type.
        //    return (NamedTypeSymbol)_containingType.TypeSubstitution.SubstituteType(_originalDefinition.FixedImplementationType(emitModule)).Type;
        //}

        public override bool Equals(object obj)
        {
            if ((object)this == obj)
            {
                return true;
            }

            var other = obj as SubstitutedFieldSymbol;
            return (object)other != null && _token == other._token && _originalDefinition == other._originalDefinition;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_token, _originalDefinition.GetHashCode());
        }
    }
}
