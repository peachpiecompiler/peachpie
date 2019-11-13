using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Pchp.Core;
using Pchp.Library.Resources;

namespace Pchp.Library.DateTime
{
    /// <summary>
    /// Represents a date interval.
    /// A date interval stores either a fixed amount of time(in years, months, days, hours etc) or a relative time string in the format that DateTime's constructor supports.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    [DebuggerDisplay(nameof(DateInterval), Type = PhpVariable.TypeNameObject)]
    public class DateInterval
    {
        public int y;
        public int m;
        public int d;
        public int h;
        public int i;
        public int s;
        public double f;
        public int invert;
        public PhpValue days = PhpValue.False;

        //private TimeSpan _span;

        internal TimeSpan AsTimeSpan()
        {
            var ts = new TimeSpan(d, h, i, s, (int)(f * 1000.0));

            if (invert != 0)
            {
                ts = ts.Negate();
            }

            return ts;
        }

        [PhpFieldsOnlyCtor]
        protected DateInterval() { }

        internal DateInterval(TimeSpan ts)
        {
            FromTimeSpan(ts);
        }

        void FromTimeSpan(TimeSpan ts)
        {
            f = ts.Milliseconds * 0.001;
            s = ts.Seconds;
            i = ts.Minutes;
            h = ts.Hours;
            d = ts.Days;    // contains also months and years
            // m
            // y

            invert = ts.Ticks >= 0 ? 0 : 1;
        }

        public DateInterval(string interval_spec)
        {
            __construct(interval_spec);
        }

        public void __construct(string interval_spec)
        {
            //var ts = System.Xml.XmlConvert.ToTimeSpan(interval_spec);
            if (DateInfo.TryParseIso8601Duration(interval_spec, out var ts))
            {
                FromTimeSpan(ts);
            }
            else
            {
                throw new ArgumentException(nameof(interval_spec));
            }
        }

        public static DateInterval createFromDateString(string time)
        {
            var result = new DateInterval();

            var scanner = new Scanner(new StringReader(time.ToLowerInvariant()));
            while (true)
            {
                Tokens token = scanner.GetNextToken();
                if (token == Tokens.ERROR || scanner.Errors > 0)
                {
                    break;
                }

                if (token == Tokens.EOF)
                {
                    break;
                }
            }

            //
            var ts = scanner.Time;

            result.f = ts.f > 0 ? ts.f : 0;

            result.s = ts.s > 0 ? ts.s : 0;
            result.i = ts.i > 0 ? ts.i : 0;
            result.h = ts.h > 0 ? ts.h : 0;

            result.d = ts.d > 0 ? ts.d : 0;
            result.m = ts.m > 0 ? ts.m : 0;
            result.y = ts.y > 0 ? ts.y : 0;

            //
            return result;
        }

        public virtual string format(string format) { throw new NotImplementedException(); }
    }
}
