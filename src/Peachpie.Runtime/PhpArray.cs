using Pchp.Core.Resources;
using Pchp.Core.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Implements ordered keyed array of <see cref="PhpValue"/> with PHP semantics.
    /// </summary>
    public partial class PhpArray : PhpHashtable, IPhpConvertible, IPhpArray, IPhpComparable, IPhpEnumerable, IPhpEnumerator
    {
        /// <summary>
        /// Used in all PHP functions determining the type name. (<c>var_dump</c>, ...)
        /// </summary>
        public const string PhpTypeName = "array";

        /// <summary>
        /// Used in <c>print_r</c> function.
        /// </summary>
        public const string PrintablePhpTypeName = "Array";

        /// <summary>
        /// Intrinsic enumerator associated with the array.
        /// </summary>
        private int _intrinsicEnumerator;

        /// <summary>
        /// Empty array singleton.
        /// Must not be modified.
        /// </summary>
        public static readonly PhpArray Empty = new PhpArray();

        /// <summary>
        /// Fast creation of an empty array
        /// by referencing internal structure of empty singleton.
        /// </summary>
        public static PhpArray NewEmpty() => Empty.DeepCopy();

        /// <summary>
        /// Helper property used by visitor algorithms.
        /// Gets value determining whether this instance has been visited during recursive pass of some structure containing <see cref="PhpArray"/>s.
        /// </summary>
        /// <remarks>
        /// Must be decreased immediately after the pass.
        /// </remarks>
        int _visited;

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="PhpArray"/> with specified capacities for integer and string keys respectively.
        /// </summary>
        public PhpArray() { }

        /// <summary>
        /// Creates a new instance of <see cref="PhpArray"/> with specified capacities for integer and string keys respectively.
        /// </summary>
        /// <param name="capacity"></param>
        public PhpArray(int capacity) : base(capacity) { }

        /// <summary>
        /// Creates new instance with given table data.
        /// </summary>
        public PhpArray(OrderedDictionary table) : base(table) { }

        /// <summary>
        /// Creates a new instance of <see cref="PhpArray"/> initialized with all values from <see cref="System.Array"/>.
        /// </summary>
        /// <param name="values"></param>
        public PhpArray(Array values) : base(values) { }

        /// <summary>
        /// Creates a new instance of <see cref="PhpArray"/> initialized with all values from <see cref="System.Array"/>.
        /// </summary>
        /// <param name="values">Array of values.</param>
        public PhpArray(PhpValue[] values)
            : base(values.Length)
        {
            for (int i = 0; i < values.Length; i++)
            {
                table.Add(values[i]);
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="PhpArray"/> initialized with all values from <see cref="System.Array"/>.
        /// </summary>
        /// <param name="values">Array of values.</param>
        public PhpArray(PhpValue?[] values)
            : base(values.Length)
        {
            for (int i = 0; i < values.Length; i++)
            {
                table.Add(values[i].GetValueOrDefault(PhpValue.Null));
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="PhpArray"/> initialized with all values from <see cref="System.Array"/>.
        /// </summary>
        public PhpArray(string[] values)
            : base(values.Length)
        {
            for (int i = 0; i < values.Length; i++)
            {
                table.Add(values[i]);
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="PhpArray"/> initialized with a portion of <see cref="System.Array"/>.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        public PhpArray(Array values, int index, int length) : base(values, index, length) { }

        /// <summary>
		/// Initializes a new instance of the <see cref="PhpArray"/> class filled by values from specified array.
		/// </summary>
		/// <param name="values">An array of values to be added to the table.</param>
		/// <param name="start">An index of the first item from <paramref name="values"/> to add.</param>
		/// <param name="length">A number of items to add.</param>
		/// <param name="value">A value to be filtered.</param>
		/// <param name="doFilter">Wheter to add all items but <paramref name="value"/> (<b>true</b>) or 
		/// all items with the value <paramref name="value"/> (<b>false</b>).</param>
		public PhpArray(int[] values, int start, int length, int value, bool doFilter)
            : base(values, start, length, value, doFilter) { }

        ///// <summary>
        ///// Initializes a new instance of the <see cref="PhpArray"/> class filled by values from specified array.
        ///// </summary>
        ///// <param name="values">An array of values to be added to the table.</param>
        ///// <param name="start">An index of the first item from <paramref name="values"/> to add.</param>
        ///// <param name="length">A number of items to add.</param>
        ///// <param name="value">A value to be filtered.</param>
        ///// <param name="doFilter">Wheter to add all items but <paramref name="value"/> (<b>true</b>) or 
        ///// all items with the value <paramref name="value"/> (<b>false</b>).</param>
        //public PhpArray(int[] values, int start, int length, int value, bool doFilter)
        //    : base(values, start, length, value, doFilter) { }

        /// <summary>
        /// Creates a new instance of <see cref="PhpArray"/> filled by data from an enumerator.
        /// </summary>
        /// <param name="data">The enumerator containing values added to the new instance.</param>
        public PhpArray(IEnumerable data)
            : base(data is ICollection collection ? collection.Count : 0)
        {
            if (data != null)
            {
                AddRange(data);
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="PhpArray"/> filled by data from an enumerator.
        /// </summary>
        /// <param name="data">The enumerator containing values added to the new instance.</param>
        public PhpArray(IEnumerable<PhpValue> data)
            : base(data is ICollection collection ? collection.Count : 0)
        {
            if (data != null)
            {
                AddRange(data);
            }
        }

        public PhpArray(IEnumerable<string> data)
            : base(data is ICollection collection ? collection.Count : 0)
        {
            if (data != null)
            {
                AddRange(data);
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="PhpArray"/> filled by data from an enumerator.
        /// </summary>
        /// <param name="data">The enumerator containing values added to the new instance.</param>
        public PhpArray(IEnumerable<KeyValuePair<IntStringKey, PhpValue>> data)
            : base(data is ICollection collection ? collection.Count : 0)
        {
            if (data != null)
            {
                foreach (var item in data)
                {
                    Add(item);
                }
            }
        }

        public static explicit operator PhpArray(string value) => New(value);
        public static explicit operator PhpArray(PhpString value) => New(value);
        public static explicit operator PhpArray(long value) => New(value);
        public static explicit operator PhpArray(double value) => New(value);
        public static explicit operator PhpArray(bool value) => New(value);

        /// <summary>
        /// Copy constructor. Creates <see cref="PhpArray"/> that shares internal data table with another <see cref="PhpArray"/>.
        /// </summary>
        /// <param name="array">Table to be shared.</param>
        public PhpArray(PhpArray/*!*/array)
            : base(array)
        {
            //// preserve intrinsic enumerator state
            //_intrinsicEnumerator = array._intrinsicEnumerator?.WithTable(this); // copies state of intrinsic enumerator or null
        }

        /// <summary>
        /// Creates an instance of <see cref="PhpArray"/> filled by given values.
        /// </summary>
        /// <param name="values">Values to be added to the new instance. 
        /// Keys will correspond order of values in the array.</param>
        public static PhpArray New(params object[] values)
        {
            var result = new PhpArray(values.Length);

            for (int i = 0; i < values.Length; i++)
            {
                result.Add(values[i]);
            }

            return result;
        }

        /// <summary>
        /// Creates an instance of <see cref="PhpArray"/> filled by given values.
        /// </summary>
        /// <param name="values">Values to be added to the new instance. 
        /// Keys will correspond order of values in the array.</param>
        public static PhpArray New(params PhpValue[] values)
        {
            var result = new PhpArray(values.Length);

            for (int i = 0; i < values.Length; i++)
            {
                result.AddToEnd(values[i]);
            }

            return result;
        }

        /// <summary>
        /// Makes new array containing union of two arrays.
        /// </summary>
        public static PhpArray Union(PhpArray x, PhpArray y) => (PhpArray)x.DeepCopy().Unite(y);

        /// <summary>
        /// Creates an instance of <see cref="PhpArray"/> filled by given entries.
        /// </summary>
        /// <param name="keysValues">Keys and values (alternating) or values only.</param>
        /// <remarks>If the length of <paramref name="keysValues"/> is odd then its last item is added without a key.</remarks>
        public static PhpArray Keyed(params object[] keysValues)
        {
            PhpArray result = new PhpArray();
            int length = keysValues.Length;
            int remainder = length % 2;

            for (int i = 0; i < length - remainder; i += 2)
                result.Add(keysValues[i], keysValues[i + 1]);

            if (remainder > 0)
                result.Add(keysValues[length - 1]);

            return result;
        }

        /// <summary>
        /// Creates an instance of <see cref="PhpArray"/> filled by given values.
        /// </summary>
        /// <param name="value">Value to be added to the new instance. 
        /// Keys will correspond order of values in the array.</param>
        public static PhpArray New(PhpValue value)
        {
            return new PhpArray(1)
            {
                value
            };
        }

        /// <summary>
        /// Creates array from PHP enumerator.
        /// </summary>
        internal static PhpArray Create(IPhpEnumerator phpenumerator)
        {
            Debug.Assert(phpenumerator != null);

            var arr = new PhpArray();

            while (phpenumerator.MoveNext())
            {
                var current = phpenumerator.Current;
                arr.Add(current.Key.ToIntStringKey(), current.Value);
            }

            //
            return arr;
        }

        #endregion

        #region Operators

        /// <summary>
        /// Creates copy of this instance using shared underlaying hashtable.
        /// </summary>
        public PhpArray DeepCopy() => new PhpArray(table.AddRef());

        /// <summary>
        /// Makes clone of this array with deeply copied values.
        /// </summary>
        /// <returns>Cloned instance of <see cref="PhpArray"/>.</returns>
        public override object Clone()
        {
            var clone = DeepCopy();
            clone.EnsureWritable();
            return clone;
        }

        /// <summary>
        /// Adds a variable into the array while keeping duplicit keys in sub-arrays of indexed items.
        /// </summary>
        /// <param name="name">Key, respecting <c>[subkey]</c> notation.</param>
        /// <param name="value">The value.</param>
        /// <remarks>See <see cref="NameValueCollectionUtils.AddVariable(PhpArray, string, PhpValue, string)"/> for details.</remarks>
        public void AddVariable(string name, string value) => NameValueCollectionUtils.AddVariable(this, name, value);

        /// <summary>
        /// Gets reference (<c>ref</c> <see cref="PhpValue"/>) to the item at given index.
        /// </summary>
        public ref PhpValue GetItemRef(IntStringKey key) => ref table.EnsureValue(key);

        /// <summary>
        /// Gets value indicating the PHP variable is empty (empty array).
        /// </summary>
        public bool IsEmpty() => Count == 0;

        /// <summary>
        /// Not used.
        /// </summary>
        public void Dispose() { }

        #endregion

        #region Conversion operators

        /// <summary>
        /// Explicit cast of array to <see cref="int"/>.
        /// Gets number of items in the array.
        /// </summary>
        public static explicit operator int(PhpArray array) => array.Count;

        /// <summary>
        /// Explicit cast of array to <see cref="double"/>.
        /// Gets number of items in the array.
        /// </summary>
        public static explicit operator double(PhpArray array) => array.Count;

        /// <summary>
        /// Explicit cast of array to <see cref="string"/>.
        /// Always returns <c>"Array"</c> string literal.
        /// </summary>
        public static explicit operator string(PhpArray _)
        {
            PhpException.Throw(PhpError.Notice, ErrResources.array_to_string_conversion);
            return PrintablePhpTypeName;
        }

        /// <summary>
        /// Explicit cast of array to <see cref="bool"/>.
        /// Gets <c>true</c> if array contains any elements.
        /// </summary>
        public static explicit operator bool(PhpArray array) => array != null && array.Count != 0;

        #endregion

        #region IPhpConvertible

        public PhpTypeCode TypeCode => PhpTypeCode.PhpArray;

        double IPhpConvertible.ToDouble() => Count;

        long IPhpConvertible.ToLong() => Count;

        bool IPhpConvertible.ToBoolean() => Count != 0;

        Convert.NumberInfo IPhpConvertible.ToNumber(out PhpNumber number)
        {
            number = PhpNumber.Create(Count);
            return Convert.NumberInfo.IsPhpArray | Convert.NumberInfo.LongInteger;
        }

        string IPhpConvertible.ToString(Context ctx) => (string)this;

        /// <summary>
        /// Creates <see cref="stdClass"/> with runtime instance fields copied from entries of this array.
        /// </summary>
        object IPhpConvertible.ToClass() => ToObject();

        /// <summary>
        /// Creates <see cref="stdClass"/> instance which runtime fields are copied from this array.
        /// </summary>
        /// <returns>Instance of <see cref="stdClass"/>. Cannot be <c>null</c>.</returns>
        public stdClass/*!*/ToObject() => this.DeepCopy().AsStdClass();

        public PhpArray ToArray() => this;

        #endregion

        #region IPhpComparable

        public int Compare(PhpValue value) => Compare(value, PhpComparer.Default);

        public int Compare(PhpValue value, IComparer<PhpValue> comparer)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Null:
                    return Count;

                case PhpTypeCode.Object:
                    return 1;

                case PhpTypeCode.Boolean:
                    return (Count > 0 ? 2 : 1) - (value.Boolean ? 2 : 1);

                case PhpTypeCode.PhpArray:
                    // compare elements:
                    bool incomparable;
                    int result = CompareArrays(this, value.Array, comparer, out incomparable);
                    if (incomparable)
                    {
                        PhpException.Throw(PhpError.Warning, ErrResources.incomparable_arrays_compared);
                    }
                    return result;

                case PhpTypeCode.Alias:
                    return Compare(value.Alias.Value, comparer);
            }

            //
            return 1;
        }

        /// <summary>
		/// Compares two instances of <see cref="PhpArray"/>.
		/// </summary>
        /// <param name="x">First operand. Cannot be <c>null</c>.</param>
        /// <param name="y">Second operand. Cannot be <c>null</c>.</param>
		/// <param name="comparer">The comparer.</param>
		/// <param name="incomparable">Whether arrays are incomparable 
		/// (no difference is found before both arrays enters an infinite recursion). 
		/// Returns zero then.</param>
		public static int CompareArrays(PhpArray x, PhpArray y, IComparer<PhpValue> comparer, out bool incomparable)
        {
            Debug.Assert(x != null && y != null);

            incomparable = false;

            // if both operands point to the same internal dictionary:
            if (ReferenceEquals(x.table, y.table)) return 0; // => x == y

            //
            PhpArray array_x, array_y;
            PhpArray sorted_x, sorted_y;

            // if numbers of elements differs:
            int result = x.Count - y.Count;
            if (result != 0) return result;

            // marks arrays as visited (will be always restored to false value before return):
            x._visited++;
            y._visited++;

            // it will be more effective to implement OrderedHashtable.ToOrderedList method and use it here (in future version):
            sorted_x = x.DeepCopy();
            sorted_x.Sort(KeyComparer.ArrayKeys);
            sorted_y = y.DeepCopy();
            sorted_y.Sort(KeyComparer.ArrayKeys);

            var iter_x = sorted_x.GetFastEnumerator();
            var iter_y = sorted_y.GetFastEnumerator();

            result = 0;

            try
            {
                // compares corresponding elements (keys first values then):
                while (iter_x.MoveNext())
                {
                    iter_y.MoveNext();

                    // compares keys:
                    result = iter_x.CurrentKey.CompareTo(iter_y.CurrentKey);
                    if (result != 0) break;

                    // dereferences childs if they are references:
                    var child_x = iter_x.CurrentValue.GetValue();
                    var child_y = iter_y.CurrentValue.GetValue();

                    // compares values:
                    if ((array_x = child_x.ArrayOrNull()) != null)
                    {
                        if ((array_y = child_y.ArrayOrNull()) != null)
                        {
                            // at least one child has not been visited yet => continue with recursion:
                            if (array_x._visited == 0 || array_y._visited == 0)
                            {
                                result = CompareArrays(array_x, array_y, comparer, out incomparable);
                            }
                            else
                            {
                                incomparable = true;
                            }

                            // infinity recursion has been detected:
                            if (incomparable) break;
                        }
                        else
                        {
                            // compares an array with a non-array:
                            array_x.Compare(child_y, comparer);
                        }
                    }
                    else
                    {
                        // compares unknown item with a non-array:
                        result = -comparer.Compare(child_y, child_x);
                    }

                    if (result != 0) break;
                } // while
            }
            finally
            {
                x._visited--;
                y._visited--;
            }
            return result;
        }

        #endregion

        #region Strict Comparison

        /// <summary>
        /// Compares this instance with another <see cref="PhpArray"/>.
        /// </summary>
        /// <param name="array">The array to be strictly compared.</param>
        /// <returns>Whether this instance strictly equals to <paramref name="array"/>.</returns>
        /// <remarks>
        /// Arrays are strictly equal if all entries are strictly equal and in the same order in both arrays.
        /// Entries are strictly equal if keys are the same and values are strictly equal 
        /// in the terms of operator <B>===</B>.
        /// </remarks>
        public bool StrictCompareEq(PhpArray array)
        {
            bool incomparable = false;

            var result = array != null && StrictCompareArrays(this, array, out incomparable);
            if (incomparable)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.incomparable_arrays_compared);
            }

            return result;
        }

        /// <summary>
        /// Compares two instances of <see cref="PhpArray"/> for strict equality.
        /// </summary>
        /// <param name="x">First operand. Cannot be <c>null</c>.</param>
        /// <param name="y">Second operand. Cannot be <c>null</c>.</param>
		/// <param name="incomparable">Whether arrays are incomparable 
        /// (no difference is found before both arrays enters an infinite recursion). 
        /// Returns <B>true</B> then.</param>
        static bool StrictCompareArrays(PhpArray x, PhpArray y, out bool incomparable)
        {
            Debug.Assert(x != null && y != null);

            incomparable = false;

            // if both operands point to the same internal dictionary:
            if (ReferenceEquals(x.table, y.table)) return true; // => x == y

            // if numbers of elements differs:
            if (x.Count != y.Count) return false;

            var iter_x = x.GetFastEnumerator();
            var iter_y = y.GetFastEnumerator();

            PhpArray array_x, array_y;

            // marks arrays as visited (will be always restored to false value before return):
            x._visited++;
            y._visited++;

            bool result = true;

            try
            {
                // compares corresponding elements (keys first values then):
                while (result && iter_x.MoveNext())
                {
                    iter_y.MoveNext();

                    // compares keys:
                    if (!iter_x.Current.Key.Equals(iter_y.Current.Key))
                    {
                        result = false;
                        break;
                    }

                    var child_x = iter_x.CurrentValue;
                    var child_y = iter_y.CurrentValue;

                    // compares values:
                    if ((array_x = child_x.ArrayOrNull()) != null)
                    {
                        if ((array_y = child_y.ArrayOrNull()) != null)
                        {
                            // at least one child has not been visited yet => continue with recursion:
                            if (array_x._visited == 0 || array_y._visited == 0)
                            {
                                result = StrictCompareArrays(array_x, array_y, out incomparable);
                            }
                            else
                            {
                                incomparable = true;
                                break;
                            }
                        }
                        else
                        {
                            // an array with a non-array comparison:
                            result = false;
                        }
                    }
                    else
                    {
                        // compares unknown item with a non-array:
                        result = child_x.GetValue().StrictEquals(child_y.GetValue());
                    }
                } // while
            }
            finally
            {
                x._visited--;
                y._visited--;
            }
            return result;
        }

        #endregion

        #region IPhpEnumerator (IntrinsicEnumerator)

        private bool EnsureIntrinsicEnumerator() => OrderedDictionary.FastEnumerator.EnsureValid(table, ref _intrinsicEnumerator);

        bool IPhpEnumerator.MoveLast() => OrderedDictionary.FastEnumerator.MoveLast(table, out _intrinsicEnumerator);

        bool IPhpEnumerator.MoveFirst()
        {
            _intrinsicEnumerator = 0;
            return EnsureIntrinsicEnumerator();
        }

        bool IPhpEnumerator.MovePrevious() => EnsureIntrinsicEnumerator() && OrderedDictionary.FastEnumerator.MovePrevious(table, ref _intrinsicEnumerator);

        bool IPhpEnumerator.AtEnd => !EnsureIntrinsicEnumerator();

        PhpValue IPhpEnumerator.CurrentValue
        {
            get
            {
                if (EnsureIntrinsicEnumerator())
                    return new OrderedDictionary.FastEnumerator(table, _intrinsicEnumerator).CurrentValue;
                else
                    return PhpValue.False;
            }
        }

        PhpAlias IPhpEnumerator.CurrentValueAliased
        {
            get
            {
                if (EnsureIntrinsicEnumerator())
                    return new OrderedDictionary.FastEnumerator(table, _intrinsicEnumerator).CurrentValueAliased;
                else
                    return default;
            }
        }

        PhpValue IPhpEnumerator.CurrentKey => EnsureIntrinsicEnumerator()
            ? (PhpValue)new OrderedDictionary.FastEnumerator(table, _intrinsicEnumerator).CurrentKey
            : PhpValue.Null;

        void IEnumerator.Reset() => ((IPhpEnumerator)this).MoveFirst();

        bool IEnumerator.MoveNext() => EnsureIntrinsicEnumerator() && OrderedDictionary.FastEnumerator.MoveNext(table, ref _intrinsicEnumerator);

        object IEnumerator.Current => ((IPhpEnumerator)this).CurrentValue.ToClr();

        KeyValuePair<PhpValue, PhpValue> IEnumerator<KeyValuePair<PhpValue, PhpValue>>.Current
        {
            get
            {
                if (EnsureIntrinsicEnumerator())
                {
                    var pair = new OrderedDictionary.FastEnumerator(table, _intrinsicEnumerator).Current;
                    return new KeyValuePair<PhpValue, PhpValue>(pair.Key, pair.Value);
                }
                return default;
            }
        }

        #endregion

        #region IPhpEnumerable Members

        /// <summary>
        /// Intrinsic enumerator associated with the array.
        /// The enumerator points to the first item of the array immediately after the initialization if exists,
        /// otherwise it points to an invalid item and <see cref="IPhpEnumerator.AtEnd"/> is <B>true</B>.
        /// </summary>
        public IPhpEnumerator/*!*/IntrinsicEnumerator => this;

        /// <summary>
        /// Restarts intrinsic enumerator - moves it to the first item.
        /// </summary>
        /// <remarks>
        /// If the intrinsic enumerator has never been used on this instance nothing happens.
        /// </remarks>
        public void RestartIntrinsicEnumerator() => ((IEnumerator)this).Reset();

        /// <summary>
        /// Creates an enumerator used in foreach statement.
        /// </summary>
        /// <param name="aliasedValues">Whether the values returned by enumerator are assigned by reference.</param>
        /// <returns>The dictionary enumerator.</returns>
        public IPhpEnumerator/*!*/GetForeachEnumerator(bool aliasedValues)
        {
            if (Count == 0)
            {
                return EmptyPhpEnumerator.Instance;
            }

            if (aliasedValues)
            {
                EnsureWritable();

                // when enumerating aliases, changes are reflected to the enumerator;
                return new OrderedDictionary.Enumerator(table);
            }
            else
            {
                // when enumerating values, any upcoming changes to the array do not take effect to the enumerator
                return new OrderedDictionary.ReadonlyEnumerator(table);
            }
        }

        /// <summary>
        /// Creates an enumerator used in foreach statement.
        /// </summary>
        /// <param name="aliasedValues">Whether the values returned by enumerator are assigned by reference.</param>
        /// <param name="caller">Type of the caller (ignored).</param>
        /// <returns>The dictionary enumerator.</returns>
        /// <remarks>Used for internal purposes only!</remarks>
        public IPhpEnumerator GetForeachEnumerator(bool aliasedValues, RuntimeTypeHandle caller) => GetForeachEnumerator(aliasedValues);

        #endregion

        #region IPhpArray

        public PhpValue GetItemValue(IntStringKey key) => table.GetValueOrNull(key);

        public PhpValue GetItemValue(PhpValue index)
        {
            if (index.TryToIntStringKey(out var key))
            {
                return GetItemValue(key);
            }
            else
            {
                PhpException.IllegalOffsetType();
                return PhpValue.Null;
            }
        }

        public void SetItemValue(IntStringKey key, PhpValue value)
        {
            this.EnsureWritable();
            table.AssignValue(key, value);
        }

        public void SetItemValue(PhpValue index, PhpValue value)
        {
            if (index.TryToIntStringKey(out var key))
            {
                SetItemValue(key, value);
            }
            else
            {
                PhpException.IllegalOffsetType();
            }
        }

        public void SetItemAlias(IntStringKey key, PhpAlias alias)
        {
            this.EnsureWritable();
            table[key] = PhpValue.Create(alias);
        }

        public void SetItemAlias(PhpValue index, PhpAlias alias)
        {
            if (index.TryToIntStringKey(out IntStringKey key))
            {
                SetItemAlias(key, alias);
            }
            else
            {
                throw new ArgumentException(nameof(index));
            }
        }

        public void AddValue(PhpValue value) => Add(value);

        public PhpAlias EnsureItemAlias(IntStringKey key)
        {
            this.EnsureWritable();
            return PhpValue.EnsureAlias(ref table.EnsureValue(key));
        }

        public object EnsureItemObject(IntStringKey key)
        {
            this.EnsureWritable();
            return PhpValue.EnsureObject(ref table.EnsureValue(key));
        }

        public IPhpArray EnsureItemArray(IntStringKey key)
        {
            this.EnsureWritable();
            return PhpValue.EnsureArray(ref table.EnsureValue(key));
        }

        public void RemoveKey(IntStringKey key) => this.Remove(key);

        public void RemoveKey(PhpValue index)
        {
            if (index.TryToIntStringKey(out var key))
            {
                this.Remove(key);
            }
        }

        #endregion
    }
}
