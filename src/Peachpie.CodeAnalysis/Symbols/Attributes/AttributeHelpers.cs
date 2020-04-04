using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols;

namespace Peachpie.CodeAnalysis.Symbols
{
    static class AttributeHelpers
    {
        private static readonly byte[] s_signature_HasThis_Void = new byte[] { (byte)SignatureAttributes.Instance, 0, (byte)SignatureTypeCode.Void };

        private static readonly byte[] s_signature_HasThis_Void_Int = new byte[] { (byte)SignatureAttributes.Instance, 1, (byte)SignatureTypeCode.Void, (byte)SignatureTypeCode.Int32 };

        public static readonly AttributeDescription PhpTraitAttribute = new AttributeDescription(CoreTypes.PeachpieRuntimeNamespace, CoreTypes.PhpTraitAttributeName, new[] { s_signature_HasThis_Void });

        public static readonly AttributeDescription NotNullAttribute = new AttributeDescription(CoreTypes.PeachpieRuntimeNamespace, "NotNullAttribute", new[] { s_signature_HasThis_Void });

        public static readonly AttributeDescription PhpRwAttribute = new AttributeDescription(CoreTypes.PeachpieRuntimeNamespace, "PhpRwAttribute", new[] { s_signature_HasThis_Void });

        public static readonly AttributeDescription CastToFalse = new AttributeDescription(CoreTypes.PeachpieRuntimeNamespace, "CastToFalse", new[] { s_signature_HasThis_Void });

        public static readonly AttributeDescription ImportValueAttribute = new AttributeDescription(CoreTypes.PeachpieRuntimeNamespace, "ImportValueAttribute", new[] { s_signature_HasThis_Void_Int });

        public static bool HasPhpTraitAttribute(EntityHandle token, PEModuleSymbol containingModule)
        {
            return PEModule.FindTargetAttribute(containingModule.Module.MetadataReader, token, PhpTraitAttribute).HasValue;
        }

        public static bool HasCastToFalse(EntityHandle token, PEModuleSymbol containingModule)
        {
            return containingModule != null && PEModule.FindTargetAttribute(containingModule.Module.MetadataReader, token, CastToFalse).HasValue;
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
            var attr = PEModule.FindTargetAttribute(metadataReader, token, ImportValueAttribute);
            if (attr.HasValue)
            {
                if (ReadCustomAttributeValue(attr.Handle, containingModule.Module, out var valuespec))
                {
                    Debug.Assert(valuespec != 0);
                    return new ImportValueAttributeData { Value = (ImportValueAttributeData.ValueSpec)valuespec };
                }
            }

            //
            return default;
        }

        public static bool HasNotNullAttribute(EntityHandle token, PEModuleSymbol containingModule)
        {
            // TODO: C# 8.0 NotNull

            return containingModule != null && PEModule.FindTargetAttribute(containingModule.Module.MetadataReader, token, NotNullAttribute).HasValue;
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
            /// The parameter must be of type array of <c>PhpTypeInfo</c>.
            /// </summary>
            CallerArgs,

            /// <summary>
            /// Provides reference to the current script container.
            /// The parameter must be of type <see cref="RuntimeTypeHandle"/>.
            /// </summary>
            CallerScript,
        }

        public bool IsDefault => Value == ValueSpec.Default;

        public bool IsValid => Value != ValueSpec.Default && Value != (ValueSpec)(-1);

        public static ImportValueAttributeData Invalid => new ImportValueAttributeData { Value = (ValueSpec)(-1) };

        public ValueSpec Value;
    }
}
