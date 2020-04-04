using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Peachpie.CodeAnalysis.Symbols;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// The class to represent all method parameters imported from a PE/module.
    /// </summary>
    internal class PEParameterSymbol : ParameterSymbol
    {
        #region WellKnownAttributeFlags

        [Flags]
        private enum WellKnownAttributeFlags
        {
            HasIDispatchConstantAttribute = 0x1 << 0,
            HasIUnknownConstantAttribute = 0x1 << 1,
            HasCallerFilePathAttribute = 0x1 << 2,
            HasCallerLineNumberAttribute = 0x1 << 3,
            HasCallerMemberNameAttribute = 0x1 << 4,
            IsCallerFilePath = 0x1 << 5,
            IsCallerLineNumber = 0x1 << 6,
            IsCallerMemberName = 0x1 << 7,
            NotNull = 0x1 << 8,
            PhpRw = 0x1 << 9,
        }

        #endregion

        #region PackedFlags

        private struct PackedFlags
        {
            // Layout:
            // |.............|n|rr|cccccccc|vvvvvvvv|
            // 
            // v = decoded well known attribute values. 8 bits.
            // c = completion states for well known attributes. 1 if given attribute has been decoded, 0 otherwise. 8 bits.
            // r = RefKind. 2 bits.
            // n = hasNameInMetadata. 1 bit.

            private const int WellKnownAttributeDataOffset = 0;
            private const int WellKnownAttributeCompletionFlagOffset = 10;
            private const int RefKindOffset = 20;

            private const int RefKindMask = 0x3;
            private const int WellKnownAttributeDataMask = (0x1 << WellKnownAttributeCompletionFlagOffset) - 1;
            private const int WellKnownAttributeCompletionFlagMask = WellKnownAttributeDataMask;

            private const int HasNameInMetadataBit = 0x1 << 22;
            private const int HasDefaultValueFieldPopulatedBit = 0x1 << 23;

            private const int AllWellKnownAttributesCompleteNoData = WellKnownAttributeCompletionFlagMask << WellKnownAttributeCompletionFlagOffset;

            private int _bits;

            public RefKind RefKind
            {
                get { return (RefKind)((_bits >> RefKindOffset) & RefKindMask); }
            }

            public bool HasNameInMetadata
            {
                get { return (_bits & HasNameInMetadataBit) != 0; }
            }

            public bool HasDefaultValueFieldPopulated
            {
                get { return (_bits & HasDefaultValueFieldPopulatedBit) != 0; }
            }

            public void SetDefaultValueFieldPopulated()
            {
                ThreadSafeFlagOperations.Set(ref _bits, HasDefaultValueFieldPopulatedBit);
            }

            public PackedFlags(RefKind refKind, bool attributesAreComplete, bool hasNameInMetadata)
            {
                int refKindBits = ((int)refKind & RefKindMask) << RefKindOffset;
                int attributeBits = attributesAreComplete ? AllWellKnownAttributesCompleteNoData : 0;
                int hasNameInMetadataBits = hasNameInMetadata ? HasNameInMetadataBit : 0;

                _bits = refKindBits | attributeBits | hasNameInMetadataBits;
            }

            public bool SetWellKnownAttribute(WellKnownAttributeFlags flag, bool value)
            {
                // a value has been decoded:
                int bitsToSet = (int)flag << WellKnownAttributeCompletionFlagOffset;
                if (value)
                {
                    // the actual value:
                    bitsToSet |= ((int)flag << WellKnownAttributeDataOffset);
                }

                ThreadSafeFlagOperations.Set(ref _bits, bitsToSet);
                return value;
            }

            public bool TryGetWellKnownAttribute(WellKnownAttributeFlags flag, out bool value)
            {
                int theBits = _bits; // Read this.bits once to ensure the consistency of the value and completion flags.
                value = (theBits & ((int)flag << WellKnownAttributeDataOffset)) != 0;
                return (theBits & ((int)flag << WellKnownAttributeCompletionFlagOffset)) != 0;
            }
        }

        #endregion

        private readonly Symbol _containingSymbol;
        private readonly string _name;
        private readonly TypeSymbol _type;
        private readonly ParameterHandle _handle;
        private readonly ParameterAttributes _flags;
        private readonly PEModuleSymbol _moduleSymbol;

        private ImmutableArray<AttributeData> _lazyCustomAttributes;
        private ConstantValue _lazyDefaultValue = ConstantValue.Unset;
        private ThreeState _lazyIsParams;
        private ImportValueAttributeData _lazyImportValueAttributeData;
        private FieldSymbol _lazyDefaultValueField; // field used to load a default value of the optional parameter

        /// <summary>
        /// Attributes filtered out from m_lazyCustomAttributes, ParamArray, etc.
        /// </summary>
        private ImmutableArray<AttributeData> _lazyHiddenAttributes;

        private readonly ushort _ordinal;

        private PackedFlags _packedFlags;

        internal static PEParameterSymbol Create(
            PEModuleSymbol moduleSymbol,
            PEMethodSymbol containingSymbol,
            int ordinal,
            ParamInfo<TypeSymbol> parameter,
            out bool isBad)
        {
            return Create(moduleSymbol, containingSymbol, ordinal, parameter.IsByRef, parameter.Type, parameter.Handle, parameter.CustomModifiers, out isBad);
        }

        /// <summary>
        /// Construct a parameter symbol for a property loaded from metadata.
        /// </summary>
        /// <param name="moduleSymbol"></param>
        /// <param name="containingSymbol"></param>
        /// <param name="ordinal"></param>
        /// <param name="handle">The property parameter doesn't have a name in metadata,
        /// so this is the handle of a corresponding accessor parameter, if there is one,
        /// or of the ParamInfo passed in, otherwise).</param>
        /// <param name="isBad" />
        /// <param name="parameter"></param>
        internal static PEParameterSymbol Create(
            PEModuleSymbol moduleSymbol,
            PEPropertySymbol containingSymbol,
            int ordinal,
            ParameterHandle handle,
            ParamInfo<TypeSymbol> parameter,
            out bool isBad)
        {
            return Create(moduleSymbol, containingSymbol, ordinal, parameter.IsByRef, parameter.Type, handle, parameter.CustomModifiers, out isBad);
        }

        private PEParameterSymbol(
            PEModuleSymbol moduleSymbol,
            Symbol containingSymbol,
            int ordinal,
            bool isByRef,
            TypeSymbol type,
            ParameterHandle handle,
            out bool isBad)
        {
            Debug.Assert((object)moduleSymbol != null);
            Debug.Assert((object)containingSymbol != null);
            Debug.Assert(ordinal >= 0);
            Debug.Assert((object)type != null);

            isBad = false;
            _moduleSymbol = moduleSymbol;
            _containingSymbol = containingSymbol;
            _ordinal = (ushort)ordinal;

            _handle = handle;

            RefKind refKind = RefKind.None;

            if (handle.IsNil)
            {
                refKind = isByRef ? RefKind.Ref : RefKind.None;

                _type = type;
                _lazyCustomAttributes = ImmutableArray<AttributeData>.Empty;
                _lazyHiddenAttributes = ImmutableArray<AttributeData>.Empty;
                _lazyDefaultValue = ConstantValue.NotAvailable;
                _lazyIsParams = ThreeState.False;
            }
            else
            {
                try
                {
                    moduleSymbol.Module.GetParamPropsOrThrow(handle, out _name, out _flags);
                }
                catch (BadImageFormatException)
                {
                    isBad = true;
                }

                if (isByRef)
                {
                    ParameterAttributes inOutFlags = _flags & (ParameterAttributes.Out | ParameterAttributes.In);
                    refKind = (inOutFlags == ParameterAttributes.Out) ? RefKind.Out : RefKind.Ref;
                }

                // CONSIDER: Can we make parameter type computation lazy?
                _type = type; // DynamicTypeDecoder.TransformType(type, countOfCustomModifiers, handle, moduleSymbol, refKind);
            }

            bool hasNameInMetadata = !string.IsNullOrEmpty(_name);
            if (!hasNameInMetadata)
            {
                // As was done historically, if the parameter doesn't have a name, we give it the name "value".
                _name = "value";
            }

            _packedFlags = new PackedFlags(refKind, attributesAreComplete: handle.IsNil, hasNameInMetadata: hasNameInMetadata);

            Debug.Assert(refKind == this.RefKind);
            Debug.Assert(hasNameInMetadata == this.HasNameInMetadata);
        }

        private bool HasNameInMetadata
        {
            get
            {
                return _packedFlags.HasNameInMetadata;
            }
        }

        private static PEParameterSymbol Create(
            PEModuleSymbol moduleSymbol,
            Symbol containingSymbol,
            int ordinal,
            bool isByRef,
            TypeSymbol type,
            ParameterHandle handle,
            ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers,
            out bool isBad)
        {
            if (customModifiers.IsDefaultOrEmpty)
            {
                return new PEParameterSymbol(moduleSymbol, containingSymbol, ordinal, isByRef, type, handle, out isBad);
            }

            return new PEParameterSymbolWithCustomModifiers(moduleSymbol, containingSymbol, ordinal, isByRef, type, handle, customModifiers, out isBad);
        }

        private sealed class PEParameterSymbolWithCustomModifiers : PEParameterSymbol
        {
            private readonly ImmutableArray<CustomModifier> _customModifiers;

            public PEParameterSymbolWithCustomModifiers(
                PEModuleSymbol moduleSymbol,
                Symbol containingSymbol,
                int ordinal,
                bool isByRef,
                TypeSymbol type,
                ParameterHandle handle,
                ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers,
                out bool isBad) :
                    base(moduleSymbol, containingSymbol, ordinal, isByRef, type, handle, out isBad)
            {
                _customModifiers = CSharpCustomModifier.Convert(customModifiers);

                // TODO: RefCustomModifiers
            }

            public override ImmutableArray<CustomModifier> CustomModifiers
            {
                get
                {
                    return _customModifiers;
                }
            }
        }

        public override RefKind RefKind
        {
            get
            {
                return _packedFlags.RefKind;
            }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        public override string MetadataName
        {
            get
            {
                return HasNameInMetadata ? _name : string.Empty;
            }
        }

        internal ParameterAttributes Flags
        {
            get
            {
                return _flags;
            }
        }

        public override int Ordinal
        {
            get
            {
                return _ordinal;
            }
        }

        // might be Nil
        internal ParameterHandle Handle
        {
            get
            {
                return _handle;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _containingSymbol;
            }
        }

        internal override bool HasMetadataConstantValue
        {
            get
            {
                return (_flags & ParameterAttributes.HasDefault) != 0;
            }
        }

        /// <remarks>
        /// Internal for testing.  Non-test code should use <see cref="ExplicitDefaultConstantValue"/>.
        /// </remarks>
        internal ConstantValue ImportConstantValue(bool ignoreAttributes = false)
        {
            Debug.Assert(!_handle.IsNil);

            // Metadata Spec 22.33: 
            //   6. If Flags.HasDefault = 1 then this row [of Param table] shall own exactly one row in the Constant table [ERROR]
            //   7. If Flags.HasDefault = 0, then there shall be no rows in the Constant table owned by this row [ERROR]
            ConstantValue value = null;

            if ((_flags & ParameterAttributes.HasDefault) != 0)
            {
                value = _moduleSymbol.Module.GetParamDefaultValue(_handle);
            }

            if (value == null && !ignoreAttributes)
            {
                value = GetDefaultDecimalOrDateTimeValue();
            }

            return value;
        }

        internal override ConstantValue ExplicitDefaultConstantValue
        {
            get
            {
                // The HasDefault flag has to be set, it doesn't suffice to mark the parameter with DefaultParameterValueAttribute.
                if (_lazyDefaultValue == ConstantValue.Unset)
                {
                    // From the C# point of view, there is no need to import a parameter's default value
                    // if the language isn't going to treat it as optional. However, we might need metadata constant value for NoPia.
                    // NOTE: Ignoring attributes for non-Optional parameters disrupts round-tripping, but the trade-off seems acceptable.
                    ConstantValue value = ImportConstantValue(ignoreAttributes: !IsOptional);
                    Interlocked.CompareExchange(ref _lazyDefaultValue, value, ConstantValue.Unset);
                }

                return _lazyDefaultValue;
            }
        }

        private ConstantValue GetDefaultDecimalOrDateTimeValue()
        {
            Debug.Assert(!_handle.IsNil);
            ConstantValue value = null;

            // It is possible in Visual Basic for a parameter of object type to have a default value of DateTime type.
            // If it's present, use it.  We'll let the call-site figure out whether it can actually be used.
            if (_moduleSymbol.Module.HasDateTimeConstantAttribute(_handle, out value))
            {
                return value;
            }

            // It is possible in Visual Basic for a parameter of object type to have a default value of decimal type.
            // If it's present, use it.  We'll let the call-site figure out whether it can actually be used.
            if (_moduleSymbol.Module.HasDecimalConstantAttribute(_handle, out value))
            {
                return value;
            }

            return value;
        }

        public override bool IsOptional => (_flags & ParameterAttributes.Optional) != 0;

        public override bool HasNotNull
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.NotNull;

                if (!_packedFlags.TryGetWellKnownAttribute(flag, out var value))
                {
                    value = _packedFlags.SetWellKnownAttribute(flag, AttributeHelpers.HasNotNullAttribute(Handle, (PEModuleSymbol)ContainingModule));
                }
                return value;
            }
        }

        public override bool IsPhpRw
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.PhpRw;

                if (!_packedFlags.TryGetWellKnownAttribute(flag, out var value))
                {
                    value = _packedFlags.SetWellKnownAttribute(flag, AttributeHelpers.HasPhpRwAttribute(Handle, (PEModuleSymbol)ContainingModule));
                }
                return value;
            }
        }

        internal override ImportValueAttributeData ImportValueAttributeData
        {
            get
            {
                var value = _lazyImportValueAttributeData;
                if (value.IsDefault)
                {
                    value = AttributeHelpers.HasImportValueAttribute(Handle, (PEModuleSymbol)ContainingModule);
                    if (value.IsDefault)
                    {
                        value = ImportValueAttributeData.Invalid;
                    }

                    _lazyImportValueAttributeData = value;
                }

                return value;
            }
        }

        //internal override bool IsIDispatchConstant
        //{
        //    get
        //    {
        //        const WellKnownAttributeFlags flag = WellKnownAttributeFlags.HasIDispatchConstantAttribute;

        //        bool value;
        //        if (!_packedFlags.TryGetWellKnownAttribute(flag, out value))
        //        {
        //            value = _packedFlags.SetWellKnownAttribute(flag, _moduleSymbol.Module.HasAttribute(_handle,
        //                AttributeDescription.IDispatchConstantAttribute));
        //        }
        //        return value;
        //    }
        //}

        //internal override bool IsIUnknownConstant
        //{
        //    get
        //    {
        //        const WellKnownAttributeFlags flag = WellKnownAttributeFlags.HasIUnknownConstantAttribute;

        //        bool value;
        //        if (!_packedFlags.TryGetWellKnownAttribute(flag, out value))
        //        {
        //            value = _packedFlags.SetWellKnownAttribute(flag, _moduleSymbol.Module.HasAttribute(_handle,
        //                AttributeDescription.IUnknownConstantAttribute));
        //        }
        //        return value;
        //    }
        //}

        private bool HasCallerLineNumberAttribute
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.HasCallerLineNumberAttribute;

                bool value;
                if (!_packedFlags.TryGetWellKnownAttribute(flag, out value))
                {
                    value = _packedFlags.SetWellKnownAttribute(flag, _moduleSymbol.Module.HasAttribute(_handle,
                        AttributeDescription.CallerLineNumberAttribute));
                }
                return value;
            }
        }

        private bool HasCallerFilePathAttribute
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.HasCallerFilePathAttribute;

                bool value;
                if (!_packedFlags.TryGetWellKnownAttribute(flag, out value))
                {
                    value = _packedFlags.SetWellKnownAttribute(flag, _moduleSymbol.Module.HasAttribute(_handle,
                        AttributeDescription.CallerFilePathAttribute));
                }
                return value;
            }
        }

        private bool HasCallerMemberNameAttribute
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.HasCallerMemberNameAttribute;

                bool value;
                if (!_packedFlags.TryGetWellKnownAttribute(flag, out value))
                {
                    value = _packedFlags.SetWellKnownAttribute(flag, _moduleSymbol.Module.HasAttribute(_handle,
                        AttributeDescription.CallerMemberNameAttribute));
                }
                return value;
            }
        }

        //internal override bool IsCallerLineNumber
        //{
        //    get
        //    {
        //        const WellKnownAttributeFlags flag = WellKnownAttributeFlags.IsCallerLineNumber;

        //        bool value;
        //        if (!_packedFlags.TryGetWellKnownAttribute(flag, out value))
        //        {
        //            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
        //            bool isCallerLineNumber = HasCallerLineNumberAttribute
        //                && new TypeConversions(ContainingAssembly).HasCallerLineNumberConversion(this.Type, ref useSiteDiagnostics);

        //            value = _packedFlags.SetWellKnownAttribute(flag, isCallerLineNumber);
        //        }
        //        return value;
        //    }
        //}

        //internal override bool IsCallerFilePath
        //{
        //    get
        //    {
        //        const WellKnownAttributeFlags flag = WellKnownAttributeFlags.IsCallerFilePath;

        //        bool value;
        //        if (!_packedFlags.TryGetWellKnownAttribute(flag, out value))
        //        {
        //            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
        //            bool isCallerFilePath = !HasCallerLineNumberAttribute
        //                && HasCallerFilePathAttribute
        //                && new TypeConversions(ContainingAssembly).HasCallerInfoStringConversion(this.Type, ref useSiteDiagnostics);

        //            value = _packedFlags.SetWellKnownAttribute(flag, isCallerFilePath);
        //        }
        //        return value;
        //    }
        //}

        //internal override bool IsCallerMemberName
        //{
        //    get
        //    {
        //        const WellKnownAttributeFlags flag = WellKnownAttributeFlags.IsCallerMemberName;

        //        bool value;
        //        if (!_packedFlags.TryGetWellKnownAttribute(flag, out value))
        //        {
        //            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
        //            bool isCallerMemberName = !HasCallerLineNumberAttribute
        //                && !HasCallerFilePathAttribute
        //                && HasCallerMemberNameAttribute
        //                && new TypeConversions(ContainingAssembly).HasCallerInfoStringConversion(this.Type, ref useSiteDiagnostics);

        //            value = _packedFlags.SetWellKnownAttribute(flag, isCallerMemberName);
        //        }
        //        return value;
        //    }
        //}

        internal override TypeSymbol Type => _type;

        //public override ImmutableArray<CustomModifier> CustomModifiers
        //{
        //    get
        //    {
        //        return ImmutableArray<CustomModifier>.Empty;
        //    }
        //}

        //internal override ushort CountOfCustomModifiersPrecedingByRef
        //{
        //    get
        //    {
        //        return 0;
        //    }
        //}

        //internal override bool IsMetadataIn
        //{
        //    get { return (_flags & ParameterAttributes.In) != 0; }
        //}

        //internal override bool IsMetadataOut
        //{
        //    get { return (_flags & ParameterAttributes.Out) != 0; }
        //}

        internal override bool IsMarshalledExplicitly
        {
            get
            {
                return (_flags & ParameterAttributes.HasFieldMarshal) != 0;
            }
        }

        //internal override MarshalPseudoCustomAttributeData MarshallingInformation
        //{
        //    get
        //    {
        //        // the compiler doesn't need full marshalling information, just the unmanaged type or descriptor
        //        return null;
        //    }
        //}

        internal override ImmutableArray<byte> MarshallingDescriptor
        {
            get
            {
                if ((_flags & ParameterAttributes.HasFieldMarshal) == 0)
                {
                    return default(ImmutableArray<byte>);
                }

                Debug.Assert(!_handle.IsNil);
                return _moduleSymbol.Module.GetMarshallingDescriptor(_handle);
            }
        }

        //internal override UnmanagedType MarshallingType
        //{
        //    get
        //    {
        //        if ((_flags & ParameterAttributes.HasFieldMarshal) == 0)
        //        {
        //            return 0;
        //        }

        //        Debug.Assert(!_handle.IsNil);
        //        return _moduleSymbol.Module.GetMarshallingType(_handle);
        //    }
        //}

        public override bool IsParams
        {
            get
            {
                // This is also populated by loading attributes, but loading
                // attributes is more expensive, so we should only do it if
                // attributes are requested.
                if (!_lazyIsParams.HasValue())
                {
                    _lazyIsParams = _moduleSymbol.Module.HasParamsAttribute(_handle).ToThreeState();
                }
                return _lazyIsParams.Value();
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return
                    SpecialParameterSymbol.IsContextParameter(this) ||
                    SpecialParameterSymbol.IsLateStaticParameter(this) ||
                    SpecialParameterSymbol.IsImportValueParameter(this) ||
                    SpecialParameterSymbol.IsDummyFieldsOnlyCtorParameter(this) ||
                    base.IsImplicitlyDeclared;
            }
        }

        public override FieldSymbol DefaultValueField
        {
            get
            {
                var field = _lazyDefaultValueField;

                if (!_packedFlags.HasDefaultValueFieldPopulated)
                {
                    if (AttributeHelpers.HasDefaultValueAttributeData(Handle, (PEModuleSymbol)ContainingModule))
                    {
                        var attr = GetAttributes().FirstOrDefault(attr => attr.AttributeClass.Name == "DefaultValueAttribute");
                        if (attr != null)
                        {
                            // [DefaultValueAttribute( FieldName ) { ExplicitType }]
                            var fldname = (string)attr.ConstructorArguments[0].Value;
                            var container = attr.NamedArguments.SingleOrDefault(pair => pair.Key == "ExplicitType").Value.Value as ITypeSymbol
                                ?? ContainingType;

                            field = container.GetMembers(fldname).OfType<FieldSymbol>().Single();
                            Debug.Assert(field.IsStatic);
                            _lazyDefaultValueField = field;
                        }
                    }

                    _packedFlags.SetDefaultValueFieldPopulated();
                }

                return field;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _containingSymbol.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override ImmutableArray<AttributeData> GetAttributes()
        {
            if (_lazyCustomAttributes.IsDefault)
            {
                Debug.Assert(!_handle.IsNil);
                var containingPEModuleSymbol = (PEModuleSymbol)this.ContainingModule;

                // Filter out ParamArrayAttributes if necessary and cache
                // the attribute handle for GetCustomAttributesToEmit
                bool filterOutParamArrayAttribute = (!_lazyIsParams.HasValue() || _lazyIsParams.Value());

                ConstantValue defaultValue = this.ExplicitDefaultConstantValue;
                AttributeDescription filterOutConstantAttributeDescription = default(AttributeDescription);

                if ((object)defaultValue != null)
                {
                    if (defaultValue.Discriminator == ConstantValueTypeDiscriminator.DateTime)
                    {
                        filterOutConstantAttributeDescription = AttributeDescription.DateTimeConstantAttribute;
                    }
                    else if (defaultValue.Discriminator == ConstantValueTypeDiscriminator.Decimal)
                    {
                        filterOutConstantAttributeDescription = AttributeDescription.DecimalConstantAttribute;
                    }
                }

                if (filterOutParamArrayAttribute || filterOutConstantAttributeDescription.Signatures != null)
                {
                    CustomAttributeHandle paramArrayAttribute;
                    CustomAttributeHandle constantAttribute;

                    ImmutableArray<AttributeData> attributes =
                        containingPEModuleSymbol.GetCustomAttributesForToken(
                            _handle,
                            out paramArrayAttribute,
                            filterOutParamArrayAttribute ? AttributeDescription.ParamArrayAttribute : default(AttributeDescription),
                            out constantAttribute,
                            filterOutConstantAttributeDescription);

                    if (!paramArrayAttribute.IsNil || !constantAttribute.IsNil)
                    {
                        var builder = ArrayBuilder<AttributeData>.GetInstance();

                        if (!paramArrayAttribute.IsNil)
                        {
                            builder.Add(new PEAttributeData(containingPEModuleSymbol, paramArrayAttribute));
                        }

                        if (!constantAttribute.IsNil)
                        {
                            builder.Add(new PEAttributeData(containingPEModuleSymbol, constantAttribute));
                        }

                        ImmutableInterlocked.InterlockedInitialize(ref _lazyHiddenAttributes, builder.ToImmutableAndFree());
                    }
                    else
                    {
                        ImmutableInterlocked.InterlockedInitialize(ref _lazyHiddenAttributes, ImmutableArray<AttributeData>.Empty);
                    }

                    if (!_lazyIsParams.HasValue())
                    {
                        Debug.Assert(filterOutParamArrayAttribute);
                        _lazyIsParams = (!paramArrayAttribute.IsNil).ToThreeState();
                    }

                    ImmutableInterlocked.InterlockedInitialize(
                        ref _lazyCustomAttributes,
                        attributes);
                }
                else
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyHiddenAttributes, ImmutableArray<AttributeData>.Empty);
                    containingPEModuleSymbol.LoadCustomAttributes(_handle, ref _lazyCustomAttributes);
                }
            }

            Debug.Assert(!_lazyHiddenAttributes.IsDefault);
            return _lazyCustomAttributes;
        }

        internal override IEnumerable<AttributeData> GetCustomAttributesToEmit(CommonModuleCompilationState compilationState)
        {
            foreach (AttributeData attribute in GetAttributes())
            {
                yield return attribute;
            }

            // Yield hidden attributes last, order might be important.
            foreach (AttributeData attribute in _lazyHiddenAttributes)
            {
                yield return attribute;
            }
        }

        internal override PhpCompilation DeclaringCompilation => null;
    }
}
