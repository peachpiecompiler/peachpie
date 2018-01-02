using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    internal static class PhpFieldSymbolExtension
    {
        /// <summary>
        /// Determines if given field is declared as static.
        /// Note: actual CLI metadata might be confusing since static PHP fields are represented as instance .NET fields in a class that lives within a PHP context.
        /// </summary>
        public static bool IsPhpStatic(this FieldSymbol f)
        {
            return f.IsStatic || (f is IPhpPropertySymbol phpf && (phpf.FieldKind == PhpPropertyKind.StaticField || phpf.FieldKind == PhpPropertyKind.AppStaticField));
        }

        public static TypeSymbol ContainingStaticsHolder(this FieldSymbol f) => f is IPhpPropertySymbol phpf ? phpf.ContainingStaticsHolder : null;

        /// <summary>
        /// Gets value indicating whether the field has to be contained in <see cref="SynthesizedStaticFieldsHolder"/>.
        /// </summary>
        public static bool IsInStaticsHolder(FieldSymbol f) => ContainingStaticsHolder(f) != null;

        /// <summary>
        /// Gets value indicating whether the field has to be contained in <see cref="SynthesizedStaticFieldsHolder"/>.
        /// </summary>
        public static bool RequiresHolder(FieldSymbol f, PhpPropertyKind kind)
        {
            switch (kind)
            {
                case PhpPropertyKind.AppStaticField: return false;
                case PhpPropertyKind.StaticField: return true; // PHP static field is bound to Context and has to be instantiated within holder class
                case PhpPropertyKind.InstanceField: return false;
                case PhpPropertyKind.ClassConstant: return f.IsConst == false;   // if constant has to be evaluated in runtime, we have to evaluate its value for each context separatelly within holder
                default:
                    throw Roslyn.Utilities.ExceptionUtilities.Unreachable;
            }
        }

        public static bool RequiresContext(FieldSymbol f) => f is IPhpPropertySymbol phpf && phpf.RequiresContext;
    }
}
