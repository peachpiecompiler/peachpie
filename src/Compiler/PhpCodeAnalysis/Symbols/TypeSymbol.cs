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
        public virtual ImmutableArray<INamedTypeSymbol> AllInterfaces
        {
            get
            {
                return ImmutableArray<INamedTypeSymbol>.Empty;
            }
        }

        INamedTypeSymbol ITypeSymbol.BaseType => BaseType;

        public virtual NamedTypeSymbol BaseType
        {
            get
            {
                return null;
            }
        }

        public virtual ImmutableArray<INamedTypeSymbol> Interfaces
        {
            get
            {
                return ImmutableArray<INamedTypeSymbol>.Empty;
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

        public virtual SpecialType SpecialType => SpecialType.None;

        /// <summary>
        /// In case of PHP corlibrary type, gets reference to the descriptor <see cref="CoreType"/>.
        /// </summary>
        public virtual CoreType PhpCoreType => null;

        public abstract TypeKind TypeKind { get; }

        ITypeSymbol ITypeSymbol.OriginalDefinition
        {
            get
            {
                return (ITypeSymbol)this.OriginalDefinition;
            }
        }

        public ISymbol FindImplementationForInterfaceMember(ISymbol interfaceMember)
        {
            throw new NotImplementedException();
        }
    }
}
