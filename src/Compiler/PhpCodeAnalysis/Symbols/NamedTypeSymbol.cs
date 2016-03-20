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
        public abstract int Arity { get; }

        /// <summary>
        /// Should the name returned by Name property be mangled with [`arity] suffix in order to get metadata name.
        /// Must return False for a type with Arity == 0.
        /// </summary>
        internal abstract bool MangleName
        {
            // Intentionally no default implementation to force consideration of appropriate implementation for each new subclass
            get;
        }

        public override SymbolKind Kind => SymbolKind.NamedType;

        public ISymbol AssociatedSymbol => null;

        public virtual INamedTypeSymbol ConstructedFrom
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Get the both instance and static constructors for this type.
        /// </summary>
        public virtual ImmutableArray<MethodSymbol> Constructors => GetMembers()
            .OfType<MethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Constructor || m.MethodKind == MethodKind.StaticConstructor)
            .ToImmutableArray();

        public virtual ImmutableArray<MethodSymbol> InstanceConstructors => GetMembers()
            .OfType<MethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Constructor)
            .ToImmutableArray();

        public virtual ImmutableArray<MethodSymbol> StaticConstructors => /*GetMembers()
            .OfType<MethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.StaticConstructor)
            .ToImmutableArray();*/
            ImmutableArray<MethodSymbol>.Empty;

        internal abstract ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved);

        /// <summary>
        /// Requires less computation than <see cref="TypeSymbol.TypeKind"/> == <see cref="TypeKind.Interface"/>.
        /// </summary>
        /// <remarks>
        /// Metadata types need to compute their base types in order to know their TypeKinds, and that can lead
        /// to cycles if base types are already being computed.
        /// </remarks>
        /// <returns>True if this is an interface type.</returns>
        internal abstract bool IsInterface { get; }

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
        /// Returns a flag indicating whether this symbol has at least one applied/inherited conditional attribute.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        internal bool IsConditional
        {
            get
            {
                //if (this.GetAppliedConditionalSymbols().Any())    // TODO
                //{
                //    return true;
                //}

                // Conditional attributes are inherited by derived types.
                var baseType = this.BaseType;// NoUseSiteDiagnostics;
                return (object)baseType != null ? baseType.IsConditional : false;
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

        #region INamedTypeSymbol

        /// <summary>
        /// Get the both instance and static constructors for this type.
        /// </summary>
        ImmutableArray<IMethodSymbol> INamedTypeSymbol.Constructors => StaticCast<IMethodSymbol>.From(Constructors);

        ImmutableArray<IMethodSymbol> INamedTypeSymbol.InstanceConstructors
            => StaticCast<IMethodSymbol>.From(InstanceConstructors);

        ImmutableArray<IMethodSymbol> INamedTypeSymbol.StaticConstructors
            => StaticCast<IMethodSymbol>.From(StaticConstructors);

        #endregion

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
