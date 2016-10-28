using System.Collections.Generic;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis.Emit
{
    internal abstract class TypeMemberReference : Cci.ITypeMemberReference
    {
        protected abstract Symbol UnderlyingSymbol { get; }

        public virtual Cci.ITypeReference GetContainingType(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            return moduleBeingBuilt.Translate(UnderlyingSymbol.ContainingType, context.SyntaxNodeOpt, context.Diagnostics);
        }

        string Cci.INamedEntity.Name
        {
            get
            {
                return UnderlyingSymbol.MetadataName;
            }
        }

        ///// <remarks>
        ///// Used only for testing.
        ///// </remarks>
        //public override string ToString()
        //{
        //    return UnderlyingSymbol.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat);
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
