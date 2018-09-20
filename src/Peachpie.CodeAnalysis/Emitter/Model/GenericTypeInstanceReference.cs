using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Emit;
using System.Diagnostics;
using Cci = Microsoft.Cci;
using Pchp.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Pchp.CodeAnalysis.Emit
{
    /// <summary>
    /// Represents a reference to a generic type instantiation.
    /// Subclasses represent nested and namespace types.
    /// </summary>
    internal abstract class GenericTypeInstanceReference : NamedTypeReference, Cci.IGenericTypeInstanceReference
    {
        public GenericTypeInstanceReference(NamedTypeSymbol underlyingNamedType)
            : base(underlyingNamedType)
        {
            Debug.Assert(underlyingNamedType.IsDefinition);
            // Definition doesn't have custom modifiers on type arguments
            //Debug.Assert(!underlyingNamedType.HasTypeArgumentsCustomModifiers);
        }

        public sealed override void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.IGenericTypeInstanceReference)this);
        }

        ImmutableArray<Cci.ITypeReference> Cci.IGenericTypeInstanceReference.GetGenericArguments(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            var builder = ArrayBuilder<Cci.ITypeReference>.GetInstance();
            foreach (TypeSymbol type in UnderlyingNamedType.TypeArgumentsNoUseSiteDiagnostics)
            {
                builder.Add(moduleBeingBuilt.Translate(type, syntaxNodeOpt: context.SyntaxNodeOpt, diagnostics: context.Diagnostics));
            }

            return builder.ToImmutableAndFree();
        }

        Cci.INamedTypeReference Cci.IGenericTypeInstanceReference.GetGenericType(EmitContext context)
        {
            System.Diagnostics.Debug.Assert(UnderlyingNamedType.OriginalDefinition.IsDefinition);
            return UnderlyingNamedType.OriginalDefinition;
        }
    }
}
