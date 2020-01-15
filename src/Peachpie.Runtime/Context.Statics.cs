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
            public static uint StaticsCount;

            public static uint StaticsIndex<T>() => Statics<T>.Index;

            static class Statics<T> { public static readonly uint Index = StaticsCount++; }
        }

        /// <summary>
        /// Gets static object instance within the context with given index.
        /// </summary>
        T GetStatic<T>(uint idx) where T : new()
        {
            return GetStatic<T>(ref EnsureStatics(idx));
        }

        /// <summary>
        /// Tries to get a static property stored within context.
        /// </summary>
        public T TryGetProperty<T>() where T : class
        {
            var idx = StaticIndexes.StaticsIndex<T>();
            var statics = _statics;

            return idx < statics.Length ? statics[idx] as T : default;
        }

        /// <summary>
        /// Sets a static property to be stored within context.
        /// </summary>
        public void SetProperty<T>(T value)
        {
            var idx = StaticIndexes.StaticsIndex<T>();
            EnsureStatics(idx) = value;
        }

        /// <summary>
        /// Ensures the <see cref="_statics"/> array has sufficient size to hold <paramref name="idx"/>;
        /// </summary>
        /// <param name="idx">Index of an object to be stored within statics.</param>
        ref object EnsureStatics(uint idx)
        {
            if (_statics.Length <= idx)
            {
                Array.Resize(ref _statics, (int)Math.Max((idx + 1) * 2, StaticIndexes.StaticsCount));
            }

            return ref _statics[idx];
        }

        /// <summary>
        /// Ensures the context static object is initialized.
        /// </summary>
        T GetStatic<T>(ref object slot) where T : new()
        {
            T value;

            if (ReferenceEquals(slot, null))
            {
                ((slot = value = new T()) as IStaticInit)?.Init(this);
            }
            else
            {
                Debug.Assert(slot is T);
                value = (T)slot;
            }

            //
            return value;
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
