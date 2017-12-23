using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    #region PhpPropertyInfo

    internal partial struct PhpPropertyInfo
    {
        readonly Symbol _symbol;

        public PhpPropertyInfo(FieldSymbol f)
        {
            Debug.Assert(f != null);

            _symbol = f;
        }

        public PhpPropertyInfo(PropertySymbol p)
        {
            Debug.Assert(p != null);

            _symbol = p;
        }

        public static PhpPropertyInfo Create(FieldSymbol f) => new PhpPropertyInfo(f);

        public static PhpPropertyInfo Create(PropertySymbol p) => new PhpPropertyInfo(p);

        public static PhpPropertyInfo TryCreate(ISymbol s) => (s is FieldSymbol f) ? Create(f) : (s is PropertySymbol p ? Create(p) : default(PhpPropertyInfo));

        public static bool IsValid(PhpPropertyInfo info) => info.Symbol != null;

        public Symbol Symbol => _symbol;

        public bool IsInStaticsContainer => _symbol.ContainingType.IsStaticsContainer();

        public SourceFieldSymbol.KindEnum FieldKind
        {
            get
            {
                Symbol s = _symbol;

                if (s is SubstitutedFieldSymbol sub)    // bound generic 
                {
                    s = sub.OriginalDefinition;
                }

                if (s is SourceFieldSymbol srcf)
                {
                    return srcf.FieldKind;
                }

                if (s is SynthesizedTraitFieldSymbol sf)
                {
                    return sf.FieldKind;
                }

                if (s is FieldSymbol f)
                {
                    // CLR static
                    if (f.IsStatic) return SourceFieldSymbol.KindEnum.AppStaticField;

                    // const
                    if (f.IsConst) return SourceFieldSymbol.KindEnum.ClassConstant;

                    if (IsInStaticsContainer)
                    {
                        // __statics { readonly }
                        if (f.IsReadOnly) return SourceFieldSymbol.KindEnum.ClassConstant;

                        // __statics
                        return SourceFieldSymbol.KindEnum.StaticField;
                    }

                    // var
                    return SourceFieldSymbol.KindEnum.InstanceField;
                }

                if (s is PropertySymbol p)
                {
                    if (p.IsStatic) return SourceFieldSymbol.KindEnum.AppStaticField;
                    return SourceFieldSymbol.KindEnum.InstanceField;
                }

                throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(_symbol);
            }
        }

        public NamedTypeSymbol DeclaringType
        {
            get
            {
                return IsInStaticsContainer
                    ? _symbol.ContainingType.ContainingType
                    : _symbol.ContainingType;
            }
        }
    }

    #endregion

    internal static class PhpTypeSymbolExtensions
    {
        public static bool IsStaticsContainer(this NamedTypeSymbol t)
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
        public static IEnumerable<PhpPropertyInfo> EnumerateProperties(this INamedTypeSymbol t)
        {
            var result = t.GetMembers()
                .Select(PhpPropertyInfo.TryCreate)
                .Where(PhpPropertyInfo.IsValid);

            if (t is IPhpTypeSymbol phpt && !(t is SourceTypeSymbol))   // SourceTypeSymbol lists all its fields already, __statics must not be listed
            {
                var __statics = phpt.StaticsContainer;
                if (__statics != null)
                {
                    result = result.Concat(EnumerateProperties(__statics));
                }
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

            var phpt = t as IPhpTypeSymbol;
            var statics = phpt?.StaticsContainer;
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

            var phpt = t as IPhpTypeSymbol;
            var statics = phpt?.StaticsContainer;
            if (statics != null)
            {
                // readonly __statics.CONSTANT
                field = statics.GetMembers(name).OfType<FieldSymbol>().Where(f => f.IsReadOnly).SingleOrDefault();
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

            return null;
        }

        /// <summary>
        /// Gets (PHP) type symbols that has to be declared in order to declare given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type declaration which dependant symbols will be returned.</param>
        public static IList<NamedTypeSymbol> GetDependentSourceTypeSymbols(this SourceTypeSymbol type)
        {
            // TODO: what type can be declared on Context ? SourceTypeSymbol and PENamedTypeSymbol compiled from PHP sources
            // TODO: traits

            var btype = type.BaseType as SourceTypeSymbol;
            var ifaces = type.Interfaces;

            if (ifaces.Length == 0 && btype == null)
            {
                return Array.Empty<NamedTypeSymbol>();
            }

            var list = new List<NamedTypeSymbol>(1 + ifaces.Length);
            if (btype != null) list.Add(btype);
            if (ifaces.Length != 0) list.AddRange(ifaces.Where(x => x is SourceTypeSymbol));

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
                    if (containing != null && containing.IsPchpCorLibrary)
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
