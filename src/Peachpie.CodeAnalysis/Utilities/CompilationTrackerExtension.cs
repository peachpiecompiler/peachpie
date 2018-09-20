using System;
using System.Collections.Generic;
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
            readonly IEnumerable<IObserver<object>> _observers;
            public string Name;
            public DateTime Start;

            public TimeSpanMetric(IEnumerable<IObserver<object>> observers, string name)
            {
                _observers = observers;
                Name = name;
                Start = DateTime.UtcNow;
            }

            /// <summary>
            /// Gets value indicating the object is not initialized.
            /// </summary>
            public bool IsDefault => Name == null || Start == default;

            void IDisposable.Dispose()
            {
                if (!IsDefault)
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
                    Trace.WriteLine($"{data.Item1}: {data.Item2}s");
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
            c.Observers.ForEach(OnCompleted);
        }

        public static void TrackException(this PhpCompilation c, Exception ex)
        {
            if (ex != null)
            {
                c.Observers.ForEach(o => o.OnError(ex));
            }
        }

        static void TrackMetric(this IEnumerable<IObserver<object>> observers, string name, double value)
        {
            observers.ForEach(o => o.OnNext(Tuple.Create(name, value)));
        }

        public static void TrackMetric(this PhpCompilation c, string name, double value)
        {
            TrackMetric(c.Observers, name, value);
        }

        public static void TrackEvent(this PhpCompilation c, string name)
        {
            c.Observers.ForEach(o => o.OnNext(name));
        }

        public static TimeSpanMetric StartMetric(this PhpCompilation c, string name)
        {
            return StartMetric(c.Observers, name);
        }

        public static TimeSpanMetric StartMetric(this IEnumerable<IObserver<object>> observers, string name)
        {
            return new TimeSpanMetric(observers, name);
        }
    }
}
