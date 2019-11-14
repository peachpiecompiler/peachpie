using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// The base class to represent a namespace imported from a PE/module. Namespaces that differ
    /// only by casing in name are not merged.
    /// </summary>
    internal abstract class PENamespaceSymbol : NamespaceSymbol
    {
        /// <summary>
        /// A map of types immediately contained within this namespace 
        /// grouped by their name (case-sensitively).
        /// </summary>
        protected Dictionary<string, ImmutableArray<NamedTypeSymbol>> _types;

        public sealed override ImmutableArray<Symbol> GetMembers()
        {
            EnsureAllMembersLoaded();

            return StaticCast<Symbol>.From(_types.Flatten());
        }

        public sealed override ImmutableArray<Symbol> GetMembers(string name)
        {
            EnsureAllMembersLoaded();

            ImmutableArray<NamedTypeSymbol> t;
            if (_types.TryGetValue(name, out t))
            {
                return StaticCast<Symbol>.From(t);
            }

            return ImmutableArray<Symbol>.Empty;
        }

        public override ImmutableArray<Symbol> GetMembersByPhpName(string name)
        {
            // return ImmutableArray<Symbol>.Empty; // should not be called, review
            throw new NotImplementedException();
        }

        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            EnsureAllMembersLoaded();

            return _types.Flatten();
        }

        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            EnsureAllMembersLoaded();

            ImmutableArray<NamedTypeSymbol> t;

            short arity;
            name = MetadataHelpers.InferTypeArityAndUnmangleMetadataName(name, out arity);

            if (_types.TryGetValue(name, out t) && t.Length != 0)
            {
                Debug.Assert(t != null);
                return t.WhereAsArray(x => x.Arity == arity);
            }
            else
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }
        }

        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            var result = GetTypeMembers(name);
            if (arity >= 0)
            {
                result = result.WhereAsArray(type => type.Arity == arity);
            }

            return result;
        }

        public sealed override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException(); //return ContainingPEModule.MetadataLocation.Cast<MetadataLocation, Location>();
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        /// <summary>
        /// Returns PEModuleSymbol containing the namespace.
        /// </summary>
        /// <returns>PEModuleSymbol containing the namespace.</returns>
        internal abstract PEModuleSymbol ContainingPEModule { get; }

        protected abstract void EnsureAllMembersLoaded();

        /// <summary>
        /// Create symbols for nested types and initialize types map.
        /// </summary>
        protected void LazyInitializeTypes(IEnumerable<IGrouping<string, TypeDefinitionHandle>> typeGroups)
        {
            if (_types == null)
            {
                var moduleSymbol = ContainingPEModule;

                var children = ArrayBuilder<NamedTypeSymbol>.GetInstance();
                var skipCheckForPiaType = !moduleSymbol.Module.ContainsNoPiaLocalTypes();

                foreach (var g in typeGroups)
                {
                    foreach (var t in g)
                    {
                        if (skipCheckForPiaType || !moduleSymbol.Module.IsNoPiaLocalType(t))
                        {
                            children.Add(PENamedTypeSymbol.Create(moduleSymbol, this, t, g.Key));
                        }
                        else
                        {
                            // Pia ignored
                        }
                    }
                }

                var typesDict = children.ToDictionary(c => MetadataHelpers.BuildQualifiedName(c.NamespaceName, c.Name));
                children.Free();

                //if (noPiaLocalTypes != null)
                //{
                //    Interlocked.CompareExchange(ref _lazyNoPiaLocalTypes, noPiaLocalTypes, null);
                //}

                Interlocked.CompareExchange(ref _types, typesDict, null);
            }
        }
    }
}
