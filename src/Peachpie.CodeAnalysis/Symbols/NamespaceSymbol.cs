using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Cci = Microsoft.Cci;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Symbols;
using System;

namespace Pchp.CodeAnalysis.Symbols
{

    /// <summary>
    /// Represents a namespace.
    /// </summary>
    internal abstract partial class NamespaceSymbol : NamespaceOrTypeSymbol, INamespaceSymbol, INamespaceSymbolInternal
    {
        /// <summary>
        /// Get all the members of this symbol that are namespaces.
        /// </summary>
        /// <returns>An IEnumerable containing all the namespaces that are members of this symbol.
        /// If this symbol has no namespace members, returns an empty IEnumerable. Never returns
        /// null.</returns>
        public IEnumerable<NamespaceSymbol> GetNamespaceMembers()
        {
            return this.GetMembers().OfType<NamespaceSymbol>();
        }

        /// <summary>
        /// Returns whether this namespace is the unnamed, global namespace that is 
        /// at the root of all namespaces.
        /// </summary>
        public virtual bool IsGlobalNamespace
        {
            get
            {
                return (object)ContainingNamespace == null;
            }
        }

        /// <summary>
        /// The kind of namespace: Module, Assembly or Compilation.
        /// Module namespaces contain only members from the containing module that share the same namespace name.
        /// Assembly namespaces contain members for all modules in the containing assembly that share the same namespace name.
        /// Compilation namespaces contain all members, from source or referenced metadata (assemblies and modules) that share the same namespace name.
        /// </summary>
        public virtual NamespaceKind NamespaceKind
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// The containing compilation for compilation namespaces.
        /// </summary>
        public virtual PhpCompilation ContainingCompilation
        {
            get
            {
                if (this.NamespaceKind == NamespaceKind.Compilation)
                    throw new NotSupportedException();

                return null;
            }
        }

        /// <summary>
        /// If a namespace has Assembly or Compilation extent, it may be composed of multiple
        /// namespaces that are merged together. If so, ConstituentNamespaces returns
        /// all the namespaces that were merged. If this namespace was not merged, returns
        /// an array containing only this namespace.
        /// </summary>
        public virtual ImmutableArray<NamespaceSymbol> ConstituentNamespaces
        {
            get
            {
                return ImmutableArray.Create(this);
            }
        }

        public sealed override NamedTypeSymbol ContainingType => null;

        /// <summary>
        /// Containing assembly.
        /// </summary>
        public abstract override AssemblySymbol ContainingAssembly { get; }

        internal override ModuleSymbol ContainingModule
        {
            get
            {
                if (NamespaceKind == NamespaceKind.Module)
                {
                    throw new NotSupportedException();
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the kind of this symbol.
        /// </summary>
        public sealed override SymbolKind Kind => SymbolKind.Namespace;

        public override sealed bool IsImplicitlyDeclared
        {
            get
            {
                return this.IsGlobalNamespace;
            }
        }

        // Only the compiler can create namespace symbols.
        internal NamespaceSymbol()
        {
        }

        /// <summary>
        /// Get this accessibility that was declared on this symbol. For symbols that do not have
        /// accessibility declared on them, returns NotApplicable.
        /// </summary>
        public sealed override Accessibility DeclaredAccessibility => Accessibility.Public;

        /// <summary>
        /// Returns true if this symbol is "static"; i.e., declared with the "static" modifier or
        /// implicitly static.
        /// </summary>
        public sealed override bool IsStatic => true;

        /// <summary>
        /// Returns true if this symbol was declared as requiring an override; i.e., declared with
        /// the "abstract" modifier. Also returns true on a type declared as "abstract", all
        /// interface types, and members of interface types.
        /// </summary>
        public sealed override bool IsAbstract => false;

        /// <summary>
        /// Returns true if this symbol was declared to override a base class member and was also
        /// sealed from further overriding; i.e., declared with the "sealed" modifier.  Also set for
        /// types that do not allow a derived class (declared with "sealed" or "static" or "struct"
        /// or "enum" or "delegate").
        /// </summary>
        public sealed override bool IsSealed => false;

        /// <summary>
        /// Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        /// This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        /// </summary>
        internal sealed override ObsoleteAttributeData ObsoleteAttributeData => null;

        /// <summary>
        /// Returns an implicit type symbol for this namespace or null if there is none. This type
        /// wraps misplaced global code.
        /// </summary>
        internal NamedTypeSymbol ImplicitType
        {
            get
            {
                throw new NotImplementedException();

                //var types = this.GetTypeMembers(TypeSymbol.ImplicitTypeName);
                //if (types.Length == 0)
                //{
                //    return null;
                //}

                //Debug.Assert(types.Length == 1);
                //return types[0];
            }
        }

        /// <summary>
        /// Lookup a nested namespace.
        /// </summary>
        /// <param name="names">
        /// Sequence of names for nested child namespaces.
        /// </param>
        /// <returns>
        /// Symbol for the most nested namespace, if found. Nothing 
        /// if namespace or any part of it can not be found.
        /// </returns>
        internal NamespaceSymbol LookupNestedNamespace(ImmutableArray<string> names)
        {
            NamespaceSymbol scope = this;

            foreach (string name in names)
            {
                NamespaceSymbol nextScope = null;

                foreach (NamespaceOrTypeSymbol symbol in scope.GetMembers(name))
                {
                    var ns = symbol as NamespaceSymbol;

                    if ((object)ns != null)
                    {
                        if ((object)nextScope != null)
                        {
                            Debug.Assert((object)nextScope == null, "Why did we run into an unmerged namespace?");
                            nextScope = null;
                            break;
                        }

                        nextScope = ns;
                    }
                }

                scope = nextScope;

                if ((object)scope == null)
                {
                    break;
                }
            }

            return scope;
        }

        internal NamespaceSymbol GetNestedNamespace(string name)
        {
            foreach (var sym in this.GetMembers(name))
            {
                if (sym.Kind == SymbolKind.Namespace)
                {
                    return (NamespaceSymbol)sym;
                }
            }

            return null;
        }

        #region INamespaceSymbol Members

        IEnumerable<INamespaceOrTypeSymbol> INamespaceSymbol.GetMembers()
        {
            return this.GetMembers().OfType<INamespaceOrTypeSymbol>();
        }

        IEnumerable<INamespaceOrTypeSymbol> INamespaceSymbol.GetMembers(string name)
        {
            return this.GetMembers(name).OfType<INamespaceOrTypeSymbol>();
        }

        IEnumerable<INamespaceSymbol> INamespaceSymbol.GetNamespaceMembers()
        {
            return this.GetNamespaceMembers();
        }

        NamespaceKind INamespaceSymbol.NamespaceKind
        {
            get { return this.NamespaceKind; }
        }

        Compilation INamespaceSymbol.ContainingCompilation
        {
            get
            {
                return this.ContainingCompilation;
            }
        }

        ImmutableArray<INamespaceSymbol> INamespaceSymbol.ConstituentNamespaces
        {
            get
            {
                return StaticCast<INamespaceSymbol>.From(this.ConstituentNamespaces);
            }
        }

        #endregion

        #region ISymbol Members

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitNamespace(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitNamespace(this);
        }

        #endregion
    }
}
