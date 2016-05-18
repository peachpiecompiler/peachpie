using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Pchp.Core
{
    /// <summary>
    /// DualDictionary contains two dictionaries that each one has its own comparer, but behaves as one dictionary
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <remarks>
    /// It is used for example to store constants, because some constants ignores case and others don't
    /// </remarks>
	[DebuggerNonUserCode]
    public sealed class DualDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        readonly Dictionary<TKey, TValue>/*!*/ _primary;
        readonly Dictionary<TKey, TValue>/*!*/ _secondary;

        public DualDictionary(DualDictionary<TKey, TValue>/*!*/ dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            _primary = new Dictionary<TKey, TValue>(dictionary._primary, dictionary._primary.Comparer);
            _secondary = new Dictionary<TKey, TValue>(dictionary._secondary, dictionary._secondary.Comparer);
        }

        public DualDictionary(IEqualityComparer<TKey> primaryComparer, IEqualityComparer<TKey> secondaryComparer)
        {
            _primary = new Dictionary<TKey, TValue>(primaryComparer);
            _secondary = new Dictionary<TKey, TValue>(secondaryComparer);
        }

        public int Count => _primary.Count + _secondary.Count;

        public TValue this[TKey/*!*/ key]
        {
            get
            {
                TValue result;
                if (this.TryGetValue(key, out result))
                    return result;
                else
                    throw new KeyNotFoundException();
            }
        }

        public TValue this[TKey/*!*/ key, bool isPrimary]
        {
            set
            {
                (isPrimary ? _primary : _secondary)[key] = value;
            }
        }


        public bool TryGetValue(TKey/*!*/ key, out TValue result)
            => _primary.TryGetValue(key, out result) || _secondary.TryGetValue(key, out result);

        public bool TryGetValue(TKey key, out TValue result, out bool isSensitive)
            => (isSensitive = _primary.TryGetValue(key, out result)) || _secondary.TryGetValue(key, out result);

        public bool ContainsKey(TKey/*!*/ key) => _primary.ContainsKey(key) || _secondary.ContainsKey(key);

        public void Add(TKey/*!*/ key, TValue value, bool ignoreCase)
        {
            (ignoreCase ? _secondary : _primary).Add(key, value);
        }

        public bool Remove(TKey/*!*/ key) => _primary.Remove(key) || _secondary.Remove(key);

        public IEnumerator<KeyValuePair<TKey, TValue>>/*!*/ GetEnumerator() => _primary.Concat(_secondary).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _primary.Concat(_secondary).GetEnumerator();
    }
}
