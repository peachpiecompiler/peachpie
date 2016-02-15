// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            : base(compilation, sourceModule, serializationProperties, manifestResources, OutputKind.NetModule, emitOptions)
        {
        }
    }
}
