using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// A TypeSymbol is a base class for all the symbols that represent a type in PHP.
    /// </summary>
    internal abstract partial class TypeSymbol : NamespaceOrTypeSymbol, ITypeSymbol, ITypeSymbolInternal
    {
        #region ITypeSymbol

        public abstract TypeKind TypeKind { get; }

        public virtual bool IsTupleType => false;

        INamedTypeSymbol ITypeSymbol.BaseType => BaseType;

        ImmutableArray<INamedTypeSymbol> ITypeSymbol.AllInterfaces => StaticCast<INamedTypeSymbol>.From(AllInterfaces);

        ImmutableArray<INamedTypeSymbol> ITypeSymbol.Interfaces => StaticCast<INamedTypeSymbol>.From(Interfaces);

        ITypeSymbol ITypeSymbol.OriginalDefinition => (ITypeSymbol)this.OriginalTypeSymbolDefinition;

        bool ITypeSymbol.IsNativeIntegerType => SpecialType == SpecialType.System_IntPtr || SpecialType == SpecialType.System_UIntPtr;

        bool ITypeSymbol.IsRefLikeType => false;

        bool ITypeSymbol.IsUnmanagedType => false;

        bool ITypeSymbol.IsReadOnly => false;

        NullableAnnotation ITypeSymbol.NullableAnnotation => NullableAnnotation.None;

        string ITypeSymbol.ToDisplayString(NullableFlowState topLevelNullability, SymbolDisplayFormat format)
        {
            throw new NotImplementedException();
        }

        ImmutableArray<SymbolDisplayPart> ITypeSymbol.ToDisplayParts(NullableFlowState topLevelNullability, SymbolDisplayFormat format)
        {
            throw new NotImplementedException();
        }

        string ITypeSymbol.ToMinimalDisplayString(SemanticModel semanticModel, NullableFlowState topLevelNullability, int position, SymbolDisplayFormat format)
        {
            throw new NotImplementedException();
        }

        ImmutableArray<SymbolDisplayPart> ITypeSymbol.ToMinimalDisplayParts(SemanticModel semanticModel, NullableFlowState topLevelNullability, int position, SymbolDisplayFormat format)
        {
            throw new NotImplementedException();
        }

        ITypeSymbol ITypeSymbol.WithNullableAnnotation(NullableAnnotation nullableAnnotation)
        {
            throw new NotImplementedException();
        }

        #endregion

        ITypeSymbol ITypeSymbolInternal.GetITypeSymbol() => this;

        internal NamedTypeSymbol BaseTypeWithDefinitionUseSiteDiagnostics(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var result = this.BaseType; // this.BaseTypeNoUseSiteDiagnostics;

            //if ((object)result != null)
            //{
            //    result.OriginalDefinition.AddUseSiteDiagnostics(ref useSiteDiagnostics);
            //}

            return result;
        }

        public virtual NamedTypeSymbol BaseType => null;

        public virtual ImmutableArray<NamedTypeSymbol> AllInterfaces
        {
            get
            {
                if (SpecialType == SpecialType.System_Object)
                {
                    return ImmutableArray<NamedTypeSymbol>.Empty;
                }

                // Adds the type, its base and all interfaces recursively into the set,
                // checks for cyclic dependency.
                // Counts unique interfaces (except for the root type).
                // In case there is just a single array with interfaces in whole hierarchy, returns it directly.
                void CollectTypes(NamedTypeSymbol type, HashSet<NamedTypeSymbol> resolved, ref int interfaces, ref ImmutableArray<NamedTypeSymbol> onlyinterfaces)
                {
                    if (type != null && resolved.Add(type))
                    {
                        if (type.IsInterface && resolved.Count != 1) // do not count itself
                        {
                            interfaces++;
                        }

                        var ifaces = type.Interfaces;
                        if (ifaces.IsDefaultOrEmpty == false)
                        {
                            // if interfaces == 0
                            // TODO: child interfaces might be duplicities, we don't have to drop the array we have
                            onlyinterfaces = onlyinterfaces.IsDefault ? ifaces : ImmutableArray<NamedTypeSymbol>.Empty; // only if we reach a single array of interfaces in whole hierarchy

                            foreach (var x in ifaces)
                            {
                                CollectTypes(x, resolved, ref interfaces, ref onlyinterfaces);
                            }
                        }

                        CollectTypes(type.BaseType, resolved, ref interfaces, ref onlyinterfaces);
                    }
                }

                var resolved = new HashSet<NamedTypeSymbol>(); // set of types (classes and interfaces) being collected
                var interfaces = 0;
                var onlyinterfaces = default(ImmutableArray<NamedTypeSymbol>);

                CollectTypes(this as NamedTypeSymbol, resolved, ref interfaces, ref onlyinterfaces);

                if (interfaces == 0)
                {
                    return ImmutableArray<NamedTypeSymbol>.Empty;
                }

                if (onlyinterfaces.IsDefaultOrEmpty == false) // there is a single array with interfaces
                {
                    return onlyinterfaces;
                }

                //
                var builder = ImmutableArray.CreateBuilder<NamedTypeSymbol>(interfaces);
                foreach (var x in resolved)
                {
                    if (x != this && x.IsInterface)
                    {
                        builder.Add(x);
                    }
                }

                return builder.MoveToImmutable();
            }
        }

        public virtual ImmutableArray<NamedTypeSymbol> Interfaces => ImmutableArray<NamedTypeSymbol>.Empty;

        public virtual bool IsAnonymousType => false;

        /// <summary>
        /// Returns true if this type is known to be a reference type. It is never the case that
        /// IsReferenceType and IsValueType both return true. However, for an unconstrained type
        /// parameter, IsReferenceType and IsValueType will both return false.
        /// </summary>
        public virtual bool IsReferenceType
        {
            get
            {
                var kind = TypeKind;
                return kind != TypeKind.Enum && kind != TypeKind.Struct && kind != TypeKind.Error;
            }
        }

        /// <summary>
        /// Returns true if this type is known to be a value type. It is never the case that
        /// IsReferenceType and IsValueType both return true. However, for an unconstrained type
        /// parameter, IsReferenceType and IsValueType will both return false.
        /// </summary>
        public virtual bool IsValueType
        {
            get
            {
                var kind = TypeKind;
                return kind == TypeKind.Struct || kind == TypeKind.Enum;
            }
        }

        public virtual bool IsPointerType => false;

        public virtual SpecialType SpecialType => SpecialType.None;

        /// <summary>
        /// The original definition of this symbol. If this symbol is constructed from another
        /// symbol by type substitution then OriginalDefinition gets the original symbol as it was defined in
        /// source or metadata.
        /// </summary>
        public new TypeSymbol OriginalDefinition => OriginalTypeSymbolDefinition;

        protected virtual TypeSymbol OriginalTypeSymbolDefinition
        {
            get
            {
                return this;
            }
        }

        protected override sealed Symbol OriginalSymbolDefinition => this.OriginalTypeSymbolDefinition;

        /// <summary>
        /// Gets corresponding primitive type code for this type declaration.
        /// </summary>
        internal Microsoft.Cci.PrimitiveTypeCode PrimitiveTypeCode
        {
            get
            {
                return this.IsPointerType
                    ? Microsoft.Cci.PrimitiveTypeCode.Pointer
                    : SpecialTypes.GetTypeCode(SpecialType);
            }
        }

        /// <summary>
        /// If this is a type parameter returns its effective base class, otherwise returns this type.
        /// </summary>
        internal TypeSymbol EffectiveTypeNoUseSiteDiagnostics
        {
            get
            {
                return this.IsTypeParameter() ? ((TypeParameterSymbol)this).EffectiveBaseClassNoUseSiteDiagnostics : this;
            }
        }

        internal TypeSymbol EffectiveType(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            return this.IsTypeParameter() ? ((TypeParameterSymbol)this).EffectiveBaseClass(ref useSiteDiagnostics) : this;
        }

        internal bool IsEqualToOrDerivedFrom(TypeSymbol type)
        {
            var useSiteDiagnostics = new HashSet<DiagnosticInfo>();
            return IsEqualToOrDerivedFrom(type, false, ref useSiteDiagnostics);
        }

        /// <summary>
        /// Returns true if this type derives from a given type.
        /// </summary>
        internal bool IsDerivedFrom(TypeSymbol type, bool ignoreDynamic, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)type != null);
            Debug.Assert(!type.IsTypeParameter());

            if ((object)this == (object)type)
            {
                return false;
            }

            var t = this.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics);
            while ((object)t != null)
            {
                if (type.Equals(t, ignoreDynamic: ignoreDynamic))
                {
                    return true;
                }

                t = t.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics);
            }

            return false;
        }

        /// <summary>
        /// Returns true if this type is equal or derives from a given type.
        /// </summary>
        internal bool IsEqualToOrDerivedFrom(TypeSymbol type, bool ignoreDynamic, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            return this.Equals(type, ignoreDynamic: ignoreDynamic) || this.IsDerivedFrom(type, ignoreDynamic, ref useSiteDiagnostics);
        }

        /// <summary>
        /// Determines if this type symbol represent the same type as another, according to the language
        /// semantics.
        /// </summary>
        /// <param name="t2">The other type.</param>
        /// <param name="ignoreCustomModifiersAndArraySizesAndLowerBounds">True to compare without regard to custom modifiers, false by default.</param>
        /// <param name="ignoreDynamic">True to ignore the distinction between object and dynamic, false by default.</param>
        /// <returns>True if the types are equivalent.</returns>
        internal virtual bool Equals(TypeSymbol t2, bool ignoreCustomModifiersAndArraySizesAndLowerBounds = false, bool ignoreDynamic = false)
        {
            return ReferenceEquals(this, t2);
        }

        public override sealed bool Equals(ISymbol obj, SymbolEqualityComparer equalityComparer)
        {
            var t2 = obj as TypeSymbol;
            if ((object)t2 == null) return false;
            return this.Equals(t2, false, false);
        }

        /// <summary>
        /// We ignore custom modifiers, and the distinction between dynamic and object, when computing a type's hash code.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        /// <summary>
        /// In case of PHP corlibrary type, gets reference to the descriptor <see cref="CoreType"/>.
        /// </summary>
        public virtual CoreType PhpCoreType => null;

        public ISymbol FindImplementationForInterfaceMember(ISymbol interfaceMember)
        {
            throw new NotImplementedException();
        }

        T GetSingleMember<T>(string name, Func<T, bool> predicate = null) where T : class
        {
            var candidates = this.GetMembers(name).OfType<T>();
            if (predicate != null)
                candidates = candidates.Where(predicate);

            return candidates.SingleOrDefault();
        }

        /// <summary>
        /// Lookup member of given name and type through base types and interfaces.
        /// </summary>
        public T LookupMember<T>(string name, Func<T, bool> predicate = null) where T : class
        {
            for (var t = this; t != null; t = t.BaseType)
            {
                var result = t.GetSingleMember<T>(name, predicate);
                if (result != null)
                    return result;
            }

            foreach (var t in this.AllInterfaces)
            {
                var result = t.GetSingleMember<T>(name, predicate);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// Resolves PHP method by its name.
        /// </summary>
        public List<MethodSymbol> LookupMethods(string name)
        {
            if (this.Is_PhpValue())
            {
                return new List<MethodSymbol>();
            }

            TypeSymbol topPhpType = null; // deals with PHP-like overriding, once there is PHP method that override another method (even with a different signature) in a base PHP type

            var set = new HashSet<MethodSymbol>(SignatureEqualityComparer.Instance);

            for (var t = this; t != null; t = t.BaseType)
            {
                if (topPhpType != null && set.Count != 0 && t.IsPhpType())
                {
                    // we already found a method declared in PHP class,
                    // anything in {t} is treated as overriden:
                    continue;
                }

                int count = set.Count;

                foreach (var c in t.GetMembersByPhpName(name))
                {
                    if (c is MethodSymbol m) set.Add(m);
                }

                // remember the top PHP class declaring the method:
                if (topPhpType == null && count != set.Count && t.IsPhpType()) // some methods were found in PHP type
                {
                    topPhpType = t;
                }
            }

            // remove php-hidden methods
            set.RemoveWhere(m => m.IsPhpHidden); // TODO: other attributes: "private protected", "internal"

            if (set.Count == 0 || (this.IsAbstract && set.All(m => m.IsAbstract))) // abstract or interface, otherwise all methods should be declared on this already
            {
                // lookup interface members only if this type is interface or the method is abstract

                foreach (var t in this.AllInterfaces)
                {
                    set.UnionWith(t.GetMembersByPhpName(name).OfType<MethodSymbol>());
                }
            }

            //
            return set.ToList();
        }
    }
}
