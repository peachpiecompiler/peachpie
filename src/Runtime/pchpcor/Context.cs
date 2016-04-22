using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Runtime context for a PHP application.
    /// </summary>
    /// <remarks>
    /// The object represents a current Web request or the application run.
    /// Its instance is passed to all PHP function.
    /// The context is not thread safe.
    /// </remarks>
    public partial class Context : IDisposable
    {
        #region Create

        private Context()
        {
        }

        /// <summary>
        /// Create context to be used within a console application.
        /// </summary>
        public static Context CreateConsole()
        {
            return new Context();
        }

        #endregion

        #region GetStatic

        /// <summary>
        /// Helper generic class holding an app static index to array of static objects.
        /// </summary>
        /// <typeparam name="T">Type of object kept as context static.</typeparam>
        static class IndexHolder<T>
        {
            /// <summary>
            /// Index of the object of type <typeparamref name="T"/>.
            /// </summary>
            public static int Index;
        }

        /// <summary>
        /// Gets static object instance within the context with given index.
        /// </summary>
        T GetStatic<T>(ref int idx) where T : new()
        {
            if (idx <= 0) idx = NewIdx();
            return EnsureInitialized<T>(idx);
        }

        /// <summary>
        /// Ensures the object at given index is initialized and returns its instance.
        /// </summary>
        T EnsureInitialized<T>(int idx) where T : new()
        {
            Debug.Assert(idx > 0);

            if (_statics == null || idx >= _statics.Length)
            {
                Array.Resize(ref _statics, idx << 1);
            }

            var obj = _statics[idx];
            if (obj == null)
            {
                _statics[idx] = obj = new T();
                //if (obj is IStaticInit) ((IStaticInit)obj).Init(this);
            }

            Debug.Assert(obj is T);
            return (T)obj;
        }

        /// <summary>
        /// Gets static object instance within the context.
        /// </summary>
        public T GetStatic<T>() where T : new() => GetStatic<T>(ref IndexHolder<T>.Index);

        /// <summary>
        /// Gets new index to be used within <see cref="_statics"/> array.
        /// </summary>
        int NewIdx()
        {
            int idx;

            lock (_statics)
            {
                idx = Interlocked.Increment(ref _staticsCount);
            }

            return idx;
        }

        /// <summary>
        /// Static objects within the context.
        /// </summary>
        object[] _statics;

        /// <summary>
        /// Number of static objects within context.
        /// </summary>
        static volatile int/*!*/_staticsCount;

        #endregion

        #region IDisposable

        public void Dispose()
        {

        }

        #endregion
    }
}
