using Pchp.Core;
using Pchp.Library;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Library.DateTime
{
    /// <summary>
    /// Representation of time zone.
    /// </summary>
    [PhpType("DateTimeZone")]
    public class DateTimeZone
    {
        internal TimeZoneInfo timezone;
        protected readonly Context _ctx;

        #region Constants

        /* Constants */
        public const int AFRICA = 1;
        public const int AMERICA = 2;
        public const int ANTARCTICA = 4;
        public const int ARCTIC = 8;
        public const int ASIA = 16;
        public const int ATLANTIC = 32;
        public const int AUSTRALIA = 64;
        public const int EUROPE = 128;
        public const int INDIAN = 256;
        public const int PACIFIC = 512;
        public const int UTC = 1024;
        public const int ALL = 2047;
        public const int ALL_WITH_BC = 4095;
        public const int PER_COUNTRY = 4096;

        #endregion

        private DateTimeZone(Context ctx)
        {
            Debug.Assert(ctx != null);
            _ctx = ctx;
        }

        internal DateTimeZone(Context ctx, TimeZoneInfo resolvedTimeZone)
            : this(ctx)
        {
            Debug.Assert(resolvedTimeZone != null);
            timezone = resolvedTimeZone;
        }

        public DateTimeZone(Context ctx, string timezone_name)
            : this(ctx)
        {
            __construct(timezone_name);
        }

        // public __construct ( string $timezone )
        public void __construct(string timezone_name)
        {
            if (timezone_name != null)
            {
                this.timezone = PhpTimeZone.GetTimeZone(timezone_name);

                if (this.timezone == null)
                {
                    //PhpException.Throw(PhpError.Notice, LibResources.GetString("unknown_timezone", zoneName));
                    throw new ArgumentException();
                }
            }
            else
            {
                this.timezone = PhpTimeZone.GetCurrentTimeZone(_ctx);
            }
        }

        #region Methods

        //public array getLocation ( void )
        public virtual PhpArray getLocation()
        {
            throw new NotImplementedException();
        }

        //public string getName ( void )
        public virtual string getName()
        {
            //return (timezone != null) ? timezone.Id : null;
            throw new NotImplementedException();
        }

        //public int getOffset ( DateTime $datetime )
        public virtual int getOffset(Library.DateTime.DateTime datetime)
        {
            if (timezone == null)
                //return false;
                throw new InvalidOperationException();

            if (datetime == null)
            {
                //PhpException.ArgumentNull("datetime");
                //return false;
                throw new ArgumentNullException();
            }

            return (int)timezone.BaseUtcOffset.TotalSeconds + (timezone.IsDaylightSavingTime(datetime.Time) ? 3600 : 0);
        }

        //public array getTransitions ([ int $timestamp_begin [, int $timestamp_end ]] )
        public PhpArray getTransitions()
        {
            throw new NotImplementedException();
        }

        public PhpArray getTransitions(int timestamp_begin, int timestamp_end)
        {
            // TODO: timestamp_begin, timestamp_end

            //var rules = this.timezone.GetAdjustmentRules();
            //var array = new PhpArray(rules.Length);

            ////var now = DateTime.UtcNow;
            //for (int i = 0; i < rules.Length; i++)
            //{
            //    var rule = rules[i];

            //    // TODO: timezone transitions
            //    //if (rule.DateStart > now || rule.DateEnd < now) continue;
            //    //var transition = new PhpArray(5);
            //    //transition["ts"] = (int)(new DateTime(now.Year, rule.DaylightTransitionStart.Month, rule.DaylightTransitionStart.Day) - DateTimeUtils.UtcStartOfUnixEpoch).TotalSeconds;
            //    ////transition["time"] = ;
            //    ////transition["offset"] = ;
            //    //transition["isdst"] = 1;
            //    ////transition["abbr"] = ;

            //    //array.Add(transition);
            //}

            //return array;

            throw new NotImplementedException();
        }

        //public static array listAbbreviations ( void )
        public PhpArray listAbbreviations()
        {
            throw new NotImplementedException();
        }

        //public static array listIdentifiers ([ int $what = DateTimeZone::ALL [, string $country = NULL ]] )
        public static PhpArray listIdentifiers(Context/*!*/context, int what = DateTimeZone.ALL, string country = null)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
