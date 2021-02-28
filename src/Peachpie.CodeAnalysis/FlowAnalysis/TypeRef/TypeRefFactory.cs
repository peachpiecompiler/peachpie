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
        public static TypeRefMask CreateMask(TypeRefContext ctx, TypeSymbol t, bool notNull = false)
        {
            // shortcuts:
            if (t.IsNullableType(out var ttype)) return CreateMask(ctx, ttype, notNull: true) | ctx.GetNullTypeMask();

            switch (t.SpecialType)
            {
                case SpecialType.System_Void: return 0;
                case SpecialType.System_Boolean: return ctx.GetBooleanTypeMask();
                case SpecialType.System_Int64: return ctx.GetLongTypeMask();
                case SpecialType.System_Double: return ctx.GetDoubleTypeMask();
                case SpecialType.System_String: return ctx.GetStringTypeMask() | (notNull ? 0 : ctx.GetNullTypeMask());
                case SpecialType.System_Object: return ctx.GetSystemObjectTypeMask() | (notNull ? 0 : ctx.GetNullTypeMask());
                default:

                    TypeRefMask mask;

                    if (t.Is_PhpValue())
                    {
                        return TypeRefMask.AnyType;
                    }
                    else if (t.Is_PhpAlias())
                    {
                        return TypeRefMask.AnyType.WithRefFlag;
                    }
                    else if (t.Is_PhpArray())
                    {
                        mask = ctx.GetArrayTypeMask();
                    }
                    else if (t.Is_PhpString())
                    {
                        mask = ctx.GetWritableStringTypeMask();
                    }
                    else if (t.Is_PhpResource())
                    {
                        mask = ctx.GetResourceTypeMask();
                    }
                    else
                    {
                        mask = ctx.BoundTypeRefFactory.Create(t).GetTypeRefMask(ctx);
                    }

                    if (!notNull && t.CanBeAssignedNull())
                    {
                        mask |= ctx.GetNullTypeMask();
                    }

                    return mask;
            }
        }
        
        /// <summary>
        /// Creates type context for a method within given type, determines naming, type context.
        /// </summary>
        public static TypeRefContext/*!*/CreateTypeRefContext(SourceTypeSymbol/*!*/containingType)
        {
            Contract.ThrowIfNull(containingType);

            return new TypeRefContext(
                containingType.DeclaringCompilation,
                containingType, // scope
                thisType: containingType.IsTrait ? null : containingType);
        }
    }
}
