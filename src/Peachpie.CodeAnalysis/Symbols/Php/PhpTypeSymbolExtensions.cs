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
        public static FieldSymbol ResolveClassConstant(this INamedTypeSymbol t, string name)
        {
            for (; t != null; t = t.BaseType)
            {
                var f = GetClassConstant(t, name);
                if (f != null)
                {
                    return f;
                }
            }

            return null;
        }
    }
}
