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
        public static readonly Func<Symbol, bool> s_IsReachable = new Func<Symbol, bool>(t => !t.IsUnreachable);

        public static IEnumerable<T> WhereReachable<T>(this IEnumerable<T> symbols) where T : Symbol => symbols.Where<T>(s_IsReachable);

        /// <summary>
        /// Returns a constructed named type symbol if 'type' is generic, otherwise just returns 'type'
        /// </summary>
        public static NamedTypeSymbol ConstructIfGeneric(this NamedTypeSymbol type, ImmutableArray<TypeWithModifiers> typeArguments)
        {
            Debug.Assert(type.TypeParameters.IsEmpty == (typeArguments.Length == 0));
            return type.TypeParameters.IsEmpty ? type : type.Construct(typeArguments, unbound: false);
        }

        public static bool IsPhpHidden(this Symbol s, PhpCompilation compilation = null)
        {
            if (s is MethodSymbol m)
            {
                return m.IsPhpHidden;
            }

            if (s is FieldSymbol f)
            {
                return f.IsPhpHidden;
            }

            var attrs = s.GetAttributes();
            if (attrs.Length != 0)
            {
                foreach (var attr in attrs)
                {
                    // [PhpHiddenAttribute]
                    if (attr.AttributeClass.MetadataName == "PhpHiddenAttribute")
                    {
                        return true; // => hide
                    }
                }
            }
            //
            return false;
        }

        /// <summary>
        /// Determines if
        /// - method symbol cannot return NULL. E.g. has return attribute [return: NotNullAttribute] or [return: CastToFalse]
        /// - parameter symbol cannot be NULL (has attribute [NotNullAttribute])
        /// - field symbol cannot be NULL (has [NotNullAttribute])
        /// - property cannot be NULL (has [NotNullAttribute])
        /// </summary>
        public static bool IsNotNull(this Symbol symbol)
        {
            if (symbol is IPhpValue value) // method return value, parameter, field, property
            {
                return value.HasNotNull;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets file symbol containing given symbol.
        /// </summary>
        public static SourceFileSymbol GetContainingFileSymbol(this Symbol s)
        {
            return s?.OriginalDefinition switch
            {
                SourceFileSymbol file => file,
                SourceRoutineSymbol routine => routine.ContainingFile,
                SourceTypeSymbol type => type.ContainingFile,
                _ => s != null ? GetContainingFileSymbol(s.ContainingSymbol) : null,
            };
        }

        /// <summary>
        /// Resolves [PhpTypeAttribute] and resulting type name.
        /// Returns <c>false</c> in case the type is not declared as a PHP type.
        /// </summary>
        public static bool TryGetPhpTypeAttribute(this PENamedTypeSymbol s, out QualifiedName resolvedFullName, out Version minLangVersion)
        {
            if (TryGetPhpTypeAttribute(s, out var tname, out _, out _, out minLangVersion))
            {
                resolvedFullName = tname != null
                    ? QualifiedName.Parse(tname, true)
                    : s.MakeQualifiedName();

                return true;
            }
            else
            {
                resolvedFullName = default;
                return false;
            }
        }

        /// <summary>
        /// Gets type full qualified name.
        /// </summary>
        public static QualifiedName MakeQualifiedName(this NamedTypeSymbol type)
        {
            return NameUtils.MakeQualifiedName(type.Name, type.OriginalDefinition.NamespaceName, true);
        }

        /// <summary>
        /// Gets the symbol name as it appears in PHP context.
        /// </summary>
        public static string PhpName(this Symbol s)
        {
            switch (s)
            {
                case IPhpRoutineSymbol routine: return routine.RoutineName;
                case IPhpTypeSymbol type: return type.FullName.ToString();
                default: return s.Name;
            }
        }

        /// <summary>
        /// Gets PHP type qualified name.
        /// </summary>
        public static QualifiedName PhpQualifiedName(this NamedTypeSymbol t) =>
            t is IPhpTypeSymbol phpt ? phpt.FullName : MakeQualifiedName(t);

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
            else if (
                symbol.DeclaredAccessibility == Accessibility.ProtectedAndInternal ||
                symbol.DeclaredAccessibility == Accessibility.Internal)
            {
                return classCtx.ContainingAssembly == symbol.ContainingAssembly; // TODO
            }

            return true;
        }

        /// <summary>
        /// Determines if parameter has a default value.
        /// </summary>
        public static bool IsPhpOptionalParameter(this ParameterSymbol p)
        {
            return p.ExplicitDefaultConstantValue != null || p.DefaultValueField != null || p.Initializer != null;
        }

        ///// <summary>
        ///// Gets value indicating the parameter has default value that is not supported by CLR metadata.
        ///// Such value will be stored in its <see cref="ParameterSymbol.DefaultValueField"/> static field.
        ///// </summary>
        //public static bool HasUnmappedDefaultValue(this ParameterSymbol p) => p.DefaultValueField != null;

        /// <summary>
        /// Gets [PhpType] attribute and its parameters.
        /// </summary>
        public static bool TryGetPhpTypeAttribute(this TypeSymbol symbol, out string typename, out string filename, out byte autoload) =>
            TryGetPhpTypeAttribute(symbol, out typename, out filename, out autoload, out _);

        /// <summary>
        /// Gets [PhpType] attribute and its parameters.
        /// </summary>
        public static bool TryGetPhpTypeAttribute(this TypeSymbol symbol, out string typename, out string filename, out byte autoload, out Version minLangVersion)
        {
            typename = filename = null;
            autoload = 0;
            minLangVersion = null;

            var a = symbol.GetAttribute(CoreTypes.PhpTypeAttributeFullName);
            if (a != null)
            {
                var args = a.ConstructorArguments;
                if (args.Length >= 2)
                {
                    typename = (string)args[0].Value;
                    filename = (string)args[1].Value;
                    autoload = args.Length >= 3 ? (byte)args[2].Value : (byte)0;
                }
                else if (args.Length == 1)
                {
                    var phptype = (byte)args[0].Value; // see PhpTypeAttribute.PhpTypeName
                    if (phptype == 1/*PhpTypeName.NameOnly*/) typename = symbol.Name;
                }

                var named = a.NamedArguments;
                if (named.IsDefaultOrEmpty == false)
                {
                    foreach (var pair in named)
                    {
                        if (pair.Key == "MinimumLangVersion")
                            Version.TryParse((string)pair.Value.Value, out minLangVersion);
                        else
                            Debug.Fail($"Unexpected named attribute PhpType::{pair.Key}");
                    }
                }

                return true;
            }

            //
            return false;
        }

        public static AttributeData GetAttribute(this Symbol symbol, string clrname)
        {
            var attrs = symbol.GetAttributes();
            for (int i = 0; i < attrs.Length; i++)
            {
                var a = attrs[i];

                var fullname = MetadataHelpers.BuildQualifiedName((a.AttributeClass as NamedTypeSymbol)?.NamespaceName, a.AttributeClass.Name);
                if (fullname == clrname)
                {
                    return a;
                }
            }

            return null;
        }

        public static AttributeData GetPhpExtensionAttribute(this Symbol symbol) => GetAttribute(symbol, CoreTypes.PhpExtensionAttributeFullName);

        public static AttributeData GetPhpScriptAttribute(this TypeSymbol symbol) => GetAttribute(symbol, CoreTypes.PhpScriptAttributeFullName);

        /// <summary>
        /// Gets the list of extension names specified in given <c>PhpExtensionAttribute</c>.
        /// </summary>
        /// <returns>Enumeration of extensin names. Never returns <c>null</c>.</returns>
        public static IEnumerable<string>/*!!*/PhpExtensionAttributeValues(this AttributeData phpextensionattribute)
        {
            if (phpextensionattribute != null)
            {
                var args = phpextensionattribute.CommonConstructorArguments;
                if (args.Length == 1)
                {
                    switch (args[0].Kind)
                    {
                        // [PhpExtensionAttribute(params string[] values)]
                        case TypedConstantKind.Array:
                            return args[0]
                                .Values   // string[] extensions
                                .Select(x => (string)x.Value);

                        // [PhpExtensionAttribute(string extensionName)]
                        case TypedConstantKind.Primitive:
                            if (args[0].Value is string extensionName)
                            {
                                return new[] { extensionName };
                            }
                            break;
                    }
                }
            }

            //
            return Array.Empty<string>();
        }
    }
}
