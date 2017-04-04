using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Peachpie.Library.Scripting
{
    /// <summary>
    /// Data associated with <see cref="Pchp.Core.Context"/>.
    /// </summary>
    sealed class ScriptingContext
    {
        /// <summary>
        /// Gets data associated with given context.
        /// </summary>
        public static ScriptingContext EnsureContext(Context ctx) => ctx.GetStatic<ScriptingContext>();

        public List<Script> Submissions { get; } = new List<Script>();

        public Script LastSubmission
        {
            get
            {
                return (Submissions.Count == 0) ? null : Submissions[Submissions.Count - 1];
            }
        }

        /// <summary>
        /// Index of function created with <see cref="PhpFunctions.create_function"/>.
        /// </summary>
        public int LastLambdaIndex { get; set; } = 0;
    }
}
