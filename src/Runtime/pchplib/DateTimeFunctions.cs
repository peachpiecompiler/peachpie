using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Library
{
    public static class DateTimeFunctions
    {
        #region Constants

        public const string DATE_ATOM = @"Y-m-d\TH:i:sP";

        public const string DATE_COOKIE = @"D, d M Y H:i:s T";

        public const string DATE_ISO8601 = @"Y-m-d\TH:i:sO";

        public const string DATE_RFC822 = @"D, d M y H:i:s O";

        public const string DATE_RFC850 = @"l, d-M-y H:i:s T";

        public const string DATE_RFC1123 = @"D, d M Y H:i:s T";

        public const string DATE_RFC1036 = @"l, d-M-y H:i:s T";

        public const string DATE_RFC2822 = @"D, d M Y H:i:s T";

        public const string DATE_RFC3339 = @"Y-m-d\TH:i:sP";

        public const string DATE_RSS = @"D, d M Y H:i:s T";

        public const string DATE_W3C = @"Y-m-d\TH:i:sO";

        #endregion

        #region microtime

        /// <summary>
        /// Returns the string "msec sec" where sec is the current time measured in the number of seconds
        /// since the Unix Epoch (0:00:00 January 1, 1970 GMT), and msec is the microseconds part.
        /// </summary>
        /// <returns>String containing number of miliseconds, space and number of seconds.</returns>
        public static string microtime()
        {
            // time from 1970
            TimeSpan fromUnixEpoch = System.DateTime.UtcNow - DateTimeUtils.UtcStartOfUnixEpoch;

            // seconds part to return
            long seconds = (long)fromUnixEpoch.TotalSeconds;

            // only remaining time less than one second
            TimeSpan mSec = fromUnixEpoch.Subtract(new TimeSpan(seconds * 10000000)); // convert seconds to 100 ns
            double remaining = ((double)mSec.Ticks) / 10000000; // convert from 100ns to seconds

            return String.Format("{0} {1}", remaining, seconds);
        }

        /// <summary>
        /// Returns the fractional time in seconds from the start of the UNIX epoch.
        /// </summary>
        /// <param name="returnDouble"><c>true</c> to return the double, <c>false</c> to return string.</param>
        /// <returns><see cref="String"/> containing number of miliseconds, space and number of seconds
        /// if <paramref name="returnDouble"/> is <c>false</c> and <see cref="double"/>
        /// containing the fractional count of seconds otherwise.</returns>
        public static PhpValue microtime(bool returnDouble)
        {
            if (returnDouble)
                return PhpValue.Create((DateTime.UtcNow - DateTimeUtils.UtcStartOfUnixEpoch).TotalSeconds);
            else
                return PhpValue.Create(microtime());
        }

        #endregion
    }
}
