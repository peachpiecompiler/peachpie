using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Utilities;
using System.Collections.Immutable;
using Pchp.CodeAnalysis.Symbols;
using System.Diagnostics;

namespace Pchp.CodeAnalysis
{
    partial class PhpCompilation
    {
        internal class ReferenceManager : CommonReferenceManager // TODO: inherit the generic version with all the Binding & resolving stuff
        {
            ImmutableArray<MetadataReference> _lazyExplicitReferences;
            ImmutableArray<MetadataReference> _lazyImplicitReferences = ImmutableArray<MetadataReference>.Empty;
            ImmutableDictionary<MetadataReference, IAssemblySymbol> _referencesMap;
            ImmutableDictionary<IAssemblySymbol, MetadataReference> _metadataMap;
            AssemblySymbol _lazyCorLibrary, _lazyPhpCorLibrary;

            public Dictionary<AssemblyIdentity, PEAssemblySymbol> ObservedMetadata => _observedMetadata;
            readonly Dictionary<AssemblyIdentity, PEAssemblySymbol> _observedMetadata;

            public string SimpleAssemblyName => _simpleAssemblyName;
            readonly string _simpleAssemblyName;

            public AssemblyIdentityComparer IdentityComparer => _identityComparer;
            readonly AssemblyIdentityComparer _identityComparer;
            readonly string _sdkdir;

            /// <summary>
            /// Diagnostics produced during reference resolution and binding.
            /// </summary>
            /// <remarks>
            /// When reporting diagnostics be sure not to include any information that can't be shared among 
            /// compilations that share the same reference manager (such as full identity of the compilation, 
            /// simple assembly name is ok).
            /// </remarks>
            private readonly DiagnosticBag _diagnostics = new DiagnosticBag();

            /// <summary>
            /// COR library containing base system types.
            /// </summary>
            internal AssemblySymbol CorLibrary => _lazyCorLibrary;

            /// <summary>
            /// PHP COR library containing PHP runtime.
            /// </summary>
            internal AssemblySymbol PhpCorLibrary => _lazyPhpCorLibrary;

            internal override ImmutableArray<MetadataReference> ExplicitReferences => _lazyExplicitReferences;

            internal override ImmutableArray<MetadataReference> ImplicitReferences => _lazyImplicitReferences;

            internal override IEnumerable<KeyValuePair<AssemblyIdentity, PortableExecutableReference>> GetImplicitlyResolvedAssemblyReferences()
            {
                foreach (var pair in _metadataMap)
                {
                    var per = pair.Value as PortableExecutableReference;
                    if (per != null)
                    {
                        yield return new KeyValuePair<AssemblyIdentity, PortableExecutableReference>(pair.Key.Identity, per);
                    }
                }
            }

            internal override MetadataReference GetMetadataReference(IAssemblySymbol assemblySymbol) => _metadataMap.TryGetOrDefault(assemblySymbol);

            internal override IEnumerable<KeyValuePair<MetadataReference, IAssemblySymbol>> GetReferencedAssemblies() => _referencesMap;

            internal override IEnumerable<ValueTuple<IAssemblySymbol, ImmutableArray<string>>> GetReferencedAssemblyAliases()
            {
                yield break;
            }

            internal IEnumerable<IAssemblySymbol> ExplicitReferencesSymbols => ExplicitReferences.Select(r => _referencesMap[r]).WhereNotNull();

            internal DiagnosticBag Diagnostics => _diagnostics;

            public ReferenceManager(
                string simpleAssemblyName,
                AssemblyIdentityComparer identityComparer,
                Dictionary<AssemblyIdentity, PEAssemblySymbol> observedMetadata,
                string sdkDir)
            {
                _simpleAssemblyName = simpleAssemblyName;
                _identityComparer = identityComparer ?? AssemblyIdentityComparer.Default;
                _sdkdir = sdkDir;
                _observedMetadata = observedMetadata ?? new Dictionary<AssemblyIdentity, PEAssemblySymbol>();
            }

            /// <summary>
            /// Checks the assembly identities are similar - this is a quick workaround to use assemblies as resolved by build system (versions might not match).
            /// </summary>
            static bool IsIdentitySimilar(AssemblyIdentity a, AssemblyIdentity b)
            {
                return a.Name == b.Name && (!a.HasPublicKey || !b.HasPublicKey || a.PublicKey.Equals(b.PublicKey));
            }

            AssemblySymbol CreateAssemblyFromIdentity(MetadataReferenceResolver resolver, AssemblyIdentity identity, string basePath, List<PEModuleSymbol> modules)
            {
                if (!_observedMetadata.TryGetValue(identity, out var ass))
                {
                    // temporary: lookup ignoring version number
                    foreach (var pair in _observedMetadata)
                    {
                        if (IsIdentitySimilar(pair.Key, identity))
                        {
                            _observedMetadata[identity] = pair.Value;   // do not resolve this ever again
                            return pair.Value;
                        }
                    }

                    //foreach (var m in modules)
                    //{
                    //    if (IsIdentitySimilar(m.ContainingAssembly.Identity, identity))
                    //    {
                    //        return m.ContainingAssembly as PEAssemblySymbol;
                    //    }
                    //}

                    //
                    if (resolver != null)
                    {
                        string keytoken = string.Join("", identity.PublicKeyToken.Select(b => b.ToString("x2")));
                        var pes = resolver.ResolveReference(identity.Name + ".dll", basePath, MetadataReferenceProperties.Assembly)
                            .Concat(resolver.ResolveReference($"{identity.Name}/v4.0_{identity.Version}__{keytoken}/{identity.Name}.dll", basePath, MetadataReferenceProperties.Assembly));

                        var pe = pes.FirstOrDefault();
                        if (pe != null)
                        {
                            _observedMetadata[identity] = ass = PEAssemblySymbol.Create(pe, isLinked: false);
                            ass.SetCorLibrary(_lazyCorLibrary);
                            modules.AddRange(ass.Modules.Cast<PEModuleSymbol>());
                        }
                    }

                    if (ass == null)
                    {
                        //_diagnostics.Add(Location.None, Errors.ErrorCode.ERR_MetadataFileNotFound, identity);
                        return new MissingAssemblySymbol(identity);
                    }
                }

                return ass;
            }

            void SetReferencesOfReferencedModules(MetadataReferenceResolver resolver, List<PEModuleSymbol> modules)
            {
                for (int i = 0; i < modules.Count; i++)
                {
                    if (modules[i].HasReferencesSet)
                    {
                        // module is already cached with references set
                        continue;
                    }

                    var refs = modules[i].Module.ReferencedAssemblies;
                    var symbols = new AssemblySymbol[refs.Length];
                    var ass = modules[i].ContainingAssembly;
                    var basePath = PathUtilities.GetDirectoryName((ass as PEAssemblySymbol)?.FilePath);

                    for (int j = 0; j < refs.Length; j++)
                    {
                        var symbol = CreateAssemblyFromIdentity(resolver, refs[j], basePath, modules);
                        symbols[j] = symbol;
                    }

                    //
                    modules[i].SetReferences(new ModuleReferences<AssemblySymbol>(refs, symbols.AsImmutable(), ImmutableArray<UnifiedAssembly<AssemblySymbol>>.Empty));
                }
            }

            internal SourceAssemblySymbol CreateSourceAssemblyForCompilation(PhpCompilation compilation)
            {
                if (compilation._lazyAssemblySymbol != null)
                {
                    return compilation._lazyAssemblySymbol;
                }

                var resolver = compilation.Options.MetadataReferenceResolver;
                var moduleName = compilation.MakeSourceModuleName();

                var assemblies = new List<AssemblySymbol>();

                if (_lazyExplicitReferences.IsDefault)
                {
                    //
                    var externalRefs = compilation.ExternalReferences;
                    var referencesMap = new Dictionary<MetadataReference, IAssemblySymbol>();
                    var metadataMap = new Dictionary<IAssemblySymbol, MetadataReference>();
                    var assembliesMap = new Dictionary<AssemblyIdentity, PEAssemblySymbol>();
                    var observed = new HashSet<AssemblyIdentity>();

                    foreach (PortableExecutableReference pe in externalRefs)
                    {
                        var peass = ((AssemblyMetadata)pe.GetMetadata()).GetAssembly();

                        if (!observed.Add(peass.Identity))
                        {
                            // already added reference identity, different metadata
                            referencesMap[pe] = _observedMetadata[peass.Identity];
                            Debug.Assert(referencesMap[pe] != null);
                            continue;
                        }

                        var symbol = _observedMetadata.TryGetOrDefault(peass.Identity) ?? PEAssemblySymbol.Create(pe, peass, isLinked: true);
                        if (symbol != null)
                        {
                            assemblies.Add(symbol);
                            referencesMap[pe] = symbol;
                            metadataMap[symbol] = pe;

                            if (_lazyCorLibrary == null && symbol.IsCorLibrary)
                                _lazyCorLibrary = symbol;

                            if (_lazyPhpCorLibrary == null && symbol.IsPeachpieCorLibrary)
                                _lazyPhpCorLibrary = symbol;

                            // cache bound assembly symbol
                            _observedMetadata[symbol.Identity] = symbol;
                        }
                        else
                        {
                            _diagnostics.Add(Location.None, Errors.ErrorCode.ERR_MetadataFileNotFound, peass.Identity);
                        }
                    }

                    // list of modules to initialize later
                    var refmodules = assemblies.SelectMany(symbol => symbol.Modules.Cast<PEModuleSymbol>()).ToList();

                    //
                    _lazyExplicitReferences = externalRefs;
                    _lazyImplicitReferences = ImmutableArray<MetadataReference>.Empty;
                    _metadataMap = metadataMap.ToImmutableDictionary();
                    _referencesMap = referencesMap.ToImmutableDictionary();

                    //
                    assemblies.ForEach(ass => ass.SetCorLibrary(_lazyCorLibrary));

                    // recursively initialize references of referenced modules
                    SetReferencesOfReferencedModules(resolver, refmodules);
                }
                else
                {
                    foreach (PortableExecutableReference pe in _lazyExplicitReferences)
                    {
                        var ass = (AssemblySymbol)_referencesMap[pe];
                        Debug.Assert(ass != null);
                        assemblies.Add(ass);
                    }
                }

                //
                var assembly = new SourceAssemblySymbol(compilation, this.SimpleAssemblyName, moduleName);

                assembly.SetCorLibrary(_lazyCorLibrary);
                assembly.SourceModule.SetReferences(new ModuleReferences<AssemblySymbol>(
                    assemblies.Select(x => x.Identity).AsImmutable(),
                    assemblies.AsImmutable(),
                    ImmutableArray<UnifiedAssembly<AssemblySymbol>>.Empty), assembly);

                // set cor types for this compilation
                if (_lazyPhpCorLibrary == null)
                {
                    _diagnostics.Add(Location.None, Errors.ErrorCode.ERR_MetadataFileNotFound, "Peachpie.Runtime.dll");
                    throw new DllNotFoundException("Peachpie.Runtime not found");
                }
                if (_lazyCorLibrary == null)
                {
                    throw new DllNotFoundException("A corlib not found");
                }

                compilation.CoreTypes.Update(_lazyPhpCorLibrary);
                compilation.CoreTypes.Update(_lazyCorLibrary);

                //
                return assembly;
            }
        }
    }
}
