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
        internal class ReferenceManager : CommonReferenceManager
        {
            ImmutableArray<MetadataReference> _lazyExplicitReferences;
            ImmutableArray<MetadataReference> _lazyImplicitReferences = ImmutableArray<MetadataReference>.Empty;
            ImmutableDictionary<MetadataReference, IAssemblySymbol> _referencesMap;
            ImmutableDictionary<IAssemblySymbol, MetadataReference> _metadataMap;
            AssemblySymbol _lazyCorLibrary;

            /// <summary>
            /// COR library containing base system types.
            /// </summary>
            internal AssemblySymbol CorLibrary => _lazyCorLibrary;

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

            internal void CreateSourceAssemblyForCompilation(PhpCompilation compilation)
            {
                if (compilation._lazyAssemblySymbol != null)
                    return;

                // TODO: lock

                Debug.Assert(_lazyExplicitReferences.IsDefault);
                Debug.Assert(_lazyCorLibrary == null);

                //
                var externalRefs = compilation.ExternalReferences;
                var assemblies = new List<AssemblySymbol>(externalRefs.Length);

                var referencesMap = new Dictionary<MetadataReference, IAssemblySymbol>();
                var metadataMap = new Dictionary<IAssemblySymbol, MetadataReference>();

                foreach (PortableExecutableReference pe in externalRefs)
                {
                    var symbol = PEAssemblySymbol.Create(pe);
                    if (symbol != null)
                    {
                        assemblies.Add(symbol);
                        referencesMap[pe] = symbol;
                        metadataMap[symbol] = pe;

                        if (_lazyCorLibrary == null && symbol.IsCorLibrary)
                            _lazyCorLibrary = symbol;
                    }
                }

                //
                _lazyExplicitReferences = externalRefs;
                _lazyImplicitReferences = ImmutableArray<MetadataReference>.Empty;
                _metadataMap = metadataMap.ToImmutableDictionary();
                _referencesMap = referencesMap.ToImmutableDictionary();

                //
                var assembly = new SourceAssemblySymbol(compilation, _lazyCorLibrary, compilation.Options.ModuleName, compilation.Options.ModuleName);
                compilation._lazyAssemblySymbol = assembly;

                assembly.SourceModule.SetReferences(new ModuleReferences<AssemblySymbol>(
                    assemblies.Select(x => x.Identity).AsImmutable(),
                    assemblies.AsImmutable(),
                    ImmutableArray<UnifiedAssembly<AssemblySymbol>>.Empty), assembly);
            }
        }
    }
}
