using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

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

            /// <summary>
            /// Optional. Collection of additional metadata references.
            /// </summary>
            public string[] AdditionalReferences { get; set; }
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
            /// <param name="self">Optional. Current type context in which the script is included. Used to resolve <c>self</c> and <c>parent</c> in evaluated script.</param>
            /// <returns>Return value of the script.</returns>
            PhpValue Evaluate(Context ctx, PhpArray locals, object @this = null, RuntimeTypeHandle self = default);

            /// <summary>
            /// Resolves global function handle(s).
            /// </summary>
            IEnumerable<MethodInfo> GetGlobalRoutineHandle(string name);
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
        [Obsolete("replaced with static DefaultScriptingProvider")]
        public virtual IScriptingProvider/*!*/ScriptingProvider => DefaultScriptingProvider;

        /// <summary>
        /// Gets dynamic scripting provider.
        /// Cannot be <c>null</c>.
        /// </summary>
        public static IScriptingProvider/*!*/DefaultScriptingProvider
        {
            get
            {
                if (s_lazyScriptingProvider == null)
                {
                    Type type = null;

                    // together with `eval()` function:
                    try
                    {
                        var ass = Assembly.Load(new AssemblyName("Peachpie.Library.Scripting"));
                        type = ass.GetType("Peachpie.Library.Scripting.ScriptingProvider", throwOnError: false);
                    }
                    catch // FileNotFoundException, FileLoadException
                    {
                        // missing Peachpie.Library.Scripting assembly is expected => scripting is not allowed
                    }

                    // instantiate the provider singleton
                    var provider = type != null
                        ? (IScriptingProvider)Activator.CreateInstance(type)
                        : new UnsupportedScriptingProvider();

                    Interlocked.CompareExchange(ref s_lazyScriptingProvider, provider, null);
                }

                return s_lazyScriptingProvider;
            }
        }

        static IScriptingProvider s_lazyScriptingProvider;
    }
}
