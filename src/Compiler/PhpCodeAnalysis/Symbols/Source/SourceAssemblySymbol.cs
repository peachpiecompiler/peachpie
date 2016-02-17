using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using System.Diagnostics;
using System.Reflection;

namespace Pchp.CodeAnalysis.Symbols
{
    internal sealed class SourceAssemblySymbol : AssemblySymbol
    {
        readonly string _simpleName;
        readonly PhpCompilation _compilation;
        readonly AssemblySymbol _corLibraryOpt;

        /// <summary>
        /// A list of modules the assembly consists of. 
        /// The first (index=0) module is a SourceModuleSymbol, which is a primary module, the rest are net-modules.
        /// </summary>
        readonly ImmutableArray<IModuleSymbol> _modules;

        AssemblyIdentity _lazyIdentity;

        public SourceAssemblySymbol(
            PhpCompilation compilation,
            AssemblySymbol corLibraryOpt,
            string assemblySimpleName,
            string moduleName)
        {
            Debug.Assert(compilation != null);
            Debug.Assert(!String.IsNullOrWhiteSpace(assemblySimpleName));
            Debug.Assert(!String.IsNullOrWhiteSpace(moduleName));
            
            _compilation = compilation;
            _simpleName = assemblySimpleName;
            _corLibraryOpt = corLibraryOpt;

            var moduleBuilder = new ArrayBuilder<IModuleSymbol>(1);

            moduleBuilder.Add(new SourceModuleSymbol(this, compilation.SourceSymbolTables, moduleName));

            //var importOptions = (compilation.Options.MetadataImportOptions == MetadataImportOptions.All) ?
            //    MetadataImportOptions.All : MetadataImportOptions.Internal;

            //foreach (PEModule netModule in netModules)
            //{
            //    moduleBuilder.Add(new PEModuleSymbol(this, netModule, importOptions, moduleBuilder.Count));
            //    // SetReferences will be called later by the ReferenceManager (in CreateSourceAssemblyFullBind for 
            //    // a fresh manager, in CreateSourceAssemblyReuseData for a reused one).
            //}

            _modules = moduleBuilder.ToImmutableAndFree();
        }

        public override AssemblySymbol CorLibrary => _corLibraryOpt;

        internal SourceModuleSymbol SourceModule => (SourceModuleSymbol)_modules[0];

        public override ImmutableArray<IModuleSymbol> Modules => _modules;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override PhpCompilation DeclaringCompilation => _compilation;

        public override INamespaceSymbol GlobalNamespace
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override AssemblyIdentity Identity => _lazyIdentity ?? (_lazyIdentity = ComputeIdentity());
        
        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override ImmutableArray<byte> PublicKey
        {
            get { return _compilation.StrongNameKeys.PublicKey; }
        }

        internal string SignatureKey
        {
            get
            {
                string key = null; // GetWellKnownAttributeDataStringField(data => data.AssemblySignatureKeyAttributeSetting);
                return key;
            }
        }

        /// <summary>
        /// This represents what the user claimed in source through the AssemblyFlagsAttribute.
        /// It may be modified as emitted due to presence or absence of the public key.
        /// </summary>
        internal AssemblyNameFlags Flags
        {
            get
            {
                var fieldValue = default(AssemblyNameFlags);

                //var data = GetSourceDecodedWellKnownAttributeData();
                //if (data != null)
                //{
                //    fieldValue = data.AssemblyFlagsAttributeSetting;
                //}

                //data = GetNetModuleDecodedWellKnownAttributeData();
                //if (data != null)
                //{
                //    fieldValue |= data.AssemblyFlagsAttributeSetting;
                //}

                return fieldValue;
            }
        }

        Version AssemblyVersionAttributeSetting => new Version(1, 0, 0, 0);

        string AssemblyCultureAttributeSetting => null;

        public AssemblyHashAlgorithm HashAlgorithm => AssemblyHashAlgorithm.Sha1;

        AssemblyIdentity ComputeIdentity()
        {
            return new AssemblyIdentity(
                _simpleName,
                this.AssemblyVersionAttributeSetting,
                this.AssemblyCultureAttributeSetting,
                _compilation.StrongNameKeys.PublicKey,
                hasPublicKey: !_compilation.StrongNameKeys.PublicKey.IsDefault);
        }
    }
}
