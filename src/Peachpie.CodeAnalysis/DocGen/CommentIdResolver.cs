using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.DocGen
{
    internal static class CommentIdResolver
    {
        public static string GetId(NamedTypeSymbol type) => "T:" + TypeId(type);

        static string TypeId(NamedTypeSymbol type)
        {
            var ns = type.NamespaceName;
            var name = TypeNameId(type);
            return string.IsNullOrEmpty(ns) ? name : (ns + "." + name);
        }

        static string TypeNameId(NamedTypeSymbol type)
        {
            return type.MetadataName;
        }
    }
}
