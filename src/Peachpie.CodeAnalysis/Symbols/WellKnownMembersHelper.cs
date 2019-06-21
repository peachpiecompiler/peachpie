using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    static class WellKnownMembersHelper
    {
        public static AttributeData CreateCompilerGeneratedAttribute(this PhpCompilation compilation)
        {
            // [CompilerGenerated]
            var compilergenerated = (MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor);

            return new SynthesizedAttributeData(
                compilergenerated,
                ImmutableArray<TypedConstant>.Empty,
                ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
        }

        public static AttributeData CreateObsoleteAttribute(this PhpCompilation compilation, PHPDocBlock.DeprecatedTag deprecated)
        {
            if (deprecated == null)
                throw new ArgumentNullException(nameof(deprecated));

            // [ObsoleteAttribute(message, false)]
            return new SynthesizedAttributeData(
                (MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_ObsoleteAttribute__ctor),
                    ImmutableArray.Create(
                        new TypedConstant(compilation.CoreTypes.String.Symbol, TypedConstantKind.Primitive, deprecated.Version/*NOTE:Version contains the message*/),
                        new TypedConstant(compilation.CoreTypes.Boolean.Symbol, TypedConstantKind.Primitive, false/*isError*/)),
                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
        }
    }
}
