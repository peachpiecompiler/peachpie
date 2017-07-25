using Pchp.Core;
using Pchp.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Pchp.Library.Spl
{
    public static class SplObjects
    {
        /// <summary>Return hash id for given object.</summary>
        /// <param name="obj">Object instance. Cannot be <c>null</c>.</param>
        /// <returns>Object hash code as 32-digit hexadecimal number.</returns>
        public static string spl_object_hash(object obj)
        {
            if (obj != null)
            {
                return obj.GetHashCode().ToString("x32");
            }
            else
            {
                PhpException.InvalidArgumentType(nameof(obj), "object");
                return string.Empty;
            }
        }

        /// <summary>
        /// This function returns an array with the current available SPL classes.
        /// </summary>
        public static PhpArray spl_classes()
        {
            throw new NotImplementedException();
        }
    }
}
