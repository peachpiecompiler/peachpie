using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Pchp.Core.Utilities;
using TValue = Pchp.Core.PhpValue;

namespace Pchp.Core
{
    #region SetOperations

    /// <summary>
    /// Implemented set operations over arrays.
    /// </summary>
    public enum SetOperations
    {
        Difference = 0,
        Intersection = 1,
    }

    #endregion

    #region IntStringKey

    /// <summary>
    /// Represents both integer or string array key.
    /// </summary>
    [DebuggerDisplay("[{Object}]")]
    [DebuggerNonUserCode, DebuggerStepThrough]
    public readonly struct IntStringKey : IEquatable<IntStringKey>, IComparable<IntStringKey>
    {
        /// <summary>
        /// <pre>new IntStringKey( "" )</pre>
        /// </summary>
        internal static readonly IntStringKey EmptyStringKey = new IntStringKey(string.Empty);

        [DebuggerNonUserCode, DebuggerStepThrough]
        public class EqualityComparer : IEqualityComparer<IntStringKey>
        {
            public static readonly EqualityComparer/*!*/ Default = new EqualityComparer();

            public bool Equals(IntStringKey x, IntStringKey y) => x._ikey == y._ikey && x._skey == y._skey;

            public int GetHashCode(IntStringKey x) => (int)x._ikey;
        }

        /// <summary>
        /// Max value of <see cref="Integer"/>.
        /// </summary>
        internal const long MaxKeyValue = long.MaxValue;

        /// <summary>
        /// Integer value iff <see cref="IsString"/> return <B>false</B>.
        /// </summary>
        public long Integer => _ikey;
        readonly long _ikey; // Holds string hashcode if skey != null

        /// <summary>
        /// String value iff <see cref="IsString"/> return <B>true</B>.
        /// </summary>
        public string String => _skey;
        readonly string _skey;

        /// <summary>
        /// Gets array key, string or int as object.
        /// </summary>
        public object Object => _skey ?? (object)_ikey;

        /// <summary>
        /// Gets value indicating the key is an empty string.
        /// Equivalent to <see cref="EmptyStringKey"/> which is <c>""</c>.
        /// </summary>
        public bool IsEmpty => Equals(EmptyStringKey);

        public IntStringKey(long key)
        {
            _ikey = key;
            _skey = null;
        }

        public IntStringKey(string/*!*/ key)
        {
            Debug.Assert(key != null);

            _skey = key;
            _ikey = key.GetHashCode();
        }

        public static implicit operator IntStringKey(int value) => new IntStringKey(value);

        public static implicit operator IntStringKey(string value) => value != null ? Convert.StringToArrayKey(value) : EmptyStringKey;

        public static implicit operator IntStringKey(PhpString value) => new IntStringKey(value.ToString());

        public static implicit operator IntStringKey(PhpValue value) => Convert.ToIntStringKey(value);

        public static implicit operator IntStringKey(PhpNumber value) => new IntStringKey((int)value.ToLong());

        internal static IntStringKey FromObject(object key)
        {
            if (key is string str) return new IntStringKey(str);
            if (key is long l) return new IntStringKey(l);
            if (key is int i) return new IntStringKey(i);

            throw new ArgumentException();
        }

        public bool IsString => !IsInteger;

        public bool IsInteger => ReferenceEquals(_skey, null);

        public override int GetHashCode() => unchecked((int)_ikey);

        public bool Equals(IntStringKey other) => _ikey == other._ikey && _skey == other._skey;

        public bool Equals(int ikey) => _ikey == ikey && ReferenceEquals(_skey, null);

        public override string ToString() => _skey ?? _ikey.ToString();

        public int CompareTo(IntStringKey other)
        {
            if (IsInteger)
            {
                if (other.IsInteger)
                    return _ikey.CompareTo(other._ikey);
                else
                    return string.CompareOrdinal(_ikey.ToString(), other._skey);
            }
            else
            {
                if (other.IsInteger)
                    return string.CompareOrdinal(_skey, other._ikey.ToString());
                else
                    return string.CompareOrdinal(_skey, other._skey);
            }
        }
    }

    #endregion

    /// <summary>
    /// Implementation of dictionary that keeps order of contained items.
    /// </summary>
    /// <remarks>
    /// Not thread safe.
    /// </remarks>
    [DebuggerNonUserCode, DebuggerStepThrough]
    [DebuggerDisplay("dictionary (count = {Count})")]
    public sealed class OrderedDictionary/*<TValue>*/ : IEnumerable<KeyValuePair<IntStringKey, TValue>>, IEnumerable<TValue>
    {
        [DebuggerDisplay("{DebugDisplay,nq}")]
        internal struct Bucket
        {
            string DebugDisplay => IsDeleted ? "undefined" : $"[{Key}] = {Value}";

            /// <summary>
            /// Collision detection chain.
            /// Order is not defined.
            /// </summary>
            public int Next;

            /// <summary>
            /// The bucket key.
            /// </summary>
            public IntStringKey Key;

            /// <summary>
            /// The bucket value.
            /// </summary>
            public TValue Value;

            public (IntStringKey, TValue) AsTuple() => (Key, Value);

            public KeyValuePair<IntStringKey, TValue> AsKeyValuePair() => new KeyValuePair<IntStringKey, TValue>(Key, Value);

            /// <summary>
            /// The value has been deleted - is not initialized.
            /// </summary>
            public bool IsDeleted => Value.IsInvalid;
        }

        /// <summary>Minimal internal table capacity. Must be power of 2.</summary>
        const uint _minCapacity = 8;

        const uint _minTableMask = _minCapacity - 1;

        const int _invalidIndex = -1;

        uint _mask;             // (_size - 1) bit mask of the table physical size
        Bucket[]/*!*/_data;     // table
        int[]/*!*/_hash;        // hash indices
        int _dataUsed;          // number of used elements so far => Index for next insertion
        int _dataDeleted;       // number of deleted elements within (0.._dataUsed] => Count = _dataUsed - _dataDeleted
        uint _size;             // physical size of the table (power of 2, minimum 8)
        //int nInternalPointer;   // intrinsic enumerator pointer
        long _maxIntKey;        // the maximum used integer key, used for adding elements at the end of collection. Always greater or equal to -1.

        /// <summary>
        /// Additional references sharing this object.
        /// Values greater than <c>zero</c> indicated the object is shared and should not be modified directly.
        /// </summary>
        int _refs;

        static bool _isPowerOfTwo(uint x) => (x & (x - 1)) == 0;

        static uint _tableMask(uint capacity)
        {
            if (capacity < _minCapacity)
            {
                Debug.Assert(_isPowerOfTwo(_minCapacity), "MinCapacity must be power of 2.");
                return _minCapacity - 1;
            }

            Debug.Assert(sizeof(uint) == 4); // 32 bit integer

            // set all bits after the leftmost set bit

            uint mask = capacity - 1;

            mask |= mask >> 1;
            mask |= mask >> 2;
            mask |= mask >> 4;
            mask |= mask >> 8;
            mask |= mask >> 16;

            return mask;
        }

        long _get_max_int_key()
        {
            long max = -1;
            var data = this._data;
            for (int i = 0; i < this._dataUsed; i++)
            {
                ref var k = ref data[i];
                if (!k.IsDeleted && k.Key.IsInteger && k.Key.Integer > max)
                {
                    max = k.Key.Integer;
                }
            }

            return max;
        }

        public OrderedDictionary(uint capacity)
        {
            _initialize(_tableMask(capacity));
        }

        public OrderedDictionary()
        {
            _initialize(_minTableMask); // (2^3 - 1)
        }

        /// <summary>
        /// Creates clone of the given object.
        /// </summary>
        private OrderedDictionary(OrderedDictionary/*<TValue>*/ from)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));

            _mask = from._mask;
            _data = (Bucket[])from._data.Clone();
            _hash = (int[])from._hash?.Clone();
            _dataUsed = from._dataUsed;
            _dataDeleted = from._dataDeleted;
            _size = from._size;
            //nInternalPointer = from.nInternalPointer;
            _maxIntKey = from._maxIntKey;
        }

        internal OrderedDictionary/*!*/AddRef()
        {
            Interlocked.Increment(ref _refs);
            return this;
        }

        private void _decRef()
        {
            Interlocked.Decrement(ref _refs);
        }

        /// <summary>
        /// Releases one <see cref="_refs"/> and creates new deeply copied instance of <see cref="OrderedDictionary"/>.
        /// </summary>
        internal OrderedDictionary/*!*/ReleaseRef()
        {
            var clone = new OrderedDictionary(this);
            clone._inplace_clone(this);

            _decRef();

            Debug.Assert(_refs >= 0);

            //

            return clone;
        }

        private void _inplace_clone(OrderedDictionary cloned_from)
        {
            var data = this._data;
            var num = this._dataUsed;

            PhpAlias aliased_self = null;

            for (int i = 0; i < num; i++)
            {
                ref var bucket = ref data[i];

                if (bucket.IsDeleted)
                {
                    continue;
                }

                if (bucket.Value.Object is PhpAlias alias && alias.Value.Object is PhpArray array && ReferenceEquals(array.table, cloned_from))
                {
                    bucket.Value = PhpValue.Create(aliased_self ?? (aliased_self = new PhpAlias(new PhpArray(this))));
                }
                else
                {
                    bucket.Value = bucket.Value.DeepCopy();
                }
            }
        }

        private void _initialize(uint mask)
        {
            var size = mask + 1;

            Debug.Assert(_isPowerOfTwo(size));

            //

            _mask = mask;
            _data = new Bucket[size];
            _hash = null; // no keys
            _dataUsed = 0;
            _dataDeleted = 0;
            _size = size;
            //nInternalPointer = _invalidIndex;
            _maxIntKey = -1;
        }

        private void _resize(uint size)
        {
            Debug.Assert(size > _size);
            Debug.Assert(_isPowerOfTwo(size));

            //Array.Resize(ref this.arData, (int)size); // slower

            var newData = new Bucket[size];
            Array.Copy(this._data, 0, newData, 0, this._dataUsed); // NOTE: faster than Memory<T>.CopyTo() and Array.Resize<T>

            _mask = size - 1;
            _data = newData;
            _size = size;

            if (this._hash != null)
            {
                _createhash();
            }
        }

        private void _resize() => _resize(checked(_size * 2));

        private void _rehash()
        {
            var data = this._data;
            var hash = this._hash;

            Debug.Assert(hash != null, "no hash");
            Debug.Assert(data.Length == hash.Length, "internal array size mismatch");

            hash.AsSpan().Fill(_invalidIndex);  // some optimizations
            //Array.Fill(hash, _invalidIndex);  // simple for-loop

            for (int i = this._dataUsed - 1; i >= 0; i--)
            {
                ref var bucket = ref data[i];

                if (bucket.IsDeleted)
                {
                    bucket.Next = _invalidIndex;
                    continue;
                }

                ref var index = ref hash[_index(bucket.Key)];

                bucket.Next = index;
                index = i;
            }

            //

            _debug_check();
        }

        private void _createhash()
        {
            this._hash = new int[this._size];
            _rehash();
        }

        private int _index(IntStringKey key) => unchecked((int)_mask & (int)key.Integer);

        [Conditional("DEBUG")]
        private void _debug_check()
        {
            if (this._hash == null)
            {
                Debug.Assert(this._dataDeleted == 0, "packed array has holes!");
            }
            else
            {

            }
        }

        #region Properties

        /// <summary>
        /// Number of items in the dictionary.
        /// </summary>
        public int Count => _dataUsed - _dataDeleted;

        public TValue this[IntStringKey key]
        {
            get
            {
                int i = FindIndex(key);
                return (i >= 0) ? _data[i].Value : TValue.Null; // PERF: double array lookup
            }
            set
            {
                SetValue(key, value);
            }
        }

        /// <summary>
        /// Gets value indicating the array was created in such sequence of operations
        /// so it only contains numeric keys (0..Count] in ascending order.
        /// </summary>
        /// <remarks>Such a data structure does not need a hash table.</remarks>
        public bool IsPacked => _hash == null;

        /// <summary>
        /// Gets value indicating the object is being shared.
        /// </summary>
        public bool IsShared => _refs != 0;

        #endregion

        #region IColection<TValue> Values

        sealed class ValuesCollection : ICollection<TValue>
        {
            readonly OrderedDictionary/*<TValue>*/ _array;

            public ValuesCollection(OrderedDictionary/*<TValue>*/ array)
            {
                _array = array;
            }

            public int Count => _array.Count;

            public bool IsReadOnly => true;

            public void Add(TValue item) => throw new NotSupportedException();

            public void Clear() => throw new NotSupportedException();

            public bool Remove(TValue item) => throw new NotSupportedException();

            public bool Contains(TValue item)
            {
                var enumerator = _array.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.CurrentValue.Equals(item)) return true;
                }

                return false;
            }

            public void CopyTo(TValue[] array, int arrayIndex) => _array.CopyTo(array, arrayIndex);

            public IEnumerator<TValue> GetEnumerator()
            {
                if (Count != 0)
                {
                    return new Enumerator(_array);
                }
                else
                {
                    return EmptyEnumerator<TValue>.Instance;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public ICollection<TValue> Values => new ValuesCollection(this);

        #endregion

        #region ICollection<IntStringKey> Keys

        sealed class KeysCollection : ICollection<IntStringKey>
        {
            readonly OrderedDictionary/*<TValue>*/ _array;

            public KeysCollection(OrderedDictionary/*<TValue>*/ array)
            {
                _array = array;
            }

            public int Count => _array.Count;

            public bool IsReadOnly => true;

            public void Add(IntStringKey item) => throw new NotSupportedException();

            public void Clear() => throw new NotSupportedException();

            public bool Remove(IntStringKey item) => throw new NotSupportedException();

            public bool Contains(IntStringKey item) => _array.ContainsKey(item);

            public void CopyTo(IntStringKey[] array, int arrayIndex)
            {
                if (array == null || arrayIndex < 0 || (arrayIndex + _array.Count) > array.Length)
                    throw new ArgumentException();

                var enumerator = _array.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    array[arrayIndex++] = enumerator.CurrentKey;
                }
                // FastEnumerator does not have to be disposed
            }

            public IEnumerator<IntStringKey> GetEnumerator()
            {
                if (Count != 0)
                {
                    return new Enumerator(_array);
                }
                else
                {
                    return EmptyEnumerator<IntStringKey>.Instance;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public ICollection<IntStringKey> Keys => new KeysCollection(this);

        #endregion

        #region Methods: Add*, Get*, Remove, TryRemoveLast, Clear

        /// <summary>
        /// Gets index of the item within private <see cref="_data"/> array.
        /// </summary>
        /// <param name="key">Key to find.</param>
        /// <returns>Index of the bucket or <see cref="_invalidIndex"/> if key was not found.</returns>
        private int FindIndex(IntStringKey key)
        {
            int index;

            if (IsPacked)
            {
                // packed array
                // NOTE: packed array cannot be larger than Int32.MaxValue

                if (key.IsInteger && key.Integer >= 0 && key.Integer < _dataUsed)
                {
                    Debug.Assert(!_data[key.Integer].IsDeleted);
                    index = (int)key.Integer;
                }
                else
                {
                    index = _invalidIndex;
                }
            }
            else
            {
                // indexed array

                index = _hash[_index(key)];

                while (index >= 0)
                {
                    ref var bucket = ref _data[index];
                    if (key.Equals(bucket.Key))
                    {
                        break;
                    }

                    index = bucket.Next;
                }
            }

            //

            return index;
        }

        /// <summary>
        /// Tries to get value at specified key.
        /// </summary>
        /// <param name="key">Key to lookup.</param>
        /// <param name="value">If key is in the collection, gets the associated value. Otherwise gets <c>default</c>.</param>
        /// <returns>True if the item was found in the collection.</returns>
        public bool TryGetValue(IntStringKey key, out TValue value)
        {
            var i = FindIndex(key);
            if (i >= 0)
            {
                value = _data[i].Value; // TODO // PERF: double array lookup
                return true;
            }
            else
            {
                value = default; // NULL
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue GetValueOrNull(IntStringKey key)
        {
            var i = FindIndex(key);
            if (i >= 0)
            {
                return _data[i].Value; // PERF: double array lookup
            }
            else
            {
                // TODO: warning
                return TValue.Null;
            }
        }

        /// <summary>
        /// Ensures the item is present in the collection and gets a reference to it.
        /// </summary>
        internal ref TValue EnsureValue(IntStringKey key)
        {
            Debug.Assert(!IsShared);

            int i = FindIndex(key);

            if (i >= 0)
            {
                return ref _data[i].Value; // PERF: double array lookup
            }

            // add NULL item:
            return ref Add_Impl(key, TValue.Null);
        }

        /// <summary>
        /// Gets value indicating the item with given <paramref name="key"/> is in the collection.
        /// </summary>
        /// <param name="key">Key to be checked.</param>
        /// <returns><c>true</c> iff an item with the key exists.</returns>
        public bool ContainsKey(IntStringKey key) => FindIndex(key) >= 0;

        /// <summary>
        /// Assigns value using PHP's assign operator.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Non-aliased value to be assigned.</param>
        public void AssignValue(IntStringKey key, TValue value)
        {
            Debug.Assert(!IsShared);

            var i = FindIndex(key);
            if (i < 0)
            {
                Add_Impl(key, value);
            }
            else
            {
                Operators.SetValue(ref _data[i].Value, value);
            }
        }

        /// <summary>
        /// Sets the value at given key.
        /// Previous value will be overriden.
        /// </summary>
        private void SetValue(IntStringKey key, TValue value)
        {
            Debug.Assert(!IsShared);

            var i = FindIndex(key);
            if (i < 0)
            {
                Add_Impl(key, value);
            }
            else
            {
                _data[i].Value = value;
            }
        }

        /// <summary>
        /// Adds value to the end of collection with newly assigned numeric key.
        /// </summary>
        public void Add(TValue value)
        {
            var key = _maxIntKey;
            if (key < IntStringKey.MaxKeyValue)
            {
                Add_NoCheck(new IntStringKey(_maxIntKey = key + 1), value);
            }
            else
            {
                PhpException.NextArrayKeyUnavailable();
            }
        }

        /// <summary>
        /// Adds item at the end of collection.
        /// Does not check the <paramref name="key"/> exists already!
        /// Updates <see cref="_maxIntKey"/> if necessary.
        /// </summary>
        /// <param name="key">Item key.</param>
        /// <param name="value">Item value.</param>
        internal ref TValue Add_Impl(IntStringKey key, TValue value)
        {
            ref var bucket = ref Add_NoCheck(key, value);

            if (key.IsInteger && key.Integer > _maxIntKey)
            {
                _maxIntKey = key.Integer;
            }

            return ref bucket.Value;
        }

        /// <summary>
        /// Adds item at the end of collection.
        /// Does not check the <paramref name="key"/> exists already!
        /// Does not update <see cref="_maxIntKey"/>.
        /// </summary>
        /// <param name="key">Item key.</param>
        /// <param name="value">Item value.</param>
        private ref Bucket Add_NoCheck(IntStringKey key, TValue value)
        {
            Debug.Assert(FindIndex(key) < 0);
            Debug.Assert(!IsShared);

            var i = this._dataUsed;
            if (i >= this._size)
            {
                _resize();
            }

            ref var bucket = ref _data[i];

            bucket.Key = key;
            bucket.Value = value;

            this._dataUsed = i + 1; // TODO: Overflow check

            // hash table

            if (_hash == null)
            {
                if (key.Integer != i || key.IsString)
                {
                    // upgrade to index array:
                    _createhash();
                }
            }
            else
            {
                ref var index = ref _hash[_index(key)];

                bucket.Next = index;
                index = i;
            }

            //

            return ref bucket;
        }

        /// <summary>
        /// Remove item with given key.
        /// </summary>
        /// <returns><c>true</c> if the item was found and removed, otherwise <c>false</c>.</returns>
        public bool Remove(IntStringKey key)
        {
            Debug.Assert(!IsShared);

            //var i = FindEntry(key);
            //if (i >= 0)
            //{
            //    RemoveBucket(i);
            //    return true;
            //}
            //else
            //{
            //    return false;
            //}

            if (this._hash == null)
            {
                // packed array

                var i = key.Integer;
                if (i >= 0 && i < this._dataUsed && key.IsInteger)
                {
                    ref var bucket = ref this._data[i];
                    bucket.Key = default;
                    bucket.Value = TValue.CreateInvalid();

                    if (i == _dataUsed - 1)
                    {
                        _dataUsed--; // deleting last entry, will be reused
                    }
                    else
                    {
                        // deleting somewhere inside the allocated array:
                        _dataDeleted++;

                        _createhash();
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                // indexed array

                ref var i = ref this._hash[_index(key)];
                while (i >= 0)
                {
                    ref var bucket = ref this._data[i];
                    if (key.Equals(bucket.Key))
                    {
                        bucket.Key = default;
                        bucket.Value = TValue.CreateInvalid();

                        if (i == _dataUsed - 1)
                        {
                            _dataUsed--; // deleting last entry, will be reused
                        }
                        else
                        {
                            _dataDeleted++; // hole
                        }

                        i = bucket.Next; // unlink bucket from the linked list

                        return true;
                    }

                    i = ref bucket.Next;
                }

                return false;
            }
        }

        /// <summary>
        /// Remove the last item in the array and return it, if it exists. If the next free index was its index,
        /// decrement it (semantics of <c>array_pop</c>).
        /// </summary>
        /// <param name="value">The removed item with its key.</param>
        /// <returns><c>true</c> if the array was non-empty and the item was removed, otherwise <c>false</c></returns>
        public bool TryRemoveLast(out KeyValuePair<IntStringKey, TValue> value)
        {
            var enumerator = GetEnumerator();
            if (enumerator.MoveLast())
            {
                value = enumerator.Current;
                enumerator.DeleteCurrent();

                // array_pop decrements the next free index if it removed the last record before it
                if (value.Key.IsInteger)
                {
                    var intkey = value.Key.Integer;
                    if (intkey == _maxIntKey && intkey >= 0)
                    {
                        _maxIntKey--;
                    }
                }

                return true;
            }

            //

            value = default;
            return false;
        }

        /// <summary>
        /// Prepends an item.
        /// </summary>
        /// <remarks>Used mostly by <c>array_unshift</c>.</remarks>
        public void AddFirst(IntStringKey key, TValue value)
        {
            Debug.Assert(!IsShared);

            if (FindIndex(key) >= 0)
            {
                throw new ArgumentException();
            }

            if (Count == 0)
            {
                // no items, normal add:
                Add_Impl(key, value);
                return;
            }

            if (this._dataDeleted == 0 || !this._data[0].IsDeleted)
            {
                // move items and create hole at beginning:
                if (this._dataUsed == this._size)
                {
                    _resize();
                }

                Array.Copy(this._data, 0, this._data, 1, this._dataUsed); // faster
                //this.arData.AsMemory(0, this._dataUsed).CopyTo(this.arData.AsMemory(1, this._dataUsed)); // slower
                this._data[0].Value = TValue.CreateInvalid();
                Debug.Assert(this._data[0].IsDeleted);

                this._dataUsed++;
                this._dataDeleted++;

                if (this._hash == null)
                {
                    _createhash();
                }
                else
                {
                    _rehash();
                }
            }

            Debug.Assert(this._hash != null); // must be indexed array (Count != 0 && there is deleted a bucket)

            ref var bucket = ref this._data[0];
            bucket.Key = key;
            bucket.Value = value;

            ref var index = ref _hash[_index(key)];
            bucket.Next = index;
            index = 0;

            //

            this._dataDeleted--;
        }

        /// <summary>
        /// Clears the data.
        /// </summary>
        public void Clear()
        {
            Debug.Assert(!IsShared);

            // just drop everything:
            _initialize(_minTableMask);
        }

        #endregion

        #region CopyTo

        /// <summary>
        /// Copy key-value pairs into given array.
        /// </summary>
        /// <param name="array">Target array.</param>
        /// <param name="arrayIndex">Index where to start copying in <paramref name="array"/>.</param>
        public void CopyTo(KeyValuePair<IntStringKey, TValue>[] array, int arrayIndex)
        {
            if (array == null || arrayIndex < 0 || (arrayIndex + this.Count) > array.Length)
                throw new ArgumentException();

            var enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                array[arrayIndex++] = enumerator.Current;
            }
            // FastEnumerator does not have to be disposed
        }

        /// <summary>
        /// Copy values into given array.
        /// </summary>
        /// <param name="array">Target array.</param>
        /// <param name="arrayIndex">Index where to start copying in <paramref name="array"/>.</param>
        public void CopyTo(TValue[] array, int arrayIndex)
        {
            if (array == null || arrayIndex < 0 || (arrayIndex + this.Count) > array.Length)
                throw new ArgumentException();

            var enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                array[arrayIndex++] = enumerator.CurrentValue;
            }
            // FastEnumerator does not have to be disposed
        }

        #endregion

        #region Shuffle, Reverse, Sort, SetOperation

        /// <summary>
        /// Shiffles order of the items randomly.
        /// </summary>
        public void Shuffle(Random/*!*/generator)
        {
            if (generator == null)
            {
                throw new ArgumentNullException(nameof(generator));
            }

            if (this.Count <= 1)
            {
                // nothing to shuffle
                return;
            }

            // shuffle and compact elements:

            var newData = new Bucket[_size];
            var i = 0; // where to put next element

            var enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                ref var bucket = ref newData[i];

                int j = generator.Next(i + 1); // [0 .. i)
                if (j < i)
                {
                    // [i] = [j]
                    // [j] = current
                    bucket = newData[j];
                    bucket = ref newData[j];
                }

                bucket.Key = current.Key;
                bucket.Value = current.Value;

                i++;
            }

            _data = newData;
            _dataDeleted = 0;
            _dataUsed = i;
            //nInternalPointer = 0;

            // always indexed array after shuffle:

            if (this._hash == null)
            {
                this._hash = new int[this._size];
            }

            _rehash();
        }

        /// <summary>
        /// Reverses order of the items.
        /// </summary>
        public void Reverse()
        {
            if (Count <= 1)
            {
                return;
            }

            // copy elements in reverse order and compact:

            var newData = new Bucket[_size];
            var i = Count; // where to put next element

            var enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                ref var bucket = ref newData[--i];

                bucket.Key = current.Key;
                bucket.Value = current.Value;
            }

            _data = newData;
            _dataUsed = Count; // before changing _dataDeleted !!
            _dataDeleted = 0;
            //nInternalPointer = 0;

            // always indexed array after shuffle:

            if (this._hash == null)
            {
                this._hash = new int[this._size];
            }

            _rehash();
        }

        /// <summary>Helper comparer that wraps <see cref="IComparer{KeyValuePair}"/>.</summary>
        sealed class BucketsComparer : IComparer<Bucket>
        {
            readonly IComparer<KeyValuePair<IntStringKey, TValue>>/*!*/_comparer;

            /// <summary>
            /// Gets value indicating the comparer returned a positive value at some point.
            /// </summary>
            public bool DidSwap { get; private set; }

            public BucketsComparer(IComparer<KeyValuePair<IntStringKey, TValue>> comparer)
            {
                _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
            }

            public int Compare(Bucket x, Bucket y)
            {
                if (x.IsDeleted) { return y.IsDeleted ? 0 : +1; }   // keep undefined entries on right
                if (y.IsDeleted) { DidSwap = true; return -1; }     // move undefined entries to right

                // compare

                var cmp = _comparer.Compare(x.AsKeyValuePair(), y.AsKeyValuePair());
                if (cmp < 0) DidSwap = true;
                return cmp;
            }
        }

        /// <summary>
        /// Sorts the collection.
        /// </summary>
        public void Sort(IComparer<KeyValuePair<IntStringKey, TValue>>/*!*/comparer)
        {
            // TODO: packed array - KEY ASC already sorted

            if (Count > 1)
            {
                var bucketcomparer = new BucketsComparer(comparer);
                Array.Sort(this._data, 0, _dataUsed, bucketcomparer);

                // array was compacted,
                // holes (deleted items) moved to the end
                this._dataUsed = Count;
                this._dataDeleted = 0;
                //this.nInternalPointer = 0;

                //if (bucketcomparer.DidSwap) // TODO: is it correct?   // check the comparer swapped something, if not we don't have to _rehash()
                {
                    if (this._hash == null)
                    {
                        this._hash = new int[this._size];
                    }

                    _rehash();
                }
            }
        }

        /// <summary>
        /// Helper comparer for array of indices into array of hash tables. Used for <c>multisort</c>.
        /// </summary>
        sealed class MultisortComparer : IComparer<Bucket[]>
        {
            readonly IComparer<KeyValuePair<IntStringKey, TValue>>[]/*!*/ comparers;

            public MultisortComparer(IComparer<KeyValuePair<IntStringKey, TValue>>[]/*!*/ comparers)
            {
                this.comparers = comparers;
            }

            public int Compare(Bucket[] x, Bucket[] y)
            {
                Debug.Assert(x != null && y != null);
                Debug.Assert(x.Length == y.Length);

                for (int i = 0; i < x.Length; i++)
                {
                    var cmp = comparers[i].Compare(x[i].AsKeyValuePair(), y[i].AsKeyValuePair());
                    if (cmp != 0)
                    {
                        return cmp;
                    }
                }

                return 0;
            }
        }

        public static void Sort(
            int count,
            PhpArray[]/*!*/ hashtables,
            IComparer<KeyValuePair<IntStringKey, TValue>>[]/*!*/ comparers)
        {
            Debug.Assert(hashtables != null);

            // create matrix Count*N with indices to hashtables
            var idx = new Bucket[count][]; // rows of our sort algorithm, we will be swapping entire rows

            for (int i = 0; i < idx.Length; i++)
            {
                idx[i] = new Bucket[hashtables.Length];
            }

            for (int h = 0; h < hashtables.Length; h++)
            {
                var table = hashtables[h].table;

                Debug.Assert(table.Count == count);

                var data = table._data;
                var ndata = table._dataUsed;
                int n = 0;

                for (int i = 0; i < ndata && n < count; i++)
                {
                    ref var bucket = ref data[i];
                    if (bucket.IsDeleted == false)
                    {
                        idx[n++][h] = bucket;
                    }
                }
            }

            // sort indices
            Array.Sort(idx, comparer: new MultisortComparer(comparers));

            //
            for (int h = 0; h < hashtables.Length; h++)
            {
                var table = hashtables[h].table;
                hashtables[h].RestartIntrinsicEnumerator();

                int ikey = 0;
                for (int i = 0; i < count; i++)
                {
                    ref var bucket = ref idx[i][h];
                    if (bucket.Key.IsInteger)
                    {
                        bucket.Key = ikey++;
                    }

                    table._data[i] = bucket;
                }

                table._dataUsed = count;
                table._dataDeleted = 0;
                table._maxIntKey = ikey - 1;

                if (ikey == count)
                {
                    table._hash = null; // packed now for sure
                }
                else
                {
                    Debug.Assert(table._hash != null);
                    table._rehash();
                }
            }
        }

        public OrderedDictionary/*<TValue>*//*!*/SetOperation(SetOperations op, PhpArray/*<TValue>*/[]/*!!*/others, IComparer<KeyValuePair<IntStringKey, TValue>>/*!*/comparer)
        {
            Debug.Assert(op == SetOperations.Difference || op == SetOperations.Intersection);
            Debug.Assert(others != null, "others == null");
            Debug.Assert(comparer != null, "comparer == null");

            if (this.Count == 0)
            {
                return new OrderedDictionary/*<TValue>*/();
            }

            var bucketcomparer = new BucketsComparer(comparer);

            //
            // create following arrays, sort them, perform the op:
            //
            // resultData[0..table_size] // will be used as the resulting array
            // array_0[0..[0]._dataUsed]
            // array_1[0..[1]._dataUsed]
            // ...
            // array_N[0..[N]._dataUsed]
            //

            var result = new OrderedDictionary/*<TValue>*/(this._size)
            {
                _dataUsed = this.Count
            };

            var resultData = result._data;
            var resultIndexes = new int[result._dataUsed];

            var enumerator = this.GetEnumerator();
            for (int i = 0; i < resultIndexes.Length; i++)
            {
                enumerator.MoveNext(); // assert: true
                var current = enumerator.Current;
                resultData[i] = new Bucket { Key = current.Key, Value = current.Value, Next = _invalidIndex, };
                resultIndexes[i] = i;
            }
            Debug.Assert(!enumerator.MoveNext());

            Array.Sort(resultData, resultIndexes, 0, result._dataUsed, comparer: bucketcomparer); // QSort log(N)

            for (int i = 0; i < others.Length && result.Count > 0; i++)
            {
                var array = others[i]?.table;

                // two special cases:

                if (array == null || array.Count == 0)
                {
                    if (op == SetOperations.Intersection) return new OrderedDictionary/*<TValue>*/();
                    else /*if (op == SetOperations.Difference)*/ continue;
                }

                if (array == this)
                {
                    if (op == SetOperations.Intersection) continue;
                    else /*if (op == SetOperations.Difference)*/ return new OrderedDictionary/*<TValue>*/();
                }

                //

                var data = new Bucket[array._dataUsed];
                var dataLength = array.Count; // unused buckets moved to end
                //array.arData.AsMemory(0, array._dataUsed).CopyTo(data); // slower
                Array.Copy(array._data, data, array._dataUsed); // faster
                Array.Sort(data, bucketcomparer);   // QSort log(N)

                // op:

                int result_i = 0;
                int array_i = 0;

                while (result_i < result._dataUsed && array_i < dataLength)
                {
                    if (i > 0 && resultData[result_i].IsDeleted)
                    {
                        result_i++;
                        continue;
                    }

                    var cmp = bucketcomparer.Compare(resultData[result_i], data[array_i]);
                    if (cmp > 0)
                    {
                        array_i++;
                    }
                    else if (cmp < 0 ^ op == SetOperations.Difference) // cmp == 0 && difference || cmp < 0 && intersect
                    {
                        resultData[result_i].Value = TValue.CreateInvalid();
                        Debug.Assert(resultData[result_i].IsDeleted);

                        result._dataDeleted++;
                        result_i++;
                    }
                    else
                    {
                        result_i++;
                    }
                }

                if (op == SetOperations.Intersection) // delete remaining not matched elements
                {
                    while (result_i < result._dataUsed)
                    {
                        resultData[result_i].Value = TValue.CreateInvalid();
                        Debug.Assert(resultData[result_i].IsDeleted);
                        result._dataDeleted++;
                        result_i++;
                    }
                }
            }

            // trim _dataUsed:
            if (result._dataDeleted == result._dataUsed)
            {
                result._dataUsed = 0;
                result._dataDeleted = 0;
                //result.nNextFreeElement = 0;
            }
            else
            {
                for (int i = result._dataUsed - 1; i >= 0 && result._dataDeleted > 0 && resultData[i].IsDeleted; i--)
                {
                    result._dataDeleted--;
                    result._dataUsed--;
                }

                // nNextFreeElement
                result._maxIntKey = result._get_max_int_key();

                // restore the order
                Array.Sort(resultIndexes, resultData, 0, result._dataUsed);

                // create hash (result is most probably not packed array)
                result._createhash();
            }

            //

            return result;
        }

        #endregion

        #region ReindexAll, ReindexAndReplace

        /// <summary>
        /// Sets all keys to increasing integers according to their respective order in the list.
        /// </summary>
        public void ReindexAll()
        {
            if (IsPacked) // only if dictionary was not packed, otherwise it does not make sense
            {
                return;
            }

            Debug.Assert(_hash != null);

            var data = this._data;
            var key = 0;

            for (int i = 0; i < _dataUsed; i++)
            {
                ref var bucket = ref data[i];
                if (!bucket.IsDeleted)
                {
                    bucket.Key = key++;
                }
            }

            if (key > 0) // only if there were any keys
            {
                this._maxIntKey = key - 1;
                this._rehash();
            }
        }

        /// <summary>
        /// Sets all keys to increasing integers according to their respective order in the list.
        /// </summary>
        /// <param name="startIndex">An index from which to start indexing.</param>
        /// <remarks>If indexing overflows a capacity of integer type it continues with <see cref="int.MinValue"/>.</remarks>
        public void ReindexIntegers(int startIndex)
        {
            if (IsPacked && startIndex == 0)
            {
                // no need for reindexing and rehashing
                return;
            }

            var data = this._data;
            var key = startIndex;

            for (int i = 0; i < _dataUsed; i++)
            {
                ref var bucket = ref data[i];
                if (!bucket.IsDeleted && bucket.Key.IsInteger)
                {
                    bucket.Key = key++;
                }
            }

            //
            if (key > startIndex) // only if there were any integer keys
            {
                this._maxIntKey = key - 1;

                if (_hash == null) this._createhash();
                else this._rehash();
            }
        }

        /// <summary>
        /// Replaces a part of the hashtable with specified item(s) and reindexes all integer keys in result.
        /// </summary>
        /// <param name="offset">
        /// The ordinary number of the first item to be replaced. 
        /// <paramref name="offset"/> should be at least zero and at most equal as the number of items in the array.
        /// </param>
        /// <param name="length">
        /// The number of items to be replaced. Should be at least zero and at most equal 
        /// to the number of items in the array.
        /// </param>
        /// <param name="replacementValues">
        /// The enumerable collection of values by which items in the range specified by
        /// <paramref name="offset"/> and <paramref name="length"/> is replaced.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><pararef name="offset"/> or <paramref name="length"/> has invalid value.</exception>
        public OrderedDictionary ReindexAndReplace(int offset, int length, ICollection<TValue> replacementValues)
        {
            int count = this.Count;

            if (offset < 0 || offset > count)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0 || offset + length > count)
                throw new ArgumentOutOfRangeException(nameof(length));

            var replaced = new OrderedDictionary((uint)length);

            var newcapacity = (uint)(_dataUsed + (replacementValues != null ? replacementValues.Count : 0) - length);
            var newself = new OrderedDictionary(newcapacity);

            int ikey = 0; // next integer key
            int n = 0;

            var enumerator = GetEnumerator();

            // reindex [0..offset)
            while (n < offset && enumerator.MoveNext())
            {
                ref var bucket = ref enumerator.Bucket;

                // reindexes integer keys of elements before the first replaced item:
                if (bucket.Key.IsInteger)
                {
                    bucket.Key = ikey++;
                }

                newself._data[n++] = bucket;
            }

            // remove [offset..offset+length)

            int removed = 0;
            int rkey = 0;
            while (removed++ < length && enumerator.MoveNext())
            {
                ref var bucket = ref enumerator.Bucket;
                if (bucket.Key.IsInteger)
                {
                    replaced.Add_NoCheck(rkey++, bucket.Value);
                }
                else
                {
                    replaced.Add_NoCheck(bucket.Key, bucket.Value);
                }
            }

            replaced._maxIntKey = rkey - 1;

            // adds new elements at newtarget:
            if (replacementValues != null && replacementValues.Count != 0)
            {
                foreach (var value in replacementValues)
                {
                    newself._data[n++] = new Bucket { Key = ikey++, Value = value.DeepCopy(), Next = _invalidIndex, };
                }
            }

            // reindex [offset+length..)
            while (enumerator.MoveNext())
            {
                ref var bucket = ref enumerator.Bucket;

                // reindexes integer keys of elements before the first replaced item:
                if (bucket.Key.IsInteger)
                {
                    bucket.Key = ikey++;
                }

                newself._data[n++] = bucket;
            }

            //

            newself._maxIntKey = ikey - 1;
            newself._dataUsed = n;
            newself._dataDeleted = 0;
            newself._createhash();
            newself._debug_check();

            _size = newself._size;
            _mask = newself._mask;
            _data = newself._data;
            _hash = newself._hash;
            _dataUsed = newself._dataUsed;
            _dataDeleted = newself._dataDeleted; // 0
            _maxIntKey = newself._maxIntKey; // ikey - 1

            _debug_check();

            //

            return replaced;
        }

        #endregion

        #region IEnumerable<KeyValuePair<IntStringKey, TValue>>, IEnumerable<TValue>

        /// <summary>
        /// Enumerator object.
        /// </summary>
        internal class Enumerator : IEnumerator<KeyValuePair<IntStringKey, TValue>>, IEnumerator<TValue>, IEnumerator<IntStringKey>, IPhpEnumerator
        {
            FastEnumerator/*!*/_enumerator;

            internal OrderedDictionary UnderlayingArray => _enumerator.UnderlayingArray;

            public Enumerator(OrderedDictionary/*<TValue>*/ array)
            {
                _enumerator = array.GetEnumerator();
            }

            public KeyValuePair<IntStringKey, TValue> Current => _enumerator.Current;

            public bool AtEnd => _enumerator.AtEnd;

            /// <summary>
            /// Current dereferenced value.
            /// </summary>
            public virtual TValue CurrentValue => _enumerator.CurrentValue.GetValue();

            public PhpAlias CurrentValueAliased => _enumerator.CurrentValueAliased;

            public TValue CurrentKey => _enumerator.CurrentKey;

            IntStringKey IEnumerator<IntStringKey>.Current => _enumerator.CurrentKey;

            TValue IEnumerator<TValue>.Current => _enumerator.CurrentValue;

            object IEnumerator.Current => _enumerator.CurrentValue.ToClr();

            KeyValuePair<TValue, TValue> IEnumerator<KeyValuePair<TValue, TValue>>.Current => new KeyValuePair<TValue, TValue>(CurrentKey, CurrentValue);

            public virtual void Dispose()
            {
                _enumerator = default;
            }

            public bool MoveFirst()
            {
                _enumerator.Reset();
                return _enumerator.MoveNext();
            }

            public bool MoveLast() => _enumerator.MoveLast();

            public bool MoveNext() => _enumerator.MoveNext();

            public bool MovePrevious() => _enumerator.MovePrevious();

            public void Reset() => _enumerator.Reset();
        }

        /// <summary>
        /// Performs enumeration on the current state of the array.
        /// Array can be changed during the enumeration with no effect to this enumerator.
        /// </summary>
        internal sealed class ReadonlyEnumerator : Enumerator
        {
            public ReadonlyEnumerator(OrderedDictionary/*!*/array)
                : base(array.AddRef()) // enumerates over readonly copy of given array
            {

            }

            public override TValue CurrentValue => base.CurrentValue.DeepCopy();

            public override void Dispose()
            {
                UnderlayingArray._decRef();
                base.Dispose();
            }
        }

        /// <summary>
        /// Gets enumerator object.
        /// </summary>
        IEnumerator<KeyValuePair<IntStringKey, TValue>> IEnumerable<KeyValuePair<IntStringKey, TValue>>.GetEnumerator()
        {
            if (Count != 0)
            {
                return new Enumerator(this);
            }
            else
            {
                return EmptyEnumerator<KeyValuePair<IntStringKey, TValue>>.Instance;
            }
        }

        /// <summary>
        /// Gets enumerator object.
        /// </summary>
        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
        {
            if (Count != 0)
            {
                return new Enumerator(this);
            }
            else
            {
                return EmptyEnumerator<TValue>.Instance;
            }
        }

        /// <summary>
        /// Gets enumerator object.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<IntStringKey, TValue>>)this).GetEnumerator();

        #endregion

        #region Enumerable (value type)

        /// <summary>
        /// Enumerator not allocating any memory.
        /// </summary>
        public struct FastEnumerator
        {
            readonly OrderedDictionary/*<TValue>*/ _array;
            int _i;

            internal OrderedDictionary UnderlayingArray => _array;

            public FastEnumerator(OrderedDictionary/*<TValue>*/ array)
                : this(array, _invalidIndex)
            {
            }

            public FastEnumerator(OrderedDictionary/*<TValue>*/ array, int i)
            {
                Debug.Assert(array != null);

                _array = array;
                _i = i;
            }

            /// <summary>
            /// Gets current key-value pair.
            /// </summary>
            public KeyValuePair<IntStringKey, TValue> Current => Bucket.AsKeyValuePair();

            /// <summary>
            /// Gets current key.
            /// </summary>
            public IntStringKey CurrentKey
            {
                get => _array._data[_i].Key;
                internal set => _array._data[_i].Key = value; // NOTE: array must be rehashed
            }

            /// <summary>
            /// Gets ref to current value.
            /// </summary>
            public ref TValue CurrentValue => ref Bucket.Value;

            /// <summary>
            /// Ensures the current entry is wrapped in alias and gets its reference.
            /// </summary>
            public PhpAlias CurrentValueAliased => PhpValue.EnsureAlias(ref Bucket.Value);

            /// <summary>
            /// Move to the next item.
            /// </summary>
            public bool MoveNext() => MoveNext(_array, ref _i);

            /// <summary>
            /// Tries to move to a previous item.
            /// </summary>
            public bool MovePrevious() => MovePrevious(_array, ref _i);

            /// <summary>
            /// Moves to the last item.
            /// </summary>
            public bool MoveLast()
            {
                _i = _array._dataUsed;
                return MovePrevious();
            }

            /// <summary>
            /// Resets the enumeration.
            /// </summary>
            public void Reset()
            {
                _i = _invalidIndex;
            }

            #region Helpers

            internal static bool MoveNext(OrderedDictionary array, ref int i)
            {
                Debug.Assert(i >= -1);

                do
                {
                    if (++i >= array._dataUsed)
                    {
                        return false;
                    }
                } while (array._data[i].IsDeleted);

                return true;
            }

            internal static bool MovePrevious(OrderedDictionary array, ref int i)
            {
                do
                {
                    if (--i < 0) { i = _invalidIndex; return false; }
                } while (array._data[i].IsDeleted);

                return true;
            }

            internal static bool MoveLast(OrderedDictionary array, out int i)
            {
                i = array._dataUsed;
                return MovePrevious(array, ref i);
            }

            internal static bool EnsureValid(OrderedDictionary array, ref int i)
            {
                if (i >= 0)
                {
                    // skips deleted entries if any
                    while (i < array._dataUsed)
                    {
                        if (array._data[i].IsDeleted)
                        {
                            i++;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            internal ref Bucket Bucket => ref _array._data[_i];

            #endregion

            /// <summary>
            /// Gets value indicating the value is not initialized.
            /// </summary>
            public bool IsDefault => ReferenceEquals(_array, null);

            /// <summary>
            /// Gets value indicating the internal pointer is in bounds.
            /// </summary>
            public bool IsValid => !IsDefault && _i >= 0 && _i < _array._dataUsed;

            /// <summary>
            /// Gets value indicating the enumerator reached the end of array.
            /// </summary>
            public bool AtEnd => _i >= _array._dataUsed;

            /// <summary>
            /// Deletes current entry. Does not move the internal enumerator.
            /// </summary>
            /// <remarks>Call <see cref="MoveNext()"/> or <see cref="MovePrevious()"/> manually.</remarks>
            public void DeleteCurrent()
            {
                if (IsValid)
                {
                    _array.Remove(CurrentKey);
                }
            }
        }

        /// <summary>
        /// Gets enumerator object as a value type.
        /// Used implicitly by C#'s `foreach` construct.
        /// </summary>
        public FastEnumerator GetEnumerator() => new FastEnumerator(this);

        #endregion
    }

    /// <summary>
    /// Extension methods for <see cref="OrderedDictionary"/>.
    /// </summary>
    public static class OrderedDictionaryExtensions
    {
        public static bool TryRemoveFirst(this OrderedDictionary/*<TValue>*/ array, out KeyValuePair<IntStringKey, TValue> value)
        {
            var enumerator = array.GetEnumerator();
            if (enumerator.MoveNext())
            {
                value = enumerator.Current;
                enumerator.DeleteCurrent();
                return true;
            }

            //

            value = default;
            return false;
        }

        /// <summary>
        /// Copies values to a new array.
        /// </summary>
        public static TValue[] GetValues(this OrderedDictionary/*<TValue>*/ table)
        {
            if (table.Count != 0)
            {
                var array = new TValue[table.Count];
                table.CopyTo(array, 0);
                return array;
            }
            else
            {
                return Array.Empty<PhpValue>();
            }
        }
    }
}
