using System;
using System.Collections;
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
            _statics = new object[_staticsCount];

            _globals = new PhpArray();
            // TODO: InitGlobalVariables(); //_globals.SetItemAlias(new IntStringKey("GLOBALS"), new PhpAlias(PhpValue.Create(_globals)));
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
        /// Initializes the index with new unique value if necessary.
        /// </summary>
        T GetStatic<T>(ref int idx) where T : new()
        {
            if (idx <= 0)
                idx = NewIdx();

            return GetStatic<T>(idx);
        }

        /// <summary>
        /// Gets static object instance within the context with given index.
        /// </summary>
        T GetStatic<T>(int idx) where T : new()
        {
            EnsureStaticsSize(idx);
            return GetStatic<T>(ref _statics[idx]);
        }

        /// <summary>
        /// Ensures the <see cref="_statics"/> array has sufficient size to hold <paramref name="idx"/>;
        /// </summary>
        /// <param name="idx">Index of an object to be stored within statics.</param>
        void EnsureStaticsSize(int idx)
        {
            if (_statics.Length <= idx)
            {
                Array.Resize(ref _statics, (idx + 1) * 2);
            }
        }

        /// <summary>
        /// Ensures the context static object is initialized.
        /// </summary>
        T GetStatic<T>(ref object obj) where T : new()
        {
            if (obj == null)
            {
                obj = new T();
                //if (obj is IStaticInit) ((IStaticInit)obj).Init(this);
            }

            Debug.Assert(obj is T);
            return (T)obj;
        }

        /// <summary>
        /// Gets context static object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type of the object to be stored within context.</typeparam>
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
        /// Cannot be <c>null</c>.
        /// </summary>
        object[] _statics;

        /// <summary>
        /// Number of static objects so far registered within context.
        /// </summary>
        static volatile int/*!*/_staticsCount;

        #endregion

        #region Superglobals

        /// <summary>
        /// Array of global variables. Cannot be <c>null</c>.
        /// </summary>
        public PhpArray Globals
        {
            get { return _globals; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }

                _globals = value;
            }
        }
        PhpArray _globals;

        #endregion

        #region IDisposable

        public void Dispose()
        {

        }

        #endregion
    }
}
