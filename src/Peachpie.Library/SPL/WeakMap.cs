using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.Spl
{
    [PhpType(PhpTypeAttribute.InheritName, MinimumLangVersion = "8.0")]
    public sealed class WeakMap : ArrayAccess, Countable, IteratorAggregate, Traversable
    {
        // - Setting dynamic properties on it is forbidden (does not have RuntimeFields)
        // - Using a non-object key in $map[$key] or one of the offset*() methods results in a TypeError exception
        // - Appending to a weak map using $map[] results in an Error exception. // <== NULL key
        // - Reading a non-existent key results in an Error exception

        struct WeakEntryKey : IEquatable<WeakEntryKey>
        {
            public int HashCode { get; private set; }

            /// <summary>
            /// Key or WeakReference to the weak key.
            /// </summary>
            public object Key { get; private set; }

            public static WeakEntryKey CreateWeak(object key)
            {
                return new WeakEntryKey { HashCode = key.GetHashCode(), Key = new WeakReference<object>(key) };
            }

            public static WeakEntryKey CreateTemp(object key)
            {
                return new WeakEntryKey { HashCode = key.GetHashCode(), Key = key };
            }

            public bool IsValid => TryGetKey(out _);

            public bool TryGetKey(out object key)
            {
                if (this.Key is WeakReference<object> weak)
                {
                    return weak.TryGetTarget(out key);
                }
                else
                {
                    key = this.Key;
                    return key != null;
                }
            }

            public override int GetHashCode() => HashCode;

            public override bool Equals(object obj) => obj is WeakEntryKey other && Equals(other);

            public bool Equals(WeakEntryKey other) =>
                other.HashCode == HashCode &&
                other.TryGetKey(out var otherkey) &&
                TryGetKey(out var key) &&
                otherkey == key;
        }

        /// <summary>
        /// Underlying map of values by the object hash code.
        /// </summary>
        [PhpHidden]
        readonly Dictionary<WeakEntryKey, PhpValue>/*!*/_map = new Dictionary<WeakEntryKey, PhpValue>();

        #region Helpers

        [PhpHidden]
        bool TryFindEntry(object key, out PhpValue value)
        {
            if (key == null)
            {
                throw ObjectNullException();
            }

            return _map.TryGetValue(WeakEntryKey.CreateTemp(key), out value);
        }

        /// <summary>
        /// Gets the offset as object key or throws corresponding exception.
        /// </summary>
        [PhpHidden]
        static object AsKey(PhpValue offset)
        {
            return offset.AsObject()
                ?? throw (offset.IsNull ? ObjectNullException() : TypeErrorException());
        }

        [PhpHidden]
        static System.Exception ObjectNullException() => new Error();

        [PhpHidden]
        static System.Exception TypeErrorException() => new TypeError();

        #endregion

        /// <summary>
        /// Initializes the WeakMap.
        /// </summary>
        public WeakMap()
        {

        }

        internal WeakMap(WeakMap/*!*/other)
        {
            Debug.Assert(other != null);

            // copy not GC'ed entries
            foreach (var pair in other._map)
            {
                if (pair.Key.IsValid)
                {
                    _map[pair.Key] = pair.Value.DeepCopy();
                }
            }
        }

        public long count()
        {
            int count = 0;

            // non-GC'ed keys
            foreach (var key in _map.Keys)
            {
                if (key.IsValid)
                {
                    count++;
                }
            }

            //
            return count;
        }

        public PhpValue offsetGet(object @object)
        {
            if (TryFindEntry(@object, out var value))
            {
                return value.DeepCopy();
            }

            return PhpValue.Null;
        }

        public void offsetSet(object @object, PhpValue value)
        {
            if (@object == null)
            {
                throw ObjectNullException();
            }

            _map[WeakEntryKey.CreateWeak(@object)] = value.DeepCopy();
        }

        public bool offsetExists(object @object)
        {
            return TryFindEntry(@object, out _);
        }

        public void offsetUnset(object @object)
        {
            if (@object != null)
            {
                _map.Remove(WeakEntryKey.CreateTemp(@object));
            }
        }

        bool ArrayAccess.offsetExists(PhpValue offset) => offsetExists(AsKey(offset));

        PhpValue ArrayAccess.offsetGet(PhpValue offset) => offsetGet(AsKey(offset));

        void ArrayAccess.offsetSet(PhpValue offset, PhpValue value) => offsetSet(AsKey(offset), value);

        void ArrayAccess.offsetUnset(PhpValue offset) => offsetUnset(AsKey(offset));

        public WeakMap __clone() => new WeakMap(this);

        public Traversable getIterator()
        {
            throw new NotImplementedException();
        }
    }
}
