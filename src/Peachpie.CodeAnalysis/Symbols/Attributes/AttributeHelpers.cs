using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols;

namespace Peachpie.CodeAnalysis.Symbols
{
    static class AttributeHelpers
    {
        private static readonly byte[] s_signature_HasThis_Void = new byte[] { (byte)SignatureAttributes.Instance, 0, (byte)SignatureTypeCode.Void };

        public static readonly AttributeDescription PhpTraitAttribute = new AttributeDescription(CoreTypes.PeachpieRuntimeNamespace, CoreTypes.PhpTraitAttributeName, new[] { s_signature_HasThis_Void });

        public static readonly AttributeDescription PhpRwAttribute = new AttributeDescription(CoreTypes.PeachpieRuntimeNamespace, "PhpRwAttribute", new[] { s_signature_HasThis_Void });

        public static readonly AttributeDescription PhpHiddenAttribute = new AttributeDescription(CoreTypes.PeachpieRuntimeNamespace, "PhpHiddenAttribute", new[] { s_signature_HasThis_Void });

        public static readonly AttributeDescription PhpFieldsOnlyCtorAttribute = new AttributeDescription(CoreTypes.PeachpieRuntimeNamespace, CoreTypes.PhpFieldsOnlyCtorAttributeName, new[] { s_signature_HasThis_Void });

        public static readonly AttributeDescription CastToFalse = new AttributeDescription(CoreTypes.PeachpieRuntimeNamespace, "CastToFalse", new[] { s_signature_HasThis_Void });

        public static bool HasPhpTraitAttribute(EntityHandle token, PEModuleSymbol containingModule)
        {
            return PEModule.FindTargetAttribute(containingModule.Module.MetadataReader, token, PhpTraitAttribute).HasValue;
        }

        public static bool HasCastToFalse(EntityHandle token, PEModuleSymbol containingModule)
        {
            return containingModule != null && PEModule.FindTargetAttribute(containingModule.Module.MetadataReader, token, CastToFalse).HasValue;
        }

        public static bool HasPhpHiddenAttribute(EntityHandle token, PEModuleSymbol containingModule)
        {
            return containingModule != null && PEModule.FindTargetAttribute(containingModule.Module.MetadataReader, token, PhpHiddenAttribute).HasValue;
        }

        public static bool HasPhpFieldsOnlyCtorAttribute(EntityHandle token, PEModuleSymbol containingModule)
        {
            return containingModule != null && PEModule.FindTargetAttribute(containingModule.Module.MetadataReader, token, PhpFieldsOnlyCtorAttribute).HasValue;
        }

        static bool ReadCustomAttributeValue(CustomAttributeHandle handle, PEModule module, out int value)
        {
            // PEModule.cs

            var valueBlob = module.GetCustomAttributeValueOrThrow(handle);
            if (!valueBlob.IsNil)
            {
                // TODO: error checking offset in range
                var reader = module.MetadataReader.GetBlobReader(valueBlob);

                if (reader.Length > 4)
                {
                    // check prolog
                    if (reader.ReadByte() == 1 && reader.ReadByte() == 0)
                    {
                        // read Int32
                        if (reader.RemainingBytes >= 4)
                        {
                            value = reader.ReadInt32();
                            return true;
                        }
                    }
                }
            }

            value = default;
            return false;
        }

        public static ImportValueAttributeData HasImportValueAttribute(EntityHandle token, PEModuleSymbol containingModule)
        {
            var metadataReader = containingModule.Module.MetadataReader;
            foreach (var attr in metadataReader.GetCustomAttributes(token))
            {
                if (containingModule.Module.IsTargetAttribute(attr, CoreTypes.PeachpieRuntimeNamespace, "ImportValueAttribute", out _))
                {
                    // [ImportValue(Int32)]
                    if (ReadCustomAttributeValue(attr, containingModule.Module, out var valuespec))
                    {
                        Debug.Assert(valuespec != 0);
                        return new ImportValueAttributeData { Value = (ImportValueAttributeData.ValueSpec)valuespec };
                    }
                }
            }

            //
            return default;
        }

        /// <summary>
        /// Looks for <c>Peachpie.Runtime</c>'s <c>DefaultValueAttribute</c>.
        /// </summary>
        public static bool HasDefaultValueAttributeData(EntityHandle token, PEModuleSymbol containingModule)
        {
            try
            {
                var metadataReader = containingModule.Module.MetadataReader;
                foreach (var attr in metadataReader.GetCustomAttributes(token))
                {
                    if (containingModule.Module.IsTargetAttribute(attr, CoreTypes.PeachpieRuntimeNamespace, "DefaultValueAttribute", out _))
                    {
                        return true;
                    }
                }
            }
            catch (BadImageFormatException)
            { }

            //
            return false;
        }

        public static bool IsNotNullable(Symbol symbol, EntityHandle token, PEModuleSymbol containingModule)
        {
            // NOTE: This code must be kept in sync with the behaviour of ReflectionUtils.IsNullable in the runtime

            // C# basic 8.0 Nullability check
            bool isNotNullable;
            if (containingModule != null && containingModule.Module.HasNullableAttribute(token, out byte attrArg, out var attrArgs))
            {
                // Directly annotated [Nullable(x)] or [Nullable(new byte[]{x, y, z})]
                isNotNullable =
                    attrArgs.IsDefault
                    ? attrArg == NullableContextUtils.NotAnnotatedAttributeValue
                    : attrArgs[0] == NullableContextUtils.NotAnnotatedAttributeValue;   // For generics and arrays, the first byte represents the type itself
            }
            else
            {
                // Recursively look in containing symbols for [NullableContext(x)], which specifies the default value
                isNotNullable = symbol.GetNullableContextValue() == NullableContextUtils.NotAnnotatedAttributeValue;
            }

            // Special C# 8.0 attributes for flow static analysis
            if (DecodeFlowAnalysisAttributes(containingModule.Module, token) is var flowAnnotations && flowAnnotations != FlowAnalysisAnnotations.None)
            {
                // Specified attributes can override nullability in both directions
                isNotNullable =
                    isNotNullable
                    ? (flowAnnotations & (FlowAnalysisAnnotations.AllowNull | FlowAnalysisAnnotations.MaybeNull)) == 0
                    : ((flowAnnotations & FlowAnalysisAnnotations.DisallowNull) != 0 || (flowAnnotations & FlowAnalysisAnnotations.NotNull) == FlowAnalysisAnnotations.NotNull);
            }

            return isNotNullable;
        }

        private static FlowAnalysisAnnotations DecodeFlowAnalysisAttributes(PEModule module, EntityHandle handle)
        {
            // NOTE: This code must be kept in sync with the behaviour of ReflectionUtils.DecodeFlowAnalysisAttributes in the runtime

            FlowAnalysisAnnotations annotations = FlowAnalysisAnnotations.None;

            if (handle.IsNil)
            {
                return annotations;
            }

            if (module.HasAttribute(handle, AttributeDescription.AllowNullAttribute)) annotations |= FlowAnalysisAnnotations.AllowNull;
            if (module.HasAttribute(handle, AttributeDescription.DisallowNullAttribute)) annotations |= FlowAnalysisAnnotations.DisallowNull;

            if (module.HasAttribute(handle, AttributeDescription.MaybeNullAttribute))
            {
                annotations |= FlowAnalysisAnnotations.MaybeNull;
            }
            else if (module.HasMaybeNullWhenOrNotNullWhenOrDoesNotReturnIfAttribute(handle, AttributeDescription.MaybeNullWhenAttribute, out bool when))
            {
                annotations |= (when ? FlowAnalysisAnnotations.MaybeNullWhenTrue : FlowAnalysisAnnotations.MaybeNullWhenFalse);
            }

            if (module.HasAttribute(handle, AttributeDescription.NotNullAttribute))
            {
                annotations |= FlowAnalysisAnnotations.NotNull;
            }
            //else if (module.HasMaybeNullWhenOrNotNullWhenOrDoesNotReturnIfAttribute(handle, AttributeDescription.NotNullWhenAttribute, out bool when))
            //{
            //    annotations |= (when ? FlowAnalysisAnnotations.NotNullWhenTrue : FlowAnalysisAnnotations.NotNullWhenFalse);
            //}

            //if (module.HasMaybeNullWhenOrNotNullWhenOrDoesNotReturnIfAttribute(handle, AttributeDescription.DoesNotReturnIfAttribute, out bool condition))
            //{
            //    annotations |= (condition ? FlowAnalysisAnnotations.DoesNotReturnIfTrue : FlowAnalysisAnnotations.DoesNotReturnIfFalse);
            //}

            // NOTE: Uncomment the code above if we decide to use these attributes for our flow analysis as well

            return annotations;
        }

        public static bool HasPhpRwAttribute(EntityHandle token, PEModuleSymbol containingModule)
        {
            return containingModule != null && PEModule.FindTargetAttribute(containingModule.Module.MetadataReader, token, PhpRwAttribute).HasValue;
        }
    }

    struct ImportValueAttributeData
    {
        /// <summary>
        /// Value to be imported.
        /// From `Pchp.Core.ImportValueAttribute+ValueSpec`.
        /// </summary>
        public enum ValueSpec
        {
            Default = 0,

            /// <summary>
            /// Current class context.
            /// The parameter must be of type <see cref="RuntimeTypeHandle"/>, <c>PhpTypeInfo</c> or <see cref="string"/>.
            /// </summary>
            CallerClass,

            /// <summary>
            /// Current late static bound class (<c>static</c>).
            /// The parameter must be of type <c>PhpTypeInfo</c>.
            /// </summary>
            CallerStaticClass,

            /// <summary>
            /// Calue of <c>$this</c> variable or <c>null</c> if variable is not defined.
            /// The parameter must be of type <see cref="object"/>.
            /// </summary>
            This,

            /// <summary>
            /// Provides a reference to the array of local PHP variables.
            /// The parameter must be of type <c>PhpArray</c>.
            /// </summary>
            Locals,

            /// <summary>
            /// Provides callers parameters.
            /// The parameter must be of type array of <c>PhpValue</c>.
            /// </summary>
            CallerArgs,

            /// <summary>
            /// Provides reference to the current script container.
            /// The parameter must be of type <see cref="RuntimeTypeHandle"/>.
            /// </summary>
            CallerScript,

            /// <summary>
            /// Provides reference to local variable with same name as the parameter name.
            /// </summary>
            LocalVariable,
        }

        public bool IsDefault => Value == ValueSpec.Default;

        public bool IsValid => Value != ValueSpec.Default && Value != (ValueSpec)(-1);

        public static ImportValueAttributeData Invalid => new ImportValueAttributeData { Value = (ValueSpec)(-1) };

        public ValueSpec Value;
    }
}
