using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Cci = Microsoft.Cci;
using System.Collections.Immutable;
using Pchp.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.CodeGen;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    internal partial class ParameterSymbol :
        Cci.IParameterTypeInformation,
        Cci.IParameterDefinition
    {
        ImmutableArray<Cci.ICustomModifier> Cci.IParameterTypeInformation.CustomModifiers => this.CustomModifiers.As<Cci.ICustomModifier>();

        ImmutableArray<Cci.ICustomModifier> Cci.IParameterTypeInformation.RefCustomModifiers => this.RefCustomModifiers.As<Cci.ICustomModifier>();

        bool Cci.IParameterTypeInformation.IsByReference => this.RefKind != Microsoft.CodeAnalysis.RefKind.None;

        public virtual ushort CountOfCustomModifiersPrecedingByRef => 0;

        Cci.ITypeReference Cci.IParameterTypeInformation.GetType(EmitContext context)
        {
            return ((PEModuleBuilder)context.Module).Translate(this.Type, null, context.Diagnostics);
        }

        ushort Cci.IParameterListEntry.Index => (ushort)this.Ordinal;

        /// <summary>
        /// Gets constant value to be stored in metadata Constant table.
        /// </summary>
        MetadataConstant Cci.IParameterDefinition.GetDefaultValue(EmitContext context)
        {
            CheckDefinitionInvariant();
            return this.GetMetadataConstantValue(context);
        }

        internal MetadataConstant GetMetadataConstantValue(EmitContext context)
        {
            if (!HasMetadataConstantValue)
            {
                return null;
            }

            ConstantValue constant = this.ExplicitDefaultConstantValue;
            TypeSymbol type;
            if (constant.SpecialType != SpecialType.None)
            {
                // preserve the exact type of the constant for primitive types,
                // e.g. it should be Int16 for [DefaultParameterValue((short)1)]int x
                type = this.ContainingAssembly.GetSpecialType(constant.SpecialType);
            }
            else
            {
                // default(struct), enum
                type = this.Type;
            }

            return ((PEModuleBuilder)context.Module).CreateConstant(type, constant.Value,
                                                           syntaxNodeOpt: context.SyntaxNodeOpt,
                                                           diagnostics: context.Diagnostics);
        }

        internal virtual bool HasMetadataConstantValue
        {
            get
            {
                CheckDefinitionInvariant();

                return HasExplicitDefaultValue;
            }
        }

        bool Cci.IParameterDefinition.HasDefaultValue
        {
            get
            {
                CheckDefinitionInvariant();
                return HasMetadataConstantValue;
            }
        }

        public bool HasExplicitDefaultValue => this.ExplicitDefaultConstantValue != null;

        /// <summary>
        /// Returns the default value of the parameter. If <see cref="HasExplicitDefaultValue"/>
        /// returns false then DefaultValue throws an InvalidOperationException.
        /// </summary>
        /// <remarks>
        /// If the parameter type is a struct and the default value of the parameter
        /// is the default value of the struct type or of type parameter type which is 
        /// not known to be a referenced type, then this property will return null.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The parameter has no default value.</exception>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public object ExplicitDefaultValue
        {
            get
            {
                if (HasExplicitDefaultValue)
                {
                    return ExplicitDefaultConstantValue.Value;
                }

                throw new InvalidOperationException();
            }
        }

        bool Cci.IParameterDefinition.IsOptional
        {
            get
            {
                CheckDefinitionInvariant();
                return this.IsOptional;
            }
        }

        bool Cci.IParameterDefinition.IsIn
        {
            get
            {
                CheckDefinitionInvariant();
                return false; // this.IsMetadataIn;
            }
        }

        bool Cci.IParameterDefinition.IsMarshalledExplicitly
        {
            get
            {
                CheckDefinitionInvariant();
                return this.IsMarshalledExplicitly;
            }
        }

        internal virtual bool IsMarshalledExplicitly
        {
            get
            {
                CheckDefinitionInvariant();
                return ((Cci.IParameterDefinition)this).MarshallingInformation != null;
            }
        }

        bool Cci.IParameterDefinition.IsOut
        {
            get
            {
                CheckDefinitionInvariant();
                return false; // IsMetadataOut;
            }
        }

        Cci.IMarshallingInformation Cci.IParameterDefinition.MarshallingInformation
        {
            get
            {
                CheckDefinitionInvariant();
                return null; // this.MarshallingInformation;
            }
        }

        ImmutableArray<byte> Cci.IParameterDefinition.MarshallingDescriptor
        {
            get
            {
                CheckDefinitionInvariant();
                return this.MarshallingDescriptor;
            }
        }

        internal virtual ImmutableArray<byte> MarshallingDescriptor
        {
            get
            {
                CheckDefinitionInvariant();
                return default(ImmutableArray<byte>);
            }
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable;
            //At present we have no scenario that needs this method.
            //Should one arise, uncomment implementation and add a test.
#if false   
            Debug.Assert(this.IsDefinitionOrDistinct());

            if (!this.IsDefinition)
            {
                visitor.Visit((IParameterTypeInformation)this);
            }
            else if (this.ContainingModule == ((Module)visitor.Context).SourceModule)
            {
                visitor.Visit((IParameterDefinition)this);
            }
            else
            {
                visitor.Visit((IParameterTypeInformation)this);
            }
#endif
        }

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            Debug.Assert(this.IsDefinitionOrDistinct());

            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            if (this.IsDefinition &&
                object.ReferenceEquals(this.ContainingModule, moduleBeingBuilt.SourceModule))
            {
                return this;
            }

            return null;
        }

        string Cci.INamedEntity.Name
        {
            get { return this.MetadataName; }
        }
    }
}
