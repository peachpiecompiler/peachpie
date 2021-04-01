#nullable enable

using Pchp.Core;
using Pchp.Library;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System_DateTime = System.DateTime;

namespace Pchp.Library.DateTime
{
    /// <summary>
    /// Internal container for last datetime parse operation errors.
    /// </summary>
    sealed class DateTimeErrors
    {
        public static DateTimeErrors Empty { get; } = new DateTimeErrors();

        public IList<string>? Warnings { get; set; }
        public IList<string>? Errors { get; set; }

        public bool HasErrors => Errors != null && Errors.Count != 0;

        internal static void AddWarning(ref DateTimeErrors errors, string message)
        {
            if (errors == null) errors = new DateTimeErrors();
            if (errors.Warnings == null) errors.Warnings = new List<string>();

            errors.Warnings.Add(message);
        }

        internal static void AddError(ref DateTimeErrors errors, string message)
        {
            if (errors == null) errors = new DateTimeErrors();
            if (errors.Errors == null) errors.Errors = new List<string>();

            errors.Errors.Add(message);
        }

        public PhpArray ToPhpArray()
        {
            var warnings = new PhpArray(Warnings);
            var errors = new PhpArray(Errors);

            return new PhpArray(4)
            {
                { "warning_count", warnings.Count },
                { "warnings", warnings },
                { "error_count", errors.Count },
                { "errors", errors },
            };
        }
    }

    /// <summary>
    /// Representation of date and time.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(PhpExtensionAttribute.KnownExtensionNames.Date)]
    [DebuggerDisplay(nameof(DateTime), Type = PhpVariable.TypeNameObject)]
    public class DateTime : DateTimeInterface, IPhpComparable, IPhpCloneable
    {
        #region Fields

        /// <summary>
        /// Time string representing current date and time.
        /// </summary>
        internal const string s_Now = "now";

        /// <summary>
        /// Get the date-time value, corresponding to the specified time zone.
        /// </summary>
        internal System_DateTime LocalTime { get; private set; }

        /// <summary>
        /// Get the time zone for this DateTime object
        /// </summary>
        internal TimeZoneInfo LocalTimeZone { get; private set; } = TimeZoneInfo.Utc;

        // used for serialization:
        // note: we incorrectly show following in reflection as well.

        /// <summary>
        /// Allows to get or set local time in format <c>"yyyy-MM-dd HH:mm:ss.ffff"</c>.
        /// </summary>
        public string date
        {
            get => LocalTime.ToString("yyyy-MM-dd HH:mm:ss.ffff");
            set => LocalTime = System_DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
        }

        /// <summary>
        /// Gets the timezone type.
        /// </summary>
        /// <remarks>
        /// 1: offset in form of +00:00
        /// 2: timezone abbreviation
        /// 3: TimeZone object
        /// </remarks>
        public int timezone_type
        {
            get => DateInfo.GetTimeLibZoneType(LocalTimeZone);
            set { } // ignore, but allow unserialize of this value
        }

        /// <summary>
        /// Gets the timezone identifier.
        /// </summary>
        public string timezone
        {
            get
            {
                return LocalTimeZone.Id;
            }
            set
            {
                LocalTimeZone = PhpTimeZone.GetTimeZone(value) ?? throw new ArgumentException();
            }
        }

        #endregion

        #region Construction

        [PhpFieldsOnlyCtor]
        protected DateTime(Context ctx)
        {
            LocalTimeZone = PhpTimeZone.GetCurrentTimeZone(ctx);
            LocalTime = TimeZoneInfo.ConvertTimeFromUtc(System_DateTime.UtcNow, LocalTimeZone);
        }

        private DateTime(System_DateTime datetime, TimeZoneInfo timezone)
        {
            LocalTime = datetime;
            LocalTimeZone = timezone ?? throw new ArgumentNullException(nameof(timezone));
        }

        // public __construct ([ string $time = "now" [, DateTimeZone $timezone = NULL ]] )
        public DateTime(Context ctx, string? datetime = s_Now, DateTimeZone? timezone = null)
        {
            __construct(ctx, datetime, timezone);
        }

        // public __construct ([ string $time = "now" [, DateTimeZone $timezone = NULL ]] )
        public void __construct(Context ctx, string? datetime = s_Now, DateTimeZone? timezone = null)
        {
            Parse(ctx, datetime ?? string.Empty, timezone?._timezone, out var localdate, out var localtz);

            //
            LocalTime = localdate;
            LocalTimeZone = localtz;
        }

        #endregion

        #region Helpers

        [PhpHidden]
        internal static void Parse(Context ctx, string timestr, TimeZoneInfo? timezone, out System_DateTime localdate, out TimeZoneInfo localtz)
        {
            Debug.Assert(ctx != null);
            Debug.Assert(timestr != null);

            //
            ctx!.SetProperty(DateTimeErrors.Empty);

            timestr = timestr != null ? timestr.Trim() : string.Empty;
            localtz = timezone ?? PhpTimeZone.GetCurrentTimeZone(ctx);

            Debug.Assert(localtz != null);

            //
            if (timestr.Length == 0 || timestr.EqualsOrdinalIgnoreCase(s_Now))
            {
                // most common case
                localdate = TimeZoneInfo.ConvertTimeFromUtc(System_DateTime.UtcNow, localtz);
            }
            else
            {
                var result = DateInfo.Parse(timestr, System.DateTime.UtcNow, ref localtz, out var error);
                if (error != null)
                {
                    ctx.SetProperty<DateTimeErrors>(new DateTimeErrors { Errors = new[] { error } });
                    throw new Spl.Exception(error);
                }

                localdate = result;
            }
        }

        internal DateTimeImmutable AsDateTimeImmutable() => new DateTimeImmutable(LocalTime, LocalTimeZone);

        #endregion

        #region Methods

        public virtual void __wakeup()
        {

        }

        /// <summary>
        /// Returns a new instance of a DateTime object.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="array">Initialization array <c>("date", "timezone_type", "timezone")</c>.</param>
        public static DateTime __set_state(Context ctx, PhpArray array)
        {
            if (array == null || array.Count == 0)
            {
                return new DateTime(ctx);
            }
            else
            {
                // resolve date/time
                var date = array.TryGetValue("date", out var dateval) && dateval.IsString(out var datestr)
                    ? System_DateTime.Parse(datestr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal)
                    : System_DateTime.UtcNow;

                // resolve timezone or current
                var localtz = PhpTimeZone.GetCurrentTimeZone(ctx);

                if (array.TryGetValue("timezone", out var tzval) && tzval.IsString(out var tz) && tz.Length != 0)
                {
                    //if (array.TryGetValue("timezone_type", out var typeval) && typeval.IsLong(out var type) &&
                    //    type == 1 &&
                    //    DateInfo.TryParseTimeZoneOffset(tz, 0, out var tz_minutes))
                    //{
                    //    // UTC offset
                    //    localtz = DateInfo.ResolveTimeZone(tz_minutes);
                    //}
                    //else
                    {
                        // deals with any tz format
                        localtz = PhpTimeZone.GetTimeZone(tz) ?? throw new Spl.InvalidArgumentException($"timezone:'{tz}'");
                    }
                }

                //

                if (date.Kind == DateTimeKind.Utc)
                {
                    date = TimeZoneInfo.ConvertTimeFromUtc(date, localtz);
                }

                return new DateTime(date, localtz);
            }
        }

        /// <summary>
        /// Adds an amount of days, months, years, hours, minutes and seconds to a DateTime object.
        /// </summary>
        /// <returns>Returns the <see cref="DateTime"/> object for method chaining or <c>FALSE</c> on failure.</returns>
        //[return: CastToFalse]
        public virtual DateTime add(DateInterval interval)
        {
            LocalTime = interval.Apply(LocalTime, negate: false);
            return this;
        }

        /// <summary>
        /// Subtracts an amount of days, months, years, hours, minutes and seconds from a DateTime object.
        /// </summary>
        //[return: CastToFalse]
        public virtual DateTime sub(DateInterval interval)
        {
            LocalTime = interval.Apply(LocalTime, negate: true);
            return this;
        }

        /// <summary>
        /// Returns the difference between two DateTime objects
        /// </summary>
        public virtual DateInterval diff(DateTimeInterface datetime2, bool absolute = false) => DateTimeFunctions.date_diff(this, datetime2, absolute);

        public virtual DateTime setTimezone(DateTimeZone timezone)
        {
            if (timezone == null)
            {
                //PhpException.InvalidArgumentType(nameof(timezone), nameof(DateTimeZone));
                throw new Spl.TypeError(string.Format(Core.Resources.ErrResources.invalid_argument_type, nameof(timezone), nameof(DateTimeZone)));
            }

            var newtz = timezone._timezone;
            if (newtz != LocalTimeZone)
            {
                LocalTime = TimeZoneInfo.ConvertTime(LocalTime, LocalTimeZone, newtz);
                LocalTimeZone = newtz;
            }

            return this;
        }

        [return: CastToFalse]
        public virtual string format(string format)
        {
            if (format == null)
            {
                //PhpException.ArgumentNull("format");
                //return false;
                throw new ArgumentNullException();
            }

            return DateTimeFunctions.FormatDate(format, LocalTime, LocalTimeZone);
        }

        public virtual long getOffset()
        {
            if (LocalTimeZone == null)
            {
                //return false;
                throw new InvalidOperationException();
            }

            // CONSIDER: (??) LocalTimeZone.IsDaylightSavingTime(datetime.Time) ? 3600 : 0

            return (long)LocalTimeZone.BaseUtcOffset.TotalSeconds;
        }

        public virtual DateTimeZone getTimezone() => new DateTimeZone(LocalTimeZone);

        /// <summary>Returns the warnings and errors</summary>
        /// <remarks>Unlike in PHP, we never return <c>FALSE</c>, according to the documentation and for (our) sanity.</remarks>
        public static PhpArray/*!*/getLastErrors(Context ctx)
        {
            var errors = ctx.TryGetProperty<DateTimeErrors>();
            return errors != null
                ? errors.ToPhpArray()
                : PhpArray.NewEmpty();
        }

        public virtual DateTime modify(Context ctx, string modify)
        {
            if (string.IsNullOrEmpty(modify))
            {
                //PhpException.ArgumentNull("modify");
                //return false;

                // Warning: failed to parse string at position 0
                throw new ArgumentNullException();
            }

            Parse(ctx, modify, LocalTimeZone, out var localdate, out var localtz);

            LocalTime = localdate;
            LocalTimeZone = localtz;

            return this;
        }

        [return: CastToFalse]
        public static DateTime? createFromFormat(Context ctx, string format, string time, DateTimeZone? timezone = null)
        {
            // arguments

            // format: the time format string
            // time: given time value
            // timezone: timezone object or null, ignored when "time" specifies the timezone already

            var dateinfo = DateInfo.ParseFromFormat(format, time, out var errors);

            ctx.SetProperty<DateTimeErrors>(errors);

            if (errors != null && errors.HasErrors)
            {
                return null;
            }

            var localtz = timezone?._timezone ?? PhpTimeZone.GetCurrentTimeZone(ctx);
            var localdate = dateinfo.GetDateTime(System_DateTime.UtcNow, ref localtz);

            return new DateTime(localdate, localtz);
        }

        /// <summary>
        /// Returns new DateTime object encapsulating the given DateTimeImmutable object.
        /// </summary>
        public static DateTime? createFromImmutable(DateTimeImmutable datetime)
        {
            if (datetime == null)
            {
                PhpException.ArgumentNull(nameof(datetime));
                return null;
            }

            return new DateTime(datetime.LocalTime, datetime.LocalTimeZone);
        }

        public static DateTime createFromInterface(DateTimeInterface datetime)
        {
            if (DateTimeFunctions.GetDateTimeFromInterface(datetime, out var dt, out var tz))
            {
                return new DateTime(dt, tz);
            }
            else
            {
                throw new Spl.InvalidArgumentException();
            }
        }

        public virtual DateTime setDate(int year, int month, int day)
        {
            try
            {
                var time = LocalTime;

                LocalTime = new System_DateTime(
                    year, month, day,
                    time.Hour, time.Minute, time.Second,
                    time.Millisecond);
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new ArgumentOutOfRangeException($"The date {year}-{month}-{day} is not valid.", e); // TODO: resouces
            }

            return this;
        }

        public virtual DateTime setISODate(int year, int week, int day = 1)
        {
            var jan1 = new System_DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            // Use first Thursday in January to get first week of the year as
            // it will never be in Week 52/53
            var firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            // As we're adding days to a date in Week 1,
            // we need to subtract 1 in order to get the right date for week #1
            if (firstWeek == 1)
            {
                week -= 1;
            }

            // Using the first Thursday as starting week ensures that we are starting in the right year
            // then we add number of weeks multiplied with days
            var result = firstThursday.AddDays(week * 7);

            // Subtract 3 days from Thursday to get Monday, which is the first weekday in ISO8601
            result = result.AddDays(-3);

            // days
            result = result.AddDays(day - 1);

            var time = LocalTime;

            LocalTime = new System_DateTime(
                result.Year, result.Month, result.Day,
                time.Hour, time.Minute, time.Second,
                time.Millisecond);

            return this;
        }

        public virtual DateTime setTime(int hour, int minute, int second)
        {
            try
            {
                var time = LocalTime;

                LocalTime = new System_DateTime(time.Year, time.Month, time.Day, hour, minute, second);
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new ArgumentOutOfRangeException($"The time {hour}:{minute}:{second} is not valid.", e); // TODO: resources
            }

            return this;
        }

        public virtual long getTimestamp()
        {
            return DateTimeUtils.UtcToUnixTimeStamp(TimeZoneInfo.ConvertTimeToUtc(LocalTime, LocalTimeZone));
        }

        /// <summary>
        /// Sets the date and time based on an Unix timestamp.
        /// </summary>
        /// <returns>Returns the <see cref="DateTime"/> object for method chaining.</returns>
        public DateTime setTimestamp(long unixtimestamp)
        {
            var utc = DateTimeUtils.UnixTimeStampToUtc(unixtimestamp);

            LocalTime = TimeZoneInfo.ConvertTimeFromUtc(utc, LocalTimeZone);

            return this;
        }

        #endregion

        #region IPhpComparable

        int IPhpComparable.Compare(PhpValue obj) => DateTimeFunctions.CompareTime(this, obj);

        #endregion

        #region IPhpCloneable

        object IPhpCloneable.Clone()
        {
            if (GetType() == typeof(DateTime))
            {
                // quick new instance
                return new DateTime(LocalTime, LocalTimeZone);
            }
            else
            {
                return Operators.CloneInPlace(MemberwiseClone());
            }
        }

        #endregion

        #region CLR Conversions

        /// <summary>
        /// Gets given PHP DateTime as <see cref="System_DateTime"/> (UTC).
        /// </summary>
        public static implicit operator System_DateTime(DateTime dt) => TimeZoneInfo.ConvertTimeToUtc(dt.LocalTime, dt.LocalTimeZone);

        #endregion
    }

    /// <summary>
    /// Representation of date and time.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(PhpExtensionAttribute.KnownExtensionNames.Date)]
    [DebuggerDisplay(nameof(DateTimeImmutable), Type = PhpVariable.TypeNameObject)]
    public class DateTimeImmutable : DateTimeInterface, IPhpComparable, IPhpCloneable
    {
        /// <summary>
        /// Get the date-time value, stored in UTC
        /// </summary>
        internal System_DateTime LocalTime { get; private set; }

        /// <summary>
        /// Get the time zone for this DateTime object
        /// </summary>
        internal TimeZoneInfo LocalTimeZone { get; private set; } = TimeZoneInfo.Utc;

        [PhpFieldsOnlyCtor]
        protected DateTimeImmutable(Context ctx)
        {
            LocalTimeZone = PhpTimeZone.GetCurrentTimeZone(ctx);
            LocalTime = TimeZoneInfo.ConvertTimeFromUtc(System_DateTime.UtcNow, LocalTimeZone);
        }

        internal DateTimeImmutable(System_DateTime datetime, TimeZoneInfo tz)
        {
            LocalTime = datetime;
            LocalTimeZone = tz;
        }

        // public __construct ([ string $time = "now" [, DateTimeZone $timezone = NULL ]] )
        public DateTimeImmutable(Context ctx, string? datetime = DateTime.s_Now, DateTimeZone? timezone = null)
        {
            __construct(ctx, datetime, timezone);
        }

        public void __construct(Context ctx, string? datetime = DateTime.s_Now, DateTimeZone? timezone = null)
        {
            DateTime.Parse(ctx, datetime ?? string.Empty, timezone?._timezone, out var localdate, out var localtz);

            //
            LocalTime = localdate;
            LocalTimeZone = localtz;
        }

        #region DateTimeInterface

        [return: CastToFalse]
        public string format(string format)
        {
            if (format == null)
            {
                //PhpException.ArgumentNull("format");
                //return false;
                throw new ArgumentNullException();
            }

            return DateTimeFunctions.FormatDate(format, LocalTime, LocalTimeZone);
        }

        public long getOffset()
        {
            return (long)LocalTimeZone.BaseUtcOffset.TotalSeconds;
        }

        public long getTimestamp()
        {
            return DateTimeUtils.UtcToUnixTimeStamp(TimeZoneInfo.ConvertTimeToUtc(LocalTime, LocalTimeZone));
        }

        public DateTimeImmutable setTimestamp(int unixtimestamp)
        {
            var utc = DateTimeUtils.UnixTimeStampToUtc(unixtimestamp);

            return new DateTimeImmutable(TimeZoneInfo.ConvertTimeFromUtc(utc, LocalTimeZone), LocalTimeZone);
        }

        public DateTimeZone getTimezone()
        {
            return new DateTimeZone(LocalTimeZone);
        }

        public virtual DateTimeImmutable modify(Context ctx, string modify)
        {
            if (string.IsNullOrEmpty(modify))
            {
                //PhpException.ArgumentNull("modify");
                //return false;

                // Warning: failed to parse string at position 0
                throw new ArgumentNullException();
            }

            DateTime.Parse(ctx, modify, LocalTimeZone, out var localdate, out var localtz);

            return new DateTimeImmutable(localdate, localtz);
        }

        public void __wakeup() { }

        #endregion

        public virtual DateTimeImmutable add(DateInterval interval) => new DateTimeImmutable(interval.Apply(LocalTime, negate: false), LocalTimeZone);

        public virtual DateTimeImmutable sub(DateInterval interval) => new DateTimeImmutable(interval.Apply(LocalTime, negate: true), LocalTimeZone);

        [return: CastToFalse]
        public static DateTimeImmutable? createFromFormat(Context ctx, string format, string? time, DateTimeZone? timezone = null)
        {
            // arguments

            // format: the time format string
            // time: given time value
            // timezone: timezone object or null, ignored when "time" specifies the timezone already

            var dateinfo = DateInfo.ParseFromFormat(format, time, out var errors);

            ctx.SetProperty<DateTimeErrors>(errors);

            if (errors != null && errors.HasErrors)
            {
                return null;
            }

            var localtz = timezone?._timezone ?? PhpTimeZone.GetCurrentTimeZone(ctx);
            var localdate = dateinfo.GetDateTime(System_DateTime.UtcNow, ref localtz);

            return new DateTimeImmutable(localdate, localtz);
        }

        public static DateTimeImmutable? createFromMutable(DateTime datetime)
        {
            if (datetime == null)
            {
                PhpException.ArgumentNull(nameof(datetime));
                return null;
            }
            else
            {
                return datetime.AsDateTimeImmutable();
            }
        }

        public static DateTimeImmutable createFromInterface(DateTimeInterface datetime)
        {
            return datetime switch
            {
                DateTimeImmutable immutable => immutable,
                DateTime dt => dt.AsDateTimeImmutable(),
                _ => throw new Spl.InvalidArgumentException(),
            };
        }

        public static PhpArray/*!*/getLastErrors(Context ctx) => DateTime.getLastErrors(ctx);

        public static DateTimeImmutable __set_state(PhpArray array) => throw new NotImplementedException();
        public virtual DateTimeImmutable setDate(int year, int month, int day) => throw new NotImplementedException();
        public virtual DateTimeImmutable setISODate(int year, int week, int day = 1) => throw new NotImplementedException();
        public virtual DateTimeImmutable setTime(int hour, int minute, int second = 0, int microseconds = 0) => throw new NotImplementedException();

        public virtual DateTimeImmutable setTimezone(DateTimeZone timezone)
        {
            if (timezone == null)
            {
                //PhpException.InvalidArgumentType(nameof(timezone), nameof(DateTimeZone));
                throw new Spl.TypeError(string.Format(Core.Resources.ErrResources.invalid_argument_type, nameof(timezone), nameof(DateTimeZone)));
            }

            var newtz = timezone._timezone;
            if (newtz != LocalTimeZone)
            {
                return new DateTimeImmutable(TimeZoneInfo.ConvertTime(LocalTime, LocalTimeZone, newtz), newtz);
            }
            else
            {
                return this;
            }
        }
        /// <summary>
        /// Returns the difference between two DateTime objects
        /// </summary>
        public virtual DateInterval diff(DateTimeInterface datetime2, bool absolute = false) => DateTimeFunctions.date_diff(this, datetime2, absolute);

        #region IPhpComparable

        int IPhpComparable.Compare(PhpValue obj) => DateTimeFunctions.CompareTime(this, obj);

        #endregion

        #region IPhpCloneable

        object IPhpCloneable.Clone()
        {
            if (GetType() == typeof(DateTimeImmutable))
            {
                // quick new instance
                return new DateTimeImmutable(LocalTime, LocalTimeZone);
            }
            else
            {
                return Operators.CloneInPlace(MemberwiseClone());
            }
        }

        #endregion
    }
}
