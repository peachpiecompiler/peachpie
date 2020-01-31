using Pchp.Core;
using Pchp.Library;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pchp.Library.DateTime
{
    /// <summary>
    /// Representation of time zone.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("date")]
    public class DateTimeZone : IPhpCloneable
    {
        internal TimeZoneInfo _timezone;

        /// <summary>The timezone name if it differs from <see cref="TimeZoneInfo.Id"/>.</summary>
        private protected string _name;

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

        internal DateTimeZone(TimeZoneInfo resolvedTimeZone)
        {
            _timezone = resolvedTimeZone ?? throw new ArgumentNullException();
        }

        [PhpFieldsOnlyCtor]
        protected DateTimeZone()
        {
            // empty, __construct to be called by implementor
        }

        public DateTimeZone(Context ctx, string timezone_name)
        {
            __construct(ctx, timezone_name);
        }

        // public __construct ( string $timezone )
        public void __construct(Context ctx, string timezone_name)
        {
            if (timezone_name == null)
            {
                _timezone = PhpTimeZone.GetCurrentTimeZone(ctx);
            }
            else
            {
                var tz = PhpTimeZone.GetTimeZone(timezone_name);
                if (tz == null)
                {
                    PhpException.Throw(PhpError.Notice, Resources.LibResources.unknown_timezone, timezone_name);
                    throw new Spl.InvalidArgumentException();
                }

                //
                _timezone = tz;
                _name = timezone_name;
            }
        }

        object IPhpCloneable.Clone()
        {
            if (GetType() == typeof(DateTimeZone))
            {
                // quick new instance
                return new DateTimeZone(_timezone) { _name = _name, };
            }
            else
            {
                // 
                return Operators.CloneInPlace(MemberwiseClone());
            }
        }

        #region Methods

        /// <summary>
        /// Returns location information for a timezone, including country code, latitude/longitude and comments.
        /// </summary>
        [return: CastToFalse]
        public virtual PhpArray getLocation()
        {
            //Array
            //(
            //    [country_code] => CZ
            //    [latitude] => 50.08333
            //    [longitude] => 14.43333
            //    [comments] => 
            //)

            //if (_timezone != null)
            //{
            //    return new PhpArray(4)
            //    {
            //        { .., .. }
            //    }
            //}
            //else
            //{
            //    return null;
            //}

            throw new NotImplementedException();
        }

        //public string getName ( void )
        public virtual string getName()
        {
            return _name ?? _timezone.Id;
        }

        //public int getOffset ( DateTime $datetime )
        public virtual int getOffset(Library.DateTime.DateTime datetime)
        {
            if (_timezone == null)
                //return false;
                throw new InvalidOperationException();

            if (datetime == null)
            {
                //PhpException.ArgumentNull("datetime");
                //return false;
                throw new ArgumentNullException();
            }

            return (int)_timezone.BaseUtcOffset.TotalSeconds + (_timezone.IsDaylightSavingTime(datetime.Time) ? 3600 : 0);
        }

        //public array getTransitions ([ int $timestamp_begin [, int $timestamp_end ]] )
        [return: CastToFalse]
        public PhpArray getTransitions(long timestamp_begin = Environment.PHP_INT_MIN, long timestamp_end = Environment.PHP_INT_MAX)
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

            PhpException.FunctionNotSupported(nameof(getTransitions));

            return null;
        }

        /// <summary>
        /// Returns associative array containing dst, offset and the timezone name.
        /// </summary>
        public static PhpArray listAbbreviations() => PhpTimeZone.timezone_abbreviations_list();

        //public static array listIdentifiers ([ int $what = DateTimeZone::ALL [, string $country = NULL ]] )
        public static PhpArray listIdentifiers(int what = DateTimeZone.ALL, string country = null) => PhpTimeZone.timezone_identifiers_list(what, country);

        #endregion
    }
}
