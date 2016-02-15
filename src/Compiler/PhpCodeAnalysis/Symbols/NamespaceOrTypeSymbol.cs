using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using System;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents either a namespace or a type.
    /// </summary>
    internal abstract class NamespaceOrTypeSymbol : Symbol, INamespaceOrTypeSymbol
    {
        // Only the compiler can create new instances.
        internal NamespaceOrTypeSymbol()
        {
        }

        /// <summary>
        /// Returns true if this symbol is a namespace. If it is not a namespace, it must be a type.
        /// </summary>
        public bool IsNamespace => Kind == SymbolKind.Namespace;

        /// <summary>
        /// Returns true if this symbols is a type. Equivalent to !IsNamespace.
        /// </summary>
        public bool IsType => !IsNamespace;

        /// <summary>
        /// Returns true if this symbol is "virtual", has an implementation, and does not override a
        /// base class member; i.e., declared with the "virtual" modifier. Does not return true for
        /// members declared as abstract or override.
        /// </summary>
        /// <returns>
        /// Always returns false.
        /// </returns>
        public sealed override bool IsVirtual => false;

        /// <summary>
        /// Returns true if this symbol was declared to override a base class member; i.e., declared
        /// with the "override" modifier. Still returns true if member was declared to override
        /// something, but (erroneously) no member to override exists.
        /// </summary>
        /// <returns>
        /// Always returns false.
        /// </returns>
        public sealed override bool IsOverride => false;

        /// <summary>
        /// Returns true if this symbol has external implementation; i.e., declared with the 
        /// "extern" modifier. 
        /// </summary>
        /// <returns>
        /// Always returns false.
        /// </returns>
        public sealed override bool IsExtern => false;

        /// <summary>
        /// Get all the members of this symbol.
        /// </summary>
        /// <returns>An ImmutableArray containing all the members of this symbol. If this symbol has no members,
        /// returns an empty ImmutableArray. Never returns null.</returns>
        public abstract ImmutableArray<Symbol> GetMembers();
        
        /// <summary>
        /// Get all the members of this symbol that have a particular name.
        /// </summary>
        /// <returns>An ImmutableArray containing all the members of this symbol with the given name. If there are
        /// no members with this name, returns an empty ImmutableArray. Never returns null.</returns>
        public abstract ImmutableArray<Symbol> GetMembers(string name);

        /// <summary>
        /// Get all the members of this symbol that are types.
        /// </summary>
        /// <returns>An ImmutableArray containing all the types that are members of this symbol. If this symbol has no type members,
        /// returns an empty ImmutableArray. Never returns null.</returns>
        public abstract ImmutableArray<NamedTypeSymbol> GetTypeMembers();

        /// <summary>
        /// Get all the members of this symbol that are types that have a particular name, of any arity.
        /// </summary>
        /// <returns>An ImmutableArray containing all the types that are members of this symbol with the given name.
        /// If this symbol has no type members with this name,
        /// returns an empty ImmutableArray. Never returns null.</returns>
        public abstract ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name);

        /// <summary>
        /// Get all the members of this symbol that are types that have a particular name and arity
        /// </summary>
        /// <returns>An IEnumerable containing all the types that are members of this symbol with the given name and arity.
        /// If this symbol has no type members with this name and arity,
        /// returns an empty IEnumerable. Never returns null.</returns>
        public virtual ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            // default implementation does a post-filter. We can override this if its a performance burden, but 
            // experience is that it won't be.
            return GetTypeMembers(name).WhereAsArray(t => t.Arity == arity);
        }

        /// <summary>
        /// Finds types or namespaces described by a qualified name.
        /// </summary>
        /// <param name="qualifiedName">Sequence of simple plain names.</param>
        /// <returns>
        /// A set of namespace or type symbols with given qualified name (might comprise of types with multiple generic arities), 
        /// or an empty set if the member can't be found (the qualified name is ambiguous or the symbol doesn't exist).
        /// </returns>
        /// <remarks>
        /// "C.D" matches C.D, C{T}.D, C{S,T}.D{U}, etc.
        /// </remarks>
        internal IEnumerable<NamespaceOrTypeSymbol> GetNamespaceOrTypeByQualifiedName(IEnumerable<string> qualifiedName)
        {
            NamespaceOrTypeSymbol namespaceOrType = this;
            IEnumerable<NamespaceOrTypeSymbol> symbols = null;
            foreach (string name in qualifiedName)
            {
                if (symbols != null)
                {
                    throw new NotImplementedException();
                    //// there might be multiple types of different arity, prefer a non-generic type:
                    //namespaceOrType = symbols.OfMinimalArity();
                    //if ((object)namespaceOrType == null)
                    //{
                    //    return SpecializedCollections.EmptyEnumerable<NamespaceOrTypeSymbol>();
                    //}
                }

                symbols = namespaceOrType.GetMembers(name).OfType<NamespaceOrTypeSymbol>();
            }

            return symbols;
        }

        #region INamespaceOrTypeSymbol Members

        ImmutableArray<ISymbol> INamespaceOrTypeSymbol.GetMembers()
        {
            return StaticCast<ISymbol>.From(this.GetMembers());
        }

        ImmutableArray<ISymbol> INamespaceOrTypeSymbol.GetMembers(string name)
        {
            return StaticCast<ISymbol>.From(this.GetMembers(name));
        }

        ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers()
        {
            return StaticCast<INamedTypeSymbol>.From(this.GetTypeMembers());
        }

        ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers(string name)
        {
            return StaticCast<INamedTypeSymbol>.From(this.GetTypeMembers(name));
        }

        ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers(string name, int arity)
        {
            return StaticCast<INamedTypeSymbol>.From(this.GetTypeMembers(name, arity));
        }

        #endregion
    }
}
