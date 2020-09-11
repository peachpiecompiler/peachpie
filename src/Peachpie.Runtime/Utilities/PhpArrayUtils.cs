using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Utilities
{
    /// <summary>
    /// Extension methods for <see cref="PhpArray"/> objects.
    /// </summary>
    public static class PhpArrayUtils
    {
        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from given <see cref="PhpArray"/>.
        /// </summary>
        public static Dictionary<TKey, PhpValue> ToDictionary<TSource, TKey>(this PhpHashtable source, Func<IntStringKey, TKey> keySelector)
        {
            return ToDictionary(source, keySelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from given <see cref="PhpArray"/>.
        /// </summary>
        public static Dictionary<TKey, PhpValue> ToDictionary<TKey>(this PhpHashtable source, Func<IntStringKey, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            return ToDictionary(source, keySelector, FuncExtensions.Identity<PhpValue>(), comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from given <see cref="PhpArray"/>.
        /// </summary>
        public static Dictionary<TKey, TElement> ToDictionary<TKey, TElement>(this PhpHashtable source, Func<IntStringKey, TKey> keySelector, Func<PhpValue, TElement> elementSelector)
        {
            return ToDictionary(source, keySelector, elementSelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from given <see cref="PhpArray"/>.
        /// </summary>
        public static Dictionary<TKey, TElement> ToDictionary<TKey, TElement>(this PhpHashtable source, Func<IntStringKey, TKey> keySelector, Func<PhpValue, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            var result = new Dictionary<TKey, TElement>(source.Count, comparer);

            var enumerator = source.GetFastEnumerator();
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                result[keySelector(current.Key)] = elementSelector(current.Value);
            }

            return result;
        }
    }
}
