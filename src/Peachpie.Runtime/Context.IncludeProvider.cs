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
        /// Abstraction providing script resolution.
        /// </summary>
        public interface IIncludeResolver
        {
            /// <summary>
            /// Tries to resolve a script at given path.
            /// </summary>
            ScriptInfo ResolveScript(Context ctx, string cd, string path);
        }

        /// <summary>
        /// Helper object that manages inclusion resolution for
        /// non-declared scripts and paths prefixed with a scheme (scheme://).
        /// </summary>
        public sealed class IncludeProvider
        {
            /// <summary>
            /// Singleton instance, cannot be <c>null</c>.
            /// </summary>
            public static IncludeProvider/*!*/Instance = new IncludeProvider();

            readonly Dictionary<string, IIncludeResolver>/*!*/_resolvers = new Dictionary<string, IIncludeResolver>(StringComparer.Ordinal);

            private IncludeProvider() { }

            /// <summary>
            /// Registers inclusion handler for paths prefixed with given scheme.
            /// </summary>
            public void RegisterSchemeIncluder(string scheme, IIncludeResolver resolver)
            {
                Debug.Assert(!string.IsNullOrEmpty(scheme));

                _resolvers.Add(scheme, resolver ?? throw new ArgumentNullException(nameof(resolver)));
            }

            /// <summary>
            /// Resolves wrapper for given scheme.
            /// </summary>
            public bool TryResolveSchemeIncluder(string scheme, out IIncludeResolver resolver)
            {
                return _resolvers.TryGetValue(scheme, out resolver);
            }
        }
    }
}
