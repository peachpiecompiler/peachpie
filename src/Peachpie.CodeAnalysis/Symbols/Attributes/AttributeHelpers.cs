using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols;

namespace Peachpie.CodeAnalysis.Symbols
{
    static class AttributeHelpers
    {
        private static readonly byte[] s_signature_HasThis_Void = new byte[] { (byte)SignatureAttributes.Instance, 0, (byte)SignatureTypeCode.Void };

        public static readonly AttributeDescription PhpTraitAttribute = new AttributeDescription(CoreTypes.PeachpieRuntimeNamespace, CoreTypes.PhpTraitAttributeName, new[] { s_signature_HasThis_Void });

        public static readonly AttributeDescription CastToFalse = new AttributeDescription(CoreTypes.PeachpieRuntimeNamespace, "CastToFalse", new[] { s_signature_HasThis_Void });

        public static bool HasPhpTraitAttribute(EntityHandle token, PEModuleSymbol containingModule)
        {
            return PEModule.FindTargetAttribute(containingModule.Module.MetadataReader, token, PhpTraitAttribute).HasValue;
        }

        public static bool HasCastToFalse(EntityHandle token, PEModuleSymbol containingModule)
        {
            return PEModule.FindTargetAttribute(containingModule.Module.MetadataReader, token, CastToFalse).HasValue;
        }
    }
}
