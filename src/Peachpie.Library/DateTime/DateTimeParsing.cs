
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
			/// Number of years/months/days.
			/// </summary>
			public int y, m, d;

			/// <summary>
			/// Number of hours/minutes/seconds.
			/// </summary>
			public int h, i, s;

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

		#endregion

		#region Parse

		public static long Parse(Context ctx, string/*!*/ str, System.DateTime utcStart, out string error)
		{
			Debug.Assert(str != null);

            var scanner = new Scanner(new StringReader(str.ToLowerInvariant()));
			while (true)
			{
				Tokens token = scanner.GetNextToken();
				if (token == Tokens.ERROR || scanner.Errors > 0)
				{
					error = string.Format(LibResources.parse_error, scanner.Position, str.Substring(scanner.Position));
					return 0;
				}

				if (token == Tokens.EOF)
				{
				    return scanner.Time.GetUnixTimeStamp(ctx, utcStart, out error);
				}
			}
		}

		#endregion

		#region GetUnixTimeStamp

		private long GetUnixTimeStamp(Context ctx, System.DateTime utcStart, out string error)
		{
            var zone = PhpTimeZone.GetCurrentTimeZone(ctx);
            var start = TimeZoneInfo.ConvertTime(utcStart, TimeZoneInfo.Utc, zone);// zone.ToLocalTime(utcStart);

            // following operates on local time defined by the parsed info or by the current time zone //

			if (have_date > 0 && have_time == 0)
			{
				h = 0;
				i = 0;
				s = 0;
			}
			else
			{
				if (h == -1) h = start.Hour;
                if (i == -1) i = start.Minute;
				if (s == -1) s = start.Second;
			}

			if (y == -1) y = start.Year;
			if (m == -1) m = start.Month;
            else if (m == 0) { m = 1; --relative.m;}
			if (d == -1) d = start.Day;
            else if (d == 0) { d = 1; --relative.d; }

            int days_overflow;
            CheckOverflows(y, m, ref d, ref h, out days_overflow);
            
			var result = new System.DateTime(y, m, d, h, i, s, DateTimeKind.Unspecified);

			result = result.AddDays(relative.d + days_overflow);
			result = result.AddMonths(relative.m);
			result = result.AddYears(relative.y);
			result = result.AddHours(relative.h);
			result = result.AddMinutes(relative.i);
			result = result.AddSeconds(relative.s);

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

			error = null;
			return DateTimeUtils.UtcToUnixTimeStamp(result);
		}

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

            int daysinmonth_overflow = d - System.DateTime.DaysInMonth(y, m);
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

            if (h==24)
            {
                h = 0;
                ++days_overflow;
            }            
        }

		/// <summary>
		/// Parse integer with possible sign of a specified maximal number of digits.
		/// </summary>
		public static int ParseSignedInt(string str, ref int pos, int maxDigits)
		{
			int sign = +1;

			// skip non-digit, non-sign chars:
			while (pos < str.Length && !Char.IsDigit(str, pos) && str[pos] != '+' && str[pos] != '-')
			{
				pos++;
			}
			if (pos == str.Length) return -1;

			// set sign:
			if (str[pos] == '+')
			{
				pos++;
			}
			else if (str[pos] == '-')
			{
				sign = -1;
				pos++;
			}

			return sign * ParseUnsignedInt(str, ref pos, maxDigits);
		}

		/// <summary>
		/// Parse unsigned integer of a specified maximal length.
		/// </summary>
		public static int ParseUnsignedInt(string str, ref int pos, int maxDigits)          // PHP: timelib_get_nr
		{
			int len = 0;

			// skips non-digits:
			while (pos < str.Length && !Char.IsDigit(str, pos))
			{
				pos++;
			}
			if (pos == str.Length) return -1;

			int begin = pos;
			while (pos < str.Length && Char.IsDigit(str, pos) && len < maxDigits)
			{
				pos++;
				len++;
			}

			return Int32.Parse(str.Substring(begin, len));
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

			switch (str.Substring(begin, pos - begin))
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

		/// <summary>
		/// Sets relative time and date information according to the parsed text.
		/// </summary>
		public void SetRelative(string str, int amount, int behavior)
		{
			switch (str)
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
					relative.m += amount;
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
					relative.y += amount;
					return;
			}

			if (SetWeekDay(str))
			{
				relative.d += (amount > 0 ? amount - 1 : amount) * 7;

				// TIMELIB_HAVE_WEEKDAY_RELATIVE
				have_weekday_relative = 1;
				relative.weekday_behavior = behavior;

				// TIMELIB_UNHAVE_TIME 
				have_time = 0;
				h = 0;
				i = 0;
				s = 0;
				f = 0;
			}
		}

		/// <summary>
		/// Sets relative week day according to a specified text.
		/// </summary>
		public bool SetWeekDay(string str)
		{
			switch (str)
			{
				case "mon":
				case "monday": relative.weekday = 1; break;
				case "tue":
				case "tuesday": relative.weekday = 2; break;
				case "wed":
				case "wednesday": relative.weekday = 3; break;
				case "thu":
				case "thursday": relative.weekday = 4; break;
				case "fri":
				case "friday": relative.weekday = 5; break;
				case "sat":
				case "saturday": relative.weekday = 6; break;
				case "sun":
				case "sunday": relative.weekday = 0; break;
				default: return false;
			}
			return true;
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

			while (pos < str.Length && (str[pos] == ' ' || str[pos] == '('))
				pos++;

			bool result;

			if (pos < str.Length && str[pos] == '+')
			{
				pos++;
				z = ParseTimeZone(str, ref pos);
				result = true;
			}
			else if (pos < str.Length && str[pos] == '-')
			{
				pos++;
				z = -ParseTimeZone(str, ref pos);
				result = true;
			}
			else
			{
				result = SetZoneOffset(str.Substring(pos, str.Length - pos));
				pos = str.Length;
			}

			while (pos < str.Length && str[pos] == ')')
				pos++;

			if (result) HAVE_TZ();

			return result;
		}

		/// <summary>
		/// Parses numeric timezones. Returns offset in minutes.
		/// </summary>
		static int ParseTimeZone(string str, ref int pos)                               // PHP: timelib_parse_tz_cor
		{
			int result = 0;
			int length = str.Length - pos;

			switch (length)
			{
				case 1: // 0
				case 2: // 00
					result = Int32.Parse(str.Substring(pos, length)) * 60;
					break;

				case 3: // 000, 0:0
				case 4: // 0000, 0:00, 00:0
					if (str[pos + 1] == ':')       // 0:0, 0:00
					{
						result = (str[pos] - '0') * 60 + Int32.Parse(str.Substring(pos + 2, length - 2));
					}
					else if (str[pos + 2] == ':')  // 00:0
					{
						result = ((str[pos] - '0') * 10 + (str[pos + 1] - '0')) * 60 + (str[pos + 3] - '0');
					}
					else                          // 000, 0000
					{
						result = Int32.Parse(str.Substring(pos, length));
						result = (result / 100) * 60 + result % 100;
					}
					break;

				case 5: // 00:00
					result = Int32.Parse(str.Substring(pos, 2)) * 60 + Int32.Parse(str.Substring(pos + 3, 2));
					break;
			}

			return result;
		}

		/// <summary>
		/// Sets zone offset by zone abbreviation.
		/// </summary>
		private bool SetZoneOffset(string/*!*/ abbreviation)                             // PHP: timelib_lookup_zone, zone_search
		{
			// source http://www.worldtimezone.com/wtz-names/timezonenames.html
			switch (abbreviation)
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

				default: return false;
			}

			return true;
		}

		#endregion
	}
}
