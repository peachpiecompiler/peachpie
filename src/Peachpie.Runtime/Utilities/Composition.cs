using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Pchp.Core.Utilities
{
    static class CompositionExtension
    {
        /// <summary>
        /// Gets exported value or <c>null</c>.
        /// </summary>
        public static T TryGetExport<T>(this CompositionContext context)
        {
            T value;
            context.TryGetExport<T>(out value);
            return value;
        }

        /// <summary>
        /// Loads assembly by name. Returns <c>null</c> if load fails.
        /// </summary>
        public static Assembly TryLoad(string assemblyName)
        {
            try
            {
                return Assembly.Load(new AssemblyName(assemblyName));
            }
            catch
            {
                return null;
            }
        }
    }
}
