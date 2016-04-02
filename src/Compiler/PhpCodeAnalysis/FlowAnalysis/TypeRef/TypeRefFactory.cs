using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Symbols;
using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis
{
    internal static class TypeRefFactory
    {
        #region Primitive Types

        internal static readonly PrimitiveTypeRef/*!*/BoolTypeRef = new PrimitiveTypeRef(PhpTypeCode.Boolean);
        internal static readonly PrimitiveTypeRef/*!*/LongTypeRef = new PrimitiveTypeRef(PhpTypeCode.Long);
        internal static readonly PrimitiveTypeRef/*!*/DoubleTypeRef = new PrimitiveTypeRef(PhpTypeCode.Double);
        internal static readonly PrimitiveTypeRef/*!*/StringTypeRef = new PrimitiveTypeRef(PhpTypeCode.String);
        internal static readonly PrimitiveTypeRef/*!*/WritableStringRef = new PrimitiveTypeRef(PhpTypeCode.WritableString);
        internal static readonly PrimitiveTypeRef/*!*/ArrayTypeRef = new PrimitiveTypeRef(PhpTypeCode.PhpArray);

        #endregion

        /// <summary>
        /// Converts CLR type symbol to TypeRef used by flow analysis.
        /// </summary>
        public static ITypeRef Create(TypeSymbol t)
        {
            Contract.ThrowIfNull(t);

            switch (t.SpecialType)
            {
                case SpecialType.System_Void: throw new ArgumentException();
                case SpecialType.System_Int64: return LongTypeRef;
                case SpecialType.System_String: return StringTypeRef;
                case SpecialType.System_Double: return DoubleTypeRef;
                case SpecialType.System_Boolean: return BoolTypeRef;
                default:
                    throw new NotImplementedException();
            }
        }

        public static TypeRefMask CreateMask(TypeRefContext ctx, TypeSymbol t)
        {
            Contract.ThrowIfNull(t);

            if (t.SpecialType == SpecialType.System_Void)
            {
                return 0;
            }

            return CreateMask(ctx, Create(t));
        }

        public static TypeRefMask CreateMask(TypeRefContext ctx, ITypeRef tref)
        {
            Contract.ThrowIfNull(tref);

            TypeRefMask result = 0;

            result.AddType(ctx.AddToContext(tref));

            if (!tref.IsPrimitiveType && !tref.IsArray)
            {
                result.IncludesSubclasses = true;
            }

            return result;
        }
    }
}
