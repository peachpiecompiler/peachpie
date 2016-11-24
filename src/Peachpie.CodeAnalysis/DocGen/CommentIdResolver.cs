using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.DocGen
{
    internal static class CommentIdResolver
    {
        public static string GetId(NamedTypeSymbol type) => "T:" + TypeId(type);

        public static string GetId(SourceRoutineSymbol routine) => "M:" + TypeId(routine.ContainingType) + "." + RoutineSignatureId(routine);

        static string TypeId(NamedTypeSymbol type)
        {
            var ns = type.NamespaceName.Replace("<", "&lt;").Replace(">", "&gt;");
            var name = TypeNameId(type);
            return string.IsNullOrEmpty(ns) ? name : (ns + "." + name);
        }

        static string TypeNameId(NamedTypeSymbol type)
        {
            return GetEscapedMetadataName(type.MetadataName);
        }

        static string RoutineSignatureId(SourceRoutineSymbol routine)
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

                builder.Append(TypeId((NamedTypeSymbol)parameter.Type));

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
