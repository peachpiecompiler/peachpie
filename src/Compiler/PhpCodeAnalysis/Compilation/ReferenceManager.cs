using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Utilities;
using System.Collections.Immutable;
using Pchp.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis
{
    partial class PhpCompilation
    {
        internal class ReferenceManager : CommonReferenceManager
        {
            internal override ImmutableArray<MetadataReference> ExplicitReferences
            {
                get
                {
                    return ImmutableArray<MetadataReference>.Empty;
                }
            }

            internal override ImmutableArray<MetadataReference> ImplicitReferences
            {
                get
                {
                    return ImmutableArray<MetadataReference>.Empty;
                }
            }

            internal override IEnumerable<KeyValuePair<AssemblyIdentity, PortableExecutableReference>> GetImplicitlyResolvedAssemblyReferences()
            {
                yield break;
            }

            internal override MetadataReference GetMetadataReference(IAssemblySymbol assemblySymbol)
            {
                return null;
            }

            internal override IEnumerable<KeyValuePair<MetadataReference, IAssemblySymbol>> GetReferencedAssemblies()
            {
                yield break;
            }

            internal override IEnumerable<ValueTuple<IAssemblySymbol, ImmutableArray<string>>> GetReferencedAssemblyAliases()
            {
                yield break;
            }

            internal void CreateSourceAssemblyForCompilation(PhpCompilation compilation)
            {
                if (compilation._lazyAssemblySymbol == null)
                {
                    var assembly = new SourceAssemblySymbol(compilation, compilation.Options.ModuleName, compilation.Options.ModuleName);
                    compilation._lazyAssemblySymbol = assembly;

                    assembly.SourceModule.SetReferences(new ModuleReferences<AssemblySymbol>(ImmutableArray<AssemblyIdentity>.Empty, ImmutableArray<AssemblySymbol>.Empty, ImmutableArray<UnifiedAssembly<AssemblySymbol>>.Empty), assembly);
                }
            }
        }
    }
}
