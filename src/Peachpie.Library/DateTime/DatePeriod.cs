using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.DateTime
{
    /// <summary>
    /// A date period allows iteration over a set of dates and times, recurring at regular intervals, over a given period.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("date")]
    public class DatePeriod : Traversable, IEnumerable<DateTimeInterface>
    {
        /// <summary>
        /// Exclude start date option.
        /// </summary>
        public const int EXCLUDE_START_DATE = 1;

        public int recurrences { get; private set; }
        public bool include_start_date { get; private set; }
        public DateTimeInterface start { get; private set; }
        public DateTimeInterface end { get; private set; }
        public DateInterval interval { get; private set; }

        public DateInterval getDateInterval() => interval;
        public DateTimeInterface getEndDate() => end;
        public int getRecurrences() => recurrences;
        public DateTimeInterface getStartDate() => start;

        [PhpFieldsOnlyCtor]
        protected DatePeriod()
        {
            //
        }

        public DatePeriod(DateTimeInterface start, DateInterval interval, int recurrences, int options = 0)
        {
            __construct(start, interval, recurrences, options);
        }

        public DatePeriod(DateTimeInterface start, DateInterval interval, DateTimeInterface end, int options = 0)
        {
            __construct(start, interval, end, options);
        }

        //public DatePeriod(string isostr, int options = 0)
        //{
        //    __construct(isostr, options);
        //}

        public void __construct(DateTimeInterface start, DateInterval interval, int recurrences, int options = 0)
        {
            this.start = start ?? throw new ArgumentNullException(nameof(start));
            this.interval = interval ?? throw new ArgumentNullException(nameof(interval));
            this.recurrences = recurrences;
            this.include_start_date = (options & EXCLUDE_START_DATE) == 0;
        }

        public void __construct(DateTimeInterface start, DateInterval interval, DateTimeInterface end, int options = 0)
        {
            this.start = start ?? throw new ArgumentNullException(nameof(start));
            this.interval = interval;
            this.end = end ?? throw new ArgumentNullException(nameof(end));
            this.include_start_date = (options & EXCLUDE_START_DATE) == 0;
        }

        //public void __construct(string isostr, int options = 0)
        //{
        //    // https://en.wikipedia.org/wiki/ISO_8601#Repeating_intervals
        //    // "R5/2008-03-01T13:00:00Z/P1Y2M10DT2H30M"
        //    throw new NotImplementedException();
        //}

        IEnumerator<DateTimeInterface> IEnumerable<DateTimeInterface>.GetEnumerator()
        {
            DateTimeImmutable current = start as DateTimeImmutable ?? (start as DateTime)?.AsDateTimeImmutable() ?? throw new InvalidOperationException();
            
            if (include_start_date)
            {
                yield return current;
            }

            if (end == null && recurrences == 0)
            {
                yield break;
            }

            if (interval.IsZero)
            {
                yield break;
            }

            for (int index = 0; ; index++)
            {
                current = current.add(interval);

                if (end != null)
                {
                    if (current.Time > DateTimeFunctions.TimeFromInterface(end))
                    {
                        yield break;
                    }
                }
                else if (index >= recurrences)
                {
                    yield break;
                }

                //
                yield return current;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<DateTimeInterface>)this).GetEnumerator();
    }
}
