using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Pchp.CodeAnalysis;

namespace Peachpie.CodeAnalysis.Semantics
{
    static class TypeRefExtension
    {
        /// <summary>
        /// Gets value indicating the type refers to a nullable type (<c>?TYPE</c>).
        /// </summary>
        public static bool IsNullable(this TypeRef tref)
        {
            return tref is NullableTypeRef;
            //return tref switch
            //{
            //    NullableTypeRef _ => true,
            //    PrimitiveTypeRef pt => pt.PrimitiveTypeName == PrimitiveTypeRef.PrimitiveType.@null;
            //    _ => false;
            //};
        }

        /// <summary>
        /// Whether the type refers to a special "null" class name, valid only within a union.
        /// </summary>
        public static bool IsNullClass(this TypeRef tref)
        {
            var ct = (tref is TranslatedTypeRef tr ? tr.OriginalType : tref) as ClassTypeRef;
            return ct != null && ct.ClassName == QualifiedName.Null;
        }

        /// <summary>
        /// Gets value indicating the type refers to a mixed type (<c>mixed</c>).
        /// </summary>
        public static bool IsMixed(this TypeRef tref)
        {
            return tref is PrimitiveTypeRef pt && pt.PrimitiveTypeName == PrimitiveTypeRef.PrimitiveType.mixed;
        }

        /// <summary>
        /// Gets value indicating the type refers to "void" type (<c>void</c>).
        /// </summary>
        public static bool IsVoid(this TypeRef tref)
        {
            return tref is PrimitiveTypeRef pt && pt.PrimitiveTypeName == PrimitiveTypeRef.PrimitiveType.@void;
        }

        /// <summary>
        /// Gets value indicating the type refers to <c>callable</c> or <c>?callable</c>.
        /// </summary>
        public static bool IsCallable(this TypeRef tref)
        {
            if (tref is NullableTypeRef nullable)
            {
                tref = nullable.TargetType;
            }

            return tref is PrimitiveTypeRef primitiveType &&
                primitiveType.PrimitiveTypeName == PrimitiveTypeRef.PrimitiveType.callable;
        }

        /// <summary>
        /// determines whether given type can represent a NULL type.
        /// </summary>
        public static bool CanBeNull(this TypeRef tref)
        {
            Contract.ThrowIfNull(tref);

            if (tref is MultipleTypeRef mt)
            {
                // check the union contains a nullable or special "null" type
                return mt.MultipleTypes.Any(t => t.CanBeNull() || t.IsNullClass());
            }
            else
            {
                return tref.IsNullable() || tref.IsMixed();
            }
        }
    }
}
