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
            return typeof(Context).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        }

        /// <summary>
        /// Gets runtime version suffix including the leading dash, or empty string is runtime is build without siffix.
        /// </summary>
        /// <returns></returns>
        public static string GetRuntimeVersionSuffix()
        {
            var str = GetRuntimeInformationalVersion();
            var dash = str.IndexOf('-');
            return dash < 0 ? string.Empty : str.Substring(dash);
        }
    }
}
