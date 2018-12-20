using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Symbols;
using AST = Devsense.PHP.Syntax.Ast;

namespace Pchp.CodeAnalysis
{
    internal static partial class TypeRefFactory
    {
        public static TypeRefMask CreateMask(TypeRefContext ctx, TypeSymbol t) => BoundTypeRefFactory.Create(t).GetTypeRefMask(ctx);
        
        /// <summary>
        /// Creates type context for a method within given type, determines naming, type context.
        /// </summary>
        public static TypeRefContext/*!*/CreateTypeRefContext(SourceTypeSymbol/*!*/containingType)
        {
            Contract.ThrowIfNull(containingType);

            return new TypeRefContext(
                containingType, // scope
                thisType: containingType.IsTrait ? null : containingType);
        }
    }
}
