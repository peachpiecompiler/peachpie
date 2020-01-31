using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    internal static class PhpTypeSymbolExtensions
    {
        /// <summary>
        /// Gets special <c>_statics</c> nested class holding static fields bound to context.
        /// </summary>
        /// <returns></returns>
        public static NamedTypeSymbol TryGetStaticsHolder(this INamedTypeSymbol t)
        {
            if (t is SourceTypeSymbol srct) return (NamedTypeSymbol)srct.StaticsContainer;

            // a nested class `_statics`:
            return (NamedTypeSymbol)t.GetTypeMembers(WellKnownPchpNames.StaticsHolderClassName).Where(IsStaticsContainer).SingleOrDefault();
        }

        public static bool IsStaticsContainer(this INamedTypeSymbol t)
        {
            return
                t.Name == WellKnownPchpNames.StaticsHolderClassName &&
                t.DeclaredAccessibility == Accessibility.Public &&
                t.Arity == 0 &&
                !t.IsStatic &&
                t.ContainingType != null;
        }

        /// <summary>
        /// Enumerates class fields and properties as declared in PHP.
        /// </summary>
        public static IEnumerable<IPhpPropertySymbol> EnumerateProperties(this INamedTypeSymbol t)
        {
            var result = t.GetMembers()
                .OfType<IPhpPropertySymbol>()
                .Where(p => p.Name.IndexOf('<') < 0);   // only valid declared properties // TODO: helper method

            var __statics = TryGetStaticsHolder(t);
            if (__statics != null)
            {
                result = result.Concat(__statics.GetMembers().OfType<IPhpPropertySymbol>());
            }

            var btype = t.BaseType;
            if (btype != null && btype.SpecialType != SpecialType.System_Object)
            {
                result = result.Concat(EnumerateProperties(btype));
            }

            return result;
        }

        static FieldSymbol GetStaticField(INamedTypeSymbol t, string name)
        {
            FieldSymbol field = null;

            var statics = TryGetStaticsHolder(t);
            if (statics != null)
            {
                // __statics.FIELD
                field = statics.GetMembers(name).OfType<FieldSymbol>().Where(f => !f.IsReadOnly).SingleOrDefault();
            }

            // static FIELD
            if (field == null)
            {
                field = t.GetMembers(name).OfType<FieldSymbol>().Where(f => !f.IsConst && f.IsStatic).SingleOrDefault();
            }

            //
            return field;
        }

        static FieldSymbol GetClassConstant(INamedTypeSymbol t, string name)
        {
            FieldSymbol field = null;

            var statics = TryGetStaticsHolder(t);
            if (statics != null)
            {
                // readonly __statics.CONSTANT
                field = statics.GetMembers(name).OfType<FieldSymbol>().Where(f => f.IsReadOnly || f.IsConst).SingleOrDefault();
            }

            // const CONSTANT
            if (field == null)
            {
                field = t.GetMembers(name).OfType<FieldSymbol>().Where(f => f.IsConst).SingleOrDefault();
            }

            //
            return field;
        }

        /// <summary>
        /// Resolves field or property on an instance.
        /// </summary>
        public static Symbol ResolveInstanceProperty(this INamedTypeSymbol type, string name)
        {
            Symbol candidate;

            for (var t = type; t != null; t = t.BaseType)
            {
                candidate = t.GetMembers(name).OfType<FieldSymbol>().Where(f => !f.IsConst && !f.IsPhpStatic()).SingleOrDefault();
                if (candidate != null)
                {
                    return candidate;
                }

                candidate = t.GetMembers(name).OfType<PropertySymbol>().Where(p => !p.IsStatic).SingleOrDefault();
                if (candidate != null)
                {
                    return candidate;
                }
            }

            // properties on interfaces
            foreach (var i in type.AllInterfaces)
            {
                candidate = i.GetMembers(name).OfType<PropertySymbol>().SingleOrDefault();
                if (candidate != null)
                {
                    return candidate;
                }
            }

            //
            return null;
        }

        /// <summary>
        /// Tries to find static field with given name.
        /// Lookups through the class inheritance.
        /// Does not handle member visibility.
        /// </summary>
        public static FieldSymbol ResolveStaticField(this INamedTypeSymbol t, string name)
        {
            for (; t != null; t = t.BaseType)
            {
                var f = GetStaticField(t, name);
                if (f != null)
                {
                    return f;
                }
            }

            return null;
        }

        /// <summary>
        /// Tries to find class constant with given name.
        /// Lookups through the class inheritance.
        /// Does not handle member visibility.
        /// </summary>
        public static FieldSymbol ResolveClassConstant(this INamedTypeSymbol type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var f = GetClassConstant(t, name);
                if (f != null)
                {
                    return f;
                }
            }

            // constants on interfaces
            foreach (var i in type.AllInterfaces)
            {
                var f = GetClassConstant(i, name);
                if (f != null)
                {
                    return f;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets (PHP) type symbols that has to be declared in order to declare given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type declaration which dependant symbols will be returned.</param>
        public static IList<NamedTypeSymbol> GetDependentSourceTypeSymbols(this SourceTypeSymbol type)
        {
            // TODO: traits

            var btype = (type.BaseType != null && type.BaseType.IsPhpUserType()) ? type.BaseType : null;
            var ifaces = type.Interfaces;

            if (ifaces.Length == 0 && btype == null)
            {
                return Array.Empty<NamedTypeSymbol>();
            }

            var list = new List<NamedTypeSymbol>(1 + ifaces.Length);
            if (btype != null) list.Add(btype);
            if (ifaces.Length != 0) list.AddRange(ifaces.Where(x => x.IsPhpUserType()));

            return list;
        }

        /// <summary>
        /// For known types, gets their PHP type name.
        /// Used for diagnostic reasons.
        /// </summary>
        public static string GetPhpTypeNameOrNull(this TypeSymbol t)
        {
            switch (t.SpecialType)
            {
                case SpecialType.System_Void: return "void";
                case SpecialType.System_Int32:
                case SpecialType.System_Int64: return "integer";
                case SpecialType.System_String: return "string";
                case SpecialType.System_Single:
                case SpecialType.System_Double: return "double";
                case SpecialType.System_Boolean: return "boolean";
                default:
                    var containing = t.ContainingAssembly;
                    if (containing != null && containing.IsPeachpieCorLibrary)
                    {
                        if (t.Name == "PhpNumber") return "number";
                        if (t.Name == "PhpString") return "string";
                        if (t.Name == "PhpArray" || t.Name == "IPhpArray") return "array";
                        if (t.Name == "IPhpCallable") return "callable";
                        if (t.Name == "PhpResource") return "resource";
                    }
                    break;
            }

            return null;
        }
    }
}
