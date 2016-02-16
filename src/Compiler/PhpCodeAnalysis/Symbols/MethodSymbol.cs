using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a method or method-like symbol (including constructor,
    /// destructor, operator, or property/event accessor).
    /// </summary>
    internal abstract partial class MethodSymbol : Symbol, IMethodSymbol
    {
        public virtual int Arity => 0;

        public INamedTypeSymbol AssociatedAnonymousDelegate
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ISymbol AssociatedSymbol
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IMethodSymbol ConstructedFrom
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ImmutableArray<IMethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool HidesBaseMethodsByName
        {
            get
            {
                throw new NotImplementedException();
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

        public virtual bool IsGenericMethod => false;

        public virtual bool IsVararg => false;

        public abstract MethodKind MethodKind { get; }

        public virtual IMethodSymbol OverriddenMethod => null;

        public abstract ImmutableArray<IParameterSymbol> Parameters { get; }

        public IMethodSymbol PartialDefinitionPart => null;

        public IMethodSymbol PartialImplementationPart => null;

        public ITypeSymbol ReceiverType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IMethodSymbol ReducedFrom
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public abstract bool ReturnsVoid { get; }

        public abstract ITypeSymbol ReturnType { get; }

        public virtual ImmutableArray<CustomModifier> ReturnTypeCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public virtual ImmutableArray<ITypeSymbol> TypeArguments => ImmutableArray<ITypeSymbol>.Empty;

        public virtual ImmutableArray<ITypeParameterSymbol> TypeParameters => ImmutableArray<ITypeParameterSymbol>.Empty;

        IMethodSymbol IMethodSymbol.OriginalDefinition
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IMethodSymbol Construct(params ITypeSymbol[] typeArguments)
        {
            throw new NotImplementedException();
        }

        public DllImportData GetDllImportData()
        {
            throw new NotImplementedException();
        }

        public ImmutableArray<AttributeData> GetReturnTypeAttributes()
        {
            throw new NotImplementedException();
        }

        public ITypeSymbol GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter)
        {
            throw new NotImplementedException();
        }

        public IMethodSymbol ReduceExtensionMethod(ITypeSymbol receiverType)
        {
            throw new NotImplementedException();
        }
    }
}
