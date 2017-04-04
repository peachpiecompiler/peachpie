using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Pchp.Core
{
    partial class Context
    {
        /// <summary>
        /// Script options.
        /// </summary>
        public sealed class ScriptOptions
        {
            /// <summary>
            /// Script context.
            /// </summary>
            public Context Context { get; set; }

            /// <summary>
            /// The path and location within the script source if it originated from a file, empty otherwise.
            /// </summary>
            public Location Location;

            /// <summary>
            /// Specifies whether debugging symbols should be emitted.
            /// </summary>
            public bool EmitDebugInformation { get; set; }

            /// <summary>
            /// Value indicating the script is a submission (without opening PHP tag).
            /// </summary>
            public bool IsSubmission { get; set; }
        }

        /// <summary>
        /// Encapsulates a compiled script that can be evaluated.
        /// </summary>
        public interface IScript
        {
            /// <summary>
            /// Evaluates the script.
            /// </summary>
            /// <param name="ctx">Current runtime context.</param>
            /// <param name="locals">Array of local variables.</param>
            /// <param name="this">Optional. Reference to current <c>$this</c> object.</param>
            /// <returns>Return value of the script.</returns>
            PhpValue Evaluate(Context ctx, PhpArray locals, object @this);

            /// <summary>
            /// Resolves global function handle(s).
            /// </summary>
            IEnumerable<System.Reflection.MethodInfo> GetGlobalRoutineHandle(string name);
        }

        /// <summary>
        /// Provides dynamic scripts compilation in current context.
        /// </summary>
        public interface IScriptingProvider
        {
            /// <summary>
            /// Gets compiled code.
            /// </summary>
            /// <param name="options">Compilation options.</param>
            /// <param name="code">Script source code.</param>
            /// <returns>Compiled script instance.</returns>
            IScript CreateScript(ScriptOptions options, string code);
        }

        private sealed class UnsupportedScriptingProvider : IScriptingProvider
        {
            public IScript CreateScript(ScriptOptions options, string code)
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Gets dynamic scripting provider.
        /// Cannot be <c>null</c>.
        /// </summary>
        public virtual IScriptingProvider ScriptingProvider => GlobalServices.GetService<IScriptingProvider>();
    }
}
