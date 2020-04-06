using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics.Graph;
using System.Reflection;
using System.Diagnostics;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a method or method-like symbol (including constructor,
    /// destructor, operator, or property/event accessor).
    /// </summary>
    internal abstract partial class MethodSymbol : Symbol, IMethodSymbol, IPhpRoutineSymbol
    {
        public virtual int Arity => 0;

        public INamedTypeSymbol AssociatedAnonymousDelegate
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual ISymbol AssociatedSymbol
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual IMethodSymbol ConstructedFrom => this;

        ImmutableArray<IMethodSymbol> IMethodSymbol.ExplicitInterfaceImplementations => StaticCast<IMethodSymbol>.From(ExplicitInterfaceImplementations);

        public virtual ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

        /// <summary>
        /// Returns value 'Method' of the <see cref="SymbolKind"/>
        /// </summary>
        public sealed override SymbolKind Kind => SymbolKind.Method;

        /// <summary>
        /// True if this method is hidden if a derived type declares a method with the same name and signature. 
        /// If false, any method with the same name hides this method. This flag is ignored by the runtime and is only used by compilers.
        /// </summary>
        public virtual bool HidesBaseMethodsByName
        {
            get
            {
                return true;
            }
        }

        public virtual bool IsAsync => false;

        public virtual bool IsCheckedBuiltin
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual bool IsExtensionMethod => false;

        /// <summary>
        /// Returns whether this method is generic; i.e., does it have any type parameters?
        /// </summary>
        public virtual bool IsGenericMethod
        {
            get
            {
                return this.Arity != 0;
            }
        }

        public virtual bool IsVararg => false;

        internal virtual bool HasSpecialName => false;

        /// <summary>
        /// Gets value determining the method has special <c>this</c> hidden parameter at index <c>0</c>.
        /// </summary>
        internal bool HasThis => (this.CallingConvention & Microsoft.Cci.CallingConvention.HasThis) != 0;

        internal virtual MethodImplAttributes ImplementationAttributes => MethodImplAttributes.IL;

        internal virtual bool RequiresSecurityObject => false;

        public abstract MethodKind MethodKind { get; }

        public virtual IMethodSymbol OverriddenMethod => null;

        /// <summary>
        /// Source: Was the member name qualified with a type name?
        /// Metadata: Is the member an explicit implementation?
        /// </summary>
        /// <remarks>
        /// Will not always agree with ExplicitInterfaceImplementations.Any()
        /// (e.g. if binding of the type part of the name fails).
        /// </remarks>
        internal virtual bool IsExplicitInterfaceImplementation
        {
            get { return ExplicitInterfaceImplementations.Any(); }
        }

        internal MethodSymbol AsMember(NamedTypeSymbol newOwner)
        {
            Debug.Assert(this.IsDefinition);
            Debug.Assert(ReferenceEquals(newOwner.OriginalDefinition, this.ContainingSymbol.OriginalDefinition));
            return (newOwner == this.ContainingSymbol) ? this : new SubstitutedMethodSymbol((SubstitutedNamedTypeSymbol)newOwner, this);
        }

        ImmutableArray<IParameterSymbol> IMethodSymbol.Parameters => StaticCast<IParameterSymbol>.From(Parameters);

        ImmutableArray<ITypeSymbol> IMethodSymbol.TypeArguments => StaticCast<ITypeSymbol>.From(TypeArguments);

        public abstract ImmutableArray<ParameterSymbol> Parameters { get; }

        public virtual int ParameterCount => this.Parameters.Length;

        public IMethodSymbol PartialDefinitionPart => null;

        public IMethodSymbol PartialImplementationPart => null;

        /// <summary>
        /// If this method can be applied to an object, returns the type of object it is applied to.
        /// </summary>
        public virtual ITypeSymbol ReceiverType
        {
            get
            {
                return this.ContainingType;
            }
        }

        public virtual IMethodSymbol ReducedFrom
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public abstract bool ReturnsVoid { get; }

        ITypeSymbol IMethodSymbol.ReturnType => ReturnType;

        public abstract RefKind RefKind { get; }

        public abstract TypeSymbol ReturnType { get; }

        public virtual ImmutableArray<CustomModifier> ReturnTypeCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public virtual ImmutableArray<TypeSymbol> TypeArguments => ImmutableArray<TypeSymbol>.Empty;

        public virtual ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        IMethodSymbol IMethodSymbol.OriginalDefinition => OriginalDefinition;

        public new virtual MethodSymbol OriginalDefinition => (MethodSymbol)OriginalSymbolDefinition;

        ImmutableArray<ITypeParameterSymbol> IMethodSymbol.TypeParameters => StaticCast<ITypeParameterSymbol>.From(this.TypeParameters);

        bool IMethodSymbol.ReturnsByRef => false;

        bool IMethodSymbol.ReturnsByRefReadonly => false;

        ImmutableArray<CustomModifier> IMethodSymbol.RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        IMethodSymbol IMethodSymbol.Construct(params ITypeSymbol[] typeArguments) => Construct(typeArguments);

        public MethodSymbol Construct(params ITypeSymbol[] typeArguments)
        {
            return this.Construct(typeArguments.Cast<TypeSymbol>().ToImmutableArray());
        }

        internal static readonly Func<TypeSymbol, bool> TypeSymbolIsNullFunction = type => (object)type == null;

        /// <summary>
        /// Apply type substitution to a generic method to create an method symbol with the given type parameters supplied.
        /// </summary>
        /// <param name="typeArguments"></param>
        /// <returns></returns>
        public MethodSymbol Construct(ImmutableArray<TypeSymbol> typeArguments)
        {
            if (!ReferenceEquals(this, ConstructedFrom) || this.Arity == 0)
            {
                throw new InvalidOperationException();
            }

            if (typeArguments.IsDefault)
            {
                throw new ArgumentNullException(nameof(typeArguments));
            }

            if (typeArguments.Any(TypeSymbolIsNullFunction))
            {
                throw new ArgumentException(); // (CSharpResources.TypeArgumentCannotBeNull, nameof(typeArguments));
            }

            if (typeArguments.Length != this.Arity)
            {
                throw new ArgumentException(); // (CSharpResources.WrongNumberOfTypeArguments, nameof(typeArguments));
            }

            if (TypeParametersMatchTypeArguments(this.TypeParameters, typeArguments))
            {
                return this;
            }

            return new ConstructedMethodSymbol(this, typeArguments);
        }

        internal static bool TypeParametersMatchTypeArguments(ImmutableArray<TypeParameterSymbol> typeParameters, ImmutableArray<TypeSymbol> typeArguments)
        {
            int n = typeParameters.Length;
            Debug.Assert(typeArguments.Length == n);
            Debug.Assert(typeArguments.Length > 0);

            for (int i = 0; i < n; i++)
            {
                if (!ReferenceEquals(typeArguments[i], typeParameters[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the map from type parameters to type arguments.
        /// If this is not a generic method instantiation, returns null.
        /// The map targets the original definition of the method.
        /// </summary>
        internal virtual TypeMap TypeSubstitution
        {
            get { return null; }
        }

        /// <summary>
        /// If this method is a reduced extension method, returns the extension method that
        /// should be used at call site during ILGen. Otherwise, returns null.
        /// </summary>
        internal virtual MethodSymbol CallsiteReducedFromMethod
        {
            get { return null; }
        }

        public virtual DllImportData GetDllImportData() => null;

        public virtual ImmutableArray<AttributeData> GetReturnTypeAttributes() => ImmutableArray<AttributeData>.Empty;

        public virtual ITypeSymbol GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter)
        {
            throw new NotImplementedException();
        }

        public IMethodSymbol ReduceExtensionMethod(ITypeSymbol receiverType)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets value indicating the method is annotated with [PhpHiddenAttribute] metadata.
        /// </summary>
        public virtual bool IsPhpHidden => false;

        #region IPhpRoutineSymbol

        public virtual bool CastToFalse => false;

        public virtual bool IsInitFieldsOnly => false;

        public virtual bool HasNotNull => false;

        /// <summary>
        /// For source routines, gets their control flow graph.
        /// Can be <c>null</c> for routines from PE or synthesized routines.
        /// </summary>
        public virtual ControlFlowGraph ControlFlowGraph { get => null; internal set { } }

        /// <summary>
        /// Gets the routine name, equivalent to a PHP pseudoconstant <c>__FUNCTION__</c>.
        /// </summary>
        public virtual string RoutineName => Name; // TODO: "Name" struct with correct comparer

        /// <summary>
        /// Whether routine represents a global code.
        /// </summary>
        public virtual bool IsGlobalScope => false;

        public BoundExpression Initializer => null; // not applicable for methods

        #endregion
    }
}
