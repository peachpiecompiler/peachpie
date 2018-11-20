using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Symbols;
using AST = Devsense.PHP.Syntax.Ast;

namespace Pchp.CodeAnalysis
{
    internal static partial class TypeRefFactory
    {
        #region Primitive Types

        internal static readonly PrimitiveTypeRef/*!*/VoidTypeRef = new PrimitiveTypeRef(PhpTypeCode.Void);
        internal static readonly PrimitiveTypeRef/*!*/NullTypeRef = new PrimitiveTypeRef(PhpTypeCode.Null);
        internal static readonly PrimitiveTypeRef/*!*/BoolTypeRef = new PrimitiveTypeRef(PhpTypeCode.Boolean);
        internal static readonly PrimitiveTypeRef/*!*/LongTypeRef = new PrimitiveTypeRef(PhpTypeCode.Long);
        internal static readonly PrimitiveTypeRef/*!*/DoubleTypeRef = new PrimitiveTypeRef(PhpTypeCode.Double);
        internal static readonly PrimitiveTypeRef/*!*/StringTypeRef = new PrimitiveTypeRef(PhpTypeCode.String);
        internal static readonly PrimitiveTypeRef/*!*/WritableStringRef = new PrimitiveTypeRef(PhpTypeCode.WritableString);
        internal static readonly PrimitiveTypeRef/*!*/ArrayTypeRef = new PrimitiveTypeRef(PhpTypeCode.PhpArray);
        internal static readonly PrimitiveTypeRef/*!*/ResourceTypeRef = new PrimitiveTypeRef(PhpTypeCode.Resource);

        #endregion

        /// <summary>
        /// Converts CLR type symbol to TypeRef used by flow analysis.
        /// </summary>
        public static ITypeRef CreateTypeRef(TypeRefContext ctx, TypeSymbol t)
        {
            Contract.ThrowIfNull(t);

            switch (t.SpecialType)
            {
                case SpecialType.System_Void: throw new ArgumentException();
                case SpecialType.System_Int32:
                case SpecialType.System_Int64: return LongTypeRef;
                case SpecialType.System_String: return StringTypeRef;
                case SpecialType.System_Single:
                case SpecialType.System_Double: return DoubleTypeRef;
                case SpecialType.System_Boolean: return BoolTypeRef;
                case SpecialType.System_Object: return CreateTypeRef(NameUtils.SpecialNames.System_Object);
                case SpecialType.System_DateTime: return CreateTypeRef(NameUtils.SpecialNames.System_DateTime);
                default:
                    if (t is NamedTypeSymbol)
                    {
                        return CreateClassTypeRef(ctx, (NamedTypeSymbol)t);
                    }
                    else if (t is ArrayTypeSymbol)
                    {
                        var arr = (ArrayTypeSymbol)t;
                        if (!arr.IsSZArray)
                        {
                            throw new NotImplementedException();
                        }

                        return new ArrayTypeRef(null, CreateMask(ctx, arr.ElementType));
                    }
                    else
                    {
                        throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(t);
                    }
            }
        }

        public static ITypeRef CreateTypeRef(AST.TypeRef tref)
        {
            if (tref is AST.PrimitiveTypeRef)
            {
                switch (((AST.PrimitiveTypeRef)tref).PrimitiveTypeName)
                {
                    case AST.PrimitiveTypeRef.PrimitiveType.@int: return TypeRefFactory.LongTypeRef;
                    case AST.PrimitiveTypeRef.PrimitiveType.@float: return TypeRefFactory.DoubleTypeRef;
                    case AST.PrimitiveTypeRef.PrimitiveType.@string: return TypeRefFactory.StringTypeRef;
                    case AST.PrimitiveTypeRef.PrimitiveType.@bool: return TypeRefFactory.BoolTypeRef;
                    //case AST.PrimitiveTypeRef.PrimitiveType.array: return TypeRefFactory.ArrayTypeRef;
                    //case AST.PrimitiveTypeRef.PrimitiveType.callable: return TypeRefFactory.CallableTypeRef;
                    //case AST.PrimitiveTypeRef.PrimitiveType.@void: return 0;
                    //case AST.PrimitiveTypeRef.PrimitiveType.iterable: return GetArrayTypeMask() | GetTypeMask(NameUtils.SpecialNames.Traversable, true);   // array | Traversable
                    case AST.PrimitiveTypeRef.PrimitiveType.@object: return TypeRefFactory.CreateTypeRef(NameUtils.SpecialNames.System_Object);
                }
            }
            else if (tref is AST.INamedTypeRef named) return TypeRefFactory.CreateTypeRef(named.ClassName);
            //else if (tref is AST.ReservedTypeRef) return GetTypeMaskOfReservedClassName(((AST.ReservedTypeRef)tref).QualifiedName.Value.Name);
            else if (tref is AST.GenericTypeRef generic) return new GenericClassTypeRef(
                    generic.QualifiedName.Value, // throw if indirect
                    generic.GenericParams.Select(CreateTypeRef).ToImmutableArray());

            //
            throw new ArgumentException();
        }

        public static ITypeRef CreateTypeRef(QualifiedName qname) => new ClassTypeRef(qname);

        static ITypeRef CreateClassTypeRef(TypeRefContext ctx, NamedTypeSymbol t)
        {
            if (t.Arity <= 0)
            {
                return CreateTypeRef(t.PhpQualifiedName());
            }
            else
            {
                return new GenericClassTypeRef(
                    t.OriginalDefinition.PhpQualifiedName(),
                    t.TypeArguments.SelectAsArray(targ => CreateTypeRef(ctx, targ)).AsImmutable());
            }
        }

        public static ITypeRef Create(ConstantValue c)
        {
            Contract.ThrowIfNull(c);

            switch (c.SpecialType)
            {
                case SpecialType.System_Boolean: return BoolTypeRef;
                case SpecialType.System_Int32:
                case SpecialType.System_Int64: return LongTypeRef;
                case SpecialType.System_String: return StringTypeRef;
                case SpecialType.System_Single:
                case SpecialType.System_Double: return DoubleTypeRef;
                // case SpecialType.System_Array: return WritableStringRef; // TODO: only array of bytes/chars
                default:
                    if (c.IsNull) return NullTypeRef;
                    throw new NotImplementedException();
            }
        }

        public static TypeRefMask CreateMask(TypeRefContext ctx, TypeSymbol t)
        {
            Contract.ThrowIfNull(t);

            switch (t.SpecialType)
            {
                case SpecialType.System_Void: return 0;
                case SpecialType.System_Boolean: return ctx.GetBooleanTypeMask();
                case SpecialType.System_Int32:
                case SpecialType.System_Int64: return ctx.GetLongTypeMask();
                case SpecialType.System_String: return ctx.GetStringTypeMask() | ctx.GetNullTypeMask();
                case SpecialType.System_Single:
                case SpecialType.System_Double: return ctx.GetDoubleTypeMask();
                case SpecialType.None:
                    var containing = t.ContainingAssembly;
                    if (containing != null && containing.IsPeachpieCorLibrary)
                    {
                        if (t.Name == "PhpValue") return TypeRefMask.AnyType;
                        if (t.Name == "PhpAlias") return TypeRefMask.AnyType.WithRefFlag;
                        if (t.Name == "PhpNumber") return ctx.GetNumberTypeMask();
                        if (t.Name == "PhpString") return ctx.GetWritableStringTypeMask() | ctx.GetNullTypeMask();
                        if (t.Name == "PhpArray") return ctx.GetArrayTypeMask() | ctx.GetNullTypeMask();
                        if (t.Name == "IPhpCallable") return ctx.GetCallableTypeMask() | ctx.GetNullTypeMask();
                        if (t.Name == "PhpResource") return ctx.GetResourceTypeMask();
                    }

                    if (t.IsNullableType())
                    {
                        // <T> | NULL
                        return CreateMask(ctx, ((NamedTypeSymbol)t).TypeArguments[0]) | ctx.GetNullTypeMask();
                    }

                    break;
            }

            return CreateMask(ctx, CreateTypeRef(ctx, t));
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

            if (tref.IsObject)
            {
                result |= ctx.GetNullTypeMask();
            }

            return result;
        }

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
