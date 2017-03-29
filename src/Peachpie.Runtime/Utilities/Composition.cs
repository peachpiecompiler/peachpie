using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
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
    }
}
