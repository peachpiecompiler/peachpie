using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("date")]
    [DebuggerDisplay(nameof(DateInterval), Type = PhpVariable.TypeNameObject)]
    public class DateInterval
    {
        /// <summary>
        /// Years.
        /// </summary>
        public int y;

        /// <summary>
        /// Months.
        /// </summary>
        public int m;

        public int d
        {
            get => _span.Days;
            set => _span = new TimeSpan(value, _span.Hours, _span.Minutes, _span.Seconds, _span.Milliseconds);
        }

        public int h
        {
            get => _span.Hours;
            set => _span = new TimeSpan(_span.Days, value, _span.Minutes, _span.Seconds, _span.Milliseconds);
        }

        public int i
        {
            get => _span.Minutes;
            set => _span = new TimeSpan(_span.Days, _span.Hours, value, _span.Seconds, _span.Milliseconds);
        }

        public int s
        {
            get => _span.Seconds;
            set => _span = new TimeSpan(_span.Days, _span.Hours, _span.Minutes, value, _span.Milliseconds);
        }

        public double f
        {
            get => _span.Milliseconds * .001;
            set => _span = new TimeSpan(_span.Days, _span.Hours, _span.Minutes, _span.Seconds, (int)(value * 1000));
        }

        public int invert;

        // additional properties

        /// <summary>
        /// </summary>
        /// <remarks>
        /// If the <see cref="DateInterval"/> object was created by <see cref="DateTime.diff"/>,
        /// then this is the total number of days between the start and end dates.
        /// </remarks>
        public int days { get; set; }

        /// <summary>
        /// Internal <see cref="TimeSpan"/> representing days, hours, minutes, seconds and milliseconds.
        /// </summary>
        private protected TimeSpan _span;

        [PhpFieldsOnlyCtor]
        protected DateInterval() { }

        internal DateInterval(DateInfo ts, bool negative) => Initialize(ts, negative);

        internal DateInterval(TimeSpan ts) => Initialize(ts);

        internal DateInterval(System.DateTime date1, System.DateTime date2)
        {
            // TODO: preserve difference of years and months,
            // now {y} and {m} are always '0'
            Initialize(date2 - date1);
        }

        internal bool IsZero
        {
            get
            {
                return m == 0 && y == 0 && _span.Ticks == 0;
            }
        }

        internal System.DateTime Apply(System.DateTime datetime, bool negate)
        {
            var span = _span;
            var months = y * 12 + m;

            var invert = this.invert != 0 ^ negate;
            if (invert)
            {
                // substract
                span = span.Negate();
                months = -months;   // ignoring possible OF
            }

            if (months != 0)
            {
                datetime = datetime.AddMonths(months);
            }

            datetime = datetime.Add(span);

            //
            return datetime;
        }

        private protected void Initialize(DateInfo ts, bool negative)
        {
            Initialize(new TimeSpan(
                ts.d > 0 ? ts.d : 0,
                ts.h > 0 ? ts.h : 0,
                ts.i > 0 ? ts.i : 0,
                ts.s > 0 ? ts.s : 0,
                ts.f > 0 ? (int)(ts.f * 1000.0) : 0
            ));

            m = ts.m > 0 ? ts.m : 0;
            y = ts.y > 0 ? ts.y : 0;

            invert = negative ? 1 : 0;
        }

        private protected void Initialize(TimeSpan ts)
        {
            _span = ts.Duration(); // absolutize the range
            days = (int)ts.TotalDays;
            invert = ts.Ticks < 0 ? 1 : 0;
        }

        public DateInterval(string interval_spec)
        {
            __construct(interval_spec);
        }

        public void __construct(string interval_spec)
        {
            //var ts = System.Xml.XmlConvert.ToTimeSpan(interval_spec);
            if (DateInfo.TryParseIso8601Duration(interval_spec, out var ts, out var negative))
            {
                Initialize(ts, negative);
            }
            else
            {
                throw new ArgumentException(nameof(interval_spec));
            }
        }

        public static DateInterval createFromDateString(string time)
        {
            var scanner = new Scanner(new StringReader(time.ToLowerInvariant()));
            while (true)
            {
                var token = scanner.GetNextToken();
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
            return new DateInterval(scanner.Time, negative: false);
        }

        [return: NotNull]
        public virtual string format(string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                return string.Empty;
            }

            // here we are creating output string
            bool percent = false;
            var result = StringBuilderUtilities.Pool.Get();

            foreach (char ch in format)
            {
                if (percent)
                {
                    percent = false;

                    switch (ch)
                    {
                        // % Literal %   %
                        case '%':
                            result.Append('%');
                            break;

                        //Y   Years, numeric, at least 2 digits with leading 0    01, 03
                        case 'Y':
                            result.Append(y.ToString("D2", CultureInfo.InvariantCulture));
                            break;

                        //y Years, numeric  1, 3
                        case 'y':
                            result.Append(y.ToString(CultureInfo.InvariantCulture));
                            break;

                        //M Months, numeric, at least 2 digits with leading 0   01, 03, 12
                        case 'M':
                            result.Append(m.ToString("D2", CultureInfo.InvariantCulture));
                            break;

                        //m Months, numeric 1, 3, 12
                        case 'm':
                            result.Append(m.ToString(CultureInfo.InvariantCulture));
                            break;

                        //D Days, numeric, at least 2 digits with leading 0 01, 03, 31
                        case 'D':
                            result.Append(d.ToString("D2", CultureInfo.InvariantCulture));
                            break;

                        //d Days, numeric   1, 3, 31
                        case 'd':
                            result.Append(d.ToString(CultureInfo.InvariantCulture));
                            break;

                        //a Total number of days as a result of a DateTime::diff() or(unknown) otherwise   4, 18, 8123

                        //H Hours, numeric, at least 2 digits with leading 0    01, 03, 23
                        case 'H':
                            result.Append(h.ToString("D2", CultureInfo.InvariantCulture));
                            break;

                        //h Hours, numeric  1, 3, 23
                        case 'h':
                            result.Append(h.ToString(CultureInfo.InvariantCulture));
                            break;

                        //I Minutes, numeric, at least 2 digits with leading 0  01, 03, 59
                        case 'I':
                            result.Append(i.ToString("D2", CultureInfo.InvariantCulture));
                            break;

                        //i Minutes, numeric    1, 3, 59
                        case 'i':
                            result.Append(i.ToString(CultureInfo.InvariantCulture));
                            break;

                        //S Seconds, numeric, at least 2 digits with leading 0  01, 03, 57
                        case 'S':
                            result.Append(s.ToString("D2", CultureInfo.InvariantCulture));
                            break;
                        //s Seconds, numeric    1, 3, 57
                        case 's':
                            result.Append(i.ToString(CultureInfo.InvariantCulture));
                            break;

                        //F Microseconds, numeric, at least 6 digits with leading 0 007701, 052738, 428291
                        case 'F':
                            result.Append((_span.Milliseconds * 1000).ToString("D6", CultureInfo.InvariantCulture));
                            break;

                        //f Microseconds, numeric   7701, 52738, 428291
                        case 'f':
                            result.Append((_span.Milliseconds * 1000).ToString(CultureInfo.InvariantCulture));
                            break;

                        //R Sign "-" when negative, "+" when positive   -, +
                        case 'R':
                            result.Append(invert != 0 ? '-' : '+');
                            break;

                        //r   Sign "-" when negative, empty when positive -,
                        case 'r':
                            if (invert != 0)
                            {
                                result.Append('-');
                            }
                            break;

                        default:
                            // unrecognized character, print it as-is.
                            result.Append(ch);
                            break;
                    }
                }
                else if (ch == '%')
                {
                    percent = true;
                }
                else
                {
                    result.Append(ch);
                }
            }

            if (percent)
            {
                result.Append('%');
            }

            return StringBuilderUtilities.GetStringAndReturn(result);
        }
    }
}
