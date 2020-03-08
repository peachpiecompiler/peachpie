using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Utilities
{
    public static class ArrayUtils
    {
        #region Properties

        public static PhpValue[] EmptyValues => Empty<PhpValue>();

        public static object[] EmptyObjects => Empty<object>();

        public static byte[] EmptyBytes => Empty<byte>();

        public static string[] EmptyStrings => Array.Empty<string>();

        public static T[] Empty<T>() => Array.Empty<T>();

        #endregion

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        public static void Write(this Stream stream, byte[] bytes) => stream.Write(bytes, 0, bytes.Length);

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        public static Task WriteAsync(this Stream stream, byte[] bytes) => stream.WriteAsync(bytes, 0, bytes.Length);

        /// <summary>
        /// Decodes a sequence of bytes from the specified byte array into a string.
        /// </summary>
        public static string GetString(this Encoding encoding, byte[] bytes) => encoding.GetString(bytes, 0, bytes.Length);

        /// <summary>
        /// Filters a sequence of values that are not a <c>null</c> reference.
        /// </summary>
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> e) where T : class => e.Where<T>(FuncExtensions.s_not_null);

        /// <summary>
        /// Checks two arrays are equal.
        /// </summary>
        public static bool Equals<T>(T[] first, T[] second) => Equals(first, second, EqualityComparer<T>.Default);

        /// <summary>
        /// Checks two arrays are equal.
        /// </summary>
        public static bool Equals<T>(T[] first, T[] second, IEqualityComparer<T> comparer)
        {
            if (first.Length != second.Length)
            {
                return false;
            }

            for (int i = 0; i < first.Length; i++)
            {
                if (comparer.Equals(first[i], second[i]) == false)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets last element of the list.
        /// </summary>
        /// <returns>Last element or default of {T}.</returns>
        public static T Last<T>(this IList<T> list) => list.Count != 0 ? list[list.Count - 1] : default(T);

        /// <summary>
        /// Gets last character of the string.
        /// </summary>
        /// <returns>Last character or <c>\0</c>.</returns>
        public static char Last(this string str) => StringUtils.LastChar(str);

        /// <summary>
        /// Fast trim of a specified character.
        /// </summary>
        public static string Trim(string str, char ch)
        {
            if (!string.IsNullOrEmpty(str))
            {
                int i = 0;
                int j = str.Length - 1;

                //
                while (i < str.Length && str[i] == ch) i++;
                while (j > i && str[j] == ch) j--;

                //
                return i < j ? str.Substring(i, j - i + 1) : string.Empty;
            }

            return str;
        }

        /// <summary>
        /// Fills a portion of an array of bytes by specified byte.
        /// </summary>
        /// <param name="array">The array to fill.</param>
        /// <param name="value">The value to fill the array with.</param>
        /// <param name="offset">The index of the first byte to be set.</param>
        /// <param name="count">The number of bytes to be set.</param>
        /// <remarks>This method uses fast unsafe filling of memory with bytes.</remarks>
        public static void Fill(byte[] array, byte value, int offset, int count)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (offset < 0 || offset + count > array.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (array.Length == 0)
                return;

            for (int i = offset; i < count + offset; i++)
            {
                array[i] = value;
            }
        }

        /// <summary>
        /// Searches for specified character in sorted array of characters.
        /// Specialized version of <see cref="Array.BinarySearch{T}(T[], T)"/>.
        /// </summary>
        /// <param name="array">The array to search in.</param>
        /// <param name="c">The character to search for.</param>
        /// <returns>The position of the <paramref name="c"/> in <paramref name="array"/> or -1 if not found.</returns>
        public static int BinarySearch(char[] array, char c)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            int i = 0;
            int j = array.Length - 1;
            while (i < j)
            {
                int m = (i + j) >> 1;
                char cm = array[m];
                if (c == cm) return m;

                if (c > cm)
                {
                    i = m + 1;
                }
                else
                {
                    j = m - 1;
                }
            }
            return (array[i] == c) ? i : -1;
        }

        /// <summary>
        /// Concatenates elements into a new array.
        /// </summary>
        public static T[] AppendRange<T>(T first, T[] array)
        {
            var newarr = new T[1 + array.Length];
            newarr[0] = first;
            Array.Copy(array, 0, newarr, 1, array.Length);
            return newarr;
        }

        /// <summary>
        /// Searches for the specified object and returns the index of its first occurrence in a one-dimensional array.
        /// </summary>
        public static int IndexOf<T>(this T[] arr, T value) => Array.IndexOf(arr, value);

        /// <summary>
        /// Safely returns item from array.
        /// </summary>
        public static bool TryGetItem<T>(T[] array, int idx, out T value)
        {
            if (idx >= 0 && idx < array.Length)
            {
                value = array[idx];
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Safely returns item from array.
        /// </summary>
        public static bool TryGetItem<T>(T[] array, long idx, out T value)
        {
            if (idx >= 0 && idx < array.LongLength)
            {
                value = array[idx];
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Creates new array with reversed order of items.
        /// </summary>
        public static T[] Reverse<T>(this T[] array)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));

            if (array.Length == 0) return Array.Empty<T>();

            var reversed = new T[array.Length];

            Array.Copy(array, reversed, array.Length);
            Array.Reverse(reversed);

            return reversed;
        }

        /// <summary>
        /// Gets value indicating the array is null or with no elements.
        /// </summary>
        public static bool IsNullOrEmpty<T>(T[] array) => array == null || array.Length == 0;
    }

    /// <summary>
    /// Helper class holding instance of an empty dictionary.
    /// </summary>
    public sealed class EmptyDictionary<TKey, TValue>
    {
        /// <summary>
        /// The singleton.
        /// </summary>
        public static IReadOnlyDictionary<TKey, TValue> Singleton { get; } = new System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>(new Dictionary<TKey, TValue>(0));
    }

    /// <summary>
    /// Helper class that implements empty collection and empty enumerator, GC friendly.
    /// </summary>
    public sealed class EmptyCollection<T> : ICollection<T>
    {
        public static readonly EmptyCollection<T> Instance = new EmptyCollection<T>();

        private EmptyCollection() { }

        #region ICollection

        public int Count => 0;

        public bool IsReadOnly => true;

        public void Add(T item) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public bool Contains(T item) => false;

        public void CopyTo(T[] array, int arrayIndex) { }

        public bool Remove(T item) => throw new NotSupportedException();

        #endregion

        public IEnumerator<T> GetEnumerator() => EmptyEnumerator<T>.Instance;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Helper class implementing empty <see cref="IEnumerator{T}"/>, GC friendly.
    /// </summary>
    /// <remarks>Usage: <code>return EmptyEnumerator{T}.Instance;</code></remarks>
    public sealed class EmptyEnumerator<T> : IEnumerator<T>
    {
        public static readonly EmptyEnumerator<T> Instance = new EmptyEnumerator<T>();

        private EmptyEnumerator() { }

        public T Current => default;

        object IEnumerator.Current => default;

        public void Dispose() { }

        public bool MoveNext() => false;

        public void Reset() { }
    }

    /// <summary>
    /// Helper class implementing empty <see cref="IEnumerator{T}"/>, GC friendly.
    /// </summary>
    /// <remarks>Usage: <code>return EmptyEnumerator{T}.Instance;</code></remarks>
    public sealed class EmptyPhpEnumerator : IPhpEnumerator
    {
        public static readonly EmptyPhpEnumerator Instance = new EmptyPhpEnumerator();

        public bool AtEnd => true;

        public PhpValue CurrentValue => default;

        public PhpAlias CurrentValueAliased => default;

        public PhpValue CurrentKey => default;

        public KeyValuePair<PhpValue, PhpValue> Current => default;

        object IEnumerator.Current => default;

        public void Dispose() { }

        public bool MoveFirst() => false;

        public bool MoveLast() => false;

        public bool MoveNext() => false;

        public bool MovePrevious() => false;

        public void Reset() { }
    }
}
