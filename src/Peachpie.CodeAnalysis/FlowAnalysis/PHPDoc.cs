using Devsense.PHP.Syntax;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Helpers class for resolving PHPDoc types.
    /// </summary>
    internal static class PHPDoc
    {
        private delegate TypeRefMask TypeMaskGetter(TypeRefContext ctx);

        /// <summary>
        /// Well-known PHP type names used in PHPDoc.
        /// </summary>
        private static readonly Dictionary<string, TypeMaskGetter>/*!*/_knownTypes = new Dictionary<string, TypeMaskGetter>(StringComparer.OrdinalIgnoreCase)
        {
            { "int", ctx => ctx.GetLongTypeMask()},
            { "integer", ctx => ctx.GetLongTypeMask()},
            { "long", ctx => ctx.GetLongTypeMask()},
            { "number", ctx => ctx.GetNumberTypeMask()},
            { "numeric", ctx => ctx.GetNumberTypeMask()},
            { "string", ctx => ctx.GetStringTypeMask()},
            { "bool", ctx => ctx.GetBooleanTypeMask()},
            { "boolean", ctx => ctx.GetBooleanTypeMask()},
            { "false", ctx => ctx.GetBooleanTypeMask()},
            { "true", ctx => ctx.GetBooleanTypeMask()},
            { "float", ctx => ctx.GetDoubleTypeMask()},
            { "double", ctx => ctx.GetDoubleTypeMask()},
            { "array", ctx => ctx.GetArrayTypeMask()},
            { "resource", ctx => ctx.GetResourceTypeMask()},
            { "null", ctx => ctx.GetNullTypeMask()},
            { "object", ctx => ctx.GetSystemObjectTypeMask()},
            { "void", ctx => default(TypeRefMask).WithSubclasses},  // avoid being 0 (which means uninitialized)
            //{ "nothing", ctx => 0},
            { "callable", ctx => ctx.GetCallableTypeMask()},
            { "mixed", ctx => TypeRefMask.AnyType},
        };

        /// <summary>
        /// Gets value indicating whether given parameter represents known PHPDoc type name.
        /// </summary>
        public static bool IsKnownType(string tname)
        {
            return !string.IsNullOrEmpty(tname) && _knownTypes.ContainsKey(tname);
        }

        /// <summary>
        /// Gets type mask of known PHPDoc type name or <c>0</c> if such type is now known.
        /// </summary>
        public static TypeRefMask GetKnownTypeMask(TypeRefContext/*!*/typeCtx, string tname)
        {
            Contract.ThrowIfNull(typeCtx);
            if (!string.IsNullOrEmpty(tname))
            {
                TypeMaskGetter getter;
                if (_knownTypes.TryGetValue(tname, out getter))
                {
                    return getter(typeCtx);
                }
            }

            return 0;   // void
        }

        /// <summary>
        /// Gets type mask representing given type name.
        /// </summary>
        public static TypeRefMask GetTypeMask(TypeRefContext/*!*/typeCtx, string tname, NamingContext naming, bool fullyQualified = false)
        {
            if (!string.IsNullOrEmpty(tname))
            {
                // handle various array conventions
                if (tname.LastCharacter() == ']')
                {
                    // "TName[]"
                    if (tname.EndsWith("[]", StringComparison.Ordinal))
                    {
                        var elementType = GetTypeMask(typeCtx, tname.Remove(tname.Length - 2), naming, fullyQualified);
                        return typeCtx.GetArrayTypeMask(elementType);
                    }

                    // "array[TName]"
                    var arrayTypeName = QualifiedName.Array.Name.Value;
                    if (tname.Length > arrayTypeName.Length && tname[arrayTypeName.Length] == '[' &&
                        tname.StartsWith(arrayTypeName, StringComparison.OrdinalIgnoreCase))
                    {
                        var elementTypeName = tname.Substring(arrayTypeName.Length + 1, tname.Length - arrayTypeName.Length - 2);
                        var elementType = GetTypeMask(typeCtx, elementTypeName, naming, fullyQualified);
                        return typeCtx.GetArrayTypeMask(elementType);
                    }

                    // unknown something // ...                    
                }
                else if (tname[0] == '&')
                {
                    return GetTypeMask(typeCtx, tname.Substring(1), naming, fullyQualified).WithRefFlag;
                }
                else
                {
                    var result = GetKnownTypeMask(typeCtx, tname);
                    if (result.IsUninitialized)
                    {
                        var qname = NameUtils.MakeQualifiedName(tname, false);
                        if (!fullyQualified && naming != null && !qname.IsReservedClassName)
                            qname = QualifiedName.TranslateAlias(qname, AliasKind.Type, naming.Aliases, naming.CurrentNamespace);

                        if (qname.IsPrimitiveTypeName)
                        {
                            result = GetKnownTypeMask(typeCtx, qname.Name.Value);
                            if (!result.IsUninitialized)
                                return result;
                        }

                        if (qname.IsSelfClassName)
                        {
                            return typeCtx.GetSelfTypeMask();
                        }

                        result = BoundTypeRefFactory.Create(qname, typeCtx.SelfType as SourceTypeSymbol).GetTypeRefMask(typeCtx);
                    }

                    //Contract.Assert(!result.IsUninitialized);
                    return result;
                }
            }

            return 0;
        }

        /// <summary>
        /// Gets type mask representing given type name.
        /// </summary>
        public static TypeRefMask GetTypeMask(TypeRefContext/*!*/typeCtx, string[] tnames, NamingContext naming, bool fullyQualified = false)
        {
            TypeRefMask result = 0;

            foreach (var tname in tnames)
                result |= GetTypeMask(typeCtx, tname, naming, fullyQualified);

            return result;
        }

        /// <summary>
        /// Gets type mask at target ctype context representing given type names from given routine.
        /// </summary>
        public static TypeRefMask GetTypeMask(TypeRefContext/*!*/targetCtx, Symbols.SourceRoutineSymbol/*!*/routine, string[] tnames, bool fullyQualified = false)
        {
            Contract.ThrowIfNull(targetCtx);
            Contract.ThrowIfNull(routine);

            return GetTypeMask(targetCtx, routine.TypeRefContext, tnames, routine.GetNamingContext(), fullyQualified);
        }

        /// <summary>
        /// Gets type mask at target type context representing given type names from given routine.
        /// </summary>
        public static TypeRefMask GetTypeMask(TypeRefContext/*!*/targetCtx, TypeRefContext/*!*/ctx, string[] tnames, NamingContext naming, bool fullyQualified = false)
        {
            Contract.ThrowIfNull(targetCtx);
            Contract.ThrowIfNull(ctx);

            var mask = GetTypeMask(ctx, tnames, naming, fullyQualified);
            return targetCtx.AddToContext(ctx, mask);
        }

        /// <summary>
        /// Gets parameter type from given PHPDoc block.
        /// </summary>
        public static PHPDocBlock.ParamTag GetParamTag(PHPDocBlock phpdoc, int paramIndex, string paramName)
        {
            PHPDocBlock.ParamTag result = null;

            if (phpdoc != null)
            {
                int pi = 0;
                var elements = phpdoc.Elements;
                foreach (var element in elements)
                {
                    var ptag = element as PHPDocBlock.ParamTag;
                    if (ptag != null)
                    {
                        if (string.IsNullOrEmpty(ptag.VariableName))
                        {
                            if (pi == paramIndex)
                                result = ptag;  // found @param by index
                        }
                        else if (string.Equals(ptag.VariableName.Substring(1), paramName, StringComparison.OrdinalIgnoreCase))
                        {
                            result = ptag;
                            break;
                        }

                        //
                        pi++;
                    }
                }
            }

            return result;
        }
    }
}
