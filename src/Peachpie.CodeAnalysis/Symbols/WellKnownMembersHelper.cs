using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
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
    }
}
