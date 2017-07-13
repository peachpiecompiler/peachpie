using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    internal static class PhpFieldSymbolExtension
    {
        /// <summary>
        /// In case the field is PHP static field (contained as an instance field in __statics holder class),
        /// gets its holder class type.
        /// See <see cref="SynthesizedStaticFieldsHolder"/>.
        /// </summary>
        public static TypeSymbol TryGetStatics(this FieldSymbol fld)
        {
            // nested class __statics { fld }

            if (!fld.IsStatic)
            {
                var srcfld = fld as SourceFieldSymbol;
                if (srcfld != null && srcfld.RequiresHolder)
                {
                    // SourceFieldSymbol
                    return srcfld.ContainingType.TryGetStatics();
                }
                else
                {
                    // PEFieldSymbol
                    if (fld.ContainingType.Name == WellKnownPchpNames.StaticsHolderClassName)
                    {
                        var statics = fld.ContainingType.ContainingType.TryGetStatics();
                        if (ReferenceEquals(statics, fld.ContainingType))
                        {
                            return statics;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Determines if given field is declared as static.
        /// Note: actual CLI metadata might be confusing since static PHP fields are represented as instance .NET fields in a class that lives within a PHP context.
        /// </summary>
        public static bool IsPhpStatic(this FieldSymbol f)
        {
            return f.IsStatic || (f is SourceFieldSymbol sf && sf.FieldKind == SourceFieldSymbol.KindEnum.StaticField);
        }
    }
}
