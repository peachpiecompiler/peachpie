using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.DocumentationComments
{
    internal static class CommentIdResolver
    {
        public static string GetId(Symbol symbol)
        {
            if (symbol is MethodSymbol) return GetId((MethodSymbol)symbol);
            if (symbol is TypeSymbol) return GetId((TypeSymbol)symbol);

            return null;
        }

        public static string GetId(TypeSymbol type) => "T:" + TypeId(type);

        public static string GetId(MethodSymbol routine) => "M:" + TypeId(routine.ContainingType) + "." + MethodSignatureId(routine);

        static string TypeId(TypeSymbol type)
        {
            if (type is ArrayTypeSymbol)
            {
                var arrtype = (ArrayTypeSymbol)type;
                return TypeId(arrtype.ElementType) + "[]";  // TODO: MDSize
            }
            else if (type.ContainingType != null) // nested type
            {
                return TypeId(type.ContainingType) + "." + TypeNameId(type);
            }
            else
            {
                var ns = ((NamedTypeSymbol)type).NamespaceName.Replace("<", "&lt;").Replace(">", "&gt;");
                var name = TypeNameId(type);
                return string.IsNullOrEmpty(ns) ? name : (ns + "." + name);
            }

            // todo: generics `N
        }

        static string TypeNameId(TypeSymbol type)
        {
            return GetEscapedMetadataName(type.MetadataName);
        }

        static string MethodSignatureId(MethodSymbol routine)
        {
            var builder = new StringBuilder();

            builder.Append(GetEscapedMetadataName(routine.MetadataName));
            AppendParameters(routine.Parameters, routine.IsVararg, builder);

            return builder.ToString();
        }

        static void AppendParameters(ImmutableArray<ParameterSymbol> parameters, bool isVararg, StringBuilder builder)
        {
            builder.Append('(');
            bool needsComma = false;

            foreach (var parameter in parameters)
            {
                if (needsComma)
                {
                    builder.Append(',');
                }

                builder.Append(TypeId(parameter.Type));

                // ref and out params are suffixed with @
                if (parameter.RefKind != RefKind.None)
                {
                    builder.Append('@');
                }

                needsComma = true;
            }

            if (isVararg && needsComma)
            {
                builder.Append(',');
            }

            builder.Append(')');
        }

        static string GetEscapedMetadataName(string metadataName)
        {
            int colonColonIndex = metadataName.IndexOf("::", StringComparison.Ordinal);
            int startIndex = colonColonIndex < 0 ? 0 : colonColonIndex + 2;

            return metadataName.Substring(startIndex, metadataName.Length - startIndex)
                .Replace('.', '#')
                .Replace('<', '{')
                .Replace('>', '}');
        }
    }
}
