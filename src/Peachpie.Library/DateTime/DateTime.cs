using Pchp.Core;
using Pchp.Library;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System_DateTime = System.DateTime;

namespace Pchp.Library.DateTime
{
    /// <summary>
    /// Representation of date and time.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class DateTime : DateTimeInterface, IPhpComparable
    {
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
        public const string RSS = DateTimeFunctions.DATE_RSS;// @"D, d M Y H:i:s O";
        public const string W3C = DateTimeFunctions.DATE_W3C;// @"Y-m-d\TH:i:sP";

        #endregion

        #region Fields

        // dont see what these are for, no fields/props on php DateTime obj?
        //public PhpReference date = new PhpSmartReference();
        //public PhpReference timezone_type = new PhpSmartReference();
        //public PhpReference timezone = new PhpSmartReference();

        readonly protected Context _ctx;

        /// <summary>
        /// Get the date-time value, stored in UTC
        /// </summary>
        internal System_DateTime Time { get; private set; }

        /// <summary>
        /// Get the time zone for this DateTime object
        /// </summary>
        internal TimeZoneInfo TimeZone { get; private set; }

        #endregion

        #region Construction

        [PhpFieldsOnlyCtor]
        protected DateTime(Context ctx)
        {
            Debug.Assert(ctx != null);

            _ctx = ctx;

            this.Time = System_DateTime.UtcNow;
            this.TimeZone = PhpTimeZone.GetCurrentTimeZone(ctx);
        }

        // public __construct ([ string $time = "now" [, DateTimeZone $timezone = NULL ]] )
        public DateTime(Context ctx, string time = null, DateTimeZone timezone = null)
            : this(ctx)
        {
            __construct(time, timezone);
        }

        // public __construct ([ string $time = "now" [, DateTimeZone $timezone = NULL ]] )
        public void __construct(string time = null, DateTimeZone timezone = null)
        {
            if (timezone != null)
            {
                this.TimeZone = timezone._timezone;
            }

            if (this.TimeZone == null)
            {
                //PhpException.InvalidArgument("timezone");
                //return null;
                throw new ArgumentException();
            }

            this.Time = StrToTime(_ctx, time, System_DateTime.UtcNow);

            //this.date.Value = this.Time.ToString("yyyy-mm-dd HH:mm:ss");
            //this.timezone_type.Value = 3;
            //this.timezone.Value = TimeZone.Id;
        }

        #endregion

        #region Methods

        internal static System_DateTime StrToTime(Context ctx, string timestr, System_DateTime time)
        {
            if (string.IsNullOrEmpty(timestr) || timestr.EqualsOrdinalIgnoreCase("now"))
            {
                return System_DateTime.UtcNow;
            }

            var result = DateTimeFunctions.strtotime(ctx, timestr, DateTimeUtils.UtcToUnixTimeStamp(time));
            return (result >= 0) ? DateTimeUtils.UnixTimeStampToUtc(result) : System_DateTime.UtcNow;
        }

        /// <summary>
        /// Adds an amount of days, months, years, hours, minutes and seconds to a DateTime object.
        /// </summary>
        /// <returns>Returns the <see cref="DateTime"/> object for method chaining or <c>FALSE</c> on failure.</returns>
        //[return: CastToFalse]
        public virtual DateTime add(DateInterval interval)
        {
            Time = Time.Add(interval.AsTimeSpan());

            return this;
        }

        /// <summary>
        /// Subtracts an amount of days, months, years, hours, minutes and seconds from a DateTime object.
        /// </summary>
        //[return: CastToFalse]
        public virtual DateTime sub(DateInterval interval)
        {
            Time = Time.Subtract(interval.AsTimeSpan());
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
                //PhpException.ArgumentNull(nameof(timezone));
                //return false;
                throw new ArgumentNullException();
            }

            if (timezone._timezone != this.TimeZone)
            {
                // convert this.Time from old TZ to new TZ
                this.Time = TimeZoneInfo.ConvertTime(new System_DateTime(this.Time.Ticks, DateTimeKind.Unspecified), this.TimeZone, timezone._timezone);
                this.TimeZone = timezone._timezone;
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

            return DateTimeFunctions.FormatDate(format, this.Time, this.TimeZone);
        }

        public virtual long getOffset()
        {
            if (this.TimeZone == null)
                //return false;
                throw new InvalidOperationException();

            return (long)this.TimeZone.BaseUtcOffset.TotalSeconds;
        }

        public virtual DateTimeZone getTimezone()
        {
            return new DateTimeZone(this.TimeZone);
        }

        public virtual DateTime modify(string modify)
        {
            if (modify == null)
            {
                //PhpException.ArgumentNull("modify");
                //return false;
                throw new ArgumentNullException();
            }

            this.Time = StrToTime(_ctx, modify, Time);

            return this;
        }

        public static DateTime createFromFormat(Context ctx, string format, string time, DateTimeZone timezone = null)
        {
            // arguments
            var tz = (timezone != null) ? timezone._timezone : PhpTimeZone.GetCurrentTimeZone(ctx);

            if (format == null)
            {
                //PhpException.InvalidArgument("format");
                //return false;
                throw new ArgumentNullException();
            }

            if (time == null)
            {
                //PhpException.InvalidArgument("time");
                //return false;
                throw new ArgumentNullException();
            }

            // create DateTime from format+time
            int i = 0;  // position in <timestr>
            foreach (var c in format)
            {
                switch (c)
                {
                    //case 'd':
                    //case 'j':
                    //    var day = PHP.Library.StrToTime.DateInfo.ParseUnsignedInt(timestr, ref i, 2);
                    //    // ... use day
                    //    break;
                    //case 'F':
                    //case 'M':
                    //    // parse  ...
                    //    break;
                    default:
                        if (i < time.Length && time[i] == c)
                        {
                            // match
                            i++;
                        }
                        else
                        {
                            // not match
                            //PhpException.InvalidArgument("time");   // time not matching format
                            //return false;
                            throw new ArgumentException();
                        }
                        break;
                }
            }

            if (i < time.Length)
            {
                //PhpException.InvalidArgument("time");   // time not matching format
                //return false;
                throw new ArgumentException();
            }

            ////
            //return new __PHP__DateTime(context, true)
            //{
            //     //Time = new DateTime(year, month, day, hour, minute, second, millisecond),
            //     TimeZone = tz,
            //};

            throw new NotImplementedException();
        }

        public virtual DateTime setDate(int year, int month, int day)
        {
            try
            {
                var time = TimeZoneInfo.ConvertTime(Time, TimeZone);
                this.Time = TimeZoneInfo.ConvertTime(
                    new System_DateTime(
                        year, month, day,
                        time.Hour, time.Minute, time.Second,
                        time.Millisecond
                    ),
                    TimeZone
                );
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new ArgumentOutOfRangeException(string.Format("The date {0}-{1}-{2} is not valid.", year, month, day), e);
            }

            return this;
        }

        public virtual DateTime setTime(int hour, int minute, int second)
        {
            try
            {
                var time = TimeZoneInfo.ConvertTime(Time, TimeZone);
                this.Time = TimeZoneInfo.ConvertTime(
                    new System_DateTime(time.Year, time.Month, time.Day, hour, minute, second),
                    TimeZone
                );
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new ArgumentOutOfRangeException(string.Format("The time {0}:{1}:{2} is not valid.", hour, minute, second), e);
            }


            return this;
        }

        public virtual long getTimestamp()
        {
            return DateTimeUtils.UtcToUnixTimeStamp(Time);
        }

        public virtual void __wakeup()
        {

        }

        #endregion

        #region IPhpComparable

        int IPhpComparable.Compare(PhpValue obj) => DateTimeFunctions.CompareTime(this.Time, obj);

        #endregion
    }

    /// <summary>
    /// Representation of date and time.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class DateTimeImmutable : DateTimeInterface, IPhpComparable
    {
        /// <summary>
        /// Get the date-time value, stored in UTC
        /// </summary>
        internal System_DateTime Time { get; private set; }

        /// <summary>
        /// Get the time zone for this DateTime object
        /// </summary>
        internal TimeZoneInfo TimeZone { get; private set; }

        internal DateTimeImmutable(System_DateTime time, TimeZoneInfo tz)
        {
            this.Time = time;
            this.TimeZone = tz;
        }

        // public __construct ([ string $time = "now" [, DateTimeZone $timezone = NULL ]] )
        public DateTimeImmutable(Context ctx, string time = null, DateTimeZone timezone = null)
        {
            Debug.Assert(ctx != null);

            this.TimeZone = (timezone == null)
                ? PhpTimeZone.GetCurrentTimeZone(ctx)
                : timezone._timezone;

            if (TimeZone == null)
            {
                //PhpException.InvalidArgument("timezone");
                //return null;
                throw new ArgumentException();
            }

            this.Time = DateTime.StrToTime(ctx, time, System_DateTime.UtcNow);

            //this.date.Value = this.Time.ToString("yyyy-mm-dd HH:mm:ss");
            //this.timezone_type.Value = 3;
            //this.timezone.Value = TimeZone.Id;
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

            return DateTimeFunctions.FormatDate(format, this.Time, this.TimeZone);
        }

        public long getOffset()
        {
            if (this.TimeZone == null)
                //return false;
                throw new InvalidOperationException();

            return (long)this.TimeZone.BaseUtcOffset.TotalSeconds;
        }

        public long getTimestamp()
        {
            return DateTimeUtils.UtcToUnixTimeStamp(Time);
        }

        public DateTimeZone getTimezone()
        {
            return new DateTimeZone(this.TimeZone);
        }

        public void __wakeup() { }

        #endregion

        public virtual DateTimeImmutable add(DateInterval interval) => new DateTimeImmutable(Time.Add(interval.AsTimeSpan()), TimeZone);
        public static DateTimeImmutable createFromFormat(string format, string time, DateTimeZone timezone = null) => throw new NotImplementedException();
        public static DateTimeImmutable createFromMutable(DateTime datetime) => throw new NotImplementedException();
        public static PhpArray getLastErrors() => throw new NotImplementedException();
        public virtual DateTimeImmutable modify(string modify) => throw new NotImplementedException();
        public static DateTimeImmutable __set_state(PhpArray array) => throw new NotImplementedException();
        public virtual DateTimeImmutable setDate(int year, int month, int day) => throw new NotImplementedException();
        public virtual DateTimeImmutable setISODate(int year, int week, int day = 1) => throw new NotImplementedException();
        public virtual DateTimeImmutable setTime(int hour, int minute, int second = 0, int microseconds = 0) => throw new NotImplementedException();
        public virtual DateTimeImmutable setTimestamp(int unixtimestamp) => throw new NotImplementedException();
        public virtual DateTimeImmutable setTimezone(DateTimeZone timezone) => throw new NotImplementedException();
        public virtual DateTimeImmutable sub(DateInterval interval) => new DateTimeImmutable(Time.Subtract(interval.AsTimeSpan()), TimeZone);

        /// <summary>
        /// Returns the difference between two DateTime objects
        /// </summary>
        public virtual DateInterval diff(DateTimeInterface datetime2, bool absolute = false) => DateTimeFunctions.date_diff(this, datetime2, absolute);

        #region IPhpComparable

        int IPhpComparable.Compare(PhpValue obj) => DateTimeFunctions.CompareTime(this.Time, obj);

        #endregion
    }
}
