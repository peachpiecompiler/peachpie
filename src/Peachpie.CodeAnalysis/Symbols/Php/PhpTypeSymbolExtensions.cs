using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    internal static class PhpTypeSymbolExtensions
    {
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
                candidate = t.GetMembers(name).OfType<FieldSymbol>().Where(f => !f.IsConst).SingleOrDefault();  // we do accepts static fields called on instances
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
                if(candidate != null)
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
            for (;t != null; t = t.BaseType)
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

            // const on an interface
            foreach (var iface in type.AllInterfaces)
            {
                var f = GetClassConstant(iface, name);
                if (f != null)
                {
                    return f;
                }
            }

            return null;
        }
    }
}
