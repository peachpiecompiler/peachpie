using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    internal static class WellKnownPchpNames
    {
        /// <summary>
        /// Name of function representing a script global code.
        /// </summary>
        public const string GlobalRoutineName = "<" + WellKnownMemberNames.EntryPointMethodName + ">";  // TODO: rename to `{main}` as it is in PHP

        /// <summary>
        /// Name of special script type.
        /// </summary>
        public const string DefaultScriptClassName = "<Script>";

        /// <summary>
        /// Namespace containing all script types.
        /// </summary>
        public const string ScriptsRootNamespace = "<Root>";

        /// <summary>
        /// Namespace containing script types corresponding to compiled Phar entry.
        /// </summary>
        public const string PharEntryRootNamespace = "<Phar>";

        /// <summary>
        /// Name of special nested class containing context bound static fields and constants.
        /// </summary>
        public const string StaticsHolderClassName = "_statics";

        /// <summary>
        /// Format string for a generator state machine method name.
        /// </summary>
        public const string GeneratorStateMachineNameFormatString = "<>sm_{0}";

        /// <summary>
        /// Field with flag whether the class's Dispose() was called already.
        /// </summary>
        public static string SynthesizedDisposedFieldName => "<>b_disposed";

        /// <summary>
        /// Name of method containing lambda method's implementation.
        /// This is PHP-like name that has to be equal <c>anonymous@function</c>
        /// so PHP <c>__FUNCTION__</c> constant and eventual reflection is compatible with regular PHP.
        /// </summary>
        public const string LambdaMethodName = "anonymous@function";
    }
}
