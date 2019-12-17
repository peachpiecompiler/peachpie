using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
        /// A lazily instantiated instance of <see cref="Context"/> for the current runtime context.
        /// </summary>
        /// <exception cref="NotSupportedException">Instance of <see cref="Context"/> cannot be provided.</exception>
        public static Context CurrentContext => CurrentContextProvider() ?? throw new NotSupportedException();

        /// <summary>
        /// Gets or sets a context provider to provide an instance of <see cref="Context"/> to PHP classes instantiated without given <see cref="Context"/>.
        /// </summary>
        /// <param name="value">Must not be <c>null</c>.</param>
        /// <remarks>The default implementation provides an instance of <see cref="Context"/> associated with the current <see cref="ExecutionContext"/>.</remarks>
        public static Func<Context> CurrentContextProvider
        {
            get
            {
                if (s_ContextProvider == null)
                {
                    s_ContextProvider = CreateContextProvider();
                }

                return s_ContextProvider;
            }
            set
            {
                s_ContextProvider = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// Default context provider that creates a new singleton instance lazily.
        /// This instance is not bound to a runtime context.
        /// </summary>
        static Func<Context> s_ContextProvider;

        static Func<Context> CreateContextProvider()
        {
            //// within ASP.NET Core app:
            //var tHttpContextExtension = Type.GetType("Peachpie.AspNetCore.Web.HttpContextExtension", throwOnError: false);
            //if (tHttpContextExtension != null)
            //{
            //    var mGetOrCreateContext = tHttpContextExtension.GetMethod("GetOrCreateContext", Array.Empty<Type>());
            //    Debug.Assert(mGetOrCreateContext != null);
            //    return (Func<Context>)mGetOrCreateContext.CreateDelegate(typeof(Func<Context>));
            //}

            // default provider,
            // creates PHP Context within the ExecutionContext:
            Interlocked.CompareExchange(ref s_lazyCurrentContext, new AsyncLocal<Context>(), null);

            return () =>
            {
                var value = s_lazyCurrentContext.Value;
                if (value == null)
                {
                    s_lazyCurrentContext.Value = value = Context.CreateEmpty();
                }

                return value;
            };
        }
        static AsyncLocal<Context> s_lazyCurrentContext;

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
