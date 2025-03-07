﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Emit;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pchp.CodeAnalysis.FlowAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// The class to represent all fields imported from a PE/module.
    /// </summary>
    internal sealed class PEFieldSymbol : FieldSymbol, IPhpPropertySymbol
    {
        #region IPhpPropertySymbol

        PhpPropertyKind IPhpPropertySymbol.FieldKind
        {
            get
            {
                if (IsConst) return PhpPropertyKind.ClassConstant;
                if (IsStatic) return PhpPropertyKind.AppStaticField;

                if (((IPhpPropertySymbol)this).ContainingStaticsHolder != null)
                {
                    if (IsReadOnly) return PhpPropertyKind.ClassConstant;
                    return PhpPropertyKind.StaticField;
                }

                return PhpPropertyKind.InstanceField;
            }
        }

        TypeSymbol IPhpPropertySymbol.ContainingStaticsHolder => _containingType.IsStaticsContainer() ? _containingType : null;

        bool IPhpPropertySymbol.RequiresContext => false; // not relevant

        TypeSymbol IPhpPropertySymbol.DeclaringType => _containingType.IsStaticsContainer() ? _containingType.ContainingType : _containingType;

        void IPhpPropertySymbol.EmitInit(CodeGen.CodeGenerator cg) { throw new NotSupportedException(); } // not needed

        #endregion

        private readonly FieldDefinitionHandle _handle;
        private readonly string _name;
        private readonly FieldAttributes _flags;
        private readonly PENamedTypeSymbol _containingType;
        private bool _lazyIsVolatile;
        private ImmutableArray<AttributeData> _lazyCustomAttributes;
        private ImmutableArray<CustomModifier> _lazyCustomModifiers;
        private ConstantValue _lazyConstantValue = Microsoft.CodeAnalysis.ConstantValue.Unset; // Indicates an uninitialized ConstantValue
        //private Tuple<CultureInfo, string> _lazyDocComment;
        //private DiagnosticInfo _lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 

        //private ObsoleteAttributeData _lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized;

        private TypeSymbol _lazyType;
        private int _lazyFixedSize;
        private NamedTypeSymbol _lazyFixedImplementationType;
        //private PEEventSymbol _associatedEventOpt;
        private byte _lazyIsPhpHidden;

        internal PEFieldSymbol(
            PEModuleSymbol moduleSymbol,
            PENamedTypeSymbol containingType,
            FieldDefinitionHandle fieldDef)
        {
            Debug.Assert((object)moduleSymbol != null);
            Debug.Assert((object)containingType != null);
            Debug.Assert(!fieldDef.IsNil);

            _handle = fieldDef;
            _containingType = containingType;

            try
            {
                moduleSymbol.Module.GetFieldDefPropsOrThrow(fieldDef, out _name, out _flags);
            }
            catch (BadImageFormatException)
            {
                if ((object)_name == null)
                {
                    _name = String.Empty;
                }

                throw new NotImplementedException();
                //_lazyUseSiteDiagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this);
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _containingType;
            }
        }

        public override NamedTypeSymbol ContainingType => _containingType;

        public override string Name => _name;

        internal FieldAttributes Flags
        {
            get
            {
                return _flags;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return (_flags & FieldAttributes.SpecialName) != 0;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return (_flags & FieldAttributes.RTSpecialName) != 0;
            }
        }

        internal override bool IsNotSerialized
        {
            get
            {
                return (_flags & FieldAttributes.NotSerialized) != 0;
            }
        }

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get
            {
                // the compiler doesn't need full marshalling information, just the unmanaged type or descriptor
                return null;
            }
        }

        internal override bool IsMarshalledExplicitly
        {
            get
            {
                return ((_flags & FieldAttributes.HasFieldMarshal) != 0);
            }
        }

        internal override UnmanagedType MarshallingType
        {
            get
            {
                if ((_flags & FieldAttributes.HasFieldMarshal) == 0)
                {
                    return 0;
                }

                return _containingType.ContainingPEModule.Module.GetMarshallingType(_handle);
            }
        }

        internal override ImmutableArray<byte> MarshallingDescriptor
        {
            get
            {
                if ((_flags & FieldAttributes.HasFieldMarshal) == 0)
                {
                    return default(ImmutableArray<byte>);
                }

                return _containingType.ContainingPEModule.Module.GetMarshallingDescriptor(_handle);
            }
        }

        internal override int? TypeLayoutOffset
        {
            get
            {
                return _containingType.ContainingPEModule.Module.GetFieldOffset(_handle);
            }
        }

        internal FieldDefinitionHandle Handle
        {
            get
            {
                return _handle;
            }
        }

        ///// <summary>
        ///// Mark this field as the backing field of a field-like event.
        ///// The caller will also ensure that it is excluded from the member list of
        ///// the containing type (as it would be in source).
        ///// </summary>
        //internal void SetAssociatedEvent(PEEventSymbol eventSymbol)
        //{
        //    Debug.Assert((object)eventSymbol != null);
        //    Debug.Assert(eventSymbol.ContainingType == _containingType);

        //    // This should always be true in valid metadata - there should only
        //    // be one event with a given name in a given type.
        //    if ((object)_associatedEventOpt == null)
        //    {
        //        // No locking required since this method will only be called by the thread that created
        //        // the field symbol (and will be called before the field symbol is added to the containing 
        //        // type members and available to other threads).
        //        _associatedEventOpt = eventSymbol;
        //    }
        //}

        private void EnsureSignatureIsLoaded()
        {
            if ((object)_lazyType == null)
            {
                var moduleSymbol = _containingType.ContainingPEModule;
                ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers;
                TypeSymbol type = (new MetadataDecoder(moduleSymbol, _containingType)).DecodeFieldSignature(_handle, out customModifiers);
                ImmutableArray<CustomModifier> customModifiersArray = CSharpCustomModifier.Convert(customModifiers);
                //type = DynamicTypeDecoder.TransformType(type, customModifiersArray.Length, _handle, moduleSymbol);
                _lazyIsVolatile = customModifiersArray.Any(m => !m.IsOptional && m.Modifier.SpecialType == SpecialType.System_Runtime_CompilerServices_IsVolatile);

                TypeSymbol fixedElementType;
                int fixedSize;
                if (customModifiersArray.IsEmpty && IsFixedBuffer(out fixedSize, out fixedElementType))
                {
                    _lazyFixedSize = fixedSize;
                    _lazyFixedImplementationType = type as NamedTypeSymbol;
                    //type = new PointerTypeSymbol(fixedElementType);
                    throw new NotImplementedException();
                }

                ImmutableInterlocked.InterlockedCompareExchange(ref _lazyCustomModifiers, customModifiersArray, default(ImmutableArray<CustomModifier>));
                Interlocked.CompareExchange(ref _lazyType, type, null);
            }
        }

        private bool IsFixedBuffer(out int fixedSize, out TypeSymbol fixedElementType)
        {
            fixedSize = 0;
            fixedElementType = null;

            string elementTypeName;
            int bufferSize;
            PEModuleSymbol containingPEModule = this.ContainingPEModule;
            if (containingPEModule.Module.HasFixedBufferAttribute(_handle, out elementTypeName, out bufferSize))
            {
                var decoder = new MetadataDecoder(containingPEModule);
                var elementType = decoder.GetTypeSymbolForSerializedType(elementTypeName);
                if (elementType.FixedBufferElementSizeInBytes() != 0)
                {
                    fixedSize = bufferSize;
                    fixedElementType = elementType;
                    return true;
                }
            }

            return false;
        }

        private PEModuleSymbol ContainingPEModule
        {
            get
            {
                return ((PENamespaceSymbol)ContainingNamespace).ContainingPEModule;
            }
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            EnsureSignatureIsLoaded();
            return _lazyType;
        }

        public override bool IsFixed
        {
            get
            {
                EnsureSignatureIsLoaded();
                return (object)_lazyFixedImplementationType != null;
            }
        }

        public override int FixedSize
        {
            get
            {
                EnsureSignatureIsLoaded();
                return _lazyFixedSize;
            }
        }

        internal override NamedTypeSymbol FixedImplementationType(PEModuleBuilder emitModule)
        {
            EnsureSignatureIsLoaded();
            return _lazyFixedImplementationType;
        }

        public override ImmutableArray<CustomModifier> CustomModifiers
        {
            get
            {
                EnsureSignatureIsLoaded();
                return _lazyCustomModifiers;
            }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                return null; // _associatedEventOpt;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return (_flags & FieldAttributes.InitOnly) != 0;
            }
        }

        public override bool IsVolatile
        {
            get
            {
                EnsureSignatureIsLoaded();
                return _lazyIsVolatile;
            }
        }

        public override bool IsPhpHidden
        {
            get
            {
                const byte IsPhpHiddenFlag = 2;
                if (_lazyIsPhpHidden == 0)
                {
                    _lazyIsPhpHidden |= Peachpie.CodeAnalysis.Symbols.AttributeHelpers.HasPhpHiddenAttribute(Handle, (PEModuleSymbol)ContainingModule) ? IsPhpHiddenFlag : (byte)0;
                    _lazyIsPhpHidden |= 1;
                }
                return (_lazyIsPhpHidden & IsPhpHiddenFlag) != 0;
            }
        }

        public override bool IsConst
        {
            get
            {
                return (_flags & FieldAttributes.Literal) != 0 || GetConstantValue(/*ConstantFieldsInProgress.Empty, */earlyDecodingWellKnownAttributes: false) != null;
            }
        }

        internal override ConstantValue GetConstantValue(/*ConstantFieldsInProgress inProgress, */bool earlyDecodingWellKnownAttributes)
        {
            if (_lazyConstantValue == Microsoft.CodeAnalysis.ConstantValue.Unset)
            {
                ConstantValue value = null;

                if ((_flags & FieldAttributes.Literal) != 0)
                {
                    value = _containingType.ContainingPEModule.Module.GetConstantFieldValue(_handle);
                }

                // If this is a Decimal, the constant value may come from DecimalConstantAttribute

                if (this.Type.SpecialType == SpecialType.System_Decimal)
                {
                    ConstantValue defaultValue;

                    if (_containingType.ContainingPEModule.Module.HasDecimalConstantAttribute(Handle, out defaultValue))
                    {
                        value = defaultValue;
                    }
                }

                Interlocked.CompareExchange(
                    ref _lazyConstantValue,
                    value,
                    Microsoft.CodeAnalysis.ConstantValue.Unset);
            }

            return _lazyConstantValue;
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                //return _containingType.ContainingPEModule.MetadataLocation.Cast<MetadataLocation, Location>();
                throw new NotImplementedException();
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                var access = Accessibility.Private;

                switch (_flags & FieldAttributes.FieldAccessMask)
                {
                    case FieldAttributes.Assembly:
                        access = Accessibility.Internal;
                        break;

                    case FieldAttributes.FamORAssem:
                        access = Accessibility.ProtectedOrInternal;
                        break;

                    case FieldAttributes.FamANDAssem:
                        access = Accessibility.ProtectedAndInternal;
                        break;

                    case FieldAttributes.Private:
                    case FieldAttributes.PrivateScope:
                        access = Accessibility.Private;
                        break;

                    case FieldAttributes.Public:
                        access = Accessibility.Public;
                        break;

                    case FieldAttributes.Family:
                        access = Accessibility.Protected;
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(_flags & FieldAttributes.FieldAccessMask);
                }

                return access;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return (_flags & FieldAttributes.Static) != 0;
            }
        }

        public override ImmutableArray<AttributeData> GetAttributes()
        {
            if (_lazyCustomAttributes.IsDefault)
            {
                var containingPEModuleSymbol = (PEModuleSymbol)this.ContainingModule;

                if (FilterOutDecimalConstantAttribute())
                {
                    // filter out DecimalConstantAttribute
                    var attributes = containingPEModuleSymbol.GetCustomAttributesForToken(
                        _handle,
                        out _,
                        AttributeDescription.DecimalConstantAttribute,
                        out _,
                        default(AttributeDescription));

                    ImmutableInterlocked.InterlockedInitialize(ref _lazyCustomAttributes, attributes);
                }
                else
                {
                    containingPEModuleSymbol.LoadCustomAttributes(_handle, ref _lazyCustomAttributes);
                }
            }
            return _lazyCustomAttributes;
        }

        private bool FilterOutDecimalConstantAttribute()
        {
            ConstantValue value;
            return this.Type.SpecialType == SpecialType.System_Decimal &&
                   (object)(value = GetConstantValue(/*ConstantFieldsInProgress.Empty, */earlyDecodingWellKnownAttributes: false)) != null &&
                   value.Discriminator == ConstantValueTypeDiscriminator.Decimal;
        }

        internal override IEnumerable<AttributeData> GetCustomAttributesToEmit(CommonModuleCompilationState compilationState)
        {
            foreach (AttributeData attribute in GetAttributes())
            {
                yield return attribute;
            }

            //// Yield hidden attributes last, order might be important.
            //if (FilterOutDecimalConstantAttribute())
            //{
            //    var containingPEModuleSymbol = _containingType.ContainingPEModule;
            //    yield return new PEAttributeData(containingPEModuleSymbol,
            //                              containingPEModuleSymbol.Module.FindLastTargetAttribute(_handle, AttributeDescription.DecimalConstantAttribute).Handle);
            //}
        }

        //public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        //{
        //    return PEDocumentationCommentUtils.GetDocumentationComment(this, _containingType.ContainingPEModule, preferredCulture, cancellationToken, ref _lazyDocComment);
        //}

        //internal override DiagnosticInfo GetUseSiteDiagnostic()
        //{
        //    if (ReferenceEquals(_lazyUseSiteDiagnostic, CSDiagnosticInfo.EmptyErrorInfo))
        //    {
        //        DiagnosticInfo result = null;
        //        CalculateUseSiteDiagnostic(ref result);
        //        _lazyUseSiteDiagnostic = result;
        //    }

        //    return _lazyUseSiteDiagnostic;
        //}

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                //ObsoleteAttributeHelpers.InitializeObsoleteDataFromMetadata(ref _lazyObsoleteAttributeData, _handle, (PEModuleSymbol)(this.ContainingModule));
                //return _lazyObsoleteAttributeData;
                return null;
            }
        }

        internal override PhpCompilation DeclaringCompilation => null;
    }
}
