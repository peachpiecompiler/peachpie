using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            T value;
            if (!dict.TryGetValue(key, out value))
            {
                value = default(T);
            }

            return value;
        }

        /// <summary>
        /// Calls given action for each element in given enumerable.
        /// </summary>
        public static void Foreach<T>(this IEnumerable<T>/*!*/enumerable, Action<T>/*!*/func)
        {
            Contract.ThrowIfNull(enumerable);
            Contract.ThrowIfNull(func);

            foreach (var x in enumerable)
            {
                func(x);
            }
        }
    }
}
