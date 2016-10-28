using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Emit
{
    internal sealed class PENetModuleBuilder : PEModuleBuilder
    {
        internal PENetModuleBuilder(
            PhpCompilation compilation,
            IModuleSymbol sourceModule,
            EmitOptions emitOptions,
            Microsoft.Cci.ModulePropertiesForSerialization serializationProperties,
            IEnumerable<ResourceDescription> manifestResources)
            : base(compilation, (Symbols.SourceModuleSymbol)sourceModule, serializationProperties, manifestResources, OutputKind.NetModule, emitOptions)
        {
        }
    }
}
