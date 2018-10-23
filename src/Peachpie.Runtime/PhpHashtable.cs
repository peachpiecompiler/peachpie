using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
	/// The hashtable storing entries with <see cref="string"/> and <see cref="int"/> keys in a manner of PHP.
	/// </summary>
	[DebuggerNonUserCode]
    public class PhpHashtable : IDictionary<IntStringKey, PhpValue>, IList, IList<PhpValue>, IDictionary // , ICloneable
    {
        #region Fields and Properties

        /// <summary>
        /// Ordered hashtable where integers are stored.
        /// </summary>
        /// <remarks>
        /// Expose the table to item getters on <see cref="PhpArray"/> to make them a little bit faster.
        /// </remarks>
        internal OrderedDictionary/*!*/ table;

        /// <summary>
        /// Index for next new element when key is not specified.
        /// </summary>
        private int nextNewIndex = 0;

        #region active enumerators

        /// <summary>
        /// Callback methods for entry deletion event.
        /// </summary>
        //[NonSerialized]
        internal OrderedDictionary.Enumerator activeEnumerators = null;

        /// <summary>
        /// Add given <paramref name="enumerator"/> into <see cref="activeEnumerators"/> list.
        /// </summary>
        /// <param name="enumerator">New enumerator.</param>
        internal void RegisterEnumerator(OrderedDictionary.Enumerator/*!*/enumerator)
        {
            Debug.Assert(enumerator != null, "Argument null!");
            Debug.Assert(enumerator._next == null, "Enumerator already enlisted somewhere!");
            Debug.Assert(enumerator._table == this.table, "Enumerator was not associated with this PhpHashtable!");

            enumerator._next = this.activeEnumerators;
            this.activeEnumerators = enumerator;
        }

        /// <summary>
        /// Remove given <paramref name="enumerator"/> from <see cref="activeEnumerators"/> list.
        /// </summary>
        /// <param name="enumerator"><see cref="OrderedDictionary.Enumerator"/> to be removed from the list of active enumerators.</param>
        internal void UnregisterEnumerator(OrderedDictionary.Enumerator/*!*/enumerator)
        {
            Debug.Assert(enumerator != null, "Argument null!");
            Debug.Assert(enumerator._table == this.table, "Enumerator was not associated with this PhpHashtable!");

            if (this.activeEnumerators == enumerator)
            {
                this.activeEnumerators = enumerator._next; // remove the first item from the list, most recent case
            }
            else
            {
                for (var e = this.activeEnumerators; e != null; e = e._next)
                    if (e._next == enumerator)
                    {
                        e._next = enumerator._next;
                        break;
                    }
            }
        }

        #endregion

        #endregion

        #region EnsureWritable

        /// <summary>
        /// Ensures the internal <see cref="OrderedDictionary"/> will be writable (not shared).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            this.table.Unshare();

            var oldowner = this.table.owner as PhpArray;

            this.table = new OrderedDictionary(this, table);
            this.table._deep_copy_inplace(oldowner, this as PhpArray);   // deep copy values, replace references to original array into this

            for (var e = this.activeEnumerators; e != null; e = e._next)
            {
                e.TableChanged();
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <c>PhpHashtable</c> class.
        /// </summary>
        public PhpHashtable() : this(0) { }

        /// <summary>
        /// Initializes a new instance of the <c>PhpHashtable</c> class.
        /// </summary>
        /// <param name="capacity">Initial capacity.</param>
        public PhpHashtable(int capacity)
        {
            table = new OrderedDictionary(this, capacity);
        }

        /// <summary>
        /// Initializes a new instance of the <c>PhpHashtable</c> class filled by values from specified array. 
        /// </summary>
        /// <param name="values">Values to be added.</param>
        /// <remarks>
        /// Adds all pairs key-value where the value is an item of <v>values</v> array 
        /// and the key is its index in the array.
        /// </remarks>
        public PhpHashtable(Array values) : this(values, 0, values.Length) { }

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
        {
            this.table = array.table.Share();
            this.nextNewIndex = array.nextNewIndex;
        }

        #endregion

        #region PHP Enumeration

        /// <summary>
        /// Throw an exception if this instance is not <see cref="PhpArray"/> or <see cref="PhpHashtable"/>.
        /// This should avoid using features that are not available in special derived arrays yet.
        /// </summary>
        /// <exception cref="NotImplementedException">This instance does not support the operation yet. Method has to be marked as virtual, and functionality has to be implemented in derived type.</exception>
        [Conditional("DEBUG")]
        protected void ThrowIfNotPhpArrayHelper()
        {
            if (this.GetType() == typeof(PhpHashtable) || this.GetType() == typeof(PhpArray))
                return;

            throw new NotImplementedException();
        }

        public OrderedDictionary.Enumerator/*!*/ GetPhpEnumerator()
        {
            ThrowIfNotPhpArrayHelper();
            return new OrderedDictionary.Enumerator(this); //(IPhpEnumerator)table.GetEnumerator();
        }

        /// <summary>
        /// Get fast enumerator structure to be used internally.
        /// </summary>
        /// <returns></returns>
        public OrderedDictionary.FastEnumerator GetFastEnumerator()
        {
            ThrowIfNotPhpArrayHelper();
            return table.GetFastEnumerator();
        }

        #endregion

        #region IEnumerable<KeyValuePair<IntStringKey, object>> Members

        public virtual IEnumerator<KeyValuePair<IntStringKey, PhpValue>>/*!*/ GetEnumerator()
        {
            if (this.Count == 0)
                return OrderedDictionary.EmptyEnumerator.SingletonInstance;

            return new OrderedDictionary.Enumerator(this); //table.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (this.Count == 0)
                return OrderedDictionary.EmptyEnumerator.SingletonInstance;

            return new OrderedDictionary.Enumerator(this);
        }

        IEnumerator<PhpValue> IEnumerable<PhpValue>.GetEnumerator()
        {
            if (this.Count == 0)
                return OrderedDictionary.EmptyEnumerator.SingletonInstance;

            return new OrderedDictionary.Enumerator(this);
        }

        #endregion

        #region ICollection, ICollection<PhpValue> Members

        /// <summary>Retrieves the number of items in this instance.</summary>
        public virtual int Count { get { return table.Count; } }

        /// <summary>This property is always false.</summary>
        public bool IsSynchronized { get { return false; } }

        /// <summary>This property always refers to this instance.</summary>
        public object SyncRoot { get { return table.SyncRoot; } }

        /// <summary>
        /// Copies the <see cref="PhpHashtable"/> or a portion of it to a one-dimensional array.
        /// </summary>
        /// <param name="array">The one-dimensional array.</param>
        /// <param name="index">The zero-based index in array at which copying begins.</param>
        public void CopyTo(Array/*!*/ array, int index) => table.CopyTo(array, index);

        void ICollection<PhpValue>.CopyTo(PhpValue[] array, int arrayIndex) => table.CopyTo(array, arrayIndex);

        void ICollection<PhpValue>.Add(PhpValue item) => Add(item);

        bool ICollection<PhpValue>.Contains(PhpValue item)
        {
            using (var e = GetFastEnumerator())
            {
                while (e.MoveNext())
                {
                    if (e.CurrentValue == item)
                    {
                        return true;
                    }
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

            using (var e = GetFastEnumerator())
            {
                while (e.MoveNext())
                {
                    if (e.CurrentValue == item)
                    {
                        e.DeleteCurrentEntryAndMove(this.activeEnumerators);
                        found = true;
                    }
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

        /// <summary>
        /// Returns an enumerator which iterates through values in this instance in order as they were added in it.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IDictionaryEnumerator/*!*/ IDictionary.GetEnumerator()
        {
            if (this.Count == 0)
                return OrderedDictionary.EmptyEnumerator.SingletonInstance;

            return new IDictionaryAdapter(this); // new GenericDictionaryAdapter<object, object>(GetDictionaryEnumerator(), false);
        }

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
            ThrowIfNotPhpArrayHelper();
            this.Add(IntStringKey.FromObject(key), PhpValue.FromClr(value));
        }

        public void Add(string key, long value) => Add(new IntStringKey(key), (PhpValue)value);
        public void Add(string key, string value) => Add(new IntStringKey(key), (PhpValue)value);
        public void Add(string key, PhpArray value) => Add(new IntStringKey(key), (PhpValue)value);
        public void Add(string key, bool value) => Add(new IntStringKey(key), (PhpValue)value);
        public void Add(int key, long value) => Add(new IntStringKey(key), (PhpValue)value);
        public void Add(int key, double value) => Add(new IntStringKey(key), (PhpValue)value);
        public void Add(int key, string value) => Add(new IntStringKey(key), (PhpValue)value);
        public void Add(int key, PhpArray value) => Add(new IntStringKey(key), (PhpValue)value);

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
        public void Remove(object key)
        {
            Remove(IntStringKey.FromObject(key));
        }

        ICollection/*!*/ IDictionary.Keys
        {
            get
            {
                if (_keys == null) _keys = new KeyCollection(this);
                return _keys;
            }
        }
        //[NonSerialized]
        private KeyCollection _keys;

        ICollection/*!*/ IDictionary.Values
        {
            get
            {
                return (ICollection)table.Values;
            }
        }

        #region Inner class: KeyCollection

        //[Serializable]
        public class KeyCollection : ICollection
        {
            private readonly PhpHashtable/*!*/ hashtable;

            internal KeyCollection(PhpHashtable/*!*/ hashtable)
            {
                this.hashtable = hashtable;
            }

            #region ICollection Members

            public int Count { get { return hashtable.Count; } }

            public bool IsSynchronized { get { return false; } }

            public object SyncRoot { get { return this; } }

            void ICollection.CopyTo(Array/*!*/ array, int index)
            {
                //ArrayUtils.CheckCopyTo(array, index, hashtable.Count);

                foreach (KeyValuePair<IntStringKey, PhpValue> entry in hashtable)
                    array.SetValue(entry.Key.Object, index++);
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                foreach (KeyValuePair<IntStringKey, PhpValue> pair in hashtable)
                    yield return pair.Key.Object;
            }

            #endregion
        }

        #endregion

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
            //if (MaxIntegerKey < int.MaxValue)
            {
                this.EnsureWritable();

                AddToEnd(value);
                return this.nextNewIndex;
            }
            //return 0;
        }

        void IList.RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        void IList<PhpValue>.RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        void IList.Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        void IList<PhpValue>.Insert(int index, PhpValue value)
        {
            throw new NotImplementedException();
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

        public bool ContainsKey(IntStringKey key)
        {
            ThrowIfNotPhpArrayHelper();
            return table.ContainsKey(key);
        }

        public virtual bool Remove(IntStringKey key)
        {
            //if (key.Integer == this.nextNewIndex - 1)
            //{
            //    // copy of this array has to find new max int
            //}

            this.EnsureWritable();
            return this.table._del_key_or_index(ref key, this.activeEnumerators);
        }

        public bool TryGetValue(IntStringKey key, out PhpValue value)
        {
            ThrowIfNotPhpArrayHelper();
            return table.TryGetValue(key, out value);
        }

        public ICollection<IntStringKey>/*!*/ Keys
        {
            get { ThrowIfNotPhpArrayHelper(); return table.Keys; }
        }

        public ICollection<PhpValue>/*!*/ Values
        {
            get { ThrowIfNotPhpArrayHelper(); return table.Values; }
        }

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
            ThrowIfNotPhpArrayHelper();

            this.EnsureWritable();

            return table.Remove(item);
        }

        #endregion

        #region Specific Members: Add, AddRange, Prepend, this[], Remove, RemoveLast, RemoveFirst, AddRange

        /// <summary>
        /// Simple wrapper to allow call KeyAdded without ref.
        /// </summary>
        private void KeyAdded(IntStringKey key)
        {
            KeyAdded(ref key);
        }

        protected void KeyAdded(ref IntStringKey key)
        {
            if (key.IsInteger)
                KeyAdded(key.Integer);
        }

        private void KeyAdded(int key)
        {
            if (key >= nextNewIndex) nextNewIndex = key + 1;
        }

        #region Contains

        /// <summary>
        /// Determines whether an element with the specified key is in this instance.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Whether an element with the <paramref name="key"/> key is in the table.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="key"/> is a <B>null</B> reference.</exception>
        public bool ContainsKey(string key)
        {
            return table.ContainsKey(new IntStringKey(key));
        }

        /// <summary>
        /// Determines whether an element with the specified key is in this instance.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Whether an element with the <paramref name="key"/> key is in the table.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="key"/> is a <B>null</B> reference.</exception>
        public bool ContainsKey(int key)
        {
            return table.ContainsKey(new IntStringKey(key));
        }

        #endregion

        #region Add, AddToEnd, AddRange

        /// <summary>
        /// Add an item onto the end of this array.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        /// <remarks>This method is supposed to be called on newly created arrays. Several checks are not performed to enhance performance of arrays initialization.</remarks>
        protected void AddToEnd(PhpValue value)
        {
            Debug.Assert(nextNewIndex >= 0, "This method is supposed to be called on newly created arrays which have [nextNewIndex] field initialized!");
            Debug.Assert(!this.table.IsShared, "This method is supposed to be called on newly created arrays which cannot be shared!");
            Debug.Assert(this.GetType() == typeof(PhpArray), "This method is not supposed to be called on PHpArray's inherited class!");

            var key = new IntStringKey(nextNewIndex++);
            table._add_last(ref key, value);
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
        /// <param name="enumeration">The value enumeration.Cannot be <c>null</c>.</param>
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
        /// <param name="enumeration">The value enumeration.Cannot be <c>null</c>.</param>
        public void AddRange(IEnumerable<string> enumeration)
        {
            foreach (var value in enumeration)
            {
                AddToEnd((PhpValue)value);
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

            var iskey = new IntStringKey(key);
            this.table._add_first(ref iskey, value);
            KeyAdded(key);
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

            var iskey = new IntStringKey(key);
            this.table._add_first(ref iskey, value);
            KeyAdded(key);
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

            this.table._add_first(ref key, value);
            KeyAdded(ref key);
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
            var p = table._remove_last(this.activeEnumerators);

            // array_pop decreases next free index if appropriate:
            if (p.Key.IsInteger && p.Key.Integer >= nextNewIndex - 1)
            {
                --nextNewIndex;
            }

            return p;
        }

        /// <summary>
        /// Removes the first entry of the array and returns it.
        /// </summary>
        /// <returns>The first entry of the array.</returns>
        /// <exception cref="InvalidOperationException">The table is empty.</exception>
        public KeyValuePair<IntStringKey, PhpValue> RemoveFirst()
        {
            this.EnsureWritable();
            return table._remove_first(this.activeEnumerators);
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
            get => table[new IntStringKey(skey)];
            set
            {
                this.EnsureWritable();

                var key = new IntStringKey(skey);
                if (table._add_or_update(ref key, value))
                {
                    KeyAdded(skey);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value associated with a key.
        /// </summary>
        /// <param name="ikey">The <see cref="int"/> key.</param>
        /// <remarks>If the key doesn't exist in table the new entry is added.</remarks>
        public PhpValue this[int ikey]
        {
            get
            {
                table.TryGetValue(ikey, out var value);
                return value;
            }
            set
            {
                this.EnsureWritable();

                var key = new IntStringKey(ikey);
                if (table._add_or_update(ref key, value))
                {
                    KeyAdded(ikey);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value associated with a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <remarks>If the key doesn't exist in table the new entry is added.</remarks>
        public PhpValue this[IntStringKey key]
        {
            get
            {
                return table._get(ref key);
            }
            set
            {
                this.EnsureWritable();

                if (table._add_or_update(ref key, value))
                {
                    KeyAdded(ref key);
                }
            }
        }

        public bool TryGetValue(string key, out PhpValue value)
        {
            return table.TryGetValue(new IntStringKey(key), out value);
        }

        public bool TryGetValue(int key, out PhpValue value)
        {
            return table.TryGetValue(new IntStringKey(key), out value);
        }

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
		/// Replaces values in the table with their deep copies.
		/// </summary>
        public void InplaceDeepCopy()
        {
            ThrowIfNotPhpArrayHelper();
            Debug.Assert(!this.table.IsShared);
            this.table._deep_copy_inplace();
        }

        /// <summary>
        /// Adds items of this instance to a specified instance resetting integer keys.
        /// </summary>
        /// <param name="dst">Destination table.</param>
        /// <param name="deepCopy">Whether to make deep copies of added items.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dst"/> is a <B>null</B> reference.</exception>
        public void AddTo(PhpHashtable/*!*/dst, bool deepCopy)
        {
            ThrowIfNotPhpArrayHelper();

            if (dst == null)
                throw new ArgumentNullException("dst");

            using (var enumerator = this.GetFastEnumerator())
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
        public PhpValue[] GetValues()
        {
            if (this.Count != 0)
            {
                var array = new PhpValue[this.Count];
                this.CopyValuesTo(array, 0);
                return array;
            }
            else
            {
                return Array.Empty<PhpValue>();
            }
        }

        #endregion

        #region Misc methods: Sort, Diff, Reverse, Shuffle, Unite

        /// <summary>
        /// Sorts this instance using specified comparer.
        /// </summary>
        /// <param name="comparer">The comparer to be used to compare array items.</param>
        public void Sort(IComparer<KeyValuePair<IntStringKey, PhpValue>>/*!*/ comparer)
        {
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            this.EnsureWritable();
            OrderedDictionary.sortops._sort(this.table, comparer);
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
        public static void Sort(PhpHashtable[]/*!*/ hashtables,
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

            OrderedDictionary.sortops._multisort(count, hashtables, comparers);
        }

        /// <summary>
        /// Performs a set operation <see cref="PhpHashtable"/>s.
        /// </summary>
        /// <param name="op">The operation.</param>
        /// <param name="hashtables">The <see cref="ICollection"/> of <see cref="PhpHashtable"/>s.</param>
        /// <param name="comparer">The dictionary entry comparer used to compare entries of <paramref name="hashtables"/>.</param>
        /// <param name="result">The <see cref="IDictionary"/> where to add remaining elements.</param>
        /// <remarks>
        /// Entries that will remain in this instance if a difference was made are stored into 
        /// the <paramref name="result"/> in the same order they are stored in this instance. 
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="hashtables"/> or <paramref name="comparer"/> or <paramref name="result"/> is a <B>null</B> reference.</exception>
        /// <exception cref="ArgumentException"><paramref name="result"/> references this instance.</exception>
        public void SetOperation(SetOperations op, PhpHashtable[]/*!*/ hashtables,
            IComparer<KeyValuePair<IntStringKey, PhpValue>>/*!*/ comparer, /*IDictionary<IntStringKey, object>*/PhpHashtable/*!*/ result)
        {
            #region Requires (hashtables && comparer && result && result!=this)

            if (hashtables == null)
                throw new ArgumentNullException("hashtables");
            if (comparer == null)
                throw new ArgumentNullException("comparers");
            if (result == null)
                throw new ArgumentNullException("result");
            if (result == this)
                throw new ArgumentException(/*CoreResources.GetString("argument_equals", "result", "this")*/);

            #endregion

            if (hashtables.Length == 0) return;

            this.EnsureWritable();
            this.table._set_operation(op, hashtables, comparer, result);
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
            this.table._reverse();
        }

        /// <summary>
        /// Shuffles order of elements in the hashtable at random.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="generator"/> is a <b>null</b> reference.</exception>
        public void Shuffle(Random generator)
        {
            this.EnsureWritable();
            table._shuffle_data(generator);
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

            using (var enumerator = array.GetFastEnumerator())
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

            // updates the list:
            int i = 0;

            using (var enumerator = this.table.GetFastEnumerator())
                while (enumerator.MoveNext())
                {
                    enumerator.ModifyCurrentEntryKey(new IntStringKey(i++));
                }

            //
            this.nextNewIndex = i;

            //
            this.table._rehash();
        }

        /// <summary>
        /// Sets all keys to increasing integers according to their respective order in the list.
        /// </summary>
        /// <param name="startIndex">An index from which to start indexing.</param>
        /// <remarks>If indexing overflows a capacity of integer type it continues with <see cref="int.MinValue"/>.</remarks>
        public void ReindexIntegers(int startIndex)
        {
            this.EnsureWritable();

            // updates the list:
            int i = startIndex;

            var enumerator = this.table.GetFastEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.CurrentKey.IsInteger)
                {
                    enumerator.ModifyCurrentEntryKey(new IntStringKey(i++));
                }
            }

            //
            this.nextNewIndex = i;

            //
            if (i > startIndex)
            {
                this.table._rehash();
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
        /// <param name="replaced">
        /// The hashtable where removed values will be placed. Keys are successive integers starting from zero.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><pararef name="offset"/> or <paramref name="length"/> has invalid value.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="replaced"/> is a <b>null</b> reference.</exception>
        public void ReindexAndReplace(int offset, int length, IEnumerable<PhpValue> replacementValues, PhpHashtable/*!*/ replaced)
        {
            int count = this.Count;

            if (offset < 0 || offset > count)
                throw new ArgumentOutOfRangeException("first");
            if (length < 0 || offset + length > count)
                throw new ArgumentOutOfRangeException("length");
            if (replaced == null)
                throw new ArgumentNullException("replaced");

            this.EnsureWritable();  // ensure values are deeply copied

            int ikey = 0;

            // reindexes integer keys of elements before the first replaced item:
            int i = 0;
            using (var enumerator = this.GetFastEnumerator())
            {
                // reindex first [offset] entries (whose key is integer):
                while (i++ < offset && enumerator.MoveNext())
                {
                    if (enumerator.CurrentKey.IsInteger)
                        enumerator.ModifyCurrentEntryKey(new IntStringKey(ikey++));
                }

                // [enumerator] points to last reindexed entry, have to be advanced to the next
                enumerator.MoveNext();

                // removes items with ordinal number in interval [first,last]:
                int jkey = 0;
                i = 0;
                while (i++ < length/* && enumerator.MoveNext()*/)
                {
                    Debug.Assert(enumerator.IsValid);

                    if (enumerator.CurrentKey.IsInteger)
                    {
                        replaced.Add(jkey++, enumerator.CurrentValue);
                    }
                    else
                    {
                        replaced.Add(enumerator.CurrentKey, enumerator.CurrentValue);
                    }

                    // remove item from the list:
                    enumerator.DeleteCurrentEntryAndMove(this.activeEnumerators);
                }

                // adds new elements before "enumerator" element:
                if (replacementValues != null)
                {
                    foreach (var value in replacementValues)
                        enumerator.InsertBeforeCurrentEntry(new IntStringKey(ikey++), value);
                }

                // reindexes integer keys of the rest elements:
                if (enumerator.IsValid)
                {
                    do
                    {
                        if (enumerator.CurrentKey.IsInteger)
                            enumerator.ModifyCurrentEntryKey(new IntStringKey(ikey++));

                    } while (enumerator.MoveNext());
                }
            }

            // rehashes the table (updates bucket lists)
            this.table._rehash();

            // updates max integer value in table:
            this.nextNewIndex = ikey;
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
