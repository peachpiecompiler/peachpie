#nullable enable

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
        public static long UtcToUnixTimeStamp(DateTime dt)
        {
            double seconds = UtcToUnixTimeStampFloat(dt);

            if (seconds < long.MinValue)
                return long.MinValue;

            if (seconds > long.MaxValue)
                return long.MaxValue;

            return (long)seconds;
        }

        public static double UtcToUnixTimeStampFloat(DateTime dt)
        {
            return (dt - UtcStartOfUnixEpoch).TotalSeconds;
        }

        /// <summary>
        /// Converts UNIX timestamp to <see cref="DateTime"/> representing UTC time.
        /// </summary>
        /// <param name="ts">Unix timestamp.</param>
        /// <returns>Time.</returns>
        public static DateTime UnixTimeStampToUtc(long ts)
        {
            return DateTimeOffset.FromUnixTimeSeconds(ts).UtcDateTime;
        }
    }
}
