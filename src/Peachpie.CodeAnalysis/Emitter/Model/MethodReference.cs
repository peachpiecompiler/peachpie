using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Cci = Microsoft.Cci;
using Pchp.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Emit
{
    internal abstract class MethodReference : TypeMemberReference, Cci.IMethodReference
    {
        protected readonly MethodSymbol UnderlyingMethod;

        public MethodReference(MethodSymbol underlyingMethod)
        {
            Debug.Assert((object)underlyingMethod != null);

            this.UnderlyingMethod = underlyingMethod;
        }

        protected override Symbol UnderlyingSymbol
        {
            get
            {
                return UnderlyingMethod;
            }
        }

        bool Cci.IMethodReference.AcceptsExtraArguments
        {
            get
            {
                return UnderlyingMethod.IsVararg;
            }
        }

        ushort Cci.IMethodReference.GenericParameterCount
        {
            get
            {
                return (ushort)UnderlyingMethod.Arity;
            }
        }

        bool Cci.IMethodReference.IsGeneric
        {
            get
            {
                return UnderlyingMethod.IsGenericMethod;
            }
        }

        ushort Cci.ISignature.ParameterCount
        {
            get
            {
                return (ushort)UnderlyingMethod.ParameterCount;
            }
        }

        Cci.IMethodDefinition Cci.IMethodReference.GetResolvedMethod(EmitContext context)
        {
            return null;
        }

        ImmutableArray<Cci.IParameterTypeInformation> Cci.IMethodReference.ExtraParameters
        {
            get
            {
                return ImmutableArray<Cci.IParameterTypeInformation>.Empty;
            }
        }

        Cci.CallingConvention Cci.ISignature.CallingConvention
        {
            get
            {
                return UnderlyingMethod.CallingConvention;
            }
        }

        ImmutableArray<Cci.IParameterTypeInformation> Cci.ISignature.GetParameters(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            return moduleBeingBuilt.Translate(UnderlyingMethod.Parameters);
        }

        ImmutableArray<Cci.ICustomModifier> Cci.ISignature.ReturnValueCustomModifiers
        {
            get
            {
                return UnderlyingMethod.ReturnTypeCustomModifiers.As<Cci.ICustomModifier>();
            }
        }

        bool Cci.ISignature.ReturnValueIsByRef
        {
            get
            {
                return UnderlyingMethod.ReturnValueIsByRef;
            }
        }

        Cci.ITypeReference Cci.ISignature.GetType(EmitContext context)
        {
            return ((PEModuleBuilder)context.Module).Translate(UnderlyingMethod.ReturnType, syntaxNodeOpt: context.SyntaxNodeOpt, diagnostics: context.Diagnostics);
        }

        public virtual Cci.IGenericMethodInstanceReference AsGenericMethodInstanceReference
        {
            get
            {
                return null;
            }
        }

        public virtual Cci.ISpecializedMethodReference AsSpecializedMethodReference
        {
            get
            {
                return null;
            }
        }

        ImmutableArray<Cci.ICustomModifier> Cci.ISignature.RefCustomModifiers => ImmutableArray<Cci.ICustomModifier>.Empty;
    }
}
