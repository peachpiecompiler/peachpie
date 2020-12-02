using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis
{
    internal static class EnumeratorExtension
    {
        /// <summary>
        /// Gets value from given dictionary corresponding to the key if the key is contained, otherwise default of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="K">Key type.</typeparam>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="dict">Dictionary. Cannot be null.</param>
        /// <param name="key">Key.</param>
        /// <returns>Value corresponding to key or default of <typeparamref name="T"/>.</returns>
        public static T TryGetOrDefault<K, T>(this ConcurrentDictionary<K, T> dict, K key)
        {
            T value;
            if (!dict.TryGetValue(key, out value))
            {
                value = default(T);
            }

            return value;
        }

        /// <summary>
        /// Gets value from given dictionary corresponding to the key if the key is contained, otherwise default of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="K">Key type.</typeparam>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="dict">Dictionary. Cannot be null.</param>
        /// <param name="key">Key.</param>
        /// <returns>Value corresponding to key or default of <typeparamref name="T"/>.</returns>
        public static T TryGetOrDefault<K, T>(this IDictionary<K, T> dict, K key)
        {
            if (dict.TryGetValue(key, out var value))
            {
                return value;
            }

            return default;
        }

        /// <summary>
        /// Calls given action for each element in given enumerable.
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T>/*!*/enumerable, Action<T>/*!*/func)
        {
            Contract.ThrowIfNull(enumerable);
            Contract.ThrowIfNull(func);

            foreach (var x in enumerable)
            {
                func(x);
            }
        }

        /// <summary>
        /// Calls given action for each element in given array.
        /// </summary>
        public static void ForEach<T>(this T[]/*!*/array, Action<T>/*!*/func)
        {
            Contract.ThrowIfNull(array);
            Contract.ThrowIfNull(func);

            for (int i = 0; i < array.Length; i++)
            {
                func(array[i]);
            }
        }

        /// <summary>
        /// Calls given action for each element in given array.
        /// </summary>
        public static void ForEach<T>(this T[]/*!*/array, Action<int, T>/*!*/func)
        {
            Contract.ThrowIfNull(array);
            Contract.ThrowIfNull(func);

            for (int i = 0; i < array.Length; i++)
            {
                func(i, array[i]);
            }
        }

        /// <summary>
        /// Mixes two array of the same length into new one, using <paramref name="mixer"/> function applied on each pair of elements from first and second arrays.
        /// </summary>
        /// <typeparam name="T">Elements type.</typeparam>
        /// <param name="arr1">First array.</param>
        /// <param name="arr2">Second array.</param>
        /// <param name="mixer">Mixing function.</param>
        /// <returns>Mixed array.</returns>
        public static T[]/*!*/MergeArrays<T>(T[]/*!*/arr1, T[]/*!*/arr2, Func<T, T, T>/*!*/mixer)
        {
            Contract.ThrowIfNull(arr1);
            Contract.ThrowIfNull(arr2);
            Contract.ThrowIfNull(mixer);

            // lets arr1 <= arr2
            if (arr1.Length > arr2.Length)
            {
                var h = arr2;
                arr2 = arr1;
                arr1 = h;
            }

            var tmp = new T[arr2.Length];
            int i = 0;
            for (; i < arr1.Length; i++)
            {
                tmp[i] = mixer(arr1[i], arr2[i]);
            }

            for (; i < arr2.Length; i++)
            {
                tmp[i] = mixer(default, arr2[i]);
            }

            //
            return tmp;
        }

        /// <summary>
        /// Checks entries in given arrays for equality.
        /// If arrays are of a different size, default(T) is used for comparison.
        /// </summary>
        public static bool EqualEntries<T>(T[]/*!*/arr1, T[]/*!*/arr2)// where T : IEquatable<T>
        {
            Contract.ThrowIfNull(arr1);
            Contract.ThrowIfNull(arr2);

            // lets arr1 <= arr2
            if (arr1.Length > arr2.Length)
            {
                var h = arr2;
                arr2 = arr1;
                arr1 = h;
            }

            int i = 0;
            for (; i < arr1.Length; i++)
            {
                if (!arr1[i].Equals(arr2[i]))
                    return false;
            }

            for (; i < arr2.Length; i++)
            {
                if (!arr2[i].Equals(default(T)))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Finds index of first element passing given predicate.
        /// Returns <c>-1</c> if not found.
        /// </summary>
        public static int IndexOf<T>(this ImmutableArray<T> array, Predicate<T> predicate)
        {
            Contract.ThrowIfNull(predicate);
            Debug.Assert(!array.IsDefault);

            for (int i = 0; i < array.Length; i++)
            {
                if (predicate(array[i])) return i;
            }

            return -1;
        }

        /// <summary>
        /// Converts list to <see cref="ImmutableArray{T}"/> safely. If the list is <c>null</c>, empty array is returned.
        /// </summary>
        public static ImmutableArray<T> AsImmutableSafe<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0)
            {
                return ImmutableArray<T>.Empty;
            }
            else
            {
                return list.ToImmutableArray();
            }
        }
    }

    /// <summary>
    /// <see cref="IList{T}"/> implementation where only allowed items are actually added.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    internal sealed class ConditionalList<T> : IList<T>
    {
        public ConditionalList(IList<T> list, Predicate<T> predicate)
        {
            _list = list ?? throw new ArgumentNullException(nameof(list));
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        readonly Predicate<T> _predicate;
        readonly IList<T> _list;

        public T this[int index] { get => _list[index]; set => _list[index] = value; }

        public int Count => _list.Count;

        public bool IsReadOnly => _list.IsReadOnly;

        public void Add(T item)
        {
            if (_predicate(item))
            {
                _list.Add(item);
            }
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            if (_predicate(item))
            {
                _list.Insert(index, item);
            }
        }

        public bool Remove(T item)
        {
            return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
}
