using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Core.Utilities
{
    #region Numbers

    internal static class NumberUtils
    {
        /// <summary>
        /// Determines whether given <see cref="long"/> can be safely converted to <see cref="int"/>.
        /// </summary>
        public static bool IsInt32(long l)
        {
            int i = unchecked((int)l);
            return (i == l);
        }
    }

    #endregion
}
