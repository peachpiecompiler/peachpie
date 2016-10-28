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
        /// Name of instance method that initializes the PHP type instance without calling a constructor function.
        /// In case base type also defines this method, it gets called as well.
        /// </summary>
        /// <remarks><code>
        /// void .phpnew(Context ctx, params ps) {
        ///     init_context;
        ///     init_fields;
        ///     base..phpnew(ctx, ps);
        /// }</code></remarks>
        public const string PhpNewMethodName = ".phpnew";
    }
}
