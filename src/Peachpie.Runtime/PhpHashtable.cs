using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core.Utilities;

namespace Pchp.Core
{
    /// <summary>
	/// The hashtable storing entries with <see cref="string"/> and <see cref="int"/> keys in a manner of PHP.
	/// </summary>
	[DebuggerNonUserCode, DebuggerStepThrough]
    public class PhpHashtable : IDictionary<IntStringKey, PhpValue>, IList, IList<PhpValue> // , IDictionary // , ICloneable
    {
        #region Fields and Properties

        /// <summary>
        /// Ordered hashtable where integers are stored.
        /// </summary>
        /// <remarks>
        /// Expose the table to item getters on <see cref="PhpArray"/> to make them a little bit faster.
        /// </remarks>
        internal OrderedDictionary/*!*/ table;

        #endregion

        #region EnsureWritable

        /// <summary>
        /// Ensures the internal <see cref="OrderedDictionary"/> will be writable (not shared).
        /// </summary>
        internal void EnsureWritable()
        {
            if (table.IsShared)
            {
                Unshare();
            }
        }

        private void Unshare()
        {
            Debug.Assert(table.IsShared);
            this.table = this.table.ReleaseRef();
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <c>PhpHashtable</c> class.
        /// </summary>
        public PhpHashtable()
            : this(new OrderedDictionary())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>PhpHashtable</c> class.
        /// </summary>
        /// <param name="capacity">Initial capacity.</param>
        public PhpHashtable(int capacity)
            : this(new OrderedDictionary((uint)capacity))
        {
        }

        internal PhpHashtable(OrderedDictionary table)
        {
            this.table = table;
        }

        /// <summary>
        /// Initializes a new instance of the <c>PhpHashtable</c> class filled by values from specified array. 
        /// </summary>
        /// <param name="values">Values to be added.</param>
        /// <remarks>
        /// Adds all pairs key-value where the value is an item of <v>values</v> array 
        /// and the key is its index in the array.
        /// </remarks>
        public PhpHashtable(Array values)
            : this(values, 0, values.Length)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>PhpHashtable</c> class filled by values from specified array. 
        /// </summary>
        /// <param name="values">Values to be added.</param>
        /// <param name="index">The starting index.</param>
        /// <param name="length">The number of items to add.</param>
        /// <remarks>
        /// Adds at most <c>length</c> pairs key-value where the value is an item of <v>values</v> array 
        /// and the key is its index in the array starting from the <c>index</c>.
        /// </remarks>
        public PhpHashtable(Array values, int index, int length)
            : this(length)
        {
            if (index < 0) throw new ArgumentOutOfRangeException();

            int end = Math.Min(index + length, values.Length);

            for (int i = index; i < end; i++)
            {
                Add(i, PhpValue.FromClr(values.GetValue(i)));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PhpHashtable"/> class filled by values from specified array.
        /// </summary>
        /// <param name="values">An array of values to be added to the table.</param>
        /// <param name="start">An index of the first item from <paramref name="values"/> to add.</param>
        /// <param name="length">A number of items to add.</param>
        /// <param name="value">A value to be filtered.</param>
        /// <param name="doFilter">Wheter to add all items but <paramref name="value"/> (<b>true</b>) or 
        /// all items with the value <paramref name="value"/> (<b>false</b>).</param>
        public PhpHashtable(int[] values, int start, int length, int value, bool doFilter)
            : this(length)
        {
            int end = start + length;
            int max = values.Length;
            if (end > max) end = max;

            if (doFilter)
            {
                for (int i = start; i < end; i++) if (values[i] != value) Add(i, values[i]);
            }
            else
            {
                for (int i = start; i < end; i++) if (values[i] == value) Add(i, value);
            }
        }

        /// <summary>
        /// Creates PhpHashtable that shares internal <see cref="table"/> with another array.
        /// </summary>
        /// <param name="array">The table to be shared.</param>
        public PhpHashtable(PhpHashtable/*!*/array)
            : this(array.table.AddRef())
        {
        }

        #endregion

        #region PHP Enumeration

        /// <summary>
        /// Get fast enumerator structure to be used internally.
        /// </summary>
        public OrderedDictionary.FastEnumerator GetFastEnumerator() => table.GetEnumerator();

        #endregion

        #region IEnumerable<KeyValuePair<IntStringKey, object>> Members

        public IEnumerator<KeyValuePair<IntStringKey, PhpValue>>/*!*/ GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<IntStringKey, PhpValue>>)table).GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)table).GetEnumerator();

        IEnumerator<PhpValue> IEnumerable<PhpValue>.GetEnumerator() => ((IEnumerable<PhpValue>)table).GetEnumerator();

        #endregion

        #region ICollection, ICollection<PhpValue> Members

        /// <summary>Retrieves the number of items in this instance.</summary>
        public virtual int Count { get { return table.Count; } }

        /// <summary>This property is always false.</summary>
        public bool IsSynchronized { get { return false; } }

        /// <summary>This property always refers to this instance.</summary>
        public object SyncRoot { get { return this; } }

        /// <summary>
        /// Copies the <see cref="PhpHashtable"/> or a portion of it to a one-dimensional array.
        /// </summary>
        /// <param name="array">The one-dimensional array.</param>
        /// <param name="index">The zero-based index in array at which copying begins.</param>
        public void CopyTo(Array/*!*/ array, int index)
        {
            foreach (var pair in table)
            {
                array.SetValue(pair.Value.ToClr(), index++);
            }
        }

        void ICollection<PhpValue>.CopyTo(PhpValue[] array, int arrayIndex) => table.CopyTo(array, arrayIndex);

        void ICollection<PhpValue>.Add(PhpValue item) => Add(item);

        bool ICollection<PhpValue>.Contains(PhpValue item)
        {
            var e = GetFastEnumerator();
            while (e.MoveNext())
            {
                if (e.CurrentValue == item)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes all occurances of given value.
        /// </summary>
        bool ICollection<PhpValue>.Remove(PhpValue item)
        {
            bool found = false;

            var e = GetFastEnumerator();
            while (e.MoveNext())
            {
                if (e.CurrentValue.Equals(item))
                {
                    e.DeleteCurrent(); // NOTE: enumerator is in invalid state, expecting to call "MoveNext()"
                    found = true;
                }
            }

            return found;
        }

        #endregion

        #region IDictionary Members

        #region IDictionaryAdapter

        //[Serializable]
        public class IDictionaryAdapter : IDictionaryEnumerator
        {
            #region Fields

            /// <summary>
            /// Currently pointed element.
            /// </summary>
            private OrderedDictionary.FastEnumerator enumerator;

            #endregion

            #region Construction

            public IDictionaryAdapter(PhpHashtable/*!*/table)
            {
                Debug.Assert(table != null);
                this.enumerator = table.GetFastEnumerator();
            }

            #endregion

            #region IDictionaryEnumerator Members

            public DictionaryEntry Entry
            {
                get { return new DictionaryEntry(Key, Value); }
            }

            public object Key
            {
                get
                {
                    return this.enumerator.CurrentKey.Object;
                }
            }

            public object Value
            {
                get
                {
                    return this.enumerator.CurrentValue.ToClr();
                }
            }

            #endregion

            #region IEnumerator Members

            public object Current
            {
                get { return this.Entry; }
            }

            public bool MoveNext()
            {
                return this.enumerator.MoveNext();
            }

            public void Reset()
            {
                this.enumerator.Reset();
            }

            #endregion
        }

        #endregion

        /// <summary>This property is always false.</summary>
		public bool IsFixedSize { get { return false; } }

        /// <summary>This property is always false.</summary>
		public bool IsReadOnly { get { return false; } }

        ///// <summary>
        ///// Returns an enumerator which iterates through values in this instance in order as they were added in it.
        ///// </summary>
        ///// <returns>The enumerator.</returns>
        //IDictionaryEnumerator/*!*/ IDictionary.GetEnumerator()
        //{
        //    if (this.Count == 0)
        //        return OrderedDictionary.EmptyEnumerator.SingletonInstance;

        //    return new IDictionaryAdapter(this); // new GenericDictionaryAdapter<object, object>(GetDictionaryEnumerator(), false);
        //}

        //private IEnumerator<KeyValuePair<object, object>>/*!*/ GetDictionaryEnumerator()
        //{
        //    foreach (KeyValuePair<IntStringKey, object> entry in table)
        //    {
        //        yield return new KeyValuePair<object, object>(entry.Key.Object, entry.Value);
        //    }
        //}

        /// <summary>
        /// Removes all elements from this instance.
        /// </summary>
        public virtual void Clear()
        {
            this.EnsureWritable();

            table.Clear();
        }

        /// <summary>
        /// Determines whether an element with the specified key is in this instance.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Whether an element with the <paramref name="key"/> key is in the table.</returns>
        /// <exception cref="InvalidCastException">The <paramref name="key"/> is neither <see cref="int"/> nor <see cref="string"/>.</exception>
        public bool Contains(object key)
        {
            return this.ContainsKey(IntStringKey.FromObject(key));
        }

        /// <summary>
        /// Adds an entry into the table at its logical end. 
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentException">An element with the same key already exists in this instance.</exception>
        /// <exception cref="InvalidCastException">The <paramref name="key"/> is neither <see cref="int"/> nor not null <see cref="string"/>.</exception>
        public void Add(object key, object value)
        {
            this.Add(IntStringKey.FromObject(key), PhpValue.FromClr(value));
        }

        public void Add(string key, long value) => Add(new IntStringKey(key), value);
        public void Add(string key, string value) => Add(new IntStringKey(key), value);
        public void Add(string key, PhpArray value) => Add(new IntStringKey(key), value);
        public void Add(string key, bool value) => Add(new IntStringKey(key), value);
        public void Add(int key, long value) => Add(new IntStringKey(key), value);
        public void Add(int key, double value) => Add(new IntStringKey(key), value);
        public void Add(int key, string value) => Add(new IntStringKey(key), value);
        public void Add(int key, PhpArray value) => Add(new IntStringKey(key), value);

        /// <summary>
        /// Gets or sets a value associated with a key.
        /// </summary>
        /// <remarks>If the key doesn't exist in table the new entry is added.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is a null reference.</exception>
        /// <exception cref="InvalidCastException">The <paramref name="key"/> is neither <see cref="int"/> nor not null <see cref="string"/>.</exception>
        public object this[object key]
        {
            get
            {
                return this[IntStringKey.FromObject(key)];
            }
            set
            {
                this[IntStringKey.FromObject(key)] = PhpValue.FromClr(value);
            }
        }

        /// <summary>
        /// Removes an entry having the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <exception cref="InvalidCastException">The <paramref name="key"/> is neither <see cref="int"/> nor not null <see cref="string"/>.</exception>
        public void Remove(object key) => Remove(IntStringKey.FromObject(key));

        //ICollection/*!*/ IDictionary.Keys
        //{
        //    get
        //    {
        //        if (_keys == null) _keys = new KeyCollection(this);
        //        return _keys;
        //    }
        //}
        ////[NonSerialized]
        //private KeyCollection _keys;

        //ICollection/*!*/ IDictionary.Values
        //{
        //    get
        //    {
        //        return (ICollection)table.Values;
        //    }
        //}

        #endregion

        #region IList, IList<PhpValue> Members

        /// <summary>
        /// Adds an entry into the table at its logical end. The key is generated automatically.
        /// </summary>
        /// <param name="value">The value to be added.</param>
        /// <return>
        /// 1 if the entry has been added, 0 otherwise. Note, this differs from <see cref="IList.Add"/>
        /// because <see cref="PhpHashtable"/> doesn't support fast retrieval of the element's index.
        /// </return>
        /// <remarks>
        /// The key will be the maximal value of an integer key ever added into this instance plus one
        /// provided the result of addition fits into an 32-bit integer. Otherwise, the entry is not added
        /// and <b>false</b> is returned.
        /// </remarks>
        public int Add(object value)
        {
            return Add(PhpValue.FromClr(value));
        }

        public int Add(string value)
        {
            return Add(PhpValue.Create(value));
        }

        public int Add(PhpValue value)
        {
            this.EnsureWritable();

            table.Add(value);

            return this.Count;
        }

        void IList.RemoveAt(int index)
        {
            EnsureWritable();
            table.Remove(index);
        }

        void IList<PhpValue>.RemoveAt(int index)
        {
            EnsureWritable();
            table.Remove(index);
        }

        void IList.Insert(int index, object value)
        {
            EnsureWritable();
            table[index] = PhpValue.FromClr(value);
        }

        void IList<PhpValue>.Insert(int index, PhpValue value)
        {
            EnsureWritable();
            table[index] = value;
        }

        int IList.IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        void IList.Remove(object value) => ((ICollection<PhpValue>)this).Remove(PhpValue.FromClr(value));

        int IList<PhpValue>.IndexOf(PhpValue value)
        {
            throw new NotImplementedException();
        }

        object IList.this[int index]
        {
            get { return this[index].ToClr(); }
            set { this[index] = PhpValue.FromClr(value); }
        }

        #endregion

        #region IDictionary<IntStringKey,PhpValue> Members

        public void Add(IntStringKey key, PhpValue value) => this[key] = value;

        public bool ContainsKey(IntStringKey key) => table.ContainsKey(key);

        public virtual bool Remove(IntStringKey key)
        {
            this.EnsureWritable();
            return this.table.Remove(key);
        }

        public bool TryGetValue(IntStringKey key, out PhpValue value) => table.TryGetValue(key, out value);

        public ICollection<IntStringKey>/*!*/ Keys => table.Keys;

        public ICollection<PhpValue>/*!*/ Values => table.Values;

        #endregion

        #region ICollection<KeyValuePair<IntStringKey,object>> Members

        public void Add(KeyValuePair<IntStringKey, PhpValue> item) => this[item.Key] = item.Value;

        public bool Contains(KeyValuePair<IntStringKey, PhpValue> item) => table.Contains(item);

        public void CopyTo(KeyValuePair<IntStringKey, PhpValue>[] array, int arrayIndex)
        {
            table.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<IntStringKey, PhpValue> item)
        {
            if (table.TryGetValue(item.Key, out var value) && value.Equals(item.Value))
            {
                this.EnsureWritable();
                table.Remove(item.Key); // PERF: double lookup!
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Specific Members: Add, AddRange, Prepend, this[], Remove, RemoveLast, RemoveFirst, AddRange

        #region Contains

        /// <summary>
        /// Determines whether an element with the specified key is in this instance.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Whether an element with the <paramref name="key"/> key is in the table.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="key"/> is a <B>null</B> reference.</exception>
        public bool ContainsKey(string key) => table.ContainsKey(key);

        /// <summary>
        /// Determines whether an element with the specified key is in this instance.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Whether an element with the <paramref name="key"/> key is in the table.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="key"/> is a <B>null</B> reference.</exception>
        public bool ContainsKey(int key) => table.ContainsKey(key);

        #endregion

        #region Add, AddToEnd, AddRange

        /// <summary>
        /// Add an item onto the end of this array.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        /// <remarks>This method is supposed to be called on newly created arrays. Several checks are not performed to enhance performance of arrays initialization.</remarks>
        protected void AddToEnd(PhpValue value)
        {
            Debug.Assert(!this.table.IsShared, "This method is supposed to be called on newly created arrays which cannot be shared!");
            Debug.Assert(this.GetType() == typeof(PhpArray), "This method is not supposed to be called on PHpArray's inherited class!");

            table.Add(value);
        }

        /// <summary>
        /// Adds an entry into the table at its logical end. 
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Add(int key, PhpValue value) => this[key] = value;

        /// <summary>
        /// Adds an entry into the table at its logical end. 
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is a null reference.</exception>
        public void Add(string key, PhpValue value) => this[key] = value;

        /// <summary>
        /// Adds range of values at the end of the hashtable.
        /// </summary>
        /// <param name="enumeration">The value enumeration.Cannot be <c>null</c>.</param>
        public void AddRange(IEnumerable enumeration)
        {
            foreach (var value in enumeration)
            {
                AddToEnd(PhpValue.FromClr(value));
            }
        }

        /// <summary>
        /// Adds range of values at the end of the hashtable.
        /// </summary>
        /// <param name="enumeration">The value enumeration. Cannot be <c>null</c>.</param>
        public void AddRange(IEnumerable<PhpValue> enumeration)
        {
            foreach (var value in enumeration)
            {
                AddToEnd(value);
            }
        }

        /// <summary>
        /// Adds range of values at the end of the hashtable.
        /// </summary>
        /// <param name="enumeration">The value enumeration. Cannot be <c>null</c>.</param>
        public void AddRange(IEnumerable<string> enumeration)
        {
            foreach (var value in enumeration)
            {
                AddToEnd(value);
            }
        }

        /// <summary>
        /// Adds range of values at the end of the hashtable.
        /// </summary>
        /// <param name="enumeration">The value enumeration. Cannot be <c>null</c>.</param>
        public void AddRange(string[] enumeration)
        {
            for (int i = 0; i < enumeration.Length; i++)
            {
                AddToEnd(enumeration[i]);
            }
        }

        #endregion

        #region Prepend

        /// <summary>
        /// Adds an entry into the table at its logical beginning. 
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentException">An element with the same key already exists in this instance.</exception>
        public void Prepend(string key, PhpValue value)
        {
            this.EnsureWritable();
            this.table.AddFirst(new IntStringKey(key), value);
        }

        /// <summary>
        /// Adds an entry into the table at its logical beginning. 
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentException">An element with the same key already exists in this instance.</exception>
        public void Prepend(int key, PhpValue value)
        {
            this.EnsureWritable();
            this.table.AddFirst(new IntStringKey(key), value);
        }

        /// <summary>
        /// Adds an entry into the table at its logical beginning. 
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentException">An element with the same key already exists in this instance.</exception>
        public void Prepend(IntStringKey key, PhpValue value)
        {
            this.EnsureWritable();
            this.table.AddFirst(key, value);
        }

        #endregion

        #region Remove, RemoveFirst, RemoveLast

        //  NOTE:
        //  - RemoveLast/RemoveFirst returns removed entry while Remove does not.
        //   This is because a caller of RemoveLast/RemoveFirst knows neither a key nor a value while
        //   a caller of Remove knows at least a key.

        ///// <summary>
        ///// Removes an entry having the specified <see cref="string"/> key.
        ///// </summary>
        ///// <param name="key">The key.</param>
        //public virtual void Remove(int key)
        //{
        //    this.EnsureWritable();
        //    var iskey = new IntStringKey(key);
        //    table._del_key_or_index(ref iskey, this.onDeleteEntry);
        //}

        ///// <summary>
        ///// Removes an entry having the specified <see cref="int"/> key.
        ///// </summary>
        ///// <param name="key">The key.</param>
        ///// <exception cref="ArgumentNullException"><paramref name="key"/> is a null reference.</exception>
        //public virtual void Remove(string key)
        //{
        //    this.EnsureWritable();
        //    var iskey = new IntStringKey(key);
        //    table._del_key_or_index(ref iskey, this.onDeleteEntry);
        //}

        /// <summary>
        /// Removes the last entry of the array and returns it.
        /// </summary>
        /// <returns>The last entry of the array.</returns>
        /// <exception cref="InvalidOperationException">The table is empty.</exception>
        public KeyValuePair<IntStringKey, PhpValue> RemoveLast()
        {
            // array_pop

            this.EnsureWritable();

            if (table.TryRemoveLast(out var pair))
            {
                return pair;
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Removes the first entry of the array and returns it.
        /// </summary>
        /// <returns>The first entry of the array.</returns>
        /// <exception cref="InvalidOperationException">The table is empty.</exception>
        public KeyValuePair<IntStringKey, PhpValue> RemoveFirst()
        {
            this.EnsureWritable();
            table.TryRemoveFirst(out var pair);
            return pair;
        }

        #endregion

        #region this[], TryGetValue

        /// <summary>
        /// Gets or sets a value associated with a key.
        /// </summary>
        /// <param name="skey">The <see cref="string"/> key.</param>
        /// <exception cref="ArgumentNullException"><paramref name="skey"/> is a <c>null</c> reference.</exception>
        /// <remarks>If the key doesn't exist in table the new entry is added.</remarks>
        public PhpValue this[string skey]
        {
            get => table[skey];
            set
            {
                this.EnsureWritable();
                table[skey] = value;
            }
        }

        /// <summary>
        /// Gets or sets a value associated with a key.
        /// </summary>
        /// <param name="ikey">The <see cref="int"/> key.</param>
        /// <remarks>If the key doesn't exist in table the new entry is added.</remarks>
        public PhpValue this[int ikey]
        {
            get => table[ikey];
            set
            {
                this.EnsureWritable();
                table[ikey] = value;
            }
        }

        /// <summary>
        /// Gets or sets a value associated with a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <remarks>If the key doesn't exist in table the new entry is added.</remarks>
        public PhpValue this[IntStringKey key]
        {
            get => table[key];
            set
            {
                this.EnsureWritable();
                table[key] = value;
            }
        }

        public bool TryGetValue(string key, out PhpValue value) => table.TryGetValue(key, out value);

        public bool TryGetValue(int key, out PhpValue value) => table.TryGetValue(key, out value);

        #endregion

        #endregion

        #region Clone, InplaceDeepCopy, AddTo, CopyValuesTo, GetValues

        /// <summary>
        /// Creates a shallow copy of the hashtable.
        /// </summary>
        /// <returns>A copy of the hashtable.</returns>
        public virtual object Clone()
        {
            var clone = new PhpHashtable(this);
            clone.EnsureWritable();
            return clone;
        }

        /// <summary>
        /// Adds items of this instance to a specified instance resetting integer keys.
        /// </summary>
        /// <param name="dst">Destination table.</param>
        /// <param name="deepCopy">Whether to make deep copies of added items.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dst"/> is a <B>null</B> reference.</exception>
        public void AddTo(PhpHashtable/*!*/dst, bool deepCopy)
        {
            if (dst == null)
                throw new ArgumentNullException("dst");

            var enumerator = this.GetFastEnumerator();
            while (enumerator.MoveNext())
            {
                var val = deepCopy ? enumerator.CurrentValue.DeepCopy() : enumerator.CurrentValue;
                var key = enumerator.CurrentKey;
                if (key.IsInteger)
                    dst.Add(val);
                else
                    dst.Add(key, val);
            }
        }

        /// <summary>
        /// Copy values of this array into single dimensional array.
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="offset"></param>
        public void CopyValuesTo(PhpValue[]/*!*/dst, int offset) => table.CopyTo(dst, offset);

        /// <summary>
        /// Copies values to a new array.
        /// </summary>
        public PhpValue[] GetValues() => table.GetValues();

        #endregion

        #region Misc methods: Sort, Diff, Reverse, Shuffle, Unite

        /// <summary>
        /// Sorts this instance using specified comparer.
        /// </summary>
        /// <param name="comparer">The comparer to be used to compare array items.</param>
        public void Sort(IComparer<KeyValuePair<IntStringKey, PhpValue>>/*!*/ comparer)
        {
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));

            this.EnsureWritable();
            table.Sort(comparer);
        }

        /// <summary>
        /// Sorts multiple hashtables given comparer for each hashtable.
        /// </summary>
        /// <param name="hashtables">
        /// The <see cref="ICollection"/> of <see cref="PhpHashtable"/>s. 
        /// All these tables has to be of the same length which has to be .
        /// </param> 
        /// <param name="comparers">
        /// An array of entry comparers.
        /// The number of comparers has to be the same as the number of <paramref name="hashtables"/>.
        /// </param>
        /// <remarks>
        /// Sorts lexicographically all <paramref name="hashtables"/> from the first to the last one using 
        /// <paramref name="comparers"/> successively. Changes only order of entries in <paramref name="hashtables"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="hashtables"/> or <paramref name="comparers"/> is a <B>null</B>reference.</exception>
        public static void Sort(
            PhpArray[]/*!*/ hashtables,
            IComparer<KeyValuePair<IntStringKey, PhpValue>>[]/*!*/ comparers)
        {
            #region requires (hashtables && comparer && comparers.Length==hashtables.Length)

            if (hashtables == null)
                throw new ArgumentNullException("hashtables");
            if (comparers == null)
                throw new ArgumentNullException("comparers");
            if (hashtables.Length != comparers.Length)
                throw new ArgumentException(/*CoreResources.GetString("lengths_are_different", "hashtables", "comparers")*/);

            #endregion

            if (comparers.Length == 0) return;

            // prepare tables (check they are the same length and make them writable):
            int count = hashtables[0].Count;
            for (int i = 1; i < hashtables.Length; i++)
            {
                if (hashtables[i].Count != count)
                    throw new ArgumentException(/*CoreResources.GetString("lengths_are_different", "hashtables[0]", $"hashtables[{i}]"), "hashtables"*/);
            }

            for (int i = 0; i < hashtables.Length; i++)
            {
                hashtables[i].EnsureWritable();
            }

            OrderedDictionary.Sort(count, hashtables, comparers);
        }

        /// <summary>
        /// Performs a set operation <see cref="PhpHashtable"/>s.
        /// </summary>
        /// <param name="op">The operation.</param>
        /// <param name="hashtables">The <see cref="ICollection"/> of <see cref="PhpHashtable"/>s.</param>
        /// <param name="comparer">The dictionary entry comparer used to compare entries of <paramref name="hashtables"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="hashtables"/> or <paramref name="comparer"/> is a <B>null</B> reference.</exception>
        public PhpArray SetOperation(SetOperations op,
            PhpArray[]/*!*/ hashtables,
            IComparer<KeyValuePair<IntStringKey, PhpValue>>/*!*/ comparer)
        {
            if (hashtables == null)
                throw new ArgumentNullException(nameof(hashtables));

            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));

            if (hashtables.Length == 0)
                return (PhpArray)this;

            this.EnsureWritable();

            return new PhpArray(table.SetOperation(op, hashtables, comparer));
        }

        //private IEnumerable<OrderedHashtable<IntStringKey>.Element>/*!*/ EnumerateHeads(IEnumerable<PhpHashtable>/*!*/ hashtables)
        //{
        //    foreach (PhpHashtable hashtable in hashtables)
        //        yield return hashtable.table.head;
        //}

        /// <summary>
        /// Reverses order of entries in this instance.
        /// </summary>
        public void Reverse()
        {
            this.EnsureWritable();
            this.table.Reverse();
        }

        /// <summary>
        /// Shuffles order of elements in the hashtable at random.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="generator"/> is a <b>null</b> reference.</exception>
        public void Shuffle(Random generator)
        {
            this.EnsureWritable();
            table.Shuffle(generator);
        }

        /// <summary>
        /// Unites an <paramref name="array"/> with this instance.
        /// </summary>
        /// <param name="array">An <see cref="PhpArray"/> of items to be united with this instance.</param>
        /// <returns>Reference to this instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is null reference.</exception>
        /// <remarks>
        /// All keys are preserved. Values associated with existing string keys will not be overwritten.
        /// </remarks>
        public PhpHashtable Unite(PhpHashtable array)
        {
            if (array == null) throw new ArgumentNullException("array");
            if (array.table == this.table) return this;

            var enumerator = array.GetFastEnumerator();
            while (enumerator.MoveNext())
            {
                if (!this.table.ContainsKey(enumerator.CurrentKey))
                    this[enumerator.CurrentKey] = enumerator.CurrentValue;  // TODO: DeepCopy value ?
            }

            return this;
        }

        #endregion

        #region ReindexAll, ReindexIntegers, ReindexAndReplace

        /// <summary>
		/// Sets all keys to increasing integers according to their respective order in the list.
		/// </summary>
        public void ReindexAll()
        {
            this.EnsureWritable();
            this.table.ReindexAll();
        }

        /// <summary>
        /// Sets all keys to increasing integers according to their respective order in the list.
        /// </summary>
        /// <param name="startIndex">An index from which to start indexing.</param>
        /// <remarks>If indexing overflows a capacity of integer type it continues with <see cref="int.MinValue"/>.</remarks>
        public void ReindexIntegers(int startIndex)
        {
            this.EnsureWritable();
            this.table.ReindexIntegers(startIndex);
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
        public OrderedDictionary ReindexAndReplace(int offset, int length, ICollection<PhpValue> replacementValues)
        {
            EnsureWritable();
            return table.ReindexAndReplace(offset, length, replacementValues);
        }

        #endregion

        #region Static PhpHashtable/Dictionary Switching (useful for local/global variables dictionaries)

        //public static bool TryGetValue(PhpHashtable hashtable, Dictionary<string, PhpValue> dictionary, string key, out PhpValue value)
        //{
        //    if (hashtable != null)
        //        return hashtable.TryGetValue(key, out value);
        //    else if (dictionary != null)
        //        return dictionary.TryGetValue(key, out value);
        //    else
        //        throw new ArgumentNullException("hashtable");
        //}

        //public static bool ContainsKey(PhpHashtable hashtable, Dictionary<string, PhpValue> dictionary, string key)
        //{
        //    if (hashtable != null)
        //        return hashtable.ContainsKey(key);
        //    else if (dictionary != null)
        //        return dictionary.ContainsKey(key);
        //    else
        //        throw new ArgumentNullException("hashtable");
        //}

        //public static void Add(PhpHashtable hashtable, Dictionary<string, PhpValue> dictionary, string key, PhpValue value)
        //{
        //    if (hashtable != null)
        //        hashtable.Add(key, value);
        //    else if (dictionary != null)
        //        dictionary.Add(key, value);
        //    else
        //        throw new ArgumentNullException("hashtable");
        //}

        //public static void Set(PhpHashtable hashtable, Dictionary<string, PhpValue> dictionary, string key, PhpValue value)
        //{
        //    if (hashtable != null)
        //        hashtable[key] = value;
        //    else if (dictionary != null)
        //        dictionary[key] = value;
        //    else
        //        throw new ArgumentNullException("hashtable");
        //}

        //public static void Remove(PhpHashtable hashtable, Dictionary<string, PhpValue> dictionary, string key)
        //{
        //    if (hashtable != null)
        //        hashtable.Remove(key);
        //    else if (dictionary != null)
        //        dictionary.Remove(key);
        //    else
        //        throw new ArgumentNullException("hashtable");
        //}

        //public static IEnumerable<KeyValuePair<string, PhpValue>>/*!*/ GetEnumerator(PhpArray hashtable, Dictionary<string, PhpValue> dictionary)
        //{
        //    if (hashtable != null)
        //        return hashtable.GetStringKeyEnumerable();
        //    else if (dictionary != null)
        //        return dictionary;
        //    else
        //        throw new ArgumentNullException("hashtable");
        //}

        //private IEnumerable<KeyValuePair<string, PhpValue>>/*!*/ GetStringKeyEnumerable()
        //{
        //    foreach (KeyValuePair<IntStringKey, PhpValue> entry in this)
        //        yield return new KeyValuePair<string, PhpValue>(entry.Key.ToString(), entry.Value);
        //}

        #endregion
    }
}
