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
    [PhpType("DateTime")]
    public class DateTime
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

        // public __construct ([ string $time = "now" [, DateTimeZone $timezone = NULL ]] )
        public DateTime(Context ctx) : this(ctx, "now", null) { }

        public DateTime(Context ctx, string time) : this(ctx, time, null) { }

        public DateTime(Context ctx, string time, DateTimeZone timezone)
        {
            Debug.Assert(ctx != null);

            _ctx = ctx;

            if (timezone == null)
            {
                TimeZone = PhpTimeZone.GetCurrentTimeZone(ctx);
            }
            else
            {
                //var datetimezone = timezone as DateTimeZone;
                //if (datetimezone == null)
                //{
                //    PhpException.InvalidArgumentType("timezone", "DateTimeZone");
                //    TimeZone = PhpTimeZone.CurrentTimeZone;
                //}
                //else
                {
                    TimeZone = timezone.timezone;
                }
            }

            if (TimeZone == null)
            {
                //PhpException.InvalidArgument("timezone");
                //return null;
                throw new ArgumentException();
            }

            this.Time = StrToTime(ctx, time, System_DateTime.UtcNow);

            //this.date.Value = this.Time.ToString("yyyy-mm-dd HH:mm:ss");
            //this.timezone_type.Value = 3;
            //this.timezone.Value = TimeZone.Id;
        }

        #endregion

        #region Methods

        static System_DateTime StrToTime(Context ctx, string timestr, System_DateTime time)
        {
            if (string.IsNullOrEmpty(timestr) || timestr.Equals("now", StringComparison.OrdinalIgnoreCase))
            {
                return System_DateTime.UtcNow;
            }

            var result = DateTimeFunctions.strtotime(ctx, timestr, DateTimeUtils.UtcToUnixTimeStamp(time));
            return (result >= 0) ? DateTimeUtils.UnixTimeStampToUtc(result) : System_DateTime.UtcNow;
        }

        public virtual DateTime setTimeZone(DateTimeZone timezone)
        {
            if (timezone == null)
            {
                //PhpException.ArgumentNull(nameof(timezone));
                //return false;
                throw new ArgumentNullException();
            }

            this.TimeZone = timezone.timezone;

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

        public virtual int getOffset()
        {
            if (this.TimeZone == null)
                //return false;
                throw new InvalidOperationException();

            return (int)this.TimeZone.BaseUtcOffset.TotalSeconds;
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
            var tz = (timezone != null) ? timezone.timezone : PhpTimeZone.GetCurrentTimeZone(ctx);

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

        // TODO: IPhpComparable
        //public override int CompareTo(object obj, System.Collections.IComparer comparer)
        //{
        //    var other = obj as __PHP__DateTime;
        //    return other != null
        //        ? Time.CompareTo(other.Time)
        //        : base.CompareTo(obj, comparer);
        //}

        #endregion
    }
}
