using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis.Symbols
{
    internal partial class PropertySymbol : Cci.IPropertyDefinition
    {
        #region IPropertyDefinition Members

        IEnumerable<Cci.IMethodReference> Cci.IPropertyDefinition.GetAccessors(EmitContext context)
        {
            CheckDefinitionInvariant();

            var getMethod = this.GetMethod;
            if ((object)getMethod != null)
            {
                yield return getMethod;
            }

            var setMethod = this.SetMethod;
            if ((object)setMethod != null)
            {
                yield return setMethod;
            }

            //SourcePropertySymbol sourceProperty = this as SourcePropertySymbol;
            //if ((object)sourceProperty != null)
            //{
            //    SynthesizedSealedPropertyAccessor synthesizedAccessor = sourceProperty.SynthesizedSealedAccessorOpt;
            //    if ((object)synthesizedAccessor != null)
            //    {
            //        yield return synthesizedAccessor;
            //    }
            //}
        }

        MetadataConstant Cci.IPropertyDefinition.DefaultValue
        {
            get
            {
                CheckDefinitionInvariant();
                return null;
            }
        }

        Cci.IMethodReference Cci.IPropertyDefinition.Getter
        {
            get
            {
                CheckDefinitionInvariant();
                MethodSymbol getMethod = this.GetMethod;
                if ((object)getMethod != null || !this.IsSealed)
                {
                    return getMethod;
                }

                return GetSynthesizedSealedAccessor(MethodKind.PropertyGet);
            }
        }

        bool Cci.IPropertyDefinition.HasDefaultValue
        {
            get
            {
                CheckDefinitionInvariant();
                return false;
            }
        }

        bool Cci.IPropertyDefinition.IsRuntimeSpecial
        {
            get
            {
                CheckDefinitionInvariant();
                return HasRuntimeSpecialName;
            }
        }

        internal virtual bool HasRuntimeSpecialName
        {
            get
            {
                CheckDefinitionInvariant();
                return false;
            }
        }

        bool Cci.IPropertyDefinition.IsSpecialName
        {
            get
            {
                CheckDefinitionInvariant();
                return this.HasSpecialName;
            }
        }

        ImmutableArray<Cci.IParameterDefinition> Cci.IPropertyDefinition.Parameters
        {
            get
            {
                CheckDefinitionInvariant();
                return StaticCast<Cci.IParameterDefinition>.From(this.Parameters);
            }
        }

        Cci.IMethodReference Cci.IPropertyDefinition.Setter
        {
            get
            {
                CheckDefinitionInvariant();
                MethodSymbol setMethod = this.SetMethod;
                if ((object)setMethod != null || !this.IsSealed)
                {
                    return setMethod;
                }

                return GetSynthesizedSealedAccessor(MethodKind.PropertySet);
            }
        }

        #endregion

        #region ISignature Members

        Cci.CallingConvention Cci.ISignature.CallingConvention
        {
            get
            {
                CheckDefinitionInvariant();
                return this.CallingConvention;
            }
        }

        ushort Cci.ISignature.ParameterCount
        {
            get
            {
                CheckDefinitionInvariant();
                return (ushort)this.ParameterCount;
            }
        }

        ImmutableArray<Cci.IParameterTypeInformation> Cci.ISignature.GetParameters(EmitContext context)
        {
            CheckDefinitionInvariant();
            return StaticCast<Cci.IParameterTypeInformation>.From(this.Parameters);
        }

        ImmutableArray<Cci.ICustomModifier> Cci.ISignature.ReturnValueCustomModifiers
        {
            get
            {
                CheckDefinitionInvariant();
                return this.TypeCustomModifiers.As<Cci.ICustomModifier>();
            }
        }

        bool Cci.ISignature.ReturnValueIsByRef
        {
            get
            {
                CheckDefinitionInvariant();
                return false; // this.Type is ByRefReturnErrorTypeSymbol;
            }
        }

        Cci.ITypeReference Cci.ISignature.GetType(EmitContext context)
        {
            CheckDefinitionInvariant();
            return ((PEModuleBuilder)context.Module).Translate(this.Type,
                                                      syntaxNodeOpt: context.SyntaxNodeOpt,
                                                      diagnostics: context.Diagnostics);
        }

        ImmutableArray<Cci.ICustomModifier> Cci.ISignature.RefCustomModifiers => ImmutableArray<Cci.ICustomModifier>.Empty;

        #endregion

        #region ITypeDefinitionMember Members

        Cci.ITypeDefinition Cci.ITypeDefinitionMember.ContainingTypeDefinition
        {
            get
            {
                CheckDefinitionInvariant();
                return (Cci.ITypeDefinition)this.ContainingType;
            }
        }

        Cci.TypeMemberVisibility Cci.ITypeDefinitionMember.Visibility
        {
            get
            {
                CheckDefinitionInvariant();
                return PEModuleBuilder.MemberVisibility(this);
            }
        }

        #endregion

        #region ITypeMemberReference Members

        Cci.ITypeReference Cci.ITypeMemberReference.GetContainingType(EmitContext context)
        {
            CheckDefinitionInvariant();
            return (Cci.ITypeReference)this.ContainingType;
        }

        #endregion

        #region IReference Members

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            CheckDefinitionInvariant();
            visitor.Visit((Cci.IPropertyDefinition)this);
        }

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            CheckDefinitionInvariant();
            return this;
        }

        #endregion

        #region INamedEntity Members

        string Cci.INamedEntity.Name
        {
            get
            {
                CheckDefinitionInvariant();
                return this.MetadataName;
            }
        }

        #endregion

        private Cci.IMethodReference GetSynthesizedSealedAccessor(MethodKind targetMethodKind)
        {
            //SourcePropertySymbol sourceProperty = this as SourcePropertySymbol;
            //if ((object)sourceProperty != null)
            //{
            //    SynthesizedSealedPropertyAccessor synthesized = sourceProperty.SynthesizedSealedAccessorOpt;
            //    return (object)synthesized != null && synthesized.MethodKind == targetMethodKind ? synthesized : null;
            //}

            return null;
        }
    }
}
