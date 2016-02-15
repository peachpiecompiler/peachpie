// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis.Emit
{
    internal sealed class PEAssemblyBuilder : PEModuleBuilder, Cci.IAssembly
    {
        public PEAssemblyBuilder(
            PhpCompilation compilation,
            IModuleSymbol sourceModule,
            Cci.ModulePropertiesForSerialization serializationProperties,
            IEnumerable<ResourceDescription> manifestResources,
            OutputKind outputKind,
            EmitOptions emitOptions)
            :base(compilation, sourceModule, serializationProperties, manifestResources, outputKind, emitOptions)
        {

        }

        public AssemblyContentType ContentType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string Culture
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public uint Flags
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public AssemblyHashAlgorithm HashAlgorithm
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsRetargetable
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ImmutableArray<byte> PublicKey
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ImmutableArray<byte> PublicKeyToken
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string SignatureKey
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Version Version
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string GetDisplayName()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Cci.IFileReference> GetFiles(EmitContext context)
        {
            throw new NotImplementedException();
        }
    }
}
