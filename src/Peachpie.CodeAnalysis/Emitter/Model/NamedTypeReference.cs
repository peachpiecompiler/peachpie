using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Symbols;
using Roslyn.Utilities;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis.Emit
{
    internal abstract class NamedTypeReference : Cci.INamedTypeReference
    {
        protected readonly NamedTypeSymbol UnderlyingNamedType;

        public NamedTypeReference(NamedTypeSymbol underlyingNamedType)
        {
            Debug.Assert((object)underlyingNamedType != null);

            this.UnderlyingNamedType = underlyingNamedType;
        }

        ushort Cci.INamedTypeReference.GenericParameterCount => (ushort)UnderlyingNamedType.Arity;

        bool Cci.INamedTypeReference.MangleName => UnderlyingNamedType.MangleName;

        string Cci.INamedEntity.Name => UnderlyingNamedType.MetadataName;

        bool Cci.ITypeReference.IsEnum => UnderlyingNamedType.IsEnumType();

        bool Cci.ITypeReference.IsValueType => UnderlyingNamedType.IsValueType;

        Cci.ITypeDefinition Cci.ITypeReference.GetResolvedType(EmitContext context)
        {
            return null;
        }

        Cci.PrimitiveTypeCode Cci.ITypeReference.TypeCode(EmitContext context)
        {
            return Cci.PrimitiveTypeCode.NotPrimitive;
        }

        TypeDefinitionHandle Cci.ITypeReference.TypeDef
        {
            get
            {
                return default(TypeDefinitionHandle);
            }
        }

        Cci.IGenericMethodParameterReference Cci.ITypeReference.AsGenericMethodParameterReference
        {
            get
            {
                return null;
            }
        }

        public abstract Cci.IGenericTypeInstanceReference AsGenericTypeInstanceReference
        {
            get;
        }

        Cci.IGenericTypeParameterReference Cci.ITypeReference.AsGenericTypeParameterReference
        {
            get
            {
                return null;
            }
        }

        Cci.INamespaceTypeDefinition Cci.ITypeReference.AsNamespaceTypeDefinition(EmitContext context)
        {
            return null;
        }

        public abstract Cci.INamespaceTypeReference AsNamespaceTypeReference
        {
            get;
        }

        Cci.INestedTypeDefinition Cci.ITypeReference.AsNestedTypeDefinition(EmitContext context)
        {
            return null;
        }

        public abstract Cci.INestedTypeReference AsNestedTypeReference
        {
            get;
        }

        public abstract Cci.ISpecializedNestedTypeReference AsSpecializedNestedTypeReference
        {
            get;
        }

        Cci.ITypeDefinition Cci.ITypeReference.AsTypeDefinition(EmitContext context)
        {
            return null;
        }

        //public override string ToString()
        //{
        //    return UnderlyingNamedType.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat);
        //}

        IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
        }

        public abstract void Dispatch(Cci.MetadataVisitor visitor);

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            return null;
        }
    }
}
