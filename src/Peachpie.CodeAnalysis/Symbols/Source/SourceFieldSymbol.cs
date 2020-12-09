using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Pchp.CodeAnalysis.Utilities;
using System.Globalization;
using System.Threading;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Declares a CLR field representing a PHP field (a class constant or a field).
    /// </summary>
    /// <remarks>
    /// Its CLR properties vary depending on <see cref="SourceFieldSymbol.Initializer"/> and its evaluation.
    /// Some expressions have to be evaluated in runtime which causes the field to be contained in <see cref="SynthesizedStaticFieldsHolder"/>.
    /// </remarks>
    internal partial class SourceFieldSymbol : FieldSymbol, IPhpPropertySymbol
    {
        #region IPhpPropertySymbol

        /// <summary>
        /// The PHP field kind - a class constant, an instance field or a static field.
        /// </summary>
        public PhpPropertyKind FieldKind => _fieldKind;

        TypeSymbol IPhpPropertySymbol.ContainingStaticsHolder
        {
            get
            {
                return RequiresHolder ? (TypeSymbol)_containingType.StaticsContainer : null;
            }
        }

        bool IPhpPropertySymbol.RequiresContext => this.Initializer != null && this.Initializer.RequiresContext;

        TypeSymbol IPhpPropertySymbol.DeclaringType => _containingType;

        /// <summary>
        /// Optional. The field initializer expression.
        /// </summary>
        public override BoundExpression Initializer => _initializer;

        #endregion

        readonly SourceTypeSymbol _containingType;
        readonly string _fieldName;

        readonly PhpPropertyKind _fieldKind;

        readonly Location _location;

        /// <summary>
        /// Optional associated PHPDoc block defining the field type hint.
        /// </summary>
        internal PHPDocBlock PHPDocBlock { get; }

        /// <summary>
        /// Declared accessibility - private, protected or public.
        /// </summary>
        readonly Accessibility _accessibility;

        readonly BoundExpression _initializer;

        ImmutableArray<AttributeData> _attributes;

        /// <summary>
        /// Gets enumeration of property source attributes.
        /// </summary>
        public IEnumerable<SourceCustomAttribute> SourceAttributes => _attributes.OfType<SourceCustomAttribute>();

        /// <summary>
        /// Gets value indicating whether this field redefines a field from a base type.
        /// </summary>
        public bool IsRedefinition => !ReferenceEquals(OverridenDefinition, null);

        /// <summary>
        /// Gets field from a base type that is redefined by this field.
        /// </summary>
        public FieldSymbol OverridenDefinition
        {
            get
            {
                if (ReferenceEquals(_originaldefinition, null))
                {
                    // resolve overriden field symbol
                    _originaldefinition = ResolveOverridenDefinition() ?? this;
                }

                return ReferenceEquals(_originaldefinition, this) ? null : _originaldefinition;
            }
        }
        FieldSymbol _originaldefinition;

        FieldSymbol ResolveOverridenDefinition()
        {
            // lookup base types whether this field declaration isn't a redefinition
            if (this.FieldKind == PhpPropertyKind.InstanceField)
            {
                for (var t = _containingType.BaseType; t != null && t.SpecialType != SpecialType.System_Object; t = t.BaseType)
                {
                    var candidates = t.GetMembers(_fieldName)
                        .OfType<FieldSymbol>()
                        .Where(f => f.IsStatic == false && f.DeclaredAccessibility != Accessibility.Private);

                    //
                    var fld = candidates.FirstOrDefault();
                    if (fld != null)
                    {
                        return fld is SourceFieldSymbol srcf ? srcf.OverridenDefinition ?? fld : fld;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Optional property that provides public access to <see cref="OverridenDefinition"/> if it is protected.
        /// </summary>
        public PropertySymbol FieldAccessorProperty
        {
            get
            {
                if (IsRedefinition && OverridenDefinition.DeclaredAccessibility < this.DeclaredAccessibility && _fieldAccessorProperty == null)
                {
                    // declare property accessing the field from outside:
                    var type = OverridenDefinition.Type;

                    // TYPE get_NAME()
                    var getter = new SynthesizedMethodSymbol(this.ContainingType, "get_" + this.Name, false, false, type, this.DeclaredAccessibility);

                    // void set_NAME(TYPE `value`)
                    var setter = new SynthesizedMethodSymbol(this.ContainingType, "set_" + this.Name, false, false, DeclaringCompilation.CoreTypes.Void, this.DeclaredAccessibility);
                    setter.SetParameters(new SynthesizedParameterSymbol(setter, type, 0, RefKind.None, "value"));

                    // TYPE NAME { get; set; }
                    var fieldAccessorProperty =
                        new SynthesizedPropertySymbol(
                            this.ContainingType, this.Name, false,
                            type, this.DeclaredAccessibility,
                            getter: getter, setter: setter);
                    Interlocked.CompareExchange(ref _fieldAccessorProperty, fieldAccessorProperty, null);
                }

                return _fieldAccessorProperty;
            }
        }
        PropertySymbol _fieldAccessorProperty;

        public SourceFieldSymbol(
            SourceTypeSymbol type, string name, Location location, Accessibility accessibility,
            PHPDocBlock phpdoc, PhpPropertyKind kind,
            BoundExpression initializer = null,
            ImmutableArray<AttributeData> attributes = default)
        {
            Contract.ThrowIfNull(type);
            Contract.ThrowIfNull(name);

            _containingType = type;
            _fieldName = name;
            _fieldKind = kind;
            _accessibility = accessibility;
            _initializer = initializer;
            _location = location;
            _attributes = attributes.IsDefault ? ImmutableArray<AttributeData>.Empty : attributes;
            PHPDocBlock = phpdoc;

            // implicit attributes from PHPDoc
            var deprecated = phpdoc?.GetElement<PHPDocBlock.DeprecatedTag>();
            if (deprecated != null)
            {
                // [ObsoleteAttribute(message, false)]
                _attributes = _attributes.Add(DeclaringCompilation.CreateObsoleteAttribute(deprecated));
            }
        }

        #region FieldSymbol

        public override string Name => _fieldName;

        public override Symbol AssociatedSymbol => null;

        public override Symbol ContainingSymbol => ((IPhpPropertySymbol)this).ContainingStaticsHolder ?? _containingType;

        internal override PhpCompilation DeclaringCompilation => _containingType.DeclaringCompilation;

        public override ImmutableArray<CustomModifier> CustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public override Accessibility DeclaredAccessibility => _accessibility;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences { get { throw new NotImplementedException(); } }

        public override bool IsVolatile => false;

        public override ImmutableArray<Location> Locations => ImmutableArray.Create(_location);

        internal override bool HasRuntimeSpecialName => false;

        internal override bool HasSpecialName => false;

        internal override bool IsNotSerialized => false;

        internal override MarshalPseudoCustomAttributeData MarshallingInformation => null;

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        internal override int? TypeLayoutOffset => null;

        public override ImmutableArray<AttributeData> GetAttributes() => _attributes;

        #endregion

        internal override ConstantValue GetConstantValue(bool earlyDecodingWellKnownAttributes)
        {
            return (_fieldKind == PhpPropertyKind.ClassConstant) ? Initializer?.ConstantValue.ToConstantValueOrNull() : null;
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            // TODO: PHP 7.4 typed properties // https://github.com/peachpiecompiler/peachpie/issues/766

            //
            if ((IsConst || IsReadOnly) && Initializer != null)
            {
                // resolved type symbol if possible
                if (Initializer.ResultType != null)
                {
                    return Initializer.ResultType;
                }

                // resolved value type if possible
                var cvalue = Initializer.ConstantValue;
                if (cvalue.HasValue)
                {
                    var specialType = (cvalue.Value != null)
                        ? cvalue.ToConstantValueOrNull()?.SpecialType
                        : SpecialType.System_Object;    // NULL

                    if (specialType.HasValue && specialType != SpecialType.None)
                    {
                        return DeclaringCompilation.GetSpecialType(specialType.Value);
                    }
                }

                //
                //return DeclaringCompilation.GetTypeFromTypeRef(typectx, Initializer.TypeRefMask);
            }

            // PHPDoc @var type
            if ((DeclaringCompilation.Options.PhpDocTypes & PhpDocTypes.FieldTypes) != 0)
            {
                var vartag = FindPhpDocVarTag();
                if (vartag != null && vartag.TypeNamesArray.Length != 0)
                {
                    var dummyctx = TypeRefFactory.CreateTypeRefContext(_containingType);
                    var tmask = PHPDoc.GetTypeMask(dummyctx, vartag.TypeNamesArray, NameUtils.GetNamingContext(_containingType.Syntax));
                    return DeclaringCompilation.GetTypeFromTypeRef(dummyctx, tmask);
                }
            }

            // default
            return DeclaringCompilation.CoreTypes.PhpValue;
        }

        /// <summary>
        /// <c>const</c> whether the field is a constant and its value can be resolved as constant value.
        /// </summary>
        public override bool IsConst => _fieldKind == PhpPropertyKind.ClassConstant && GetConstantValue(false) != null;

        /// <summary>
        /// <c>readonly</c> applies to class constants that have to be evaluated at runtime.
        /// </summary>
        public override bool IsReadOnly => _fieldKind == PhpPropertyKind.ClassConstant && GetConstantValue(false) == null;

        /// <summary>
        /// Whether the field is real CLR static field.
        /// </summary>
        public override bool IsStatic => _fieldKind == PhpPropertyKind.AppStaticField || IsConst; // either field is CLR static field or constant (Literal field must be Static).

        internal PHPDocBlock.TypeVarDescTag FindPhpDocVarTag()
        {
            if (PHPDocBlock != null)
            {
                foreach (var vartype in PHPDocBlock.Elements.OfType<PHPDocBlock.TypeVarDescTag>())
                {
                    if (string.IsNullOrEmpty(vartype.VariableName) || vartype.VariableName.Substring(1) == this.MetadataName)
                    {
                        return vartype;
                    }
                }
            }

            return null;
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            var summary = string.Empty;

            if (PHPDocBlock != null)
            {
                summary = PHPDocBlock.Summary;

                if (string.IsNullOrWhiteSpace(summary))
                {
                    // try @var or @staticvar:
                    var vartag = FindPhpDocVarTag();
                    if (vartag != null)
                    {
                        summary = vartag.Description;
                    }
                }
            }

            return summary;
        }
    }
}
