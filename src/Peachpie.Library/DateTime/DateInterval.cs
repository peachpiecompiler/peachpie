using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Peachpie.Library.DateTime
{
    /// <summary>
    /// Represents a date interval.
    /// A date interval stores either a fixed amount of time(in years, months, days, hours etc) or a relative time string in the format that DateTime's constructor supports.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
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

        public DateInterval(string interval_spec)
        {
            __construct(interval_spec);
        }

        public void __construct(string interval_spec)
        {
            var ts = System.Xml.XmlConvert.ToTimeSpan(interval_spec);

            f = ts.Milliseconds * 0.001;
            s = ts.Seconds;
            i = ts.Minutes;
            h = ts.Hours;
            d = ts.Days;
        }

        public static DateInterval createFromDateString(string time) { throw new NotImplementedException(); }

        public virtual string format(string format) { throw new NotImplementedException(); }
    }
}
