using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// If implemented on a request-static holder,
    /// context performs its one-time initialization.
    /// </summary>
    public interface IStaticInit
    {
        /// <summary>
        /// One-time initialization routine called by context when instance is created.
        /// </summary>
        void Init(Context ctx);
    }

    partial class Context
    {
        static class StaticIndexes
        {
            public static int StaticsCount;

            public static int StaticsIndex<T>() => Statics<T>.Index;

            static class Statics<T> { public static readonly int Index = StaticsCount++; }
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
        /// Tries to get a static property stored within context.
        /// </summary>
        public T TryGetProperty<T>() where T : class
        {
            var idx = StaticIndexes.StaticsIndex<T>();
            EnsureStaticsSize(idx);
            return _statics[idx] as T;
        }

        /// <summary>
        /// Sets a static property to be stored within context.
        /// </summary>
        public void SetProperty<T>(T value)
        {
            var idx = StaticIndexes.StaticsIndex<T>();
            EnsureStaticsSize(idx);
            _statics[idx] = value;
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

                if (obj is IStaticInit)
                {
                    ((IStaticInit)obj).Init(this);
                }
            }
            else
            {
                Debug.Assert(obj is T);
            }

            //
            return (T)obj;
        }

        /// <summary>
        /// Gets context static object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type of the object to be stored within context.</typeparam>
        public T GetStatic<T>() where T : new() => GetStatic<T>(StaticIndexes.StaticsIndex<T>());

        /// <summary>
        /// Gets context static object of type <typeparamref name="T"/> if it was addedinto the context already.
        /// </summary>
        public bool TryGetStatic<T>(out T value) where T : class => (value = TryGetProperty<T>()) != default;

        /// <summary>
        /// Static objects within the context.
        /// Cannot be <c>null</c>.
        /// </summary>
        object[] _statics;
    }
}
