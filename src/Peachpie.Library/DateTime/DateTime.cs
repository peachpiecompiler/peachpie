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

    #region DateTimeValue

    /// <summary>
    /// Internal representation of PHP's `DateTime` object.
    /// Unlike the internal <see cref="DateInfo"/>, this is immutable and provides Date-Time operations.
    /// Represents local time, local time zone, allowing for years to be specified out of the .NET usual range (&lt;= 0, > 9999).
    /// </summary>
    readonly struct DateTimeValue
    {
        /// <summary>
        /// Time string <c>"now"</c> representing current date and time.
        /// Used as default value in constructors.
        /// </summary>
        public const string Now = "now";

        /// <summary>
        /// Get the date-time value, corresponding to the specified time zone.
        /// </summary>
        public System_DateTime LocalTime { get; }

        /// <summary>
        /// The year offset.
        /// Workarounds .NET DateTime limitation; the year offset is added to the <see cref="System_DateTime.Year"/>.
        /// </summary>
        public int YearOffset { get; }

        /// <summary>
        /// Get the time zone for this DateTime object
        /// </summary>
        public TimeZoneInfo LocalTimeZone { get; }

        /// <summary>
        /// Creates the value.
        /// </summary>
        /// <param name="datetime">Local date ad time.</param>
        /// <param name="yearoffset">Workarounds .NET DateTime limitation; the year offset is added to the <see cref="System_DateTime.Year"/></param>
        /// <param name="zone">Time zone in which the <paramref name="datetime"/> is relative to. If not specified, assuming <c>UTC</c>.</param>
        public DateTimeValue(System_DateTime datetime, int yearoffset, TimeZoneInfo? zone = null)
        {
            LocalTime = datetime;
            YearOffset = yearoffset;
            LocalTimeZone = zone ?? TimeZoneInfo.Utc;
        }

        public DateTimeValue(System_DateTime datetime, TimeZoneInfo? zone = null) : this(datetime, 0, zone) { }

        public static DateTimeValue CreateNow(Context ctx)
        {
            var tz = PhpTimeZone.GetCurrentTimeZone(ctx);
            var dt = TimeZoneInfo.ConvertTimeFromUtc(System_DateTime.UtcNow, tz);

            return new DateTimeValue(dt, tz);
        }

        public static bool TryCreateFromFormat(Context ctx, string format, string? time, DateTimeZone? timezone, out DateTimeValue value)
        {
            // arguments

            // format: the time format string
            // time: given time value
            // timezone: timezone object or null, ignored when "time" specifies the timezone already

            // TODO: year offset

            var dateinfo = DateInfo.ParseFromFormat(format, time, out var errors);

            ctx.SetProperty<DateTimeErrors>(errors);

            if (errors != null && errors.HasErrors)
            {
                value = default;
                return false;
            }

            var localtz = timezone?._timezone ?? PhpTimeZone.GetCurrentTimeZone(ctx);
            var localdate = dateinfo.GetDateTime(System_DateTime.UtcNow, ref localtz);

            value = new DateTimeValue(localdate, localtz);
            return true;
        }

        public static DateTimeValue CreateFromState(Context ctx, PhpArray array)
        {
            if (array == null || array.Count == 0)
            {
                return CreateNow(ctx);
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

                return new DateTimeValue(date, localtz);
            }
        }

        public System_DateTime GetUtcTime()
        {
            return TimeZoneInfo.ConvertTimeToUtc(LocalTime, LocalTimeZone).AddYears(YearOffset);
        }

        /// <summary>
        /// Parse the <paramref name="timestr"/> in PHP manner, updating the current <see cref="DateTimeErrors"/>.
        /// </summary>
        public static DateTimeValue Parse(Context ctx, string timestr, TimeZoneInfo? timezone)
        {
            Debug.Assert(ctx != null);
            Debug.Assert(timestr != null);

            //
            ctx!.SetProperty(DateTimeErrors.Empty);

            timestr = timestr != null ? timestr.Trim() : string.Empty;

            var localtz = timezone ?? PhpTimeZone.GetCurrentTimeZone(ctx);
            Debug.Assert(localtz != null);

            System_DateTime localdate;

            //
            if (timestr.Length == 0 || timestr.EqualsOrdinalIgnoreCase(DateTimeValue.Now))
            {
                // most common case
                localdate = TimeZoneInfo.ConvertTimeFromUtc(System_DateTime.UtcNow, localtz);
            }
            else
            {
                var result = DateInfo.Parse(timestr, System_DateTime.UtcNow, ref localtz, out var error);
                if (error != null)
                {
                    ctx.SetProperty<DateTimeErrors>(new DateTimeErrors { Errors = new[] { error } });
                    throw new Spl.Exception(error);
                }

                localdate = result;
            }

            //
            return new DateTimeValue(localdate, localtz);
        }

        public DateTimeValue ChangeTimeZone(TimeZoneInfo newtz)
        {
            if (newtz == null)
            {
                throw new ArgumentNullException(nameof(newtz));
            }

            if (newtz != LocalTimeZone)
            {
                var newtime = TimeZoneInfo.ConvertTime(LocalTime, LocalTimeZone, newtz);

                return new DateTimeValue(newtime, YearOffset, newtz);
            }
            else
            {
                return this;
            }
        }

        public DateTimeValue WithTime(int hour, int minute, int second, int microsecond)
        {
            return new DateTimeValue(
                new System_DateTime(LocalTime.Year, LocalTime.Month, LocalTime.Day, hour, minute, second, microsecond / 1000),
                YearOffset, LocalTimeZone);
        }

        public DateTimeValue WithTimestamp(long unixtimestamp)
        {
            var utc = DateTimeUtils.UnixTimeStampToUtc(unixtimestamp);
            return new DateTimeValue(
                TimeZoneInfo.ConvertTimeFromUtc(utc, LocalTimeZone),
                0, LocalTimeZone);
        }

        public DateTimeValue WithDate(int year, int month, int day)
        {
            var time = LocalTime;

            // TODO: year offset for year <= 0 || year > 9999

            return new DateTimeValue(
                new System_DateTime(year, month, day, time.Hour, time.Minute, time.Second, time.Millisecond),
                0, LocalTimeZone);
        }

        public DateTimeValue WithISODate(int year, int week, int day = 1)
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

            return new DateTimeValue(
                new System_DateTime(result.Year, result.Month, result.Day, time.Hour, time.Minute, time.Second, time.Millisecond),
                0, LocalTimeZone);
        }

        public long GetUnixTimestamp()
        {
            return DateTimeUtils.UtcToUnixTimeStamp(TimeZoneInfo.ConvertTimeToUtc(LocalTime, LocalTimeZone));
        }

        public string format(string format)
        {
            // TODO: YearOffset

            return DateTimeFunctions.FormatDate(format, LocalTime, LocalTimeZone);
        }

        public long GetOffset()
        {
            if (LocalTimeZone == null)
            {
                //return false;
                throw new InvalidOperationException();
            }

            // CONSIDER: (??) LocalTimeZone.IsDaylightSavingTime(datetime.Time) ? 3600 : 0

            return (long)LocalTimeZone.BaseUtcOffset.TotalSeconds;
        }
    }

    #endregion

    /// <summary>
    /// Representation of date and time.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(PhpExtensionAttribute.KnownExtensionNames.Date)]
    [DebuggerDisplay(nameof(DateTime), Type = PhpVariable.TypeNameObject)]
    public class DateTime : DateTimeInterface, IPhpComparable, IPhpCloneable
    {
        #region Fields

        internal DateTimeValue Value => _value;
        private protected DateTimeValue _value;

        /// <summary>
        /// Get the date-time value, stored in UTC
        /// </summary>
        internal System_DateTime LocalTime => _value.LocalTime;

        /// <summary>
        /// Get the time zone for this DateTime object
        /// </summary>
        internal TimeZoneInfo LocalTimeZone => _value.LocalTimeZone;

        // used for serialization:
        // note: we incorrectly show following in reflection as well.

        /// <summary>
        /// Allows to get or set local time in format <c>"yyyy-MM-dd HH:mm:ss.ffff"</c>.
        /// </summary>
        public string date
        {
            get => LocalTime.ToString("yyyy-MM-dd HH:mm:ss.ffff");
            set => _value = new DateTimeValue(System_DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal), LocalTimeZone);
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
            get => DateInfo.GetTimeLibZoneType(_value.LocalTimeZone);
            set { } // ignore, but allow unserialize of this value
        }

        /// <summary>
        /// Gets the timezone identifier.
        /// </summary>
        public string timezone
        {
            get
            {
                return _value.LocalTimeZone.Id;
            }
            set
            {
                _value = new DateTimeValue(_value.LocalTime, _value.YearOffset, PhpTimeZone.GetTimeZone(value) ?? throw new ArgumentException());
            }
        }

        #endregion

        #region Construction

        [PhpFieldsOnlyCtor]
        protected DateTime(Context ctx) : this(DateTimeValue.CreateNow(ctx)) { }

        internal DateTime(DateTimeValue value)
        {
            _value = value;
        }

        private DateTime(System_DateTime datetime, TimeZoneInfo timezone)
        {
            _value = new DateTimeValue(datetime, timezone ?? throw new ArgumentNullException(nameof(timezone)));
        }

        // public __construct ([ string $time = "now" [, DateTimeZone $timezone = NULL ]] )
        public DateTime(Context ctx, string? datetime = DateTimeValue.Now, DateTimeZone? timezone = null)
        {
            __construct(ctx, datetime, timezone);
        }

        // public __construct ([ string $time = "now" [, DateTimeZone $timezone = NULL ]] )
        public void __construct(Context ctx, string? datetime = DateTimeValue.Now, DateTimeZone? timezone = null)
        {
            _value = DateTimeValue.Parse(ctx, datetime ?? string.Empty, timezone?._timezone);
        }

        #endregion

        #region Helpers

        internal DateTimeImmutable AsDateTimeImmutable() => new DateTimeImmutable(_value);

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
        public static DateTime __set_state(Context ctx, PhpArray array) => new DateTime(DateTimeValue.CreateFromState(ctx, array));

        /// <summary>
        /// Adds an amount of days, months, years, hours, minutes and seconds to a DateTime object.
        /// </summary>
        /// <returns>Returns the <see cref="DateTime"/> object for method chaining or <c>FALSE</c> on failure.</returns>
        //[return: CastToFalse]
        public virtual DateTime add(DateInterval interval)
        {
            _value = interval.Apply(_value, negate: false);
            return this;
        }

        /// <summary>
        /// Subtracts an amount of days, months, years, hours, minutes and seconds from a DateTime object.
        /// </summary>
        //[return: CastToFalse]
        public virtual DateTime sub(DateInterval interval)
        {
            _value = interval.Apply(_value, negate: true);
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

            _value = _value.ChangeTimeZone(timezone._timezone);

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

            return _value.format(format);
        }

        public virtual long getOffset() => _value.GetOffset();

        public virtual DateTimeZone getTimezone() => new DateTimeZone(_value.LocalTimeZone);

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

            _value = DateTimeValue.Parse(ctx, modify, _value.LocalTimeZone);

            return this;
        }

        [return: CastToFalse]
        public static DateTime? createFromFormat(Context ctx, string format, string time, DateTimeZone? timezone = null)
        {
            if (DateTimeValue.TryCreateFromFormat(ctx, format, time, timezone, out var value))
            {
                return new DateTime(value);
            }
            else
            {
                return null;
            }
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

            return datetime.AsDateTime();
        }

        public static DateTime createFromInterface(DateTimeInterface datetime)
        {
            if (DateTimeFunctions.GetDateTimeFromInterface(datetime, out var value))
            {
                return new DateTime(value);
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
                _value = _value.WithDate(year, month, day);
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new ArgumentOutOfRangeException($"The date {year}-{month}-{day} is not valid.", e); // TODO: resouces
            }

            return this;
        }

        public virtual DateTime setISODate(int year, int week, int day = 1)
        {
            _value = _value.WithISODate(year, week, day);
            return this;
        }

        public virtual DateTime setTime(int hour, int minute, int second = 0, int microsecond = 0)
        {
            try
            {
                _value = _value.WithTime(hour, minute, second, microsecond);
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new ArgumentOutOfRangeException($"The time {hour}:{minute}:{second} is not valid.", e); // TODO: resources
            }

            return this;
        }

        public virtual long getTimestamp() => _value.GetUnixTimestamp();

        /// <summary>
        /// Sets the date and time based on an Unix timestamp.
        /// </summary>
        /// <returns>Returns the <see cref="DateTime"/> object for method chaining.</returns>
        public DateTime setTimestamp(long unixtimestamp)
        {
            _value = _value.WithTimestamp(unixtimestamp);
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
        public static implicit operator System_DateTime(DateTime dt) => dt.Value.GetUtcTime();

        #endregion
    }

    /// <summary>
    /// Representation of date and time.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(PhpExtensionAttribute.KnownExtensionNames.Date)]
    [DebuggerDisplay(nameof(DateTimeImmutable), Type = PhpVariable.TypeNameObject)]
    public class DateTimeImmutable : DateTimeInterface, IPhpComparable, IPhpCloneable
    {
        internal DateTimeValue Value => _value;
        private protected DateTimeValue _value;

        [PhpFieldsOnlyCtor]
        protected DateTimeImmutable(Context ctx)
        {
            _value = DateTimeValue.CreateNow(ctx);
        }

        internal DateTimeImmutable(DateTimeValue value)
        {
            _value = value;
        }

        internal DateTimeImmutable(System_DateTime datetime, TimeZoneInfo tz)
        {
            _value = new DateTimeValue(datetime, tz);
        }

        // public __construct ([ string $time = "now" [, DateTimeZone $timezone = NULL ]] )
        public DateTimeImmutable(Context ctx, string? datetime = DateTimeValue.Now, DateTimeZone? timezone = null)
        {
            __construct(ctx, datetime, timezone);
        }

        public void __construct(Context ctx, string? datetime = DateTimeValue.Now, DateTimeZone? timezone = null)
        {
            _value = DateTimeValue.Parse(ctx, datetime ?? string.Empty, timezone?._timezone);
        }

        #region Helpers

        internal DateTime AsDateTime() => new DateTime(_value);

        #endregion

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

            return _value.format(format);
        }

        public long getOffset() => _value.GetOffset();

        public long getTimestamp() => _value.GetUnixTimestamp();

        public DateTimeImmutable setTimestamp(int unixtimestamp) => new DateTimeImmutable(_value.WithTimestamp(unixtimestamp));

        public DateTimeZone getTimezone() => new DateTimeZone(_value.LocalTimeZone);

        public virtual DateTimeImmutable modify(Context ctx, string modify)
        {
            if (string.IsNullOrEmpty(modify))
            {
                //PhpException.ArgumentNull("modify");
                //return false;

                // Warning: failed to parse string at position 0
                throw new ArgumentNullException();
            }

            return new DateTimeImmutable(DateTimeValue.Parse(ctx, modify, _value.LocalTimeZone));
        }

        public void __wakeup() { }

        #endregion

        public virtual DateTimeImmutable add(DateInterval interval) => new DateTimeImmutable(interval.Apply(_value, negate: false));

        public virtual DateTimeImmutable sub(DateInterval interval) => new DateTimeImmutable(interval.Apply(_value, negate: true));

        [return: CastToFalse]
        public static DateTimeImmutable? createFromFormat(Context ctx, string format, string? time, DateTimeZone? timezone = null)
        {
            if (DateTimeValue.TryCreateFromFormat(ctx, format, time, timezone, out var value))
            {
                return new DateTimeImmutable(value);
            }
            else
            {
                return null;
            }
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

        public static DateTimeImmutable __set_state(Context ctx, PhpArray array) => new DateTimeImmutable(DateTimeValue.CreateFromState(ctx, array));
        public virtual DateTimeImmutable setDate(int year, int month, int day) => new DateTimeImmutable(_value.WithDate(year, month, day));
        public virtual DateTimeImmutable setISODate(int year, int week, int day = 1) => new DateTimeImmutable(_value.WithISODate(year, week, day));
        public virtual DateTimeImmutable setTime(int hour, int minute, int second = 0, int microsecond = 0) => new DateTimeImmutable(_value.WithTime(hour, minute, second, microsecond));

        public virtual DateTimeImmutable setTimezone(DateTimeZone timezone)
        {
            if (timezone == null)
            {
                //PhpException.InvalidArgumentType(nameof(timezone), nameof(DateTimeZone));
                throw new Spl.TypeError(string.Format(Core.Resources.ErrResources.invalid_argument_type, nameof(timezone), nameof(DateTimeZone)));
            }

            return new DateTimeImmutable(_value.ChangeTimeZone(timezone._timezone));
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
                return new DateTimeImmutable(_value);
            }
            else
            {
                return Operators.CloneInPlace(MemberwiseClone());
            }
        }

        #endregion
    }
}
