using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Library
{
    internal static class PathUtils
    {
        public const char DirectorySeparator = '\\';
        public const char AltDirectorySeparator = '/';

        public static bool IsDirectorySeparator(this char ch) => ch == DirectorySeparator || ch == AltDirectorySeparator;
    }

    /// <summary>
    /// Unix TimeStamp to DateTime conversion and vice versa
    /// </summary>
    internal static class DateTimeUtils
    {
        #region Nested Class: UtcTimeZone, GmtTimeZone

        //private sealed class _UtcTimeZone : CustomTimeZoneBase
        //{
        //    public override string DaylightName { get { return "UTC"; } }
        //    public override string StandardName { get { return "UTC"; } }

        //    public override TimeSpan GetUtcOffset(DateTime time)
        //    {
        //        return new TimeSpan(0);
        //    }

        //    public override DaylightTime GetDaylightChanges(int year)
        //    {
        //        return new DaylightTime(new DateTime(0), new DateTime(0), new TimeSpan(0));
        //    }


        //}

        //private sealed class _GmtTimeZone : CustomTimeZoneBase
        //{
        //    public override string DaylightName { get { return "GMT Daylight Time"; } }
        //    public override string StandardName { get { return "GMT Standard Time"; } }

        //    public override TimeSpan GetUtcOffset(DateTime time)
        //    {
        //        return IsDaylightSavingTime(time) ? new TimeSpan(0, +1, 0, 0, 0) : new TimeSpan(0);
        //    }
        //    public override DaylightTime GetDaylightChanges(int year)
        //    {
        //        return new DaylightTime
        //        (
        //          new DateTime(year, 3, 27, 1, 0, 0),
        //          new DateTime(year, 10, 30, 2, 0, 0),
        //          new TimeSpan(0, +1, 0, 0, 0)
        //        );
        //    }
        //}

        #endregion

        /// <summary>
        /// Time 0 in terms of Unix TimeStamp.
        /// </summary>
        public static readonly DateTime/*!*/UtcStartOfUnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// UTC time zone.
        /// </summary>
        internal static TimeZoneInfo/*!*/UtcTimeZone { get { return TimeZoneInfo.Utc; } }

        /// <summary>
        /// Converts <see cref="DateTime"/> representing UTC time to UNIX timestamp.
        /// </summary>
        /// <param name="dt">Time.</param>
        /// <returns>Unix timestamp.</returns>
        internal static int UtcToUnixTimeStamp(DateTime dt)
        {
            double seconds = (dt - UtcStartOfUnixEpoch).TotalSeconds;

            if (seconds < Int32.MinValue)
                return Int32.MinValue;
            if (seconds > Int32.MaxValue)
                return Int32.MaxValue;

            return (int)seconds;
        }

        /// <summary>
        /// Converts UNIX timestamp (number of seconds from 1.1.1970) to <see cref="DateTime"/>.
        /// </summary>
        /// <param name="timestamp">UNIX timestamp</param>
        /// <returns><see cref="DateTime"/> structure representing UTC time.</returns>
        internal static DateTime UnixTimeStampToUtc(int timestamp)
        {
            return UtcStartOfUnixEpoch + TimeSpan.FromSeconds(timestamp);
        }

        /// <summary>
        /// Determine maximum of three given <see cref="DateTime"/> values.
        /// </summary>
        internal static DateTime Max(DateTime d1, DateTime d2)
        {
            return (d1 > d2) ? d1 : d2;
        }

        /// <summary>
        /// Determine maximum of three given <see cref="DateTime"/> values.
        /// </summary>
        internal static DateTime Max(DateTime d1, DateTime d2, DateTime d3)
        {
            return (d1 < d2) ? ((d2 < d3) ? d3 : d2) : ((d1 < d3) ? d3 : d1);
        }

        //		private static TimeZone GetTimeZoneFromRegistry(TimeZone/*!*/ zone)
        //		{
        //		  try
        //		  {
        //		    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
        //		      @"Software\Microsoft\Windows NT\CurrentVersion\Time Zones\" + zone.StandardName,false))
        //		    {
        //  		    if (key == null) return null;
        //		      
        //		      byte[] tzi = key.GetValue("TZI") as byte[];
        //		      if (tzi == null) continue;
        //    		    
        //    		  int bias = BitConverter.ToInt32(tzi,0);
        //    		  
        //  		  }  
        //		  }
        //		  catch (Exception)
        //		  {
        //		  }
        //
        //		  return null;
        //		}		
    }
}
