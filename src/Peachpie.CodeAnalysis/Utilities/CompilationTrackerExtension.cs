using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;

namespace Pchp.CodeAnalysis.Utilities
{
    internal static class CompilationTrackerExtension
    {
        /// <summary>
        /// Helper value to remember the start time of time span metric.
        /// </summary>
        public struct TimeSpanMetric : IDisposable
        {
            readonly ImmutableArray<IObserver<object>> _observers;
            public string Name;
            public DateTime Start;

            public TimeSpanMetric(ImmutableArray<IObserver<object>> observers, string name)
            {
                _observers = observers;
                Name = name;
                Start = DateTime.UtcNow;
            }

            void IDisposable.Dispose()
            {
                if (!_observers.IsDefaultOrEmpty)
                {
                    _observers.TrackMetric(Name, (DateTime.UtcNow - Start).TotalSeconds);
                    this = default;
                }
            }
        }

        public sealed class TraceObserver : IObserver<object>
        {
            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
                Trace.WriteLine(error.Message);
            }

            public void OnNext(object value)
            {
                if (value is string str)
                {
                    Trace.WriteLine(str);
                }
                else if (value is Tuple<string, double> data)
                {
                    Trace.WriteLine($"{data.Item1}: {data.Item2}");
                }
            }
        }

        static void OnCompleted(IObserver<object> o)
        {
            try { o.OnCompleted(); }
            catch
            { }
        }

        public static void TrackOnCompleted(this PhpCompilation c)
        {
            if (!c.EventSources.IsDefaultOrEmpty)
            {
                c.EventSources.ForEach(OnCompleted);
            }
        }

        public static void TrackException(this PhpCompilation c, Exception ex)
        {
            if (ex is AggregateException aex && aex.InnerExceptions != null)
            {
                foreach (var innerEx in aex.InnerExceptions)
                {
                    TrackException(c, innerEx);
                }
            }
            else if (ex != null && !c.EventSources.IsDefaultOrEmpty)
            {
                c.EventSources.ForEach(o => o.OnError(ex));
            }
        }

        static void TrackMetric(this ImmutableArray<IObserver<object>> observers, string name, double value)
        {
            if (!observers.IsDefaultOrEmpty)
            {
                observers.ForEach(o => o.OnNext(Tuple.Create(name, value)));
            }
        }

        public static void TrackMetric(this PhpCompilation c, string name, double value)
        {
            TrackMetric(c.EventSources, name, value);
        }

        public static void TrackEvent(this PhpCompilation c, string name)
        {
            c.EventSources.ForEach(o => o.OnNext(name));
        }

        public static TimeSpanMetric StartMetric(this PhpCompilation c, string name)
        {
            return StartMetric(c.EventSources, name);
        }

        public static TimeSpanMetric StartMetric(this ImmutableArray<IObserver<object>> observers, string name)
        {
            return new TimeSpanMetric(observers, name);
        }
    }
}
