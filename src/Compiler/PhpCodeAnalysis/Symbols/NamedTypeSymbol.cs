using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a type other than an array, a pointer, a type parameter, and dynamic.
    /// </summary>
    internal abstract partial class NamedTypeSymbol : TypeSymbol, INamedTypeSymbol
    {
        public virtual int Arity
        {
            get
            {
                return 0; //throw new NotImplementedException();
            }
        }

        public ISymbol AssociatedSymbol => null;

        public virtual INamedTypeSymbol ConstructedFrom
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual ImmutableArray<IMethodSymbol> Constructors
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// For delegate types, gets the delegate's invoke method.  Returns null on
        /// all other kinds of types.
        /// </summary>
        public virtual IMethodSymbol DelegateInvokeMethod
        {
            get
            {
                // TODO: look for __invoke method
                return null;
            }
        }

        public virtual INamedTypeSymbol EnumUnderlyingType => null;

        public virtual ImmutableArray<IMethodSymbol> InstanceConstructors
        {
            get
            {
                // TODO: get constructors
                return ImmutableArray<IMethodSymbol>.Empty;
            }
        }

        public virtual bool IsGenericType => false;

        public virtual bool IsImplicitClass => false;

        public virtual bool IsScriptClass => false;

        public virtual bool IsUnboundGenericType => false;

        public virtual IEnumerable<string> MemberNames
        {
            get
            {
                yield break;
            }
        }

        /// <summary>
        /// Type layout information (ClassLayout metadata and layout kind flags).
        /// </summary>
        internal abstract TypeLayout Layout { get; }

        public virtual bool MightContainExtensionMethods
        {
            get
            {
                return false;
            }
        }

        public virtual ImmutableArray<IMethodSymbol> StaticConstructors
        {
            get
            {
                return ImmutableArray<IMethodSymbol>.Empty;
            }
        }

        public virtual ImmutableArray<ITypeSymbol> TypeArguments
        {
            get
            {
                return ImmutableArray<ITypeSymbol>.Empty;
            }
        }

        public virtual ImmutableArray<ITypeParameterSymbol> TypeParameters
        {
            get
            {
                return ImmutableArray<ITypeParameterSymbol>.Empty;
            }
        }

        INamedTypeSymbol INamedTypeSymbol.OriginalDefinition
        {
            get
            {
                return (INamedTypeSymbol)this.OriginalDefinition;
            }
        }

        #region ISymbol Members

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitNamedType(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitNamedType(this);
        }

        public INamedTypeSymbol Construct(params ITypeSymbol[] typeArguments)
        {
            throw new NotImplementedException();
        }

        public INamedTypeSymbol ConstructUnboundGenericType()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
