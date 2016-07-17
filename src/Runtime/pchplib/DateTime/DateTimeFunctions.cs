using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System_DateTime = System.DateTime;

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

        #region date, idate, gmdate

        /// <summary>
		/// Returns a string formatted according to the given format string using the current local time.
		/// </summary>
		/// <param name="format">Format definition for output.</param>
		/// <returns>Formatted string.</returns>
		public static string date(Context ctx, string format)
        {
            return FormatDate(format, System_DateTime.UtcNow, PhpTimeZone.GetCurrentTimeZone(ctx));
        }

        /// <summary>
        /// Returns a string formatted according to the given format string using the given integer timestamp.
        /// </summary>
        /// <param name="format">Format definition for output.</param>
        /// <param name="timestamp">Nuber of seconds since 1970 specifying a date.</param>
        /// <returns>Formatted string.</returns>
        public static string date(Context ctx, string format, int timestamp)
        {
            return FormatDate(format, DateTimeUtils.UnixTimeStampToUtc(timestamp), PhpTimeZone.GetCurrentTimeZone(ctx));
        }

        /// <summary>
        /// Identical to the date() function except that the time returned is Greenwich Mean Time (GMT)
        /// </summary>
        /// <param name="format">Format definition for output.</param>
        /// <returns>Formatted string.</returns>
        public static string gmdate(string format)
        {
            return FormatDate(format, System_DateTime.UtcNow, DateTimeUtils.UtcTimeZone);
        }

        /// <summary>
        /// Identical to the date() function except that the time returned is Greenwich Mean Time (GMT)
        /// </summary>
        /// <param name="format">Format definition for output.</param>
        /// <param name="timestamp">Nuber of seconds since 1970 specifying a date.</param>
        /// <returns>Formatted string.</returns>
        public static string gmdate(string format, int timestamp)
        {
            return FormatDate(format, DateTimeUtils.UnixTimeStampToUtc(timestamp), DateTimeUtils.UtcTimeZone);
        }

        /// <summary>
        /// Returns a part of current time.
        /// </summary>
        /// <param name="format">Format definition for output.</param>
        /// <returns>Part of the date, e.g. month or hours.</returns>
        public static int idate(Context ctx, string format)
        {
            if (format == null || format.Length != 1)
                //PhpException.InvalidArgument("format");
                throw new ArgumentException();

            return GetDatePart(format[0], System_DateTime.UtcNow, PhpTimeZone.GetCurrentTimeZone(ctx));
        }

        /// <summary>
        /// Returns a part of a specified timestamp.
        /// </summary>
        /// <param name="format">Format definition for output.</param>
        /// <param name="timestamp">Nuber of seconds since 1970 specifying a date.</param>
        /// <returns>Part of the date, e.g. month or hours.</returns>
        public static int idate(Context ctx, string format, int timestamp)
        {
            if (format == null || format.Length != 1)
                //PhpException.InvalidArgument("format");
                throw new ArgumentException();

            return GetDatePart(format[0], DateTimeUtils.UnixTimeStampToUtc(timestamp), PhpTimeZone.GetCurrentTimeZone(ctx));
        }

        private static int GetDatePart(char format, System_DateTime utc, TimeZoneInfo/*!*/ zone)
        {
            var local = TimeZoneInfo.ConvertTime(utc, zone);// zone.ToLocalTime(utc);

            switch (format)
            {
                case 'B':
                    // Swatch Beat (Internet Time) - 000 through 999
                    return GetSwatchBeat(utc);

                case 'd':
                    // Day of the month
                    return local.Day;

                case 'g':
                case 'h':
                    // 12-hour format:
                    return (local.Hour == 12) ? 12 : local.Hour % 12;

                case 'G':
                case 'H':
                    // 24-hour format:
                    return local.Hour;

                case 'i':
                    return local.Minute;

                case 'I':
                    return zone.IsDaylightSavingTime(local) ? 1 : 0;

                case 'j':
                    goto case 'd';

                case 'L':
                    return System_DateTime.IsLeapYear(local.Year) ? 1 : 0;

                case 'm':
                    return local.Month;

                case 'n':
                    goto case 'm';

                case 's':
                    return local.Second;

                case 't':
                    return System_DateTime.DaysInMonth(local.Year, local.Month);

                case 'U':
                    return DateTimeUtils.UtcToUnixTimeStamp(utc);

                case 'w':
                    // day of the week - 0 (for Sunday) through 6 (for Saturday)
                    return (int)local.DayOfWeek;

                case 'W':
                    {
                        // ISO-8601 week number of year, weeks starting on Monday:
                        int week, year;
                        GetIsoWeekAndYear(local, out week, out year);
                        return week;
                    }

                case 'y':
                    return local.Year % 100;

                case 'Y':
                    return local.Year;

                case 'z':
                    return local.DayOfYear - 1;

                case 'Z':
                    return (int)zone.GetUtcOffset(local).TotalSeconds;

                default:
                    //PhpException.InvalidArgument("format");
                    //return 0;
                    throw new ArgumentException();
            }
        }

        internal static string FormatDate(string format, System_DateTime utc, TimeZoneInfo zone)
        {
            Debug.Assert(zone != null);

            if (format == null)
                return string.Empty;

            var local = TimeZoneInfo.ConvertTime(utc, zone);

            // here we are creating output string
            StringBuilder result = new StringBuilder();
            bool escape = false;

            foreach (char ch in format)
            {
                if (escape)
                {
                    result.Append(ch);
                    escape = false;
                    continue;
                }

                switch (ch)
                {
                    case 'a':
                        // Lowercase Ante meridiem and Post meridiem - am or pm
                        result.Append(local.ToString("tt", DateTimeFormatInfo.InvariantInfo).ToLowerInvariant());
                        break;

                    case 'A':
                        // Uppercase Ante meridiem and Post meridiem - AM or PM
                        result.Append(local.ToString("tt", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'B':
                        // Swatch Beat (Internet Time) - 000 through 999
                        result.AppendFormat("{0:000}", GetSwatchBeat(utc));
                        break;

                    case 'c':
                        {
                            // ISO 8601 date (added in PHP 5) 2004-02-12T15:19:21+00:00
                            result.Append(local.ToString("yyyy-MM-dd'T'HH:mm:ss", DateTimeFormatInfo.InvariantInfo));

                            TimeSpan offset = zone.GetUtcOffset(local);
                            result.AppendFormat("{0}{1:00}:{2:00}", (offset.Ticks < 0) ? ""/*offset.Hours already < 0*/ : "+", offset.Hours, offset.Minutes);
                            break;
                        }

                    case 'd':
                        // Day of the month, 2 digits with leading zeros - 01 to 31
                        result.Append(local.ToString("dd", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'e':
                        // Timezone identifier (added in PHP 5.1.0)
                        //result.Append(zone.Id);
                        //break;
                        throw new NotImplementedException();

                    case 'D':
                        // A textual representation of a day, three letters - Mon through Sun
                        result.Append(local.ToString("ddd", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'F':
                        // A full textual representation of a month, such as January or March - January through December
                        result.Append(local.ToString("MMMM", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'g':
                        // 12-hour format of an hour without leading zeros - 1 through 12
                        result.Append(local.ToString("%h", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'G':
                        // 24-hour format of an hour without leading zeros - 0 through 23
                        result.Append(local.ToString("%H", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'h':
                        // 12-hour format of an hour with leading zeros - 01 through 12
                        result.Append(local.ToString("hh", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'H':
                        // 24-hour format of an hour with leading zeros - 00 through 23
                        result.Append(local.ToString("HH", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'i':
                        // Minutes with leading zeros - 00 to 59
                        result.Append(local.ToString("mm", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'I':
                        // Whether or not the date is in daylights savings time - 1 if Daylight Savings Time, 0 otherwise.
                        result.Append(zone.IsDaylightSavingTime(local) ? "1" : "0");
                        break;

                    case 'j':
                        // Day of the month without leading zeros - 1 to 31
                        result.Append(local.ToString("%d", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'l':
                        // A full textual representation of the day of the week - Sunday through Saturday
                        result.Append(local.ToString("dddd", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'L':
                        // Whether it's a leap year - 1 if it is a leap year, 0 otherwise.
                        result.Append(System_DateTime.IsLeapYear(local.Year) ? "1" : "0");
                        break;

                    case 'm':
                        // Numeric representation of a month, with leading zeros - 01 through 12
                        result.Append(local.ToString("MM", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'M':
                        // A short textual representation of a month, three letters - Jan through Dec
                        result.Append(local.ToString("MMM", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'n':
                        // Numeric representation of a month, without leading zeros - 1 through 12
                        result.Append(local.ToString("%M", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'N':
                        // ISO-8601 numeric representation of the day of the week (added in PHP 5.1.0)
                        int day_of_week = (int)local.DayOfWeek;
                        result.Append(day_of_week == 0 ? 7 : day_of_week);
                        break;

                    case 'o':
                        {
                            // ISO-8601 year number. This has the same value as Y, except that if the ISO
                            // week number (W) belongs to the previous or next year, that year is used instead.
                            // (added in PHP 5.1.0)
                            int week, year;
                            GetIsoWeekAndYear(local, out week, out year);
                            result.Append(year);
                            break;
                        }

                    case 'O':
                        {
                            // Difference to Greenwich time (GMT) in hours Example: +0200
                            TimeSpan offset = zone.GetUtcOffset(local);
                            string sign = (offset.Ticks < 0) ? ((offset.Hours < 0) ? string.Empty : "-") : "+";
                            result.AppendFormat("{0}{1:00}{2:00}", sign, offset.Hours, offset.Minutes);
                            break;
                        }

                    case 'P':
                        {
                            // same as 'O' but with the extra colon between hours and minutes
                            // Difference to Greenwich time (GMT) in hours Example: +02:00
                            TimeSpan offset = zone.GetUtcOffset(local);
                            string sign = (offset.Ticks < 0) ? ((offset.Hours < 0) ? string.Empty : "-") : "+";
                            result.AppendFormat("{0}{1:00}:{2:00}", sign, offset.Hours, offset.Minutes);
                            break;
                        }

                    case 'r':
                        // RFC 822 formatted date Example: Thu, 21 Dec 2000 16:01:07 +0200
                        result.Append(local.ToString("ddd, dd MMM yyyy H:mm:ss ", DateTimeFormatInfo.InvariantInfo));
                        goto case 'O';

                    case 's':
                        // Seconds, with leading zeros - 00 through 59
                        result.Append(local.ToString("ss", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'S':
                        result.Append(GetDayNumberSuffix(local.Day));
                        break;

                    case 't':
                        // Number of days in the given month 28 through 31
                        result.Append(System_DateTime.DaysInMonth(local.Year, local.Month));
                        break;

                    case 'T':
                        // Timezone setting of this machine Examples: EST, MDT ...
                        result.Append(zone.IsDaylightSavingTime(local) ? zone.DaylightName : zone.StandardName);
                        break;

                    case 'U':
                        // Seconds since the Unix Epoch (January 1 1970 00:00:00 GMT)
                        result.Append(DateTimeUtils.UtcToUnixTimeStamp(utc));
                        break;

                    case 'u':
                        // Microseconds (added in PHP 5.2.2)
                        result.Append((utc.Millisecond / 1000).ToString("D6"));
                        break;

                    case 'w':
                        // Numeric representation of the day of the week - 0 (for Sunday) through 6 (for Saturday)
                        result.Append((int)local.DayOfWeek);
                        break;

                    case 'W':
                        {
                            // ISO-8601 week number of year, weeks starting on Monday (added in PHP 4.1.0) Example: 42 (the 42nd week in the year)
                            int week, year;
                            GetIsoWeekAndYear(local, out week, out year);
                            result.Append(week);
                            break;
                        }

                    case 'y':
                        // A two digit representation of a year Examples: 99 or 03
                        result.Append(local.ToString("yy", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'Y':
                        // A full numeric representation of a year, 4 digits Examples: 1999 or 2003
                        result.Append(local.ToString("yyyy", DateTimeFormatInfo.InvariantInfo));
                        break;

                    case 'z':
                        // The day of the year starting from 0
                        result.Append(local.DayOfYear - 1);
                        break;

                    case 'Z':
                        // TimeZone offset in seconds:
                        result.Append((int)zone.GetUtcOffset(local).TotalSeconds);
                        break;

                    case '\\':
                        // Escape char. Output next character directly to the result.
                        escape = true;
                        break;

                    default:
                        // unrecognized character, print it as-is.
                        result.Append(ch);
                        break;
                }
            }

            if (escape)
                result.Append('\\');

            return result.ToString();
        }

        /// <summary>
        /// Converts a given <see cref="DateTime"/> to the ISO week of year number and ISO year number.
        /// </summary>
        /// <param name="dt">The <see cref="DateTime"/>.</param>
        /// <param name="week">The ISO week of year number.</param>
        /// <param name="year">The ISO year number.</param>
        static void GetIsoWeekAndYear(System_DateTime dt, out int week, out int year)
        {
            int weekDay = (int)dt.DayOfWeek - 1; // Day of week (0 for Monday .. 6 for Sunday)
            int yearDay = dt.DayOfYear;     // Days since January 1st (1 .. 367)
            int firstDay = (7 + weekDay - yearDay % 7 + 1) % 7; // Weekday of 1st January

            if ((yearDay <= 7 - firstDay) && firstDay > 3)
            {
                // Week is a last year week (52 or 53)
                week = (firstDay == 4 || (firstDay == 5 && System_DateTime.IsLeapYear(dt.Year - 1))) ? 53 : 52;
                year = dt.Year - 1;
            }
            else if ((System_DateTime.IsLeapYear(dt.Year) ? 366 : 365) - yearDay < 3 - weekDay)
            {
                // Week is a next year week (1)
                week = 1;
                year = dt.Year + 1;
            }
            else
            {
                // Normal week
                week = (yearDay + 6 - weekDay + firstDay) / 7;
                if (firstDay > 3) week--;
                year = dt.Year;
            }
        }

        static int GetSwatchBeat(System_DateTime utc)
        {
            int seconds = DateTimeUtils.UtcToUnixTimeStamp(utc);
            int beat = (int)(((seconds - (seconds - ((seconds % 86400) + 3600))) * 10) / 864) % 1000;
            return (beat < 0) ? beat + 1000 : beat;
        }

        /// <summary>
        /// English ordinal suffix for the day of the month, 2 characters - st, nd, rd or th.
        /// </summary>
        /// <param name="DayNumber">Number of the day. In [1..31].</param>
        /// <returns>st, nd, rd or th</returns>
        private static string GetDayNumberSuffix(int DayNumber /* = 1..31 */)
        {
            Debug.Assert(DayNumber >= 1 && DayNumber <= 31);

            int DayNumber10 = DayNumber % 10;

            if (DayNumber10 == 1) { if (DayNumber/*%100*/ != 11) return "st"; }
            else if (DayNumber10 == 2) { if (DayNumber/*%100*/ != 12) return "nd"; }
            else if (DayNumber10 == 3) { if (DayNumber/*%100*/ != 13) return "rd"; }

            return "th";
        }

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
            TimeSpan fromUnixEpoch = System_DateTime.UtcNow - DateTimeUtils.UtcStartOfUnixEpoch;

            // seconds part to return
            long seconds = (long)fromUnixEpoch.TotalSeconds;

            // only remaining time less than one second
            TimeSpan mSec = fromUnixEpoch.Subtract(new TimeSpan(seconds * 10000000)); // convert seconds to 100 ns
            double remaining = ((double)mSec.Ticks) / 10000000; // convert from 100ns to seconds

            return remaining.ToString("G", System.Globalization.NumberFormatInfo.InvariantInfo) + " " + seconds.ToString();
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
                return PhpValue.Create((System_DateTime.UtcNow - DateTimeUtils.UtcStartOfUnixEpoch).TotalSeconds);
            else
                return PhpValue.Create(microtime());
        }

        #endregion

        #region strtotime

        /// <summary>
        /// Parses a string containing an English date format into a UNIX timestamp relative to the current time.
        /// </summary>
        /// <param name="time">String containing time definition</param>
        /// <returns>Number of seconds since 1/1/1970 or -1 on failure.</returns>
        public static int strtotime(string time)
        {
            return StringToTime(time, System_DateTime.UtcNow);
        }

        /// <summary>
        /// Parses a string containing an English date format into a UNIX timestamp relative to a specified time.
        /// </summary>
        /// <param name="time">String containing time definition.</param>
        /// <param name="start">Timestamp (seconds from 1970) to which is the new timestamp counted.</param>
        /// <returns>Number of seconds since 1/1/1970 or -1 on failure.</returns>
        public static int strtotime(string time, int start)
        {
            return StringToTime(time, DateTimeUtils.UnixTimeStampToUtc(start));
        }

        /// <summary>
        /// Implementation of <see cref="StringToTime(string,int)"/> function.
        /// </summary>
        static int StringToTime(string time, System_DateTime startUtc)
        {
            //if (time == null) return false;
            //time = time.Trim();
            //if (time.Length == 0) return false;

            //string error = null;
            //int result = StrToTime.DateInfo.Parse(time, startUtc, out error);
            //if (error != null)
            //{
            //    PhpException.Throw(PhpError.Warning, error);
            //    return false;
            //}

            //return result;

            throw new NotImplementedException();
        }

        #endregion

        #region time

        /// <summary>
        /// Returns the current time measured in the number of seconds since the Unix Epoch (January 1 1970 00:00:00 GMT).
        /// </summary>
        /// <returns>Number of seconds since 1970.</returns>
        public static int time()
        {
            return DateTimeUtils.UtcToUnixTimeStamp(System_DateTime.UtcNow);
        }

        #endregion

        #region date_sunrise, date_sunset

        public const int SUNFUNCS_RET_TIMESTAMP = (int)TimeFormats.Integer;
        public const int SUNFUNCS_RET_STRING = (int)TimeFormats.String;
        public const int SUNFUNCS_RET_DOUBLE = (int)TimeFormats.Double;

        public enum TimeFormats
        {
            Integer = 0,
            String = 1,
            Double = 2
        }

        public static PhpValue date_sunrise(Context ctx, int timestamp)
        {
            return GetSunTime(ctx, timestamp, TimeFormats.String, Double.NaN, Double.NaN, Double.NaN, Double.NaN, true);
        }

        public static PhpValue date_sunrise(Context ctx, int timestamp, TimeFormats format)
        {
            return GetSunTime(ctx, timestamp, format, Double.NaN, Double.NaN, Double.NaN, Double.NaN, true);
        }

        public static PhpValue date_sunrise(Context ctx, int timestamp, TimeFormats format, double latitude)
        {
            return GetSunTime(ctx, timestamp, format, latitude, Double.NaN, Double.NaN, Double.NaN, true);
        }

        public static PhpValue date_sunrise(Context ctx, int timestamp, TimeFormats format, double latitude, double longitude)
        {
            return GetSunTime(ctx, timestamp, format, latitude, longitude, Double.NaN, Double.NaN, true);
        }

        public static PhpValue date_sunrise(Context ctx, int timestamp, TimeFormats format, double latitude, double longitude, double zenith)
        {
            return GetSunTime(ctx, timestamp, format, latitude, longitude, zenith, Double.NaN, true);
        }

        public static PhpValue date_sunrise(Context ctx, int timestamp, TimeFormats format, double latitude, double longitude, double zenith, double offset)
        {
            return GetSunTime(ctx, timestamp, format, latitude, longitude, zenith, offset, true);
        }

        public static PhpValue date_sunset(Context ctx, int timestamp)
        {
            return GetSunTime(ctx, timestamp, TimeFormats.String, Double.NaN, Double.NaN, Double.NaN, Double.NaN, false);
        }

        public static PhpValue date_sunset(Context ctx, int timestamp, TimeFormats format)
        {
            return GetSunTime(ctx, timestamp, format, Double.NaN, Double.NaN, Double.NaN, Double.NaN, false);
        }

        public static PhpValue date_sunset(Context ctx, int timestamp, TimeFormats format, double latitude)
        {
            return GetSunTime(ctx, timestamp, format, latitude, Double.NaN, Double.NaN, Double.NaN, false);
        }

        public static PhpValue date_sunset(Context ctx, int timestamp, TimeFormats format, double latitude, double longitude)
        {
            return GetSunTime(ctx, timestamp, format, latitude, longitude, Double.NaN, Double.NaN, false);
        }

        public static PhpValue date_sunset(Context ctx, int timestamp, TimeFormats format, double latitude, double longitude, double zenith)
        {
            return GetSunTime(ctx, timestamp, format, latitude, longitude, zenith, Double.NaN, false);
        }

        public static PhpValue date_sunset(Context ctx, int timestamp, TimeFormats format, double latitude, double longitude, double zenith, double offset)
        {
            return GetSunTime(ctx, timestamp, format, latitude, longitude, zenith, offset, false);
        }

        static PhpValue GetSunTime(Context ctx, int timestamp, TimeFormats format, double latitude, double longitude, double zenith, double offset, bool getSunrise)
        {
            var zone = PhpTimeZone.GetCurrentTimeZone(ctx);
            var utc = DateTimeUtils.UnixTimeStampToUtc(timestamp);
            var local = TimeZoneInfo.ConvertTime(utc, zone);

            if (Double.IsNaN(latitude) || Double.IsNaN(longitude) || Double.IsNaN(zenith))
            {
                //LibraryConfiguration config = LibraryConfiguration.GetLocal(ScriptContext.CurrentContext);

                //if (Double.IsNaN(latitude))
                //    latitude = config.Date.Latitude;
                //if (Double.IsNaN(longitude))
                //    longitude = config.Date.Longitude;
                //if (Double.IsNaN(zenith))
                //    zenith = (getSunrise) ? config.Date.SunriseZenith : config.Date.SunsetZenith;
                throw new NotImplementedException();
            }

            if (Double.IsNaN(offset))
                offset = zone.GetUtcOffset(local).TotalHours;

            double result_utc = CalculateSunTime(local.DayOfYear, latitude, longitude, zenith, getSunrise);
            double result = result_utc + offset;

            switch (format)
            {
                case TimeFormats.Integer:
                    return PhpValue.Create((timestamp - (timestamp % (24 * 3600))) + (int)(3600 * result));

                case TimeFormats.String:
                    return PhpValue.Create(string.Format("{0:00}:{1:00}", (int)result, (int)(60 * (result - (double)(int)result))));

                case TimeFormats.Double:
                    return PhpValue.Create(result);

                default:
                    //PhpException.InvalidArgument("format");
                    //return PhpValue.Null;
                    throw new ArgumentException();
            }
        }

        private static double ToRadians(double degrees) { return degrees * Math.PI / 180; }
        private static double ToDegrees(double radians) { return radians * 180 / Math.PI; }

        /// <summary>
        /// Calculates sunrise or sunset. Adopted PHP implementation by Moshe Doron (mosdoron@netvision.net.il).
        /// Returns UTC time.
        /// </summary>
        private static double CalculateSunTime(int day, double latitude, double longitude, double zenith, bool getSunrise)
        {
            double lngHour, t, M, L, Lx, RA, RAx, Lquadrant, RAquadrant, sinDec, cosDec, cosH, H, T, UT, UTx;

            // convert the longitude to hour value and calculate an approximate time
            lngHour = longitude / 15;

            if (getSunrise)
                t = (double)day + ((6 - lngHour) / 24);
            else
                t = (double)day + ((18 - lngHour) / 24);

            // calculate the sun's mean anomaly:
            M = (0.9856 * t) - 3.289;

            // step 4: calculate the sun's true longitude:
            L = M + (1.916 * Math.Sin(ToRadians(M))) + (0.020 * Math.Sin(ToRadians(2 * M))) + 282.634;

            while (L < 0)
            {
                Lx = L + 360;
                Debug.Assert(Lx != L);
                L = Lx;
            }

            while (L >= 360)
            {
                Lx = L - 360;
                Debug.Assert(Lx != L);
                L = Lx;
            }

            // calculate the sun's right ascension:
            RA = ToDegrees(Math.Atan(0.91764 * Math.Tan(ToRadians(L))));

            while (RA < 0)
            {
                RAx = RA + 360;
                Debug.Assert(RAx != RA);
                RA = RAx;
            }

            while (RA >= 360)
            {
                RAx = RA - 360;
                Debug.Assert(RAx != RA);
                RA = RAx;
            }

            // right ascension value needs to be in the same quadrant as L:
            Lquadrant = Math.Floor(L / 90) * 90;
            RAquadrant = Math.Floor(RA / 90) * 90;
            RA = RA + (Lquadrant - RAquadrant);

            // right ascension value needs to be converted into hours:
            RA /= 15;

            // calculate the sun's declination:
            sinDec = 0.39782 * Math.Sin(ToRadians(L));
            cosDec = Math.Cos(Math.Asin(sinDec));

            // calculate the sun's local hour angle:
            cosH = (Math.Cos(ToRadians(zenith)) - (sinDec * Math.Sin(ToRadians(latitude)))) / (cosDec * Math.Cos(ToRadians(latitude)));

            // finish calculating H and convert into hours:
            if (getSunrise)
                H = 360 - ToDegrees(Math.Acos(cosH));
            else
                H = ToDegrees(Math.Acos(cosH));

            H = H / 15;

            // calculate local mean time:
            T = H + RA - (0.06571 * t) - 6.622;

            // convert to UTC:
            UT = T - lngHour;

            while (UT < 0)
            {
                UTx = UT + 24;
                Debug.Assert(UTx != UT);
                UT = UTx;
            }

            while (UT >= 24)
            {
                UTx = UT - 24;
                Debug.Assert(UTx != UT);
                UT = UTx;
            }

            return UT;
        }

        #endregion
    }
}
