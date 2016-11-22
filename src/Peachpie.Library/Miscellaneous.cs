using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library
{
    public static class Miscellaneous
    {
        // [return: CastToFalse] // once $extension will be supported
        public static string phpversion(string extension = null)
        {
            if (extension != null)
            {
                throw new NotImplementedException(nameof(extension));
            }

            return Environment.PHP_MAJOR_VERSION + "." + Environment.PHP_MINOR_VERSION + "." + Environment.PHP_RELEASE_VERSION;
        }
    }
}
