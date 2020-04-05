using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.DateTime
{
    /// <summary>
    /// DateTimeInterface is meant so that both DateTime and DateTimeImmutable can be type hinted for.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("date")]
    public interface DateTimeInterface
    {
        public class _statics   // nested "_statics" class is understood by compiler and runtime, contained readonly/const fields are treated as containing class's constants
        {
            // following can be moved into the containing class once C# compiler allows it (or using IL hack)

            #region Constants

            public const string ATOM = DateTimeFunctions.DATE_ATOM;// @"Y-m-d\TH:i:sP";
            public const string COOKIE = DateTimeFunctions.DATE_COOKIE;// @"l, d-M-y H:i:s T";
            public const string ISO8601 = DateTimeFunctions.DATE_ISO8601;// @"Y-m-d\TH:i:sO";
            public const string RFC822 = DateTimeFunctions.DATE_RFC822;// @"D, d M y H:i:s O";
            public const string RFC850 = DateTimeFunctions.DATE_RFC850;// @"l, d-M-y H:i:s T";
            public const string RFC1036 = DateTimeFunctions.DATE_RFC1036;// @"D, d M y H:i:s O";
            public const string RFC1123 = DateTimeFunctions.DATE_RFC1123;// @"D, d M Y H:i:s O";
            public const string RFC2822 = DateTimeFunctions.DATE_RFC2822;// @"D, d M Y H:i:s O";
            public const string RFC3339 = DateTimeFunctions.DATE_RFC3339;// @"Y-m-d\TH:i:sP";
            public const string RFC7231 = DateTimeFunctions.DATE_RFC7231;// @"D, d M Y H:i:s \G\M\T";
            public const string RFC3339_EXTENDED = DateTimeFunctions.DATE_RFC3339_EXTENDED;// @"Y-m-d\TH:i:s.vP";
            public const string RSS = DateTimeFunctions.DATE_RSS;// @"D, d M Y H:i:s O";
            public const string W3C = DateTimeFunctions.DATE_W3C;// @"Y-m-d\TH:i:sP";

            #endregion
        }

        /// <summary>
        /// Returns the difference between two DateTime objects
        /// </summary>
        DateInterval diff(DateTimeInterface datetime2, bool absolute = false);

        /// <summary>
        /// Returns date formatted according to given format.
        /// </summary>
        string format(string format);

        /// <summary>
        /// Returns the timezone offset.
        /// </summary>
        long getOffset();

        /// <summary>
        /// Gets the Unix timestamp.
        /// </summary>
        long getTimestamp();

        /// <summary>
        /// Return time zone relative to given DateTime.
        /// </summary>
        DateTimeZone getTimezone();

        /// <summary>
        /// The __wakeup handler.
        /// </summary>
        void __wakeup();
    }
}
