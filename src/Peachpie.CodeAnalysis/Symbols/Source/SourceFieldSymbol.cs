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
    internal partial class SourceFieldSymbol : FieldSymbol
    {
        /// <summary>
        /// The field kind.
        /// </summary>
        public enum KindEnum
        {
            InstanceField,
            StaticField,
            AppStaticField,
            ClassConstant,
        }

        readonly SourceTypeSymbol _containingType;
        readonly string _fieldName;

        /// <summary>
        /// The PHP field kind - a class constant, an instance field or a static field.
        /// </summary>
        public KindEnum FieldKind => _fieldKind;
        readonly KindEnum _fieldKind;

        readonly Location _location;

        /// <summary>
        /// Optional associated PHPDoc block defining the field type hint.
        /// </summary>
        readonly PHPDocBlock _phpDoc;

        /// <summary>
        /// Declared accessibility - private, protected or public.
        /// </summary>
        readonly Accessibility _accessibility;

        /// <summary>
        /// Optional. The field initializer expression.
        /// </summary>
        public BoundExpression Initializer => _initializer;
        readonly BoundExpression _initializer;

        /// <summary>
        /// Actual field symbol that should be used.
        /// </summary>
        public override FieldSymbol OriginalDefinition
        {
            get
            {
                if (_originaldefinition == null)
                {
                    // lookup base types whether this field declaration isn't a redefinition
                    if (this.FieldKind == KindEnum.InstanceField)
                    {
                        for (var t = _containingType.BaseType; t != null; t = t.BaseType)
                        {
                            var candidates = t.GetMembers(_fieldName, false)
                                .OfType<FieldSymbol>()
                                .Where(f => f.IsStatic == this.IsStatic && f.DeclaredAccessibility != Accessibility.Private);

                            foreach (var f in candidates)
                            {
                                // check accessibility
                                if (this.DeclaredAccessibility != f.DeclaredAccessibility)
                                {
                                    // TODO: ERR
                                    throw new ArgumentException($"Fatal error: Access level to ${_fieldName} must be {f.DeclaredAccessibility} (as in class {t.Name})");
                                }

                                //
                                _originaldefinition = f.OriginalDefinition;
                                return _originaldefinition;
                            }
                        }
                    }

                    //
                    _originaldefinition = this;
                }

                return _originaldefinition;
            }
        }
        private FieldSymbol _originaldefinition;

        public SourceFieldSymbol(SourceTypeSymbol type, string name, Location location, Accessibility accessibility, PHPDocBlock phpdoc, KindEnum kind, BoundExpression initializer = null)
        {
            Contract.ThrowIfNull(type);
            Contract.ThrowIfNull(name);

            _containingType = type;
            _fieldName = name;
            _fieldKind = kind;
            _accessibility = accessibility;
            _phpDoc = phpdoc;
            _initializer = initializer;
            _location = location;
        }

        #region FieldSymbol

        public override string Name => _fieldName;

        public override Symbol AssociatedSymbol => null;

        public override Symbol ContainingSymbol => /*RequiresHolder ? _containingType.StaticsContainer :*/ _containingType;

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

        #endregion

        internal override ConstantValue GetConstantValue(bool earlyDecodingWellKnownAttributes)
        {
            return (_fieldKind == KindEnum.ClassConstant) ? Initializer?.ConstantValue.ToConstantValueOrNull() : null;
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            // TODO: HHVM TypeHint

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
                var vartag = _phpDoc?.GetElement<PHPDocBlock.VarTag>();
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
        public override bool IsConst => _fieldKind == KindEnum.ClassConstant && GetConstantValue(false) != null;

        /// <summary>
        /// <c>readonly</c> applies to class constants that have to be evaluated at runtime.
        /// </summary>
        public override bool IsReadOnly => _fieldKind == KindEnum.ClassConstant && GetConstantValue(false) == null;

        /// <summary>
        /// Whether the field is real CLR static field.
        /// </summary>
        public override bool IsStatic => _fieldKind == KindEnum.AppStaticField || IsConst; // either field is CLR static field or constant (Literal field must be Static).

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _phpDoc?.Summary ?? string.Empty;
        }
    }
}
