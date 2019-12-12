using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a type other than an array, a pointer, a type parameter, and dynamic.
    /// </summary>
    internal abstract partial class NamedTypeSymbol : TypeSymbol, INamedTypeSymbol
    {
        public abstract int Arity { get; }

        public abstract bool IsSerializable { get; }

        /// <summary>
        /// Should the name returned by Name property be mangled with [`arity] suffix in order to get metadata name.
        /// Must return False for a type with Arity == 0.
        /// </summary>
        internal abstract bool MangleName
        {
            // Intentionally no default implementation to force consideration of appropriate implementation for each new subclass
            get;
        }

        public override string MetadataName
        {
            get
            {
                return MangleName ? MetadataHelpers.ComposeAritySuffixedMetadataName(Name, Arity) : Name;
            }
        }

        public override SymbolKind Kind => SymbolKind.NamedType;

        public ISymbol AssociatedSymbol => null;

        INamedTypeSymbol INamedTypeSymbol.ConstructedFrom => ConstructedFrom;

        public virtual NamedTypeSymbol ConstructedFrom => this;

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

        public virtual ImmutableArray<MethodSymbol> StaticConstructors =>
            GetMembers()
            .OfType<MethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.StaticConstructor)
            .ToImmutableArray();

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
        /// all other kinds of types.  Note that it is possible to have an ill-formed
        /// delegate type imported from metadata which does not have an Invoke method.
        /// Such a type will be classified as a delegate but its DelegateInvokeMethod
        /// would be null.
        /// </summary>
        public MethodSymbol DelegateInvokeMethod
        {
            get
            {
                if (TypeKind != TypeKind.Delegate)
                {
                    return null;
                }

                var methods = GetMembers(WellKnownMemberNames.DelegateInvokeName);
                if (methods.Length != 1)
                {
                    return null;
                }

                var method = methods[0] as MethodSymbol;

                //EDMAURER we used to also check 'method.IsVirtual' because section 13.6
                //of the CLI spec dictates that it be virtual, but real world
                //working metadata has been found that contains an Invoke method that is
                //marked as virtual but not newslot (both of those must be combined to
                //meet the C# definition of virtual). Rather than weaken the check
                //I've removed it, as the Dev10 compiler makes no check, and we don't
                //stand to gain anything by having it.

                //return method != null && method.IsVirtual ? method : null;
                return method;
            }
        }

        INamedTypeSymbol INamedTypeSymbol.EnumUnderlyingType => EnumUnderlyingType;

        internal abstract bool HasTypeArgumentsCustomModifiers { get; }

        /// <summary>
        /// For enum types, gets the underlying type. Returns null on all other
        /// kinds of types.
        /// </summary>
        public virtual NamedTypeSymbol EnumUnderlyingType => null;

        public virtual NamedTypeSymbol TupleUnderlyingType => null;

        public virtual ImmutableArray<IFieldSymbol> TupleElements => default(ImmutableArray<IFieldSymbol>);

        /// <summary>
        /// True if this type or some containing type has type parameters.
        /// </summary>
        public bool IsGenericType
        {
            get
            {
                for (var current = this; !ReferenceEquals(current, null); current = current.ContainingType)
                {
                    if (current.TypeArgumentsNoUseSiteDiagnostics.Length != 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

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
        /// True if the type is a Windows runtime type.
        /// </summary>
        /// <remarks>
        /// A type can me marked as a Windows runtime type in source by applying the WindowsRuntimeImportAttribute.
        /// WindowsRuntimeImportAttribute is a pseudo custom attribute defined as an internal class in System.Runtime.InteropServices.WindowsRuntime namespace.
        /// This is needed to mark Windows runtime types which are redefined in mscorlib.dll and System.Runtime.WindowsRuntime.dll.
        /// These two assemblies are special as they implement the CLR's support for WinRT.
        /// </remarks>
        internal abstract bool IsWindowsRuntimeImport { get; }

        /// <summary>
        /// True if the type should have its WinRT interfaces projected onto .NET types and
        /// have missing .NET interface members added to the type.
        /// </summary>
        internal abstract bool ShouldAddWinRTMembers { get; }

        /// <summary>
        /// Returns a flag indicating whether this symbol is declared conditionally.
        /// </summary>
        internal virtual bool IsConditional
        {
            get
            {
                //if (this.GetAppliedConditionalSymbols().Any())    // TODO
                //{
                //    return true;
                //}

                //// Conditional attributes are inherited by derived types.
                //var baseType = this.BaseType;// NoUseSiteDiagnostics;
                //return (object)baseType != null ? baseType.IsConditional : false;

                return false;
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

        internal override bool Equals(TypeSymbol t2, bool ignoreCustomModifiersAndArraySizesAndLowerBounds = false, bool ignoreDynamic = false)
        {
            //Debug.Assert(!this.IsTupleType);

            if ((object)t2 == this) return true;
            if ((object)t2 == null) return false;

            if (ignoreDynamic)
            {
                if (t2.TypeKind == TypeKind.Dynamic)
                {
                    // if ignoring dynamic, then treat dynamic the same as the type 'object'
                    if (this.SpecialType == SpecialType.System_Object)
                    {
                        return true;
                    }
                }
            }

            //if ((comparison & TypeCompareKind.IgnoreTupleNames) != 0)
            //{
            //    // If ignoring tuple element names, compare underlying tuple types
            //    if (t2.IsTupleType)
            //    {
            //        t2 = t2.TupleUnderlyingType;
            //        if (this.Equals(t2, ignoreCustomModifiersAndArraySizesAndLowerBounds, ignoreDynamic)) return true;
            //    }
            //}

            NamedTypeSymbol other = t2 as NamedTypeSymbol;
            if ((object)other == null) return false;

            // Compare OriginalDefinitions.
            var thisOriginalDefinition = this.OriginalDefinition;
            var otherOriginalDefinition = other.OriginalDefinition;

            if (((object)this == (object)thisOriginalDefinition || (object)other == (object)otherOriginalDefinition) &&
                !(ignoreCustomModifiersAndArraySizesAndLowerBounds && (this.HasTypeArgumentsCustomModifiers || other.HasTypeArgumentsCustomModifiers)))
            {
                return false;
            }

            // CONSIDER: original definitions are not unique for missing metadata type
            // symbols.  Therefore this code may not behave correctly if 'this' is List<int>
            // where List`1 is a missing metadata type symbol, and other is similarly List<int>
            // but for a reference-distinct List`1.
            if (thisOriginalDefinition != otherOriginalDefinition)
            {
                return false;
            }

            // The checks above are supposed to handle the vast majority of cases.
            // More complicated cases are handled in a special helper to make the common case scenario simple/fast (fewer locals and smaller stack frame)
            return EqualsComplicatedCases(other, ignoreCustomModifiersAndArraySizesAndLowerBounds, ignoreDynamic);
        }

        /// <summary>
        /// Helper for more complicated cases of Equals like when we have generic instantiations or types nested within them.
        /// </summary>
        private bool EqualsComplicatedCases(NamedTypeSymbol other, bool ignoreCustomModifiersAndArraySizesAndLowerBounds = false, bool ignoreDynamic = false)
        {
            if ((object)this.ContainingType != null &&
                !this.ContainingType.Equals(other.ContainingType, ignoreCustomModifiersAndArraySizesAndLowerBounds, ignoreDynamic))
            {
                return false;
            }

            var thisIsNotConstructed = ReferenceEquals(ConstructedFrom, this);
            var otherIsNotConstructed = ReferenceEquals(other.ConstructedFrom, other);

            if (thisIsNotConstructed && otherIsNotConstructed)
            {
                // Note that the arguments might appear different here due to alpha-renaming.  For example, given
                // class A<T> { class B<U> {} }
                // The type A<int>.B<int> is "constructed from" A<int>.B<1>, which may be a distinct type object
                // with a different alpha-renaming of B's type parameter every time that type expression is bound,
                // but these should be considered the same type each time.
                return true;
            }

            if (((thisIsNotConstructed || otherIsNotConstructed) &&
                 !(ignoreCustomModifiersAndArraySizesAndLowerBounds && (this.HasTypeArgumentsCustomModifiers || other.HasTypeArgumentsCustomModifiers))) ||
                this.IsUnboundGenericType != other.IsUnboundGenericType)
            {
                return false;
            }

            bool hasTypeArgumentsCustomModifiers = this.HasTypeArgumentsCustomModifiers;

            if (!ignoreCustomModifiersAndArraySizesAndLowerBounds && hasTypeArgumentsCustomModifiers != other.HasTypeArgumentsCustomModifiers)
            {
                return false;
            }

            var typeArguments = this.TypeArgumentsNoUseSiteDiagnostics.ToArray();
            var otherTypeArguments = other.TypeArgumentsNoUseSiteDiagnostics.ToArray();
            int count = typeArguments.Length;

            // since both are constructed from the same (original) type, they must have the same arity
            Debug.Assert(count == otherTypeArguments.Length);

            for (int i = 0; i < count; i++)
            {
                if (!typeArguments[i].Equals(otherTypeArguments[i], ignoreCustomModifiersAndArraySizesAndLowerBounds, ignoreDynamic)) return false;
            }

            if (!ignoreCustomModifiersAndArraySizesAndLowerBounds && hasTypeArgumentsCustomModifiers)
            {
                Debug.Assert(other.HasTypeArgumentsCustomModifiers);

                for (int i = 0; i < count; i++)
                {
                    if (!this.GetTypeArgumentCustomModifiers(i).SequenceEqual(other.GetTypeArgumentCustomModifiers(i)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        internal NamedTypeSymbol ConstructWithoutModifiers(ImmutableArray<TypeSymbol> arguments, bool unbound)
        {
            ImmutableArray<TypeWithModifiers> modifiedArguments;

            if (arguments.IsDefault)
            {
                modifiedArguments = default(ImmutableArray<TypeWithModifiers>);
            }
            else if (arguments.IsEmpty)
            {
                modifiedArguments = ImmutableArray<TypeWithModifiers>.Empty;
            }
            else
            {
                var builder = ArrayBuilder<TypeWithModifiers>.GetInstance(arguments.Length);
                foreach (TypeSymbol t in arguments)
                {
                    builder.Add((object)t == null ? default(TypeWithModifiers) : new TypeWithModifiers(t));
                }

                modifiedArguments = builder.ToImmutableAndFree();
            }

            return Construct(modifiedArguments, unbound);
        }

        internal NamedTypeSymbol Construct(ImmutableArray<TypeWithModifiers> arguments, bool unbound)
        {
            if (!ReferenceEquals(this, ConstructedFrom) || this.Arity == 0)
            {
                throw new InvalidOperationException();
            }

            if (arguments.IsDefault)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            //if (arguments.Any(TypeSymbolIsNullFunction))
            //{
            //    throw new ArgumentException(CSharpResources.TypeArgumentCannotBeNull, "typeArguments");
            //}

            if (arguments.Length != this.Arity)
            {
                throw new ArgumentException();// (CSharpResources.WrongNumberOfTypeArguments, "typeArguments");
            }

            //Debug.Assert(!unbound || arguments.All(TypeSymbolIsErrorType));

            if (ConstructedNamedTypeSymbol.TypeParametersMatchTypeArguments(this.TypeParameters, arguments))
            {
                return this;
            }

            return this.ConstructCore(arguments, unbound);
        }

        protected virtual NamedTypeSymbol ConstructCore(ImmutableArray<TypeWithModifiers> typeArguments, bool unbound)
        {
            return new ConstructedNamedTypeSymbol(this, typeArguments, unbound);
        }

        internal NamedTypeSymbol GetUnboundGenericTypeOrSelf()
        {
            if (!this.IsGenericType)
            {
                return this;
            }

            return this.ConstructUnboundGenericType();
        }

        ImmutableArray<ITypeSymbol> INamedTypeSymbol.TypeArguments => StaticCast<ITypeSymbol>.From(TypeArguments);

        public virtual ImmutableArray<TypeSymbol> TypeArguments => ImmutableArray<TypeSymbol>.Empty;

        /// <summary>
        /// Returns custom modifiers for the type argument that has been substituted for the type parameter. 
        /// The modifiers correspond to the type argument at the same ordinal within the <see cref="TypeArgumentsNoUseSiteDiagnostics"/>
        /// array.
        /// </summary>
        public abstract ImmutableArray<CustomModifier> GetTypeArgumentCustomModifiers(int ordinal);

        internal ImmutableArray<CustomModifier> GetEmptyTypeArgumentCustomModifiers(int ordinal)
        {
            if (ordinal < 0 || ordinal >= Arity)
            {
                throw new System.IndexOutOfRangeException();
            }

            return ImmutableArray<CustomModifier>.Empty;
        }

        public virtual ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        /// <summary>
        /// The original definition of this symbol. If this symbol is constructed from another
        /// symbol by type substitution then OriginalDefinition gets the original symbol as it was defined in
        /// source or metadata.
        /// </summary>
        public new virtual NamedTypeSymbol OriginalDefinition => this;

        protected override TypeSymbol OriginalTypeSymbolDefinition => OriginalDefinition;

        INamedTypeSymbol INamedTypeSymbol.OriginalDefinition => this.OriginalDefinition;

        /// <summary>
        /// Returns the map from type parameters to type arguments.
        /// If this is not a generic type instantiation, returns null.
        /// The map targets the original definition of the type.
        /// </summary>
        internal virtual TypeMap TypeSubstitution
        {
            get { return null; }
        }

        internal virtual NamedTypeSymbol AsMember(NamedTypeSymbol newOwner)
        {
            Debug.Assert(this.IsDefinition);
            Debug.Assert(ReferenceEquals(newOwner.OriginalDefinition, this.ContainingSymbol.OriginalDefinition));
            return newOwner.IsDefinition ? this : new SubstitutedNestedTypeSymbol((SubstitutedNamedTypeSymbol)newOwner, this);
        }

        /// <summary>
        /// PHP constructor method in this class.
        /// Can be <c>null</c>.
        /// </summary>
        internal MethodSymbol ResolvePhpCtor(bool recursive = false)
        {
            // resolve __construct()
            var ctor = GetMembersByPhpName(Devsense.PHP.Syntax.Name.SpecialMethodNames.Construct.Value).OfType<MethodSymbol>().FirstOrDefault();

            // resolve PHP$-like constructor (if the class is not namespaced)
            if (ctor == null && this.PhpQualifiedName().IsSimpleName)
            {
                ctor = this.GetMembersByPhpName(this.Name).OfType<MethodSymbol>().FirstOrDefault();
            }

            // if method was not resolved, look into parent
            if (ctor == null && recursive)
            {
                ctor = this.BaseType?.ResolvePhpCtor(true);
            }

            //
            return ctor;
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

        ImmutableArray<ITypeParameterSymbol> INamedTypeSymbol.TypeParameters => StaticCast<ITypeParameterSymbol>.From(this.TypeParameters);

        INamedTypeSymbol INamedTypeSymbol.ConstructUnboundGenericType() => ConstructUnboundGenericType();

        INamedTypeSymbol INamedTypeSymbol.Construct(params ITypeSymbol[] arguments)
        {
            //foreach (var arg in arguments)
            //{
            //    arg.EnsureCSharpSymbolOrNull<ITypeSymbol, TypeSymbol>("typeArguments");
            //}
            Debug.Assert(arguments.All(t => t is TypeSymbol));

            return this.Construct(arguments.Cast<TypeSymbol>().ToArray());
        }

        IMethodSymbol INamedTypeSymbol.DelegateInvokeMethod => DelegateInvokeMethod;

        bool INamedTypeSymbol.IsComImport => false;

        INamedTypeSymbol INamedTypeSymbol.TupleUnderlyingType => TupleUnderlyingType;

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

        /// <summary>
        /// Returns a constructed type given its type arguments.
        /// </summary>
        /// <param name="typeArguments">The immediate type arguments to be replaced for type
        /// parameters in the type.</param>
        public NamedTypeSymbol Construct(params TypeSymbol[] typeArguments)
        {
            return ConstructWithoutModifiers(typeArguments.AsImmutableOrNull(), false);
        }

        /// <summary>
        /// Returns a constructed type given its type arguments.
        /// </summary>
        /// <param name="typeArguments">The immediate type arguments to be replaced for type
        /// parameters in the type.</param>
        public NamedTypeSymbol Construct(ImmutableArray<TypeSymbol> typeArguments)
        {
            return ConstructWithoutModifiers(typeArguments, false);
        }

        /// <summary>
        /// Returns a constructed type given its type arguments.
        /// </summary>
        /// <param name="typeArguments"></param>
        public NamedTypeSymbol Construct(IEnumerable<TypeSymbol> typeArguments)
        {
            return ConstructWithoutModifiers(typeArguments.AsImmutableOrNull(), false);
        }

        /// <summary>
        /// Returns an unbound generic type of this named type.
        /// </summary>
        public NamedTypeSymbol ConstructUnboundGenericType()
        {
            return OriginalDefinition.AsUnboundGenericType();
        }

        #endregion
    }
}
