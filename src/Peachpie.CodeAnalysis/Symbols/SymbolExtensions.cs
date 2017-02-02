using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    internal static partial class SymbolExtensions
    {
        /// <summary>
        /// Returns a constructed named type symbol if 'type' is generic, otherwise just returns 'type'
        /// </summary>
        public static NamedTypeSymbol ConstructIfGeneric(this NamedTypeSymbol type, ImmutableArray<TypeWithModifiers> typeArguments)
        {
            Debug.Assert(type.TypeParameters.IsEmpty == (typeArguments.Length == 0));
            return type.TypeParameters.IsEmpty ? type : type.Construct(typeArguments, unbound: false);
        }

        public static bool IsPhpHidden(this Symbol s)
        {
            var attrs = s.GetAttributes();
            return attrs.Length != 0 && attrs.Any(a => a.AttributeClass.MetadataName == "PhpHiddenAttribute");
        }

        public static bool IsPhpTypeName(this PENamedTypeSymbol s) => GetPhpTypeNameOrNull(s) != null;

        public static string GetPhpTypeNameOrNull(this PENamedTypeSymbol s)
        {
            var attrs = s.GetAttributes();
            if (attrs.Length != 0)
            {
                for (int i = 0; i < attrs.Length; i++)
                {
                    if (attrs[i].AttributeClass.MetadataName == "PhpTypeAttribute")
                    {
                        var tname = attrs[i].ConstructorArguments[0];
                        return tname.IsNull ? s.MakeQualifiedName().ToString() : tname.DecodeValue<string>(SpecialType.System_String);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets type full qualified name.
        /// </summary>
        public static QualifiedName MakeQualifiedName(this NamedTypeSymbol type)
        {
            if (string.IsNullOrEmpty(type.NamespaceName))
            {
                return new QualifiedName(new Name(type.Name));
            }
            else
            {
                var ns = type.NamespaceName.Replace('.', QualifiedName.Separator);
                return NameUtils.MakeQualifiedName(ns + QualifiedName.Separator + type.Name, true);
            }
        }

        public static bool IsAccessible(this Symbol symbol, TypeSymbol classCtx)
        {
            if (symbol.DeclaredAccessibility == Accessibility.Private)
            {
                return (symbol.ContainingType == classCtx);
            }
            else if (symbol.DeclaredAccessibility == Accessibility.Protected)
            {
                return classCtx != null && (
                    symbol.ContainingType.IsEqualToOrDerivedFrom(classCtx) ||
                    classCtx.IsEqualToOrDerivedFrom(symbol.ContainingType));
            }

            return true;
        }
    }
}
