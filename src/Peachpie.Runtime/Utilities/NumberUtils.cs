#nullable enable

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
            return l == unchecked((int)l);
        }

        /// <summary>Calculates the quotient of two 32-bit signed integers and also returns the remainder in an output parameter.</summary>
        /// <returns>The quotient of the specified numbers.</returns>
        /// <param name="a">The dividend.</param>
        /// <param name="b">The divisor.</param>
        /// <param name="result">The remainder.</param>
        public static long DivRem(long a, long b, out long result)
        {
            result = a % b;
            return a / b;
        }
    }

    #endregion
}
