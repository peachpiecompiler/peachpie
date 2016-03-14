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

        ITypeSymbol ITypeSymbol.OriginalDefinition => (ITypeSymbol)this.OriginalDefinition;

        #endregion

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
                return ImmutableArray<NamedTypeSymbol>.Empty;
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
        /// In case of PHP corlibrary type, gets reference to the descriptor <see cref="CoreType"/>.
        /// </summary>
        public virtual CoreType PhpCoreType => null;

        public ISymbol FindImplementationForInterfaceMember(ISymbol interfaceMember)
        {
            throw new NotImplementedException();
        }
    }
}
