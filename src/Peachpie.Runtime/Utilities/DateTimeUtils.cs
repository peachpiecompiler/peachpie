using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Core.Utilities
{
    public static class DateTimeUtils
    {
        /// <summary>
        /// Time 0 in terms of Unix TimeStamp.
        /// </summary>
        public static readonly DateTime/*!*/UtcStartOfUnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Converts <see cref="DateTime"/> representing UTC time to UNIX timestamp.
        /// </summary>
        /// <param name="dt">Time.</param>
        /// <returns>Unix timestamp.</returns>
        public static int UtcToUnixTimeStamp(DateTime dt)
        {
            double seconds = (dt - UtcStartOfUnixEpoch).TotalSeconds;

            return (seconds < int.MinValue)
                ? int.MinValue
                : (seconds > int.MaxValue)
                    ? int.MaxValue
                    : (int)seconds;
        }
    }
}
