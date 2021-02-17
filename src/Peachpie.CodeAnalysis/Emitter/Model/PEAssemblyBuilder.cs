using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Symbols;
using Cci = Microsoft.Cci;
using Pchp.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.Emit
{
    internal sealed class PEAssemblyBuilder : PEModuleBuilder, Cci.IAssemblyReference
    {
        readonly SourceAssemblySymbol _sourceAssembly;
        ImmutableArray<Cci.IFileReference> _lazyFiles;

        /// <summary>
        /// The behavior of the C# command-line compiler is as follows:
        ///   1) If the /out switch is specified, then the explicit assembly name is used.
        ///   2) Otherwise,
        ///      a) if the assembly is executable, then the assembly name is derived from
        ///         the name of the file containing the entrypoint;
        ///      b) otherwise, the assembly name is derived from the name of the first input
        ///         file.
        /// 
        /// Since we don't know which method is the entrypoint until well after the
        /// SourceAssemblySymbol is created, in case 2a, its name will not reflect the
        /// name of the file containing the entrypoint.  We leave it to our caller to
        /// provide that name explicitly.
        /// </summary>
        /// <remarks>
        /// In cases 1 and 2b, we expect (metadataName == sourceAssembly.MetadataName).
        /// </remarks>
        readonly string _metadataName;

        public PEAssemblyBuilder(
            SourceAssemblySymbol sourceAssembly,
            Cci.ModulePropertiesForSerialization serializationProperties,
            IEnumerable<ResourceDescription> manifestResources,
            OutputKind outputKind,
            EmitOptions emitOptions)
            :base(sourceAssembly.DeclaringCompilation, (SourceModuleSymbol)sourceAssembly.Modules[0], serializationProperties, manifestResources, outputKind, emitOptions)
        {
            _sourceAssembly = sourceAssembly;
            _metadataName = (emitOptions.OutputNameOverride == null) ? sourceAssembly.MetadataName : FileNameUtilities.ChangeExtension(emitOptions.OutputNameOverride, extension: null);

            AssemblyOrModuleSymbolToModuleRefMap.Add(sourceAssembly, this);
        }

        public AssemblyContentType ContentType => _sourceAssembly.Identity.ContentType;

        public string Culture => _sourceAssembly.Identity.CultureName;

        public uint Flags
        {
            get
            {
                AssemblyNameFlags result = _sourceAssembly.Flags & ~AssemblyNameFlags.PublicKey;

                if (!this.PublicKey.IsDefaultOrEmpty)
                    result |= AssemblyNameFlags.PublicKey;

                return (uint)result;
            }
        }

        public AssemblyHashAlgorithm HashAlgorithm => _sourceAssembly.HashAlgorithm;

        public bool IsRetargetable => _sourceAssembly.Identity.IsRetargetable;

        public ImmutableArray<byte> PublicKey => _sourceAssembly.Identity.PublicKey;

        public ImmutableArray<byte> PublicKeyToken => _sourceAssembly.Identity.PublicKeyToken;

        public string SignatureKey => _sourceAssembly.SignatureKey;

        public Version Version => _sourceAssembly.Identity.Version;

        public string GetDisplayName() => _sourceAssembly.Identity.GetDisplayName();

        public override string Name => _metadataName;

        public AssemblyIdentity Identity => _sourceAssembly.Identity;

        public Version AssemblyVersionPattern => _sourceAssembly.AssemblyVersionPattern;

        public override ISourceAssemblySymbolInternal SourceAssemblyOpt => _sourceAssembly;

        /// <summary>
        /// A list of the files that constitute the assembly. These are not the source language files that may have been
        /// used to compile the assembly, but the files that contain constituent modules of a multi-module assembly as well
        /// as any external resources. It corresponds to the File table of the .NET assembly file format.
        /// </summary>
        public override IEnumerable<Cci.IFileReference> GetFiles(EmitContext context)
        {
            if (_lazyFiles.IsDefault)
            {
                var builder = ArrayBuilder<Cci.IFileReference>.GetInstance();
                try
                {
                    var modules = _sourceAssembly.Modules;
                    for (int i = 1; i < modules.Length; i++)
                    {
                        builder.Add((Cci.IFileReference)Translate(modules[i] as IAssemblySymbolInternal, context.Diagnostics));
                    }

                    foreach (ResourceDescription resource in ManifestResources)
                    {
                        if (!resource.IsEmbedded)
                        {
                            builder.Add(resource);
                        }
                    }

                    // Dev12 compilers don't report ERR_CryptoHashFailed if there are no files to be hashed.
                    if (ImmutableInterlocked.InterlockedInitialize(ref _lazyFiles, builder.ToImmutable()) && _lazyFiles.Length > 0)
                    {
                        //if (!CryptographicHashProvider.IsSupportedAlgorithm(_sourceAssembly.AssemblyHashAlgorithm))
                        //{
                        //    context.Diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_CryptoHashFailed), NoLocation.Singleton));
                        //}
                    }
                }
                finally
                {
                    builder.Free();
                }
            }

            return _lazyFiles;
        }

        protected override void AddEmbeddedResourcesFromAddedModules(ArrayBuilder<Cci.ManagedResource> builder, DiagnosticBag diagnostics)
        {
            var modules = _sourceAssembly.Modules;
            int count = modules.Length;

            for (int i = 1; i < count; i++)
            {
                var file = (Cci.IFileReference)Translate(modules[i] as IAssemblySymbolInternal, diagnostics);

                //try
                //{
                //    foreach (EmbeddedResource resource in ((Symbols.Metadata.PE.PEModuleSymbol)modules[i]).Module.GetEmbeddedResourcesOrThrow())
                //    {
                //        builder.Add(new Cci.ManagedResource(
                //            resource.Name,
                //            (resource.Attributes & ManifestResourceAttributes.Public) != 0,
                //            null,
                //            file,
                //            resource.Offset));
                //    }
                //}
                //catch (BadImageFormatException)
                //{
                //    diagnostics.Add(new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, modules[i]), NoLocation.Singleton);
                //}
            }
        }
    }
}
