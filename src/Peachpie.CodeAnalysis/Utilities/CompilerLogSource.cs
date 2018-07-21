using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;

namespace Pchp.CodeAnalysis.Utilities
{
    /// <summary>
    /// Event source for compiler tracing.
    /// </summary>
    [EventSource(Name = "PeachPie CodeAnalysis")]
    internal sealed class CompilerLogSource : EventSource
    {
        #region Nested struct: TimeSpanMetric

        /// <summary>
        /// Helper value to remember the start time of time span metric.
        /// </summary>
        public struct TimeSpanMetric : IDisposable
        {
            public string Source;
            public string Message;
            public DateTime Start;

            /// <summary>
            /// Gets value indicating the object is not initialized.
            /// </summary>
            public bool IsDefault => Message == null || Start == default(DateTime);

            void IDisposable.Dispose()
            {
                TrackMetric(this);

                this = default(TimeSpanMetric);
            }
        }

        #endregion

        #region Counts

#if DEBUG
        TimeSpanMetric _phase;
        readonly Dictionary<string, int> _counts = new Dictionary<string, int>();
#endif

        [Conditional("TRACE")]
        public void Count(string id)
        {
#if DEBUG
            lock (_counts)
            {
                _counts.TryGetValue(id, out int n);
                _counts[id] = ++n;
            }
#endif
        }

        [Conditional("TRACE")]
        public void EndPhase() => StartPhase(null);

        [Conditional("TRACE")]
        public void StartPhase(string phase)
        {
#if DEBUG
            // close previous phase
            if (!_phase.IsDefault)
            {
                TrackMetric(_phase);
                _phase = default(TimeSpanMetric);
            }

            // dump counts
            if (_counts.Count != 0)
            {
                Trace.WriteLine("=== Counts (first 25) ===");

                lock (_counts)
                {
                    foreach (var pair in _counts.OrderByDescending(p => p.Value).Take(25))
                    {
                        Trace.WriteLine(pair.Key + ": " + pair.Value);
                    }

                    _counts.Clear();
                }

                Trace.WriteLine("=== End Counts ===");
            }

#endif
            if (phase != null)
            {
                var message = "Phase: " + phase;
                var start = StartMetric(nameof(CompilerLogSource), message); // log the phase and remember start time

#if DEBUG
                // start new phase
                _phase = start;
#endif
            }
        }

        #endregion

        public static TimeSpanMetric StartMetric(string source, string message)
        {
            Log.LogInformation(nameof(CompilerLogSource), message);

            return new TimeSpanMetric
            {
                Source = source,
                Message = message,
                Start = DateTime.UtcNow,
            };
        }

        public static void TrackMetric(TimeSpanMetric metric)
        {
            Log.TrackMetric(metric.Source, metric.Message, (long)(DateTime.UtcNow - metric.Start).TotalMilliseconds, "ms");
        }

        /// <summary>
        /// Singleton to be used.
        /// </summary>
        public static readonly CompilerLogSource Log = new CompilerLogSource();

        private CompilerLogSource() { }

        /// <summary>
        /// Logs a metric.
        /// </summary>
        [Event(1, Message = "Info: {1} - {2}{3} ({0})", Level = EventLevel.Informational)]
        public void TrackMetric(string source, string message, object count, string units)
        {
            if (IsEnabled())
            {
                WriteEvent(1, string.Format("{1} - {2}{3} ({0})", source, message, count, units));
            }
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        [Event(2, Message = "Info: {1} ({0})", Level = EventLevel.Informational)]
        public void LogInformation(string source, string message)
        {
            if (IsEnabled())
            {
                WriteEvent(2, message);
            }

            Trace.TraceInformation(message);
        }

        /// <summary>
        /// Logs an error.
        /// </summary>
        [Event(3, Message = "Error: {1} ({0})", Level = EventLevel.Error)]
        public void LogError(string source, string message)
        {
            if (IsEnabled())
            {
                WriteEvent(3, message);
            }

            Trace.TraceError(message);
        }
    }
}
