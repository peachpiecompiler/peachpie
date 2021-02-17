using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Symbols;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis.Emit
{
    internal sealed class PENetModuleBuilder : PEModuleBuilder
    {
        internal PENetModuleBuilder(
            PhpCompilation compilation,
            IModuleSymbol sourceModule,
            EmitOptions emitOptions,
            Cci.ModulePropertiesForSerialization serializationProperties,
            IEnumerable<ResourceDescription> manifestResources)
            : base(compilation, (Symbols.SourceModuleSymbol)sourceModule, serializationProperties, manifestResources, OutputKind.NetModule, emitOptions)
        {
        }

        public override IEnumerable<Cci.IFileReference> GetFiles(EmitContext context) => SpecializedCollections.EmptyEnumerable<Cci.IFileReference>();

        public override ISourceAssemblySymbolInternal SourceAssemblyOpt => null;
    }
}
