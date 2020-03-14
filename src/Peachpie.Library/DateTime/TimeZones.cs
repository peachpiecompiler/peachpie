/*

 Copyright (c) 2005-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.ComponentModel;
using System.Runtime.InteropServices;

using System.Diagnostics;
using Pchp.Core;
using System.Xml;
using Pchp.Core.Utilities;
using System.Text.RegularExpressions;

namespace Pchp.Library.DateTime
{
    /// <summary>
	/// Provides timezone information for PHP functions.
	/// </summary>
    [PhpExtension("date")]
    public static class PhpTimeZone
    {
        private const string EnvVariableName = "TZ";

        [DebuggerDisplay("{PhpName} - {Info}")]
        private struct TimeZoneInfoItem
        {
            /// <summary>
            /// Comparer of <see cref="TimeZoneInfoItem"/>, comparing its <see cref="TimeZoneInfoItem.PhpName"/>.
            /// </summary>
            public class Comparer : IComparer<TimeZoneInfoItem>
            {
                public int Compare(TimeZoneInfoItem x, TimeZoneInfoItem y)
                {
                    return StringComparer.OrdinalIgnoreCase.Compare(x.PhpName, y.PhpName);
                }
            }

            /// <summary>
            /// PHP time zone name.
            /// </summary>
            public readonly string PhpName;

            /// <summary>
            /// Actual <see cref="TimeZoneInfo"/> from .NET.
            /// </summary>
            public readonly TimeZoneInfo Info;

            /// <summary>
            /// Abbreviation. If more than one, separated with comma.
            /// </summary>
            public readonly string Abbreviation;

            /// <summary>
            /// Not listed item used only as an alias for another time zone.
            /// </summary>
            public readonly bool IsAlias;

            /// <summary>
            /// Gets value indicating the given abbreviation can be used for this timezone.
            /// </summary>
            public bool HasAbbreviation(string abbr)
            {
                if (!string.IsNullOrEmpty(abbr) && Abbreviation != null)
                {
                    // Abbreviation.Split(new[] { ',' }).Contains(abbr, StringComparer.OrdinalIgnoreCase);

                    int index = 0;
                    while ((index = Abbreviation.IndexOf(abbr, index, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        int end = index + abbr.Length;
                        if (index == 0 || Abbreviation[index - 1] == ',')
                        {
                            if (end == Abbreviation.Length || Abbreviation[end] == ',')
                            {
                                return true;
                            }
                        }

                        // 
                        index++;
                    }
                }

                return false;
            }

            internal TimeZoneInfoItem(string/*!*/phpName, TimeZoneInfo/*!*/info, string abbreviation, bool isAlias)
            {
                // TODO: alter the ID with php-like name
                //if (!phpName.Equals(info.Id, StringComparison.OrdinalIgnoreCase))
                //{
                //    info = TimeZoneInfo.CreateCustomTimeZone(phpName, info.BaseUtcOffset, info.DisplayName, info.StandardName, info.DaylightName, info.GetAdjustmentRules());
                //}

                //
                this.PhpName = phpName;
                this.Info = info;
                this.Abbreviation = abbreviation;
                this.IsAlias = isAlias;
            }
        }

        #region timezones

        /// <summary>
        /// PHP time zone database.
        /// </summary>
        private readonly static Lazy<TimeZoneInfoItem[]>/*!!*/s_lazyTimeZones = new Lazy<TimeZoneInfoItem[]>(
            InitializeTimeZones,
            System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        private static TimeZoneInfoItem[]/*!!*/InitializeTimeZones()
        {
            // read list of initial timezones
            var sortedTZ = new SortedSet<TimeZoneInfoItem>(ReadTimeZones(), new TimeZoneInfoItem.Comparer());

            // add additional time zones:
            sortedTZ.Add(new TimeZoneInfoItem("UTC", TimeZoneInfo.Utc, "utc", false));
            sortedTZ.Add(new TimeZoneInfoItem("Etc/UTC", TimeZoneInfo.Utc, "utc", true));
            sortedTZ.Add(new TimeZoneInfoItem("Etc/GMT-0", TimeZoneInfo.Utc, "gmt", true));
            sortedTZ.Add(new TimeZoneInfoItem("GMT", TimeZoneInfo.Utc, "gmt", true));
            sortedTZ.Add(new TimeZoneInfoItem("GMT0", TimeZoneInfo.Utc, "gmt", true));
            sortedTZ.Add(new TimeZoneInfoItem("UCT", TimeZoneInfo.Utc, "utc", true));
            sortedTZ.Add(new TimeZoneInfoItem("Universal", TimeZoneInfo.Utc, "utc", true));
            sortedTZ.Add(new TimeZoneInfoItem("Zulu", TimeZoneInfo.Utc, "utc", true));
            //sortedTZ.Add(new TimeZoneInfoItem("MET", sortedTZ.First(t => t.PhpName == "Europe/Rome").Info, null, true));
            //sortedTZ.Add(new TimeZoneInfoItem("WET", sortedTZ.First(t => t.PhpName == "Europe/Berlin").Info, null, true));     
            //{ "PRC"              
            //{ "ROC"              
            //{ "ROK"   
            // W-SU = 
            //{ "Poland"           
            //{ "Portugal"         
            //{ "PRC"              
            //{ "ROC"              
            //{ "ROK"              
            //{ "Singapore"      = Asia/Singapore  
            //{ "Turkey"  

            //
            return sortedTZ.ToArray();
        }

        static bool IsAlias(string id)
        {
            // whether to not display such tz within timezone_identifiers_list()
            var isphpname = id.IndexOf('/') >= 0 || id.IndexOf("GMT", StringComparison.Ordinal) >= 0 && id.IndexOf(' ') < 0;
            return !isphpname;
        }

        static Dictionary<string, string> LoadAbbreviations()
        {
            var abbrs = new Dictionary<string, string>(512); // timezone_id => abbrs

            using (var abbrsstream = new System.IO.StreamReader(typeof(PhpTimeZone).Assembly.GetManifestResourceStream("Pchp.Library.Resources.abbreviations.txt")))
            {
                string line;
                while ((line = abbrsstream.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line) || line[0] == '#') continue;
                    var idx = line.IndexOf(' ');
                    if (idx > 0)
                    {
                        // abbreviation[space]timezone_id
                        var abbr = line.Remove(idx); // abbreviation
                        var tz = line.Substring(idx + 1); // timezone_id
                        if (abbrs.TryGetValue(tz, out var oldabbr))
                        {
                            // more abbrs for a single tz
                            if (oldabbr.IndexOf(abbr) >= 0) continue; // the list contains duplicities ..
                            abbr = oldabbr + "," + abbr;
                        }

                        abbrs[tz] = abbr;
                    }
                }
            }

            return abbrs;
        }

        static IEnumerable<string[]> LoadKnownTimeZones()
        {
            // collect php time zone names and match them with Windows TZ IDs:
            using (var xml = XmlReader.Create(new System.IO.StreamReader(typeof(PhpTimeZone).Assembly.GetManifestResourceStream("Pchp.Library.Resources.WindowsTZ.xml"))))
            {
                while (xml.Read())
                {
                    switch (xml.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (xml.Name == "mapZone")
                            {
                                // <mapZone other="Dateline Standard Time" type="Etc/GMT+12"/>

                                var winId = xml.GetAttribute("other");
                                var phpIds = xml.GetAttribute("type");

                                if (string.IsNullOrEmpty(phpIds))
                                {
                                    yield return new[] { winId };
                                }
                                else if (phpIds.IndexOf(' ') < 0)
                                {
                                    yield return new[] { winId, phpIds };
                                }
                                else
                                {
                                    var list = new List<string>(4) { winId };
                                    list.AddRange(phpIds.Split(' '));
                                    yield return list.ToArray();
                                }
                            }
                            break;
                    }
                }
            }

            // other time zones:

            yield return new[] { "US/Alaska", "Alaskan Standard Time" };
            //yield return new[] { "US/Aleutian", (???) };
            yield return new[] { "US/Arizona", "US Mountain Standard Time" };
            yield return new[] { "US/Central", "Central Standard Time" };
            yield return new[] { "East-Indiana", "US Eastern Standard Time" };
            yield return new[] { "Eastern", "Eastern Standard Time" };
            yield return new[] { "US/Hawaii", "Hawaiian Standard Time" };
            // "US/Indiana-Starke"
            // "US/Michigan"
            yield return new[] { "US/Mountain", "Mountain Standard Time" };
            yield return new[] { "US/Pacific", "Pacific Standard Time" };
            yield return new[] { "US/Pacific-New", "Pacific Standard Time" };
            yield return new[] { "US/Samoa", "Samoa Standard Time" };
        }

        static IEnumerable<TimeZoneInfoItem>/*!!*/ReadTimeZones()
        {
            // map of time zones:
            var tzdict = TimeZoneInfo
                .GetSystemTimeZones()
                .ToDictionary(tz => tz.Id, StringComparer.OrdinalIgnoreCase);

            // add aliases and knonwn time zones from bundled XML:
            foreach (var names in LoadKnownTimeZones())
            {
                TimeZoneInfo tz = null;

                for (int i = 0; i < names.Length; i++)
                {
                    if (tzdict.TryGetValue(names[i], out tz))
                    {
                        break;
                    }
                }

                // update the map of known time zones:
                if (tz != null)
                {
                    for (int i = 0; i < names.Length; i++)
                    {
                        tzdict[names[i]] = tz;
                    }
                }
            }

            // prepare abbreviations
            var abbrs = LoadAbbreviations();

            // yield return all discovered time zones:
            foreach (var pair in tzdict)
            {
                abbrs.TryGetValue(pair.Key, out var abbreviation);

                yield return new TimeZoneInfoItem(pair.Key, pair.Value, abbreviation, IsAlias(pair.Key));
            }
        }

        #endregion

        /// <summary>
        /// Gets the current time zone for PHP date-time library functions. Associated with the current context.
        /// </summary>
        /// <remarks>It returns the time zone set by date_default_timezone_set PHP function.
        /// If no time zone was set, the time zone is determined in following order:
        /// 1. the time zone set in configuration
        /// 2. the time zone of the current system
        /// 3. default UTC time zone</remarks>
        internal static TimeZoneInfo GetCurrentTimeZone(Context ctx)
        {
            // if timezone is set by date_default_timezone_set(), return it

            var info = ctx.TryGetProperty<TimeZoneInfo>();
            if (info == null)
            {
                // default timezone was not set, use & cache the current timezone
                info = ctx.GetStatic<CurrentTimeZoneCache>().TimeZone;
            }

            //
            return info;
        }

        internal static void SetCurrentTimeZone(Context ctx, TimeZoneInfo value)
        {
            ctx.SetProperty(value);
        }

        #region CurrentTimeZoneCache

        /// <summary>
        /// Cache of current TimeZone with auto-update ability.
        /// </summary>
        private class CurrentTimeZoneCache
        {
            public CurrentTimeZoneCache()
            {
            }
#if DEBUG
            internal CurrentTimeZoneCache(TimeZoneInfo timezone)
            {
                this._timeZone = timezone;
                this._changedFunc = (_) => false;
            }
#endif

            /// <summary>
            /// Get the TimeZone set by the current process. Depends on environment variable, or local configuration, or system time zone.
            /// </summary>
            public TimeZoneInfo TimeZone
            {
                get
                {
                    if (_timeZone == null || _changedFunc == null || _changedFunc(_timeZone) == true)
                        _timeZone = DetermineTimeZone(out _changedFunc);    // get the current timezone, update the function that determines, if the timezone has to be rechecked.

                    return _timeZone;
                }
            }

            private TimeZoneInfo _timeZone;

            /// <summary>
            /// Function that determines if the current timezone should be rechecked.
            /// </summary>
            private Func<TimeZoneInfo/*!*/, bool> _changedFunc;

            /// <summary>
            /// Finds out the time zone in the way how PHP does.
            /// </summary>
            private static TimeZoneInfo DetermineTimeZone(out Func<TimeZoneInfo, bool> changedFunc)
            {
                TimeZoneInfo result;

                //// check environment variable:
                //string env_tz = System.Environment.GetEnvironmentVariable(EnvVariableName);
                //if (!string.IsNullOrEmpty(env_tz))
                //{
                //    result = GetTimeZone(env_tz);
                //    if (result != null)
                //    {
                //        // recheck the timezone only if the environment variable changes
                //        changedFunc = (timezone) => !String.Equals(timezone.StandardName, System.Environment.GetEnvironmentVariable(EnvVariableName), StringComparison.OrdinalIgnoreCase);
                //        // return the timezone set in environment
                //        return result;
                //    }

                //    PhpException.Throw(PhpError.Notice, LibResources.GetString("unknown_timezone_env", env_tz));
                //}

                //// check configuration:
                //LibraryConfiguration config = LibraryConfiguration.Local;
                //if (config.Date.TimeZone != null)
                //{
                //    // recheck the timezone only if the local configuration changes, ignore the environment variable from this point at all
                //    changedFunc = (timezone) => LibraryConfiguration.Local.Date.TimeZone != timezone;
                //    return config.Date.TimeZone;
                //}

                // convert current system time zone to PHP zone:
                result = SystemToPhpTimeZone(TimeZoneInfo.Local);

                // UTC:
                if (result == null)
                    result = DateTimeUtils.UtcTimeZone;// GetTimeZone("UTC");

                //PhpException.Throw(PhpError.Strict, LibResources.GetString("using_implicit_timezone", result.Id));

                // recheck the timezone when the TimeZone in local configuration is set
                changedFunc = (timezone) => false;//LibraryConfiguration.Local.Date.TimeZone != null;
                return result;
            }

        }

        #endregion

        ///// <summary>
        ///// Gets/sets/resets legacy configuration setting "date.timezone".
        ///// </summary>
        //internal static object GsrTimeZone(LibraryConfiguration/*!*/ local, LibraryConfiguration/*!*/ @default, object value, IniAction action)
        //{
        //    string result = (local.Date.TimeZone != null) ? local.Date.TimeZone.StandardName : null;

        //    switch (action)
        //    {
        //        case IniAction.Set:
        //            {
        //                string name = Core.Convert.ObjectToString(value);
        //                TimeZoneInfo zone = GetTimeZone(name);

        //                if (zone == null)
        //                {
        //                    PhpException.Throw(PhpError.Warning, LibResources.GetString("unknown_timezone", name));
        //                }
        //                else
        //                {
        //                    local.Date.TimeZone = zone;
        //                }
        //                break;
        //            }

        //        case IniAction.Restore:
        //            local.Date.TimeZone = @default.Date.TimeZone;
        //            break;
        //    }
        //    return result;
        //}

        /// <summary>
        /// Gets an instance of <see cref="TimeZone"/> corresponding to specified PHP name for time zone.
        /// </summary>
        /// <param name="phpName">PHP time zone name.</param>
        /// <returns>The time zone or a <B>null</B> reference.</returns>
        internal static TimeZoneInfo GetTimeZone(string/*!*/ phpName)
        {
            if (string.IsNullOrEmpty(phpName))
            {
                return null;
            }

            // simple binary search (not the Array.BinarySearch)
            var timezones = PhpTimeZone.s_lazyTimeZones.Value;
            int a = 0, b = timezones.Length - 1;
            while (a <= b)
            {
                int x = (a + b) >> 1;
                int comparison = StringComparer.OrdinalIgnoreCase.Compare(timezones[x].PhpName, phpName);
                if (comparison == 0)
                    return timezones[x].Info;

                if (comparison < 0)
                    a = x + 1;
                else //if (comparison > 0)
                    b = x - 1;
            }

            // try custom offset or a known abbreviation:
            var dt = new DateInfo();
            var _ = 0;
            if (dt.SetTimeZone(phpName, ref _))
            {
                // +00:00
                // -00:00
                // abbr
                return dt.ResolveTimeZone();
            }

            //
            return null;
        }

        /// <summary>
        /// Tries to match given <paramref name="systemTimeZone"/> to our fixed <see cref="s_timezones"/>.
        /// </summary>
        static TimeZoneInfo SystemToPhpTimeZone(TimeZoneInfo systemTimeZone)
        {
            if (systemTimeZone == null)
                return null;

            var tzns = s_lazyTimeZones.Value;
            for (int i = 0; i < tzns.Length; i++)
            {
                var tz = tzns[i].Info;
                if (tz != null && tz.DisplayName.Equals(systemTimeZone.DisplayName, StringComparison.OrdinalIgnoreCase)) // TODO: && tz.HasSameRules(systemTimeZone))
                    return tz;
            }

            return null;
        }

        #region date_default_timezone_get, date_default_timezone_set

        public static bool date_default_timezone_set(Context ctx, string zoneName)
        {
            var zone = GetTimeZone(zoneName);
            if (zone == null)
            {
                PhpException.Throw(PhpError.Notice, Resources.LibResources.unknown_timezone, zoneName);
                return false;
            }

            SetCurrentTimeZone(ctx, zone);
            return true;
        }

        public static string date_default_timezone_get(Context ctx)
        {
            var timezone = GetCurrentTimeZone(ctx);
            return (timezone != null) ? timezone.Id : null;
        }

        #endregion

        #region date_timezone_get, date_timezone_set

        /// <summary>
        /// Alias to <see cref="DateTimeInterface.getTimezone"/>.
        /// </summary>
        public static DateTimeZone date_timezone_get(DateTimeInterface dt) => dt.getTimezone();

        /// <summary>
        /// Alias to <see cref="DateTime.setTimezone(DateTimeZone)"/>.
        /// </summary>
        public static DateTime date_timezone_set(DateTime dt, DateTimeZone timezone) => dt.setTimezone(timezone);

        #endregion

        #region timezone_identifiers_list, timezone_version_get, timezone_abbreviations_list, timezone_name_from_abbr

        static readonly Dictionary<string, int> s_what = new Dictionary<string, int>(10, StringComparer.OrdinalIgnoreCase)
        {
            {"africa", DateTimeZone.AFRICA},
            {"america", DateTimeZone.AMERICA},
            {"antarctica", DateTimeZone.ANTARCTICA},
            {"artic", DateTimeZone.ARCTIC},
            {"asia", DateTimeZone.ASIA},
            {"atlantic", DateTimeZone.ATLANTIC},
            {"australia", DateTimeZone.AUSTRALIA},
            {"europe", DateTimeZone.EUROPE},
            {"indian", DateTimeZone.INDIAN},
            {"pacific", DateTimeZone.PACIFIC},
            {"etc", DateTimeZone.UTC},
        };

        /// <summary>
        /// Gets zone constant.
        /// </summary>
        static int GuessWhat(TimeZoneInfoItem tz)
        {
            int slash = tz.PhpName.IndexOf('/');
            if (slash > 0)
            {
                s_what.TryGetValue(tz.PhpName.Remove(slash), out int code);
                return code;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Returns a numerically indexed array containing all defined timezone identifiers.
        /// </summary>
        public static PhpArray timezone_identifiers_list(int what = DateTimeZone.ALL, string country = null)
        {
            if ((what & DateTimeZone.PER_COUNTRY) == DateTimeZone.PER_COUNTRY || !string.IsNullOrEmpty(country))
            {
                throw new NotImplementedException();
            }

            var timezones = PhpTimeZone.s_lazyTimeZones.Value;

            // copy names to PHP array:
            var array = new PhpArray(timezones.Length);
            for (int i = 0; i < timezones.Length; i++)
            {
                if (timezones[i].IsAlias)
                {
                    continue;
                }

                if (what == DateTimeZone.ALL || what == DateTimeZone.ALL_WITH_BC || (what & GuessWhat(timezones[i])) != 0)
                {
                    array.Add(timezones[i].PhpName);
                }
            }

            //
            return array;
        }

        /// <summary>
        /// Gets the version of used the time zone database.
        /// </summary>
        public static string timezone_version_get()
        {
            //try
            //{
            //    using (var reg = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Time Zones"))
            //        return reg.GetValue("TzVersion", 0).ToString() + ".system";
            //}
            //catch { }


            // no windows update installed
            return "0.system";
        }

        /// <summary>
        /// Returns associative array containing dst, offset and the timezone name.
        /// Alias to <see cref="DateTimeZone.listAbbreviations"/>.
        /// </summary>
        [return: NotNull]
        public static PhpArray timezone_abbreviations_list()
        {
            var timezones = PhpTimeZone.s_lazyTimeZones.Value;
            var result = new PhpArray();

            //
            for (int i = 0; i < timezones.Length; i++)
            {
                var tz = timezones[i];
                var abbrs = tz.Abbreviation;
                if (abbrs != null)
                {
                    foreach (var abbr in abbrs.Split(new[] { ',' }))
                    {
                        if (!result.TryGetValue(abbr, out var tzs))
                            tzs = new PhpArray();

                        tzs.Array.Add(new PhpArray(3)
                        {
                            {"dst", tz.Info.SupportsDaylightSavingTime },
                            {"offset", (long)tz.Info.BaseUtcOffset.TotalSeconds },
                            {"timezone_id", tz.PhpName },
                        });

                        result[abbr] = tzs;
                    }
                }
            }

            //
            return result;
        }

        /// <summary>
        /// Returns the timezone name from abbreviation.
        /// </summary>
        [return: CastToFalse]
        public static string timezone_name_from_abbr(string abbr, int gmtOffset = -1, int isdst = -1)
        {
            var timezones = PhpTimeZone.s_lazyTimeZones.Value;
            string result = null;   // candidate

            if (string.IsNullOrEmpty(abbr) && gmtOffset == -1)
            {
                // not specified
                return null; // FALSE
            }

            //
            for (int i = 0; i < timezones.Length; i++)
            {
                var tz = timezones[i];

                if (tz.IsAlias)
                {
                    continue;
                }

                // if {abbr} is specified => {abbrs} must contain it, otherwise do not check this timezone
                var matchesabbr = tz.HasAbbreviation(abbr);

                // offset is ignored
                if (gmtOffset == -1)
                {
                    if (matchesabbr)
                    {
                        result = tz.PhpName;
                        break;
                    }

                    continue;
                }

                // resolve dst delta (if needed)
                TimeSpan dstdelta;
                if (isdst >= 0) // dst taken into account
                {
                    dstdelta = tz.Info.SupportsDaylightSavingTime
                        ? tz.Info.GetAdjustmentRules().Select(r => r.DaylightDelta).FirstOrDefault(r => r.Ticks != 0)
                        : default;

                    if (dstdelta.Ticks == 0)
                    {
                        continue;
                    }
                }
                else
                {
                    dstdelta = default;
                }

                // offset must match
                var matchesoffset = (tz.Info.BaseUtcOffset + dstdelta).TotalSeconds == gmtOffset;

                if (matchesoffset)
                {
                    if (matchesabbr || string.IsNullOrEmpty(abbr))
                        return tz.PhpName;

                    // offset matches but not the abbreviation
                    // in case nothing else is found use this as the result
                    result ??= tz.PhpName;
                }
            }

            //
            return result;
        }

        #endregion

        #region timezone_open, timezone_offset_get

        /// <summary>
        /// Alias of new <see cref="DateTimeZone"/>
        /// </summary>
        [return: CastToFalse]
        public static DateTimeZone timezone_open(string timezone)
        {
            var tz = GetTimeZone(timezone);
            if (tz == null)
                return null;

            return new DateTimeZone(tz);
        }

        /// <summary>
        /// Alias of <see cref="DateTimeZone.getOffset"/>
        /// </summary>
        [return: CastToFalse]
        public static int timezone_offset_get(DateTimeZone timezone, Library.DateTime.DateTime datetime)
        {
            return (timezone != null) ? timezone.getOffset(datetime) : -1;
        }

        [return: CastToFalse]
        public static PhpArray timezone_transitions_get(DateTimeZone timezone, int timestamp_begin = 0, int timestamp_end = 0)
        {
            return timezone?.getTransitions(timestamp_begin, timestamp_end);
        }

        #endregion

        #region timezone_location_get 

        /// <summary>
        /// Returns location information for a timezone.
        /// </summary>
        [return: CastToFalse]
        public static PhpArray timezone_location_get(DateTimeZone @object) => @object.getLocation();

        #endregion

        /// <summary>
        /// Alias to <see cref="DateTimeZone.getName"/>
        /// </summary>
        public static string timezone_name_get(DateTimeZone @object) => @object.getName();
    }
}
