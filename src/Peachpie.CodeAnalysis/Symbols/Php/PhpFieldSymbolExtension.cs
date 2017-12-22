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
        public static TypeSymbol TryGetStaticsContainer(this FieldSymbol fld)
        {
            // nested class __statics { fld }

            if (!fld.IsStatic)
            {
                if (RequiresHolder(fld))
                {
                    return fld.ContainingType.TryGetStatics();
                }
                else
                {
                    // FieldSymbol
                    if (fld.ContainingType.IsStaticsContainer())
                    {
                        return fld.ContainingType;
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

        /// <summary>
        /// Gets value indicating whether the field has to be contained in <see cref="SynthesizedStaticFieldsHolder"/>.
        /// </summary>
        public static bool RequiresHolder(FieldSymbol f)
        {
            if (f is SubstitutedFieldSymbol sub) return RequiresHolder(sub.OriginalDefinition);
            if (f is SourceFieldSymbol sf) return RequiresHolder(sf, sf.FieldKind);
            if (f is SynthesizedTraitFieldSymbol tf) return RequiresHolder(tf, tf.FieldKind);
            return false;
        }

        /// <summary>
        /// Gets value indicating whether the field has to be contained in <see cref="SynthesizedStaticFieldsHolder"/>.
        /// </summary>
        public static bool RequiresHolder(FieldSymbol f, SourceFieldSymbol.KindEnum kind)
        {
            switch (kind)
            {
                case SourceFieldSymbol.KindEnum.AppStaticField: return false;
                case SourceFieldSymbol.KindEnum.StaticField: return true; // PHP static field is bound to Context and has to be instantiated within holder class
                case SourceFieldSymbol.KindEnum.InstanceField: return false;
                case SourceFieldSymbol.KindEnum.ClassConstant: return f.GetConstantValue(false) == null;   // if constant has to be evaluated in runtime, we have to evaluate its value for each context separatelly within holder
                default:
                    throw Roslyn.Utilities.ExceptionUtilities.Unreachable;
            }
        }

        public static bool RequiresContext(FieldSymbol f)
        {
            if (f is SourceFieldSymbol sf) return sf.RequiresContext;
            if (f is SynthesizedTraitFieldSymbol tf) return tf.RequiresContext;

            return false;
        }

        public static void EmitInit(FieldSymbol f, CodeGen.CodeGenerator cg)
        {
            if (f is SourceFieldSymbol sf) sf.EmitInit(cg);
            else if (f is SynthesizedTraitFieldSymbol tf) tf.EmitInit(cg);
            else throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(f);
        }
    }
}
