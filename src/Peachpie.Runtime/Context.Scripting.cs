using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            // TODO: PhpRoutineInfo GetGlobalFunction(name)
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
        public virtual IScriptingProvider ScriptingProvider => _scriptingProvider ?? CreateScriptingProvider();
        static IScriptingProvider _scriptingProvider;

        IScriptingProvider CreateScriptingProvider()
        {
            lock (typeof(IScriptingProvider))
            {
                if (_scriptingProvider == null) // double checked lock
                {
                    //TODO: [Import(typeof(IScriptingProvider)]
                    _scriptingProvider = new UnsupportedScriptingProvider();
                }
            }

            return _scriptingProvider;
        }
    }
}
