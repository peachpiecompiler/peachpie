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
        public const string GlobalRoutineName = "<" + WellKnownMemberNames.EntryPointMethodName + ">";

        /// <summary>
        /// Name of special script type.
        /// </summary>
        public const string DefaultScriptClassName = "<Script>";

        /// <summary>
        /// Namespace containing all script types.
        /// </summary>
        public const string ScriptsRootNamespace = "<Root>";

        /// <summary>
        /// Name of special nested class containing context bound static fields and constants.
        /// </summary>
        public const string StaticsHolderClassName = "_statics";

        /// <summary>
        /// Format string for a generator state machine method name.
        /// </summary>
        public const string GeneratorStateMachineNameFormatString = "<>sm_{0}";

        /// <summary>
        /// Name of method containing lambda method's implementation.
        /// </summary>
        public const string LambdaMethodName = "anonymous@function";
    }
}
