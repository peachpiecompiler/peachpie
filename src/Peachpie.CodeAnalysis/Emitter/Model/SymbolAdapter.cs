using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis
{
    internal partial class Symbol : Cci.IReference
    {
        /// <summary>
        /// Checks if this symbol is a definition and its containing module is a SourceModuleSymbol.
        /// </summary>
        [Conditional("DEBUG")]
        internal protected void CheckDefinitionInvariant()
        {
            // can't be generic instantiation
            Debug.Assert(this.IsDefinition);

            // must be declared in the module we are building
            Debug.Assert(this.ContainingModule is SourceModuleSymbol ||
                         (this.Kind == SymbolKind.Assembly && this is SourceAssemblySymbol) ||
                         (this.Kind == SymbolKind.NetModule && this is SourceModuleSymbol));
        }

        /// <summary>
        /// Return whether the symbol is either the original definition
        /// or distinct from the original. Intended for use in Debug.Assert
        /// only since it may include a deep comparison.
        /// </summary>
        internal bool IsDefinitionOrDistinct()
        {
            return this.IsDefinition || !SymbolEqualityComparer.Default.Equals(this, OriginalDefinition);
        }

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            throw new NotSupportedException();
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            throw new NotSupportedException();
        }

        internal virtual IEnumerable<AttributeData> GetCustomAttributesToEmit(CommonModuleCompilationState compilationState)
        {
            return this.GetAttributes();
        }

        IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context)
        {
            var attrs = GetCustomAttributesToEmit(((Emit.PEModuleBuilder)context.Module).CompilationState).Cast<Cci.ICustomAttribute>();

            // add [PhpMemberVisibilityAttribute( DeclaredAccessibility )] for non-public members emitted as public
            if ((DeclaredAccessibility == Accessibility.Private || DeclaredAccessibility == Accessibility.Protected) &&
                Emit.PEModuleBuilder.MemberVisibility(this) == Cci.TypeMemberVisibility.Public)
            {
                attrs = attrs.Concat(new[]
                {
                    (Cci.ICustomAttribute)((Emit.PEModuleBuilder)context.Module).Compilation.GetPhpMemberVisibilityAttribute(this, DeclaredAccessibility)
                });
            }

            return attrs;
        }
    }
}
