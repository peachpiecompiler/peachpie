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

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// A TypeSymbol is a base class for all the symbols that represent a type in PHP.
    /// </summary>
    internal abstract partial class TypeSymbol : NamespaceOrTypeSymbol, ITypeSymbol
    {
        #region ITypeSymbol

        public abstract TypeKind TypeKind { get; }

        INamedTypeSymbol ITypeSymbol.BaseType => BaseType;

        ImmutableArray<INamedTypeSymbol> ITypeSymbol.AllInterfaces => StaticCast<INamedTypeSymbol>.From(AllInterfaces);

        ImmutableArray<INamedTypeSymbol> ITypeSymbol.Interfaces => StaticCast<INamedTypeSymbol>.From(Interfaces);

        ITypeSymbol ITypeSymbol.OriginalDefinition => (ITypeSymbol)this.OriginalTypeSymbolDefinition;

        #endregion

        internal NamedTypeSymbol BaseTypeWithDefinitionUseSiteDiagnostics(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var result = this.BaseType; // this.BaseTypeNoUseSiteDiagnostics;

            //if ((object)result != null)
            //{
            //    result.OriginalDefinition.AddUseSiteDiagnostics(ref useSiteDiagnostics);
            //}

            return result;
        }

        public virtual NamedTypeSymbol BaseType
        {
            get
            {
                return null;
            }
        }

        public virtual ImmutableArray<NamedTypeSymbol> AllInterfaces
        {
            get
            {
                if (Interfaces.Length == 0)
                {
                    return ImmutableArray<NamedTypeSymbol>.Empty;
                }

                var result = new HashSet<NamedTypeSymbol>();
                var todo = new Queue<NamedTypeSymbol>(Interfaces);
                while (todo.Count != 0)
                {
                    var t = todo.Dequeue();
                    if (result.Add(t))
                    {
                        t.Interfaces.ForEach(todo.Enqueue);
                    }
                }

                return result.AsImmutable();
            }
        }

        public virtual ImmutableArray<NamedTypeSymbol> Interfaces
        {
            get
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }
        }

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

        public sealed override bool Equals(object obj)
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
    }
}
