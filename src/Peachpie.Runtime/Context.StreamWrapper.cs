using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Pchp.Core
{
    partial class Context
    {
        /// <summary>
        /// Core stream wrapper interface providing I/O operations by protocol.
        /// </summary>
        public interface IStreamWrapper
        {
            /// <summary>
            /// Tries to resolve compiled script at given path.
            /// </summary>
            bool ResolveInclude(Context ctx, string cd, string path, out ScriptInfo script);
        }

        static readonly Dictionary<string, Lazy<IStreamWrapper>>/*!*/s_streamwrappers = new Dictionary<string, Lazy<IStreamWrapper>>();

        /// <summary>
        /// Sets a stresm wrapper for given scheme.
        /// Previously set stream wrapper is overwritten.
        /// </summary>
        public static void RegisterGlobalStreamWrapper(Lazy<IStreamWrapper, string> lazywrapper)
        {
            if (lazywrapper == null)
            {
                throw new ArgumentNullException(nameof(lazywrapper));
            }

            if (string.IsNullOrEmpty(lazywrapper.Metadata))
            {
                throw new ArgumentException();
            }

            // TODO: thread-safety
            s_streamwrappers[lazywrapper.Metadata] = lazywrapper;
        }

        /// <summary>
        /// Gets registered stream wrapper for given scheme.
        /// </summary>
        /// <returns>Stream wrapper instance or <c>null</c> if there is no wrapper registered for the given scheme or the wrapper factory method returned <c>null</c>.</returns>
        public static IStreamWrapper GetGlobalStreamWrapper(string scheme)
        {
            // TODO: thread-safety
            if (scheme != null && s_streamwrappers.TryGetValue(scheme, out var wrapper))
            {
                return wrapper.Value;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets enumeration of registered global stream wrappers.
        /// </summary>
        public static IEnumerable<string> GetGlobalStreamWrappers() => s_streamwrappers.Keys;
    }
}
