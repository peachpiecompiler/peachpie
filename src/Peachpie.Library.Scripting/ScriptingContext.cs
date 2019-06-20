using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Peachpie.Library.Scripting
{
    /// <summary>
    /// Data associated with <see cref="Context"/>.
    /// </summary>
    sealed class ScriptingContext
    {
        /// <summary>
        /// Gets data associated with given context.
        /// </summary>
        public static ScriptingContext EnsureContext(Context ctx) => ctx.GetStatic<ScriptingContext>();

        /// <summary>
        /// Set of submissions already evaluated within the context.
        /// </summary>
        public HashSet<Script> Submissions { get; } = new HashSet<Script>();

        /// <summary>
        /// Index of function created with <see cref="PhpFunctions.create_function"/>.
        /// </summary>
        public int LastLambdaIndex { get; set; } = 0;
    }
}
