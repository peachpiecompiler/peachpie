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

        /// <summary>
        /// Determines whethere given PE type symbol is an exported PHP type.
        /// </summary>
        public static bool IsPhpTypeName(this PENamedTypeSymbol s) => !s.IsStatic && !GetPhpTypeNameOrNull(s).IsEmpty();

        /// <summary>
        /// Determines PHP type name of an exported PHP type.
        /// Gets default&lt;QualifiedName&gt; if type is not exported PHP type.
        /// </summary>
        public static QualifiedName GetPhpTypeNameOrNull(this PENamedTypeSymbol s)
        {
            var attrs = s.GetAttributes();
            if (attrs.Length != 0)
            {
                for (int i = 0; i < attrs.Length; i++)
                {
                    if (attrs[i].AttributeClass.MetadataName == "PhpTypeAttribute")
                    {
                        var ctorargs = attrs[i].ConstructorArguments;
                        var tname = ctorargs[0];
                        var tnamestr = tname.IsNull ? null : tname.DecodeValue<string>(SpecialType.System_String);

                        const string InheritName = "[name]";

                        if (tnamestr == null)
                        {
                            return s.MakeQualifiedName();
                        }
                        else if (tnamestr == InheritName)
                        {
                            return new QualifiedName(new Name(s.Name));
                        }
                        else
                        {
                            return QualifiedName.Parse(tnamestr.Replace(InheritName, s.Name), true);
                        }
                    }
                }
            }

            return default(QualifiedName);
        }

        /// <summary>
        /// Gets type full qualified name.
        /// </summary>
        public static QualifiedName MakeQualifiedName(this NamedTypeSymbol type)
        {
            return NameUtils.MakeQualifiedName(type.Name, type.NamespaceName, true);
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
