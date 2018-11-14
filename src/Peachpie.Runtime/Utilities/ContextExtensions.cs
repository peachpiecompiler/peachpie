using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Utilities
{
    /// <summary>
    /// Extension context methods.
    /// </summary>
    public static class ContextExtensions
    {
        /// <summary>
        /// Gets runtime informational version including suffix if provided.
        /// </summary>
        public static string GetRuntimeInformationalVersion()
        {
            return typeof(Context).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        }

        /// <summary>
        /// Gets runtime version suffix including the leading dash, or empty string if runtime is build without suffix.
        /// </summary>
        public static string GetRuntimeVersionSuffix()
        {
            var str = GetRuntimeInformationalVersion();
            var dash = str.IndexOf('-');
            return dash < 0 ? string.Empty : str.Substring(dash);
        }

        /// <summary>
        /// Lazily instantiated instance of <see cref="Context"/> used for newly created PHP objects marked as shared.
        /// </summary>
        public static Context DefaultContext
        {
            get
            {
                // CONSIDER: extensible, web context
                return _sharedContext ?? (_sharedContext = Context.CreateEmpty());
            }
            set
            {
                _sharedContext = value;
            }
        }
        static Context _sharedContext;

        /// <summary>
        /// Gets value indicating whether the runtime was built as debug.
        /// </summary>
        public static bool IsDebugRuntime() =>
#if DEBUG
                    true
#else
                    false
#endif
            ;
    }
}
