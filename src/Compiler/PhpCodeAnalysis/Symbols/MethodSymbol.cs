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

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a method or method-like symbol (including constructor,
    /// destructor, operator, or property/event accessor).
    /// </summary>
    internal abstract partial class MethodSymbol : Symbol, IMethodSymbol, ISemanticFunction
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

        /// <summary>
        /// True if this method is hidden if a derived type declares a method with the same name and signature. 
        /// If false, any method with the same name hides this method. This flag is ignored by the runtime and is only used by compilers.
        /// </summary>
        public bool HidesBaseMethodsByName
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

        public virtual DllImportData GetDllImportData() => null;

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

        #region ISemanticFunction

        public virtual TypeRefMask GetExpectedParamType(TypeRefContext ctx, int index)
        {
            throw new NotImplementedException();
        }

        public virtual TypeRefMask GetResultType(TypeRefContext ctx)
        {
            throw new NotImplementedException();
        }

        public virtual ImmutableArray<ControlFlowGraph> CFG => ImmutableArray<ControlFlowGraph>.Empty;

        public virtual int MandatoryParamsCount => Parameters.Length;    // TODO: only mandatory

        public virtual bool IsParamByRef(int index)
        {
            throw new NotImplementedException();
        }

        public virtual bool IsParamVariadic(int index)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
