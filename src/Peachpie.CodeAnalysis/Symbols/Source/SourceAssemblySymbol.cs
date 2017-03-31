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
using Roslyn.Utilities;

namespace Pchp.CodeAnalysis.Symbols
{
    internal sealed class SourceAssemblySymbol : NonMissingAssemblySymbol
    {
        readonly string _simpleName;
        readonly PhpCompilation _compilation;
        
        /// <summary>
        /// A list of modules the assembly consists of. 
        /// The first (index=0) module is a SourceModuleSymbol, which is a primary module, the rest are net-modules.
        /// </summary>
        readonly ImmutableArray<ModuleSymbol> _modules;

        AssemblyIdentity _lazyIdentity;

        public SourceAssemblySymbol(
            PhpCompilation compilation,
            string assemblySimpleName,
            string moduleName)
        {
            Debug.Assert(compilation != null);
            Debug.Assert(!String.IsNullOrWhiteSpace(assemblySimpleName));
            Debug.Assert(!String.IsNullOrWhiteSpace(moduleName));
            
            _compilation = compilation;
            _simpleName = assemblySimpleName;
            
            var moduleBuilder = new ArrayBuilder<ModuleSymbol>(1);

            moduleBuilder.Add(new SourceModuleSymbol(this, compilation.SourceSymbolCollection, moduleName));

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

        public override string Name => _simpleName;

        internal SourceModuleSymbol SourceModule => (SourceModuleSymbol)_modules[0];

        public override ImmutableArray<ModuleSymbol> Modules => _modules;

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
                return SourceModule.GlobalNamespace;
            }
        }

        internal override bool IsLinked => false;

        public override AssemblyIdentity Identity => _lazyIdentity ?? (_lazyIdentity = ComputeIdentity());

        public override Version AssemblyVersionPattern => null; // TODO: Version attribute

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

        internal override NamedTypeSymbol TryLookupForwardedMetadataTypeWithCycleDetection(ref MetadataTypeName emittedName, ConsList<AssemblySymbol> visitedAssemblies)
        {
            int forcedArity = emittedName.ForcedArity;

            if (emittedName.UseCLSCompliantNameArityEncoding)
            {
                if (forcedArity == -1)
                {
                    forcedArity = emittedName.InferredArity;
                }
                else if (forcedArity != emittedName.InferredArity)
                {
                    return null;
                }

                Debug.Assert(forcedArity == emittedName.InferredArity);
            }

            //if (_lazyForwardedTypesFromSource == null)
            //{
            //    IDictionary<string, NamedTypeSymbol> forwardedTypesFromSource;
            //    CommonAssemblyWellKnownAttributeData<NamedTypeSymbol> wellKnownAttributeData = GetSourceDecodedWellKnownAttributeData();

            //    if (wellKnownAttributeData != null && wellKnownAttributeData.ForwardedTypes != null)
            //    {
            //        forwardedTypesFromSource = new Dictionary<string, NamedTypeSymbol>();

            //        foreach (NamedTypeSymbol forwardedType in wellKnownAttributeData.ForwardedTypes)
            //        {
            //            NamedTypeSymbol originalDefinition = forwardedType.OriginalDefinition;
            //            Debug.Assert((object)originalDefinition.ContainingType == null, "How did a nested type get forwarded?");

            //            string fullEmittedName = MetadataHelpers.BuildQualifiedName(originalDefinition.ContainingSymbol.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat),
            //                                                                        originalDefinition.MetadataName);
            //            // Since we need to allow multiple constructions of the same generic type at the source
            //            // level, we need to de-dup the original definitions.
            //            forwardedTypesFromSource[fullEmittedName] = originalDefinition;
            //        }
            //    }
            //    else
            //    {
            //        forwardedTypesFromSource = SpecializedCollections.EmptyDictionary<string, NamedTypeSymbol>();
            //    }

            //    _lazyForwardedTypesFromSource = forwardedTypesFromSource;
            //}

            //NamedTypeSymbol result;

            //if (_lazyForwardedTypesFromSource.TryGetValue(emittedName.FullName, out result))
            //{
            //    if ((forcedArity == -1 || result.Arity == forcedArity) &&
            //        (!emittedName.UseCLSCompliantNameArityEncoding || result.Arity == 0 || result.MangleName))
            //    {
            //        return result;
            //    }
            //}
            //else if (!_compilation.Options.OutputKind.IsNetModule())
            //{
            //    // See if any of added modules forward the type.

            //    // Similar to attributes, type forwarders from the second added module should override type forwarders from the first added module, etc. 
            //    for (int i = _modules.Length - 1; i > 0; i--)
            //    {
            //        var peModuleSymbol = (Metadata.PE.PEModuleSymbol)_modules[i];

            //        var forwardedToAssembly = peModuleSymbol.GetAssemblyForForwardedType(ref emittedName);
            //        if ((object)forwardedToAssembly != null)
            //        {
            //            // Don't bother to check the forwarded-to assembly if we've already seen it.
            //            if (visitedAssemblies != null && visitedAssemblies.Contains(forwardedToAssembly))
            //            {
            //                return CreateCycleInTypeForwarderErrorTypeSymbol(ref emittedName);
            //            }
            //            else
            //            {
            //                visitedAssemblies = new ConsList<AssemblySymbol>(this, visitedAssemblies ?? ConsList<AssemblySymbol>.Empty);
            //                return forwardedToAssembly.LookupTopLevelMetadataTypeWithCycleDetection(ref emittedName, visitedAssemblies, digThroughForwardedTypes: true);
            //            }
            //        }
            //    }
            //}

            return null;
        }

        public override NamedTypeSymbol GetTypeByMetadataName(string fullyQualifiedMetadataName)
        {
            return SourceModule.SymbolCollection.GetType(NameUtils.MakeQualifiedName(fullyQualifiedMetadataName.Replace('.', Devsense.PHP.Syntax.QualifiedName.Separator), true));
        }

        internal override ImmutableArray<AssemblySymbol> GetLinkedReferencedAssemblies()
        {
            return ImmutableArray<AssemblySymbol>.Empty;
        }

        internal override void SetLinkedReferencedAssemblies(ImmutableArray<AssemblySymbol> assemblies)
        {
            throw ExceptionUtilities.Unreachable;
        }
    }
}
