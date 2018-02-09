using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.DateTime
{
    /// <summary>
    /// DateTimeInterface is meant so that both DateTime and DateTimeImmutable can be type hinted for.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public interface DateTimeInterface
    {
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
