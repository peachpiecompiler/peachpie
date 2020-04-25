
/*

 Copyright (c) 2005-2006 Tomas Matousek. Based on PHP5 implementation by Derick Rethans <derick@derickrethans.nl>. 

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

// TODO: Prior to PHP 5.3.0, 24:00 was not a valid format and strtotime() returned FALSE. 
// TODO: strtotime("0000-00-00 00:00:00 +0000") should return FALSE

using System;
using System.IO;
using System.Diagnostics;
using Pchp.Core;
using Pchp.Library.Resources;
using System.Collections.Generic;
using System.Globalization;

namespace Pchp.Library.DateTime
{
    #region Enums

    internal enum Tokens
    {
        EOF,
        ERROR,
        XMLRPC_SOAP,
        TIME12,
        TIME24,
        GNU_NOCOLON,
        GNU_NOCOLON_TZ,
        ISO_NOCOLON,
        AMERICAN,
        ISO_DATE,
        DATE_FULL,
        DATE_TEXT,
        DATE_NOCOLON,
        PG_YEARDAY,
        PG_TEXT,
        PG_REVERSE,
        CLF,
        DATE_NO_DAY,
        SHORTDATE_WITH_TIME,
        DATE_FULL_POINTED,
        TIME24_WITH_ZONE,
        ISO_WEEK,
        TIMEZONE,
        AGO,
        RELATIVE,
        WEEKDAY
    }

    #endregion

    internal sealed class DateInfo
    {
        #region Fields

        public struct Relative
        {
            /// <summary>
            /// Number of years/months.
            /// </summary>
            public int y, m;

            /// <summary>
            /// Number of days.
            /// </summary>
            public long d;

            /// <summary>
            /// Number of hours/minutes/seconds.
            /// </summary>
            public long h, i, s;

            /// <summary>
            /// Weekday (e.g. "next monday").
            /// </summary>
            public int weekday;

            public int weekday_behavior;
        }

        /// <summary>
        /// Date and time specified relatively.
        /// </summary>
        public Relative relative;

        /// <summary>
        /// Absolute year/month/day.
        /// </summary>
        public int y = -1, m = -1, d = -1;

        /// <summary>
        /// Absolute hour/minute/second.
        /// </summary>
        public int h = -1, i = -1, s = -1;

        /// <summary>
        /// Fraction of second.
        /// </summary>
        public double f = -1;

        /// <summary>
        /// Number of time/date/zone/relative time/weekday components specified.
        /// </summary>
        public int have_time, have_date, have_zone, have_relative, have_weekday_relative;

        /// <summary>
        /// GMT offset in minutes.
        /// </summary>
        public int z = 0;

        /// <summary>
        /// Optional.
        /// The time zone abbreviation if specified.
        /// Upper case.
        /// </summary>
        public string z_abbr;

        #endregion

        #region Parse

        public static System.DateTime Parse(Context ctx, string/*!*/ str, System.DateTime utcStart, TimeZoneInfo timeZone, out string error)
        {
            Debug.Assert(str != null);

            var scanner = new Scanner(new StringReader(str.ToLowerInvariant()));
            while (true)
            {
                var token = scanner.GetNextToken();
                if (token == Tokens.ERROR || scanner.Errors > 0)
                {
                    error = string.Format(LibResources.parse_error, scanner.Position, str.Substring(scanner.Position));
                    return System.DateTime.MinValue;
                }

                if (token == Tokens.EOF)
                {
                    error = null;
                    return scanner.Time.GetDateTime(ctx, utcStart, timeZone);
                }
            }
        }

        #endregion

        #region

        internal const int TIMELIB_ZONETYPE_OFFSET = 1;
        internal const int TIMELIB_ZONETYPE_ABBR = 2;
        internal const int TIMELIB_ZONETYPE_ID = 3;

        /// <summary>
        /// 1: offset in form of +00:00
        /// 2: timezone abbreviation
        /// 3: TimeZone object
        /// </summary>
        public static int GetTimeLibZoneType(TimeZoneInfo zone)
        {
            if (zone == null)
            {
                return TIMELIB_ZONETYPE_OFFSET;
            }

            var tz = zone.Id;

            if (tz.Length != 0 && (tz[0] == '+' || tz[0] == '-'))
            {
                // 1: offset
                return DateInfo.TIMELIB_ZONETYPE_OFFSET;
            }
            else if (DateInfo.GetZoneOffsetFromAbbr(tz, out _))
            {
                // 2: abbreviation
                return DateInfo.TIMELIB_ZONETYPE_ABBR;
            }
            else
            {
                // 3: A timezone identifier
                return DateInfo.TIMELIB_ZONETYPE_ID;
            }
        }

        string GetTimeZoneString()
        {
            if (have_zone > 0)
            {
                if (z_abbr != null)
                {
                    Debug.Assert(z_abbr.ToUpperInvariant() == z_abbr);  // is upper case
                    return z_abbr;
                }
                else
                {
                    // [+-]00:00
                    return
                        (z < 0 ? "-" : "+") +
                        (z / 60).ToString("D2") +
                        ":" +
                        (z % 60).ToString("D2");
                }
            }

            //
            throw new InvalidOperationException();
        }

        /// <summary>Gets the time zone object describing the time zone - <see cref="z"/>.
        /// Its ID is either in the offset format or the abbreviation when specified.</summary>
        /// <exception cref="InvalidOperationException">The time zone is not specified.</exception>
        public TimeZoneInfo ResolveTimeZone()
        {
            if (have_zone > 0)
            {
                //// fast special cases:
                //if (z == 0)
                //{
                //    if (z_abbr == null || string.Equals(z_abbr, "UTC", StringComparison.OrdinalIgnoreCase))
                //    {
                //        return TimeZoneInfo.Utc;
                //    }
                //}

                // create time zone object wth our offset:
                var name = GetTimeZoneString();
                return TimeZoneInfo.CreateCustomTimeZone(name, TimeSpan.FromMinutes(z), null, null);
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Gets time zone object describing the parsed time zone, or a default time zone.
        /// </summary>
        public TimeZoneInfo ResolveTimeZone(Context ctx, TimeZoneInfo @default = null)
        {
            if (have_zone > 0)
            {
                // note: provided default time zone is ignored

                var name = GetTimeZoneString();
                return TimeZoneInfo.CreateCustomTimeZone(name, TimeSpan.FromMinutes(z), name, name);
            }
            else
            {
                return @default // provided time zone
                    ?? PhpTimeZone.GetCurrentTimeZone(ctx)    // default time zone
                    ?? throw new InvalidOperationException(); // does not happen
            }
        }

        public System.DateTime GetDateTime(Context ctx, System.DateTime utcStart, TimeZoneInfo timeZone = null)
        {
            var zone = timeZone ?? PhpTimeZone.GetCurrentTimeZone(ctx);
            var start = TimeZoneInfo.ConvertTime(utcStart, TimeZoneInfo.Utc, zone);// zone.ToLocalTime(utcStart);

            // following operates on local time defined by the parsed info or by the current time zone //

            if (have_date > 0 && have_time == 0)
            {
                h = 0;
                i = 0;
                s = 0;
                f = 0;
            }
            else
            {
                if (h == -1) h = start.Hour;
                if (i == -1) i = start.Minute;
                if (s == -1) s = start.Second;

                if (f < 0) f = start.Millisecond * 0.001;
                else if (f > 0)
                {
                    var relative_sec = Math.Truncate(f);
                    f -= relative_sec;
                    relative.s += (int)relative_sec;
                }
            }

            if (y == -1) y = start.Year;
            if (m == -1) m = start.Month;
            else if (m == 0) { m = 1; --relative.m; }
            if (d == -1) d = start.Day;
            else if (d == 0) { d = 1; --relative.d; }

            CheckOverflows(y, m, ref d, ref h, out var days_overflow);

            var result = new System.DateTime(y, m, d, h, i, s, (int)(f * 1000), DateTimeKind.Unspecified);

            // relative years and months:
            result = result.AddMonths(relative.y * 12 + relative.m);

            // check relative ranges
            if (relative.s >= long.MaxValue / TimeSpan.TicksPerSecond) return System.DateTime.MaxValue;
            if (relative.s <= long.MinValue / TimeSpan.TicksPerSecond) return System.DateTime.MinValue;

            // relative seconds
            long relative_ticks =
                relative.s * TimeSpan.TicksPerSecond +
                relative.i * TimeSpan.TicksPerMinute +
                relative.h * TimeSpan.TicksPerHour +
                (relative.d + days_overflow) * TimeSpan.TicksPerDay;

            try
            {
                result = result.AddTicks(relative_ticks);
            }
            catch (ArgumentOutOfRangeException)
            {
                if (relative_ticks > 0) return System.DateTime.MaxValue;
                if (relative_ticks < 0) return System.DateTime.MinValue;
            }

            // adds relative weekday:
            if (have_weekday_relative > 0)
            {
                int dow = (int)result.DayOfWeek;
                int difference = relative.weekday - dow;

                if ((relative.d < 0 && difference < 0) || (relative.d >= 0 && difference <= -relative.weekday_behavior))
                    difference += 7;

                if (relative.weekday >= 0)
                    result = result.AddDays(difference);
                else
                    result = result.AddDays(dow - relative.weekday - 7);
            }

            // convert to UTC:
            if (have_zone > 0)
            {
                result = result.AddMinutes(-z);
                result = TimeZoneInfo.ConvertTimeToUtc(result, TimeZoneInfo.Utc); // Just mark that it's in UTC already
            }
            else
            {
                if (zone.IsInvalidTime(result))
                {
                    // We ended up in an invalid time. This time was skipped because of day-light saving change.
                    // Figure out the direction we were moving, and step in the direction until the next valid time.
                    int secondsStep = ((result - utcStart).Ticks >= 0) ? 1 : -1;
                    do
                    {
                        result = result.AddSeconds(secondsStep);
                    }
                    while (zone.IsInvalidTime(result));
                }

                result = TimeZoneInfo.ConvertTime(result, zone, TimeZoneInfo.Utc);// zone.ToUniversalTime(result);
            }

            Debug.Assert(result.Kind == DateTimeKind.Utc);

            //
            return result;
        }

        //private long GetUnixTimeStamp(Context ctx, System.DateTime utcStart, out string error)
        //{
        //    var result = GetDateTime(ctx, utcStart);

        //    error = null;
        //    return DateTimeUtils.UtcToUnixTimeStamp(result);
        //}

        #endregion

        #region Helper Methods

        public void HAVE_TIME()
        {
            have_time = 1;
            h = 0;
            i = 0;
            s = 0;
            f = 0;
        }

        public void UNHAVE_TIME()
        {
            have_time = 0;
            h = 0;
            i = 0;
            s = 0;
            f = 0;
        }

        public void HAVE_DATE()
        {
            have_date = 1;
        }

        public void UNHAVE_DATE()
        {
            have_date = 0;
            d = 0;
            m = 0;
            y = 0;
        }

        public void HAVE_RELATIVE()
        {
            have_relative = 1;
            relative.weekday_behavior = 0;
        }

        public void HAVE_WEEKDAY_RELATIVE()
        {
            have_weekday_relative = 1;
        }

        public void HAVE_TZ()
        {
            have_zone = 1;
        }

        public static int ProcessYear(int year)
        {
            if (year == -1) return -1;

            if (year < 100)
                return (year < 70) ? year + 2000 : year + 1900;

            return year;
        }

        internal static int DaysInMonthFixed(int year, int month)
        {
            // NOTE: DateTime only works for years in [1..9999]

            if (year <= 0) year = 1;    // this is correct, since leap years only exist **after** the year 0 (there is no year 0)

            return System.DateTime.DaysInMonth(year, month);
        }

        /// <summary>
        /// Checks how many days given year/month/day/hour overflows (as it is possible in PHP format).
        /// </summary>
        /// <param name="y">parsed year</param>
        /// <param name="m">parsed month</param>
        /// <param name="d">parsed day</param>
        /// <param name="h">parsed hour (24 is problem)</param>
        /// <param name="days_overflow">resulting amount of overflowing days (will be added to the resulting DateTime).</param>
        private static void CheckOverflows(int y, int m, ref int d, ref int h, out int days_overflow)
        {
            days_overflow = 0;

            int daysinmonth_overflow = d - DateInfo.DaysInMonthFixed(y, m);
            if (daysinmonth_overflow > 0)
            {
                d -= daysinmonth_overflow;
                days_overflow += daysinmonth_overflow;
            }
            else if (d == 0)
            {
                d = 1;
                --days_overflow;
            }

            if (h == 24)
            {
                h = 0;
                ++days_overflow;
            }
        }

        public static int ParseSignedInt(string str, ref int pos, int maxDigits)
        {
            var value = ParseSignedLong(str, ref pos, maxDigits);

            if (value <= int.MinValue) return int.MinValue;
            if (value >= int.MaxValue) return int.MaxValue;

            return (int)value;
        }

        public static int ParseUnsignedInt(string str, ref int pos, int maxDigits)
        {
            var value = ParseUnsignedLong(str, ref pos, maxDigits);

            if (value >= int.MaxValue) return int.MaxValue;

            return (int)value;
        }

        public static int ParseUnsignedInt(string str, ref int pos, int maxDigits, out int len)
        {
            var value = ParseUnsignedLong(str, ref pos, maxDigits, out len);

            if (value >= int.MaxValue) return int.MaxValue;

            return (int)value;
        }

        /// <summary>
        /// Parse integer with possible sign of a specified maximal number of digits.
        /// </summary>
        public static long ParseSignedLong(string str, ref int pos, int maxDigits)
        {
            // skip non-digit, non-sign chars:
            while (pos < str.Length)
            {
                var ch = str[pos];

                if (char.IsDigit(ch) || ch == '+' || ch == '-')
                {
                    break;
                }

                pos++;
            }

            if (pos == str.Length)
            {
                return -1;
            }

            bool sign = false;

            // set sign:
            if (str[pos] == '+')
            {
                pos++;
            }
            else if (str[pos] == '-')
            {
                sign = true;
                pos++;
            }

            var value = ParseUnsignedLong(str, ref pos, maxDigits);

            if (sign)
            {
                // value is unsigned, no overflow check
                value = -value;
            }

            return value;
        }

        /// <summary>
        /// Parse unsigned integer of a specified maximal length.
        /// </summary>
        public static long ParseUnsignedLong(string str, ref int pos, int maxDigits)          // PHP: timelib_get_nr
        {
            return ParseUnsignedLong(str, ref pos, maxDigits, out _);
        }

        /// <summary>
        /// Parse unsigned integer of a specified maximal length.
        /// Returns parsed number, or <c>-1</c> if there are no digits, or <see cref="long.MaxValue"/> if value overflows.
        /// </summary>
        public static long ParseUnsignedLong(string str, ref int pos, int maxDigits, out int len)          // PHP: timelib_get_nr
        {
            len = 0;

            // skips non-digits:
            while (pos < str.Length && !char.IsDigit(str, pos))
            {
                pos++;
            }

            if (pos == str.Length)
            {
                return -1;
            }

            long result = 0;
            while (pos < str.Length && len < maxDigits)
            {
                var ch = str[pos];
                if (ch < '0' || ch > '9')
                {
                    break;
                }

                var num = ch - '0';

                if ((result < long.MaxValue / 10) ||
                    (result == long.MaxValue / 10 && num <= long.MaxValue % 10))
                {
                    result = (result * 10) + num;
                }
                else
                {
                    result = long.MaxValue;
                }

                //
                pos++;
                len++;
            }

            return result;
        }

        /// <summary>
        /// Parses real fraction ".[0-9]{1,maxDigits}". 
        /// </summary>
        public static double ParseFraction(string str, ref int pos, int maxDigits)         // PHP: timelib_get_frac_nr
        {
            Debug.Assert(pos < str.Length && str[pos] == '.');

            int begin = pos;

            // dot:
            pos++;
            int len = 1;

            // get substring of digits:
            while (pos < str.Length && Char.IsDigit(str, pos) && len < maxDigits)
            {
                pos++;
                len++;
            }

            string number = str.Substring(begin, len);
            return Double.Parse(number, System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        /// <summary>
        /// Parses meridian "[ap][.]?m[.]?" and adjusts hours accordingly.
        /// </summary>
        public bool SetMeridian(string str, ref int pos)
        {
            while (pos < str.Length && str[pos] != 'a' && str[pos] != 'p')
            {
                pos++;
            }
            if (pos == str.Length) return false;

            if (str[pos] == 'a')
            {
                if (h == 12) h = 0;
            }
            else if (h != 12)
            {
                h += 12;
            }

            pos++;

            // dot after "a"/"p", move after "m":
            pos += (str[pos] == '.') ? 2 : 1;

            // dot after "m":
            if (pos < str.Length && str[pos] == '.') pos++;

            return true;
        }

        /// <summary>
        /// Moves string position index behind "nd", "rd", "st", "th" if applicable.
        /// </summary>
        public static void SkipDaySuffix(string str, ref int pos)                   // PHP: timelib_skip_day_suffix
        {
            if (pos + 1 >= str.Length || Char.IsWhiteSpace(str[pos]))
                return;

            if
            (
              str[pos] == 'n' && str[pos + 1] == 'd' ||
              str[pos] == 'r' && str[pos + 1] == 'd' ||
              str[pos] == 's' && str[pos + 1] == 't' ||
              str[pos] == 't' && str[pos + 1] == 'h'
            )
            {
                pos += 2;
            }
        }

        /// <summary>
        /// Parses month string and returns the month number 1..12, zero on error.
        /// </summary>
        public static int ParseMonth(string str, ref int pos)
        {
            while (pos < str.Length && (str[pos] == ' ' || str[pos] == '-' || str[pos] == '.' || str[pos] == '/'))
            {
                pos++;
            }

            int begin = pos;
            while (pos < str.Length && Char.IsLetter(str, pos))
            {
                pos++;
            }

            switch (str.Substring(begin, pos - begin).ToLowerInvariant())
            {
                case "jan": return 1;
                case "feb": return 2;
                case "mar": return 3;
                case "apr": return 4;
                case "may": return 5;
                case "jun": return 6;
                case "jul": return 7;
                case "aug": return 8;
                case "sep": return 9;
                case "sept": return 9;
                case "oct": return 10;
                case "nov": return 11;
                case "dec": return 12;

                case "i": return 1;
                case "ii": return 2;
                case "iii": return 3;
                case "iv": return 4;
                case "v": return 5;
                case "vi": return 6;
                case "vii": return 7;
                case "viii": return 8;
                case "ix": return 9;
                case "x": return 10;
                case "xi": return 11;
                case "xii": return 12;

                case "january": return 1;
                case "february": return 2;
                case "march": return 3;
                case "april": return 4;
                case "june": return 6;
                case "july": return 7;
                case "august": return 8;
                case "september": return 9;
                case "october": return 10;
                case "november": return 11;
                case "december": return 12;
            }
            return 0;
        }

        /// <summary>
        /// Parses text defining ordinal number.
        /// </summary>
        public static int ParseRelativeText(string str, ref int pos, out int behavior)
        {
            while (pos < str.Length && (str[pos] == ' ' || str[pos] == '-' || str[pos] == '/'))
            {
                pos++;
            }

            int begin = pos;
            while (pos < str.Length && Char.IsLetter(str, pos))
            {
                pos++;
            }

            behavior = 0;
            switch (str.Substring(begin, pos - begin))
            {
                case "last": return -1;
                case "previous": return -1;
                case "this": behavior = 1; return 0;
                case "first": return 1;
                case "next": return 1;
                case "second": return 2;
                case "third": return 3;
                case "fourth": return 4;
                case "fifth": return 5;
                case "sixth": return 6;
                case "seventh": return 7;
                case "eight": return 8;
                case "ninth": return 9;
                case "tenth": return 10;
                case "eleventh": return 11;
                case "twelfth": return 12;
            }

            return 0;
        }

        static void SkipSpaces(string str, ref int pos) // timelib_eat_spaces
        {
            while (pos < str.Length && char.IsWhiteSpace(str, pos))
            {
                pos++;
            }
        }

        /// <summary>
        /// Reads characters up to the first space.
        /// </summary>
        public static string ReadToSpace(string str, ref int pos)
        {
            int begin = pos;
            while (pos < str.Length && str[pos] != ' ')
                pos++;

            return str.Substring(begin, pos - begin);
        }

        public static ReadOnlySpan<char> ReadToDelimiter(string str, ref int pos)
        {
            int start = pos;
            int end = -1;
            while (pos < str.Length && end < 0)
            {
                switch (str[pos])
                {
                    case ' ':
                    case '\t':
                    case ',':
                    case ';':
                    case ':':
                    case '/':
                    case '.':
                    case '-':
                    case '(':
                    case ')':
                        end = pos;
                        continue;
                }

                pos++;
            }

            return end < 0 ? str.AsSpan(start) : str.AsSpan(start, end - start);
        }

        /// <summary>
        /// Sets relative time and date information according to the parsed text.
        /// </summary>
        public void SetRelative(string str, long amount, int behavior)
        {
            switch (str.ToLowerInvariant())
            {
                case "sec":
                case "secs":
                case "second":
                case "seconds":
                    relative.s += amount;
                    return;

                case "min":
                case "mins":
                case "minute":
                case "minutes":
                    relative.i += amount;
                    return;

                case "hour":
                case "hours":
                    relative.h += amount;
                    return;

                case "day":
                case "days":
                    relative.d += amount;
                    return;

                case "month":
                case "months":
                    relative.m += (int)amount;
                    return;

                case "week":
                case "weeks":
                    relative.d += 7 * amount;
                    return;

                case "fortnight":
                case "fortnights":
                case "forthnight":
                case "forthnights":
                    relative.d += 14 * amount;
                    return;

                case "year":
                case "years":
                    relative.y += (int)amount;
                    return;
            }

            if (SetWeekDay(str))
            {
                relative.d += (amount > 0 ? amount - 1 : amount) * 7;

                // TIMELIB_HAVE_WEEKDAY_RELATIVE
                HAVE_WEEKDAY_RELATIVE();
                relative.weekday_behavior = behavior;

                // TIMELIB_UNHAVE_TIME 
                have_time = 0;
                h = 0;
                i = 0;
                s = 0;
                f = 0;
            }
        }

        /// <summary>Map of a week day name into it's ordinal. Sunday is zero.</summary>
        readonly static Dictionary<string, int> s_weedays = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "mon", 1 },
            { "monday", 1 },

            { "tue", 2 },
            { "tuesday" , 2 },

            { "wed", 3 },
            { "wednesday" , 3 },

            { "thu", 4 },
            { "thursday" , 4 },

            { "fri", 5 },
            { "friday" , 5 },

            { "sat", 6 },
            { "saturday" , 6 },

            { "sun", 0 },
            { "sunday" , 0 },
        };

        /// <summary>
        /// Sets relative week day according to a specified text.
        /// </summary>
        public bool SetWeekDay(string str)
        {
            if (s_weedays.TryGetValue(str, out var weekday))
            {
                relative.weekday = weekday;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Converts week number to day number.
        /// </summary>
        public static int WeekToDay(int year, int week, int day)
        {
            int dow = (int)new System.DateTime(year, 1, 1).DayOfWeek;

            // the offset for day 1 of week 1:
            int offset = 0 - (dow > 4 ? dow - 7 : dow);

            // add weeks and days:
            return offset + ((week - 1) * 7) + day;
        }

        /// <summary>
        /// Parses and sets time zone information.
        /// </summary>
        public bool SetTimeZone(string str, ref int pos)                               // PHP: timelib_get_zone
        {
            if (have_zone > 0) return false;

            // skip leading whitespace and opening parenthesis:
            while (pos < str.Length && (str[pos] == ' ' || str[pos] == '('))
                pos++;

            if (pos >= str.Length)
            {
                return false;
            }

            if (str[pos] == '+')
            {
                pos++;
                if (TryParseTimeZoneOffset(str, pos, out z))
                {
                    HAVE_TZ();
                }
                pos = str.Length;
            }
            else if (str[pos] == '-')
            {
                pos++;
                if (TryParseTimeZoneOffset(str, pos, out z))
                {
                    z = -z;
                    HAVE_TZ();
                }
                pos = str.Length;
            }
            else
            {
                var abbr = str.Substring(pos);
                if (GetZoneOffsetFromAbbr(abbr, out z))
                {
                    z_abbr = abbr.ToUpperInvariant();
                    HAVE_TZ();
                }

                pos = str.Length;
            }

            // skip trailing whitespace and closing parenthesis:
            while (pos < str.Length && str[pos] == ')')
                pos++;

            return have_zone > 0;
        }

        /// <summary>
        /// Parses numeric timezones. Resolves offset in minutes.
        /// </summary>
        internal static bool TryParseTimeZoneOffset(string str, int pos, out int minutes)                               // PHP: timelib_parse_tz_cor
        {
            int length = str.Length - pos;
            int value;

            switch (length)
            {
                case 1: // 0
                case 2: // 00
                    if (int.TryParse(str.Substring(pos, length), out value))
                    {
                        minutes = value * 60;
                        return true;
                    }
                    break;

                case 3: // 000, 0:0
                case 4: // 0000, 0:00, 00:0

                    // TODO: "TryParse":

                    if (str[pos + 1] == ':')       // 0:0, 0:00
                    {
                        minutes = (str[pos] - '0') * 60 + int.Parse(str.Substring(pos + 2, length - 2));
                    }
                    else if (str[pos + 2] == ':')  // 00:0
                    {
                        minutes = ((str[pos] - '0') * 10 + (str[pos + 1] - '0')) * 60 + (str[pos + 3] - '0');
                    }
                    else                          // 000, 0000
                    {
                        minutes = int.Parse(str.Substring(pos, length));
                        minutes = (minutes / 100) * 60 + minutes % 100;
                    }
                    return true;

                case 5: // 00:00

                    if (int.TryParse(str.Substring(pos, 2), NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat, out value))
                    {
                        minutes = value * 60;

                        if (int.TryParse(str.Substring(pos + 3, 2), NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat, out value))
                        {
                            minutes += value;
                            return true;
                        }
                    }
                    break;
            }

            //
            minutes = 0;
            return false;
        }

        /// <summary>
        /// Sets zone offset by zone abbreviation.
        /// </summary>
        internal static bool GetZoneOffsetFromAbbr(string/*!*/ abbreviation, out int z)                             // PHP: timelib_lookup_zone, zone_search
        {
            // source http://www.worldtimezone.com/wtz-names/timezonenames.html
            switch (abbreviation.ToLowerInvariant())
            {
                case "z":
                case "utc":
                case "gmt":
                case "wet":
                case "wez":
                case "gt":
                case "wz":
                case "hg":
                case "gz":
                case "cut":
                case "tuc": z = 0; break;

                case "a":
                case "cet":
                case "met":
                case "mez":
                case "wat":
                case "fwt":
                case "hfh":
                case "wut":
                case "set":
                case "swt":
                case "nor":
                case "hoe":
                case "dnt": z = 60; break;

                case "b":
                case "eet":
                case "oez":
                case "usz1":
                case "hfe": z = +2 * 60; break;

                case "c":
                case "msk":
                case "usz2":
                case "r2t":
                case "tst":
                case "eat":
                case "bat":
                case "bt": z = +3 * 60; break;
                case "it":
                case "irt": z = +3 * 60 + 30; break;

                case "d":
                case "usz3":
                case "ret":
                case "mut":
                case "sct":
                case "azt":
                case "amst": z = +4 * 60; break;
                case "aft": z = +4 * 60 + 30; break;

                case "e":
                case "usz4":
                case "iot":
                case "tft":
                case "mvt":
                case "tmt":
                case "kgt":
                case "tjt":
                case "pkt":
                case "uzt":
                case "yekt": z = +5 * 60; break;
                case "npt": z = +5 * 60 + 45; break;

                case "f":
                case "usz5":
                case "almt":
                case "bdt":
                case "btt":
                case "lkt":
                case "novt":
                case "omst":
                case "mawt": z = +6 * 60; break;
                case "mmt":
                case "nst":
                case "nsut": z = +6 * 60 + 30; break;

                case "g":
                case "usz6":
                case "jt":
                case "javt":
                case "tha":
                case "ict":
                case "cxt":
                case "krat":
                case "davt": z = +7 * 60; break;

                case "h":
                case "awst":
                case "usz7":
                case "bnt":
                case "hkt":
                case "myt":
                case "pht":
                case "bort":
                case "ulat": z = +8 * 60; break;

                case "i":
                case "usz8":
                case "jst":
                case "kst":
                case "pwt":
                case "jayt":
                case "yakt": z = +9 * 60; break;
                case "acst":
                case "cast": z = +9 * 60 + 30; break;

                case "k":
                case "aest":
                case "usz9":
                case "yapt":
                case "mpt":
                case "pgt":
                case "trut":
                case "vlat":
                case "ddut": z = +10 * 60; break;
                case "lhst": z = +10 * 60 + 30; break;

                case "l":
                case "uz10":
                case "vut":
                case "sbt":
                case "nct":
                case "pont":
                case "magt": z = +11 * 60; break;
                case "nft": z = +11 * 60 + 30; break;

                case "m":
                case "uz11":
                case "nzt":
                case "fjt":
                case "tvt":
                case "kost":
                case "mht":
                case "nrt":
                case "gilt":
                case "wakt":
                case "wft":
                case "pett":
                case "idle": z = +12 * 60; break;
                case "chast": z = +12 * 60 + 45; break;
                case "phot":
                case "tot": z = +13 * 60; break;
                case "lint": z = +14 * 60; break;

                case "y":
                case "idlw": z = -12 * 60; break;

                case "x":
                case "best":
                case "nut": z = -11 * 60; break;

                case "w":
                case "hst":
                case "hast":
                case "tkt":
                case "ckt":
                case "that":
                case "ahst": z = -10 * 60; break;

                case "v":
                case "akst":
                case "yst":
                case "gamt": z = -9 * 60; break;
                case "mart": z = -9 * 60 - 30; break;

                case "u":
                case "pst":
                case "hnp": z = -8 * 60; break;

                case "t":
                case "mst": z = -7 * 60; break;

                case "s":
                case "cst":
                case "mex":
                case "east":
                case "galt": z = -6 * 60; break;

                case "r":
                case "est":
                case "cot":
                case "pet":
                case "ect": z = -5 * 60; break;

                case "q":
                case "clt":
                case "bot":
                case "gyt":
                case "vet":
                case "pyt":
                case "fkt": z = -4 * 60; break;


                case "p":
                case "bst":
                case "bzt2":
                case "art":
                case "uyt":
                case "srt":
                case "gft":
                case "pmt":
                case "utz":
                case "wgt": z = -3 * 60; break;

                case "o":
                case "fst":
                case "vtz": z = -2 * 60; break;

                case "n":
                case "at":
                case "azot":
                case "cvt":
                case "egt": z = -1 * 60; break;

                case "acdt": z = 60 * 10 + 30; break;
                case "adt": z = -60 * 3; break;        // Atlantic Daylight Time
                case "aedt": z = 60 * 11; break;
                case "akdt": z = -60 * 8; break;
                case "ast": z = 60 * 3; break;        // Arabia Standard Time
                case "cadt": z = 60 * 10 + 30; break;
                case "cat": z = -60 * 10; break;
                case "cct": z = 60 * 8; break;
                case "cdt": z = -60 * 5; break;       // Central Daylight Time (USA)
                case "cedt": z = 60 * 2; break;
                case "cest": z = 60 * 2; break;
                case "eadt": z = 60 * 11; break;
                case "edt": z = -60 * 4; break;
                case "eedt": z = 60 * 3; break;
                case "eest": z = 60 * 3; break;
                case "gst": z = 60 * 4; break;        // Gulf Standard Time
                case "haa": z = -60 * 3; break;
                case "hac": z = -60 * 5; break;
                case "hadt": z = -60 * 9; break;
                case "hae": z = -60 * 4; break;
                case "hap": z = -60 * 7; break;
                case "har": z = -60 * 6; break;
                case "hat": z = -60 * 2 - 30; break;
                case "hay": z = -60 * 8; break;
                case "hdt": z = -60 * 9 - 30; break;
                case "hna": z = -60 * 4; break;
                case "hnc": z = -60 * 6; break;
                case "hne": z = -60 * 5; break;
                case "hnr": z = -60 * 7; break;
                case "hnt": z = -60 * 3 - 30; break;
                case "hny": z = -60 * 9; break;
                case "ist": z = 60 * 1; break;
                case "mdt": z = -60 * 6; break;
                case "mest": z = 60 * 2; break;
                case "mesz": z = 60 * 2; break;
                case "mewt": z = 60 * 1; break;
                case "ndt": z = -60 * 2 - 30; break;
                case "nzdt": z = 60 * 13; break;
                case "nzst": z = 60 * 12; break;
                case "pdt": z = -60 * 7; break;
                case "rok": z = 60 * 9; break;
                case "sst": z = -11 * 60; break; // Samoa Standard Time
                case "ut": z = 0; break;
                case "wedt": z = 60 * 1; break;
                case "west": z = 60 * 1; break;
                case "wst": z = 60 * 8; break;
                case "ydt": z = -60 * 8; break;
                case "zp4": z = 60 * 4; break;
                case "zp5": z = 60 * 5; break;
                case "zp6": z = 60 * 6; break;

                default:
                    z = default;
                    return false;
            }

            return true;
        }

        private static void AddWarning(ref DateTimeErrors errors, string message) => DateTimeErrors.AddWarning(ref errors, message);

        private static void AddError(ref DateTimeErrors errors, string message) => DateTimeErrors.AddError(ref errors, message);

        #endregion

        #region TryParseIso8601Duration

        /// <summary>
        /// Parses interval string according to ISO8601 Duration.
        /// C# implementation: <see cref="System.Xml.XmlConvert.ToTimeSpan"/>; but it does not recognize <c>'W'</c> speification.
        /// </summary>
        internal static bool TryParseIso8601Duration(string s, out DateInfo result, out bool negative)
        {
            int length;
            int value, pos, numDigits;

            int years = 0, months = 0, days = 0, hours = 0, minutes = 0, seconds = 0;
            uint nanoseconds = 0;

            negative = false;

            s = s.Trim();
            length = s.Length;

            pos = 0;
            numDigits = 0;

            if (pos >= length) goto InvalidFormat;

            if (s[pos] == '-')
            {
                pos++;
                negative = true;
            }

            if (pos >= length) goto InvalidFormat;

            if (s[pos++] != 'P') goto InvalidFormat;

            if (!Core.Convert.TryParseDigits(s, ref pos, false, out value, out numDigits)) goto Error;

            if (pos >= length) goto InvalidFormat;

            if (s[pos] == 'Y')
            {
                if (numDigits == 0) goto InvalidFormat;

                years = value;
                if (++pos == length) goto Done;

                if (!Core.Convert.TryParseDigits(s, ref pos, false, out value, out numDigits)) goto Error;

                if (pos >= length) goto InvalidFormat;
            }

            if (s[pos] == 'M')
            {
                if (numDigits == 0) goto InvalidFormat;

                months = value;
                if (++pos == length) goto Done;

                if (!Core.Convert.TryParseDigits(s, ref pos, false, out value, out numDigits)) goto Error;

                if (pos >= length) goto InvalidFormat;
            }

            if (s[pos] == 'W')
            {
                if (numDigits == 0) goto InvalidFormat;

                days = value * 7;
                if (++pos == length) goto Done;

                if (!Core.Convert.TryParseDigits(s, ref pos, false, out value, out numDigits)) goto Error;

                if (pos >= length) goto InvalidFormat;
            }

            if (s[pos] == 'D')
            {
                if (numDigits == 0) goto InvalidFormat;

                days = value;
                if (++pos == length) goto Done;

                if (!Core.Convert.TryParseDigits(s, ref pos, false, out value, out numDigits)) goto Error;

                if (pos >= length) goto InvalidFormat;
            }

            if (s[pos] == 'T')
            {
                if (numDigits != 0) goto InvalidFormat;

                pos++;
                if (!Core.Convert.TryParseDigits(s, ref pos, false, out value, out numDigits)) goto Error;

                if (pos >= length) goto InvalidFormat;

                if (s[pos] == 'H')
                {
                    if (numDigits == 0) goto InvalidFormat;

                    hours = value;
                    if (++pos == length) goto Done;

                    if (!Core.Convert.TryParseDigits(s, ref pos, false, out value, out numDigits)) goto Error;

                    if (pos >= length) goto InvalidFormat;
                }

                if (s[pos] == 'M')
                {
                    if (numDigits == 0) goto InvalidFormat;

                    minutes = value;
                    if (++pos == length) goto Done;

                    if (!Core.Convert.TryParseDigits(s, ref pos, false, out value, out numDigits)) goto Error;

                    if (pos >= length) goto InvalidFormat;
                }

                if (s[pos] == '.')
                {
                    pos++;

                    seconds = value;

                    if (!Core.Convert.TryParseDigits(s, ref pos, true, out value, out numDigits)) goto Error;

                    if (numDigits == 0)
                    { //If there are no digits after the decimal point, assume 0
                        value = 0;
                    }
                    // Normalize to nanosecond intervals
                    for (; numDigits > 9; numDigits--)
                        value /= 10;

                    for (; numDigits < 9; numDigits++)
                        value *= 10;

                    nanoseconds = (uint)value;

                    if (pos >= length) goto InvalidFormat;

                    if (s[pos] != 'S') goto InvalidFormat;
                    if (++pos == length) goto Done;
                }
                else if (s[pos] == 'S')
                {
                    if (numDigits == 0) goto InvalidFormat;

                    seconds = value;
                    if (++pos == length) goto Done;
                }
            }

            // Duration cannot end with digits
            if (numDigits != 0) goto InvalidFormat;

            // No further characters are allowed
            if (pos != length) goto InvalidFormat;

            Done:
            //// At least one part must be defined
            //if (parts == Parts.HasNone) goto InvalidFormat;

            result = new DateInfo
            {
                y = years,
                m = months,
                d = days,
                h = hours,
                i = minutes,
                s = seconds,
                f = nanoseconds / 1000000000.0,
            };

            return true;

        InvalidFormat:
        Error:

            result = default;
            negative = default;
            return false;
        }

        #endregion

        #region ParseFromFormat

        /// <summary>
        /// This code should provide results similar to <c>timelib_parse_from_format</c> function.
        /// Comments taken from their implementation in order to keep track of what it does.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="str">Time string.</param>
        /// <param name="errors">Filled with errors or <c>null</c>.</param>
        /// <returns>Parsed date information.</returns>
        public static DateInfo ParseFromFormat(string format, string str, out DateTimeErrors errors)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (str == null) throw new ArgumentNullException(nameof(str));

            errors = null;

            var time = new DateInfo();
            int len;

            int si = 0; // str index
            int fi = 0; // format index
            var allow_extra = false;

            for (; fi < format.Length && (si < str.Length || format[fi] == '!' || format[fi] == '|' || format[fi] == '+'); fi++)
            {
                switch (format[fi])
                {
                    case 'D': /* three letter day */
                    case 'l': /* full day */
                        if (time.SetWeekDay(ReadToDelimiter(str, ref si).ToString()))
                        {
                            time.HAVE_RELATIVE();
                            time.HAVE_WEEKDAY_RELATIVE();
                            time.relative.weekday_behavior = 1;
                        }
                        else
                        {
                            AddError(ref errors, DateResources.day_notfound);
                        }
                        break;
                    case 'd': /* two digit day, with leading zero */
                    case 'j': /* two digit day, without leading zero */
                        //TIMELIB_CHECK_NUMBER;
                        if ((time.d = ParseUnsignedInt(str, ref si, 2)) < 0)
                        {
                            AddError(ref errors, DateResources.two_digit_day_notfound);
                        }
                        break;
                    case 'S': /* day suffix, ignored, nor checked */
                        SkipDaySuffix(str, ref si);
                        break;
                    case 'z': /* day of year - resets month (0 based) - also initializes everything else to !TIMELIB_UNSET */
                        //TIMELIB_CHECK_NUMBER;
                        var days = ParseUnsignedInt(str, ref si, 3);
                        if (days >= 0)
                        {
                            time.m = 1;
                            time.d = days + 1;  // notmalized in GetDateTime()
                        }
                        else
                        {
                            AddError(ref errors, DateResources.three_digit_doy_notfound);
                        }
                        break;
                    case 'm': /* two digit month, with leading zero */
                    case 'n': /* two digit month, without leading zero */
                        //TIMELIB_CHECK_NUMBER;
                        if ((time.m = ParseUnsignedInt(str, ref si, 2)) < 0)
                        {
                            AddError(ref errors, DateResources.two_digit_month_notfound);
                        }
                        break;
                    case 'M': /* three letter month */
                    case 'F': /* full month */
                        if ((time.m = ParseMonth(str, ref si)) == 0)
                        {
                            AddError(ref errors, DateResources.month_notfound);
                        }
                        break;
                    case 'y': /* two digit year */
                        //TIMELIB_CHECK_NUMBER;
                        if ((time.y = ProcessYear(ParseUnsignedInt(str, ref si, 2))) < 0)
                        {
                            AddError(ref errors, DateResources.two_digit_year_notfound);
                        }
                        break;
                    case 'Y': /* four digit year */
                        //TIMELIB_CHECK_NUMBER;
                        if ((time.y = ParseUnsignedInt(str, ref si, 4)) < 0)
                        {
                            AddError(ref errors, DateResources.four_digit_year_notfound);
                        }
                        break;
                    case 'g': /* two digit hour, with leading zero */
                    case 'h': /* two digit hour, without leading zero */
                        //TIMELIB_CHECK_NUMBER;
                        if ((time.h = ParseUnsignedInt(str, ref si, 2)) < 0)
                        {
                            AddError(ref errors, DateResources.two_digit_hour_notfound);
                        }
                        else if (time.h > 12)
                        {
                            AddError(ref errors, DateResources.hour_gt_12);
                        }
                        break;
                    case 'G': /* two digit hour, with leading zero */
                    case 'H': /* two digit hour, without leading zero */
                        //TIMELIB_CHECK_NUMBER;
                        if ((time.h = ParseUnsignedInt(str, ref si, 2)) < 0)
                        {
                            AddError(ref errors, DateResources.two_digit_hour_notfound);
                        }
                        break;
                    case 'a': /* am/pm/a.m./p.m. */
                    case 'A': /* AM/PM/A.M./P.M. */
                        if (time.h < 0)
                        {
                            AddError(ref errors, DateResources.meridian_missing_hour);
                        }
                        else if (!time.SetMeridian(str, ref si))
                        {
                            AddError(ref errors, DateResources.meridian_notfound);
                        }
                        break;
                    case 'i': /* two digit minute, with leading zero */
                        //TIMELIB_CHECK_NUMBER;
                        if ((time.i = ParseUnsignedInt(str, ref si, 2, out len)) < 0 || len != 2)
                        {
                            AddError(ref errors, DateResources.two_digit_min_notfound);
                        }
                        break;
                    case 's': /* two digit second, with leading zero */
                        //TIMELIB_CHECK_NUMBER;
                        if ((time.s = ParseUnsignedInt(str, ref si, 2, out len)) < 0 || len != 2)
                        {
                            AddError(ref errors, DateResources.two_digit_sec_notfound);
                        }
                        break;
                    case 'u': /* up to six digit millisecond */
                        {
                            //TIMELIB_CHECK_NUMBER;
                            //tptr = ptr;
                            var f = ParseUnsignedInt(str, ref si, 6, out len);
                            if (f < 0 || len < 1)
                            {
                                AddError(ref errors, DateResources.six_digit_ms_notfound);
                            }
                            else
                            {
                                time.f = f / Math.Pow(10, len);
                            }
                        }
                        break;
                    case ' ': /* any sort of whitespace (' ' and \t) */
                        SkipSpaces(str, ref si);
                        break;
                    case 'U': /* epoch seconds */
                        //TIMELIB_CHECK_SIGNED_NUMBER;
                        time.HAVE_RELATIVE();
                        time.y = 1970;
                        time.m = 1;
                        time.d = 1;
                        time.h = time.i = time.s = 0;
                        time.relative.s += ParseUnsignedInt(str, ref si, 24);
                        //time.is_localtime = 1;
                        //time.zone_type = TIMELIB_ZONETYPE_OFFSET;
                        time.z = 0;
                        //time.dst = 0;
                        break;
                    case 'e': /* timezone */
                    case 'P': /* timezone */
                    case 'T': /* timezone */
                    case 'O': /* timezone */
                        {
                            //int tz_not_found;
                            //s->time->z = timelib_parse_zone((char**)&ptr, &s->time->dst, s->time, &tz_not_found, s->tzdb, tz_get_wrapper);
                            //if (tz_not_found)

                            if (!time.SetTimeZone(str, ref si))
                            {
                                AddError(ref errors, DateResources.tz_notfound);
                            }
                        }
                        break;
                    case '#': /* separation symbol */
                        switch (str[si])
                        {
                            case ';':
                            case ':':
                            case '/':
                            case '.':
                            case ',':
                            case '-':
                            case '(':
                            case ')':
                                si++;
                                break;
                            default:
                                AddError(ref errors, string.Format(DateResources.separation_notfound, "[;:/.,-]"));
                                break;
                        }
                        break;
                    case ';':
                    case ':':
                    case '/':
                    case '.':
                    case ',':
                    case '-':
                    case '(':
                    case ')':
                        if (format[fi] == str[si])
                        {
                            si++;
                        }
                        else
                        {
                            AddError(ref errors, string.Format(DateResources.separation_notfound, format[fi].ToString()));
                        }
                        break;
                    case '!': /* reset all fields to default */
                        time.y = 1970;
                        time.m = 1;
                        time.d = 1;
                        time.h = time.i = time.s = 0;
                        time.f = 0.0;
                        time.z = 0;
                        break;
                    case '|': /* reset all fields to default when not set */
                        if (time.y < 0) time.y = 1970;
                        if (time.m < 0) time.m = 1;
                        if (time.d < 0) time.d = 1;
                        if (time.h < 0) time.h = 0;
                        if (time.i < 0) time.i = 0;
                        if (time.s < 0) time.s = 0;
                        if (time.f < 0) time.f = 0.0;
                        break;

                    case '?': /* random char */
                        si++;
                        break;

                    case '\\': /* escaped char */
                        if (++fi >= format.Length)
                        {
                            AddError(ref errors, DateResources.esc_char_expected);
                            break;
                        }

                        if (format[fi] == str[si])
                        {
                            si++;
                        }
                        else
                        {
                            AddError(ref errors, DateResources.esc_char_notfound);
                        }
                        break;

                    case '*': /* random chars until a separator or number ([ \t.,:;/-0123456789]) */
                        do { si++; }
                        while (si < str.Length && " \t.,:;/-0123456789".IndexOf(str[si]) < 0);
                        break;
                    case '+':
                        allow_extra = true;
                        break;

                    default:
                        if (format[fi] != str[si]) AddError(ref errors, DateResources.separator_does_not_match);
                        si++;
                        break;
                }
            }

            //

            if (si < str.Length)
            {
                if (allow_extra) AddWarning(ref errors, DateResources.trailing_data);
                else AddError(ref errors, DateResources.trailing_data);
            }

            //

            return time;
        }

        #endregion
    }
}
