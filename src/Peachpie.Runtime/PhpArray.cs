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
    public partial class PhpArray : PhpHashtable, IPhpConvertible, IPhpArray, IPhpComparable, IPhpEnumerable
    {
        /// <summary>
        /// Used in all PHP functions determining the type name. (var_dump, ...)
        /// </summary>
        public const string PhpTypeName = "array";

        /// <summary>
        /// Used in print_r function.
        /// </summary>
        public const string PrintablePhpTypeName = "Array";

        ///// <summary>
		///// If this flag is <B>true</B> the array will be copied inplace by the immediate <see cref="Copy"/> call.
		///// </summary>
        //public bool InplaceCopyOnReturn { get { return this.table.InplaceCopyOnReturn; } set { this.table.InplaceCopyOnReturn = value; } }

        /// <summary>
        /// Intrinsic enumerator associated with the array. Initialized lazily.
        /// </summary>
        protected OrderedDictionary.Enumerator _intrinsicEnumerator;

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
        /// Must be set to <c>false</c> immediately after the pass.
        /// </remarks>
        public bool Visited { get { return _visited; } set { _visited = value; } }
        bool _visited = false;

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="PhpArray"/> with specified capacities for integer and string keys respectively.
        /// </summary>
        public PhpArray() : base() { }

        /// <summary>
        /// Creates a new instance of <see cref="PhpArray"/> with specified capacities for integer and string keys respectively.
        /// </summary>
        /// <param name="capacity"></param>
        public PhpArray(int capacity) : base(capacity) { }

        /// <summary>
        /// Creates a new instance of <see cref="PhpArray"/> initialized with all values from <see cref="System.Array"/>.
        /// </summary>
        /// <param name="values"></param>
        public PhpArray(Array values) : base(values) { }

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
            : base((data is ICollection) ? ((ICollection)data).Count : 0)
        {
            if (data != null)
            {
                foreach (object value in data)
                {
                    AddToEnd(PhpValue.FromClr(value));
                }
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="PhpArray"/> filled by data from an enumerator.
        /// </summary>
        /// <param name="data">The enumerator containing values added to the new instance.</param>
        public PhpArray(IEnumerable<KeyValuePair<IntStringKey, PhpValue>> data)
            : base((data is ICollection) ? ((ICollection)data).Count : 0)
        {
            if (data != null)
            {
                foreach (var item in data)
                {
                    Add(item);
                }
            }
        }

        /// <summary>
        /// Copy constructor. Creates <see cref="PhpArray"/> that shares internal data table with another <see cref="PhpArray"/>.
        /// </summary>
        /// <param name="array">Table to be shared.</param>
        /// <param name="preserveMaxInt">True to copy the <see cref="PhpHashtable.MaxIntegerKey"/> from <paramref name="array"/>.
        /// Otherwise the value will be recomputed when needed. See http://phalanger.codeplex.com/workitem/31484 for more details.</param>
        public PhpArray(PhpArray/*!*/array, bool preserveMaxInt)
            : base(array, preserveMaxInt)
        {

        }

        /// <summary>
        /// Creates an instance of <see cref="PhpArray"/> filled by given values.
        /// </summary>
        /// <param name="values">Values to be added to the new instance. 
        /// Keys will correspond order of values in the array.</param>
        public static PhpArray New(params object[] values)
        {
            PhpArray result = new PhpArray(values.Length);
            foreach (object value in values)
            {
                result.Add(value);
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
            PhpArray result = new PhpArray(values.Length);
            foreach (var value in values)
            {
                result.Add(value);
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

        #endregion

        #region Operators

        /// <summary>
        /// Creates copy of this instance using shared underlaying hashtable.
        /// </summary>
        public PhpArray DeepCopy() => new PhpArray(this, true);

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
        /// Gets PHP enumerator for this array.
        /// </summary>
        public new OrderedDictionary.Enumerator GetEnumerator() => new OrderedDictionary.Enumerator(this);

        /// <summary>
        /// Adds a variable into the array while keeping duplicit keys in sub-arrays of indexed items.
        /// </summary>
        /// <param name="name">Key, respecting <c>[subkey]</c> notation.</param>
        /// <param name="value">The value.</param>
        /// <remarks>See <see cref="NameValueCollectionUtils.AddVariable(IPhpArray, string, string, string)"/> for details.</remarks>
        public void AddVariable(string name, string value) => NameValueCollectionUtils.AddVariable(this, name, value);

        #endregion

        #region IPhpConvertible

        public PhpTypeCode TypeCode => PhpTypeCode.PhpArray;

        public double ToDouble() => Count;

        public long ToLong() => Count;

        public bool ToBoolean() => Count != 0;

        public Convert.NumberInfo ToNumber(out PhpNumber number)
        {
            number = PhpNumber.Create(Count);
            return Convert.NumberInfo.IsPhpArray | Convert.NumberInfo.LongInteger;
        }

        public string ToString(Context ctx)
        {
            return PrintablePhpTypeName;
        }

        public string ToStringOrThrow(Context ctx)
        {
            PhpException.Throw(PhpError.Notice, ErrResources.array_to_string_conversion);            
            return ToString(ctx);
        }

        /// <summary>
        /// Creates <see cref="stdClass"/> with runtime instance fields copied from entries of this array.
        /// </summary>
        public object ToClass()
        {
            return new stdClass()
            {
                __peach__runtimeFields = this.DeepCopy()
            };
        }

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
                    return (value.Object == null) ? Count : 1;

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
            if (object.ReferenceEquals(x.table, y.table)) return 0;

            //
            PhpArray array_x, array_y;
            PhpArray sorted_x, sorted_y;
            
            // if numbers of elements differs:
            int result = x.Count - y.Count;
            if (result != 0) return result;

            // comparing with the same instance:
            if (x == y) return 0;

            // marks arrays as visited (will be always restored to false value before return):
            x.Visited = true;
            y.Visited = true;

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
                            if (!array_x.Visited || !array_y.Visited)
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
                x.Visited = false;
                y.Visited = false;
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
            if (object.ReferenceEquals(x.table, y.table)) return true;

            // if numbers of elements differs:
            if (x.Count != y.Count) return false;

            // comparing with the same instance:
            if (x == y) return true;

            var iter_x = x.GetFastEnumerator();
            var iter_y = y.GetFastEnumerator();

            PhpArray array_x, array_y;

            // marks arrays as visited (will be always restored to false value before return):
            x.Visited = true;
            y.Visited = true;

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
                            if (!array_x.Visited || !array_y.Visited)
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
                x.Visited = false;
                y.Visited = false;
            }
            return result;
        }

        #endregion

        #region IPhpEnumerable Members

        /// <summary>
        /// Intrinsic enumerator associated with the array. Initialized lazily when read for the first time.
        /// The enumerator points to the first item of the array immediately after the initialization if exists,
        /// otherwise it points to an invalid item and <see cref="IPhpEnumerator.AtEnd"/> is <B>true</B>.
        /// </summary>
        public IPhpEnumerator/*!*/ IntrinsicEnumerator
        {
            get
            {
                // initializes enumerator:
                if (_intrinsicEnumerator == null)
                {
                    _intrinsicEnumerator = this.GetPhpEnumerator();
                    _intrinsicEnumerator.MoveNext();
                }
                return _intrinsicEnumerator;
            }
        }

        /// <summary>
        /// Restarts intrinsic enumerator - moves it to the first item.
        /// </summary>
        /// <remarks>
        /// If the intrinsic enumerator has never been used on this instance nothing happens.
        /// </remarks>
        public void RestartIntrinsicEnumerator()
        {
            _intrinsicEnumerator?.MoveFirst();
        }

        /// <summary>
        /// Creates an enumerator used in foreach statement.
        /// </summary>
        /// <param name="aliasedValues">Whether the values returned by enumerator are assigned by reference.</param>
        /// <returns>The dictionary enumerator.</returns>
        public IPhpEnumerator GetForeachEnumerator(bool aliasedValues)
        {
            if (aliasedValues)
            {
                EnsureWritable();
            }

            return aliasedValues
                ? new OrderedDictionary.Enumerator(this)            // when enumerating aliases, changes are reflected to the enumerator
                : new OrderedDictionary.ReadonlyEnumerator(this);   // when enumerating values, any upcoming changes to the array do not take effect to the enumerator
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

        public PhpValue GetItemValue(IntStringKey key) => table._get(ref key);

        public void SetItemValue(IntStringKey key, PhpValue value)
        {
            this.EnsureWritable();
            table._add_or_update_preserve_ref(ref key, value);
            this.KeyAdded(ref key);
        }

        public void SetItemAlias(IntStringKey key, PhpAlias alias)
        {
            this.EnsureWritable();
            table._add_or_update(ref key, PhpValue.Create(alias));
            this.KeyAdded(ref key);
        }

        public void AddValue(PhpValue value) => Add(value);

        public PhpAlias EnsureItemAlias(IntStringKey key) => table._ensure_item_alias(ref key, this);

        public object EnsureItemObject(IntStringKey key) => table._ensure_item_object(ref key, this);

        public IPhpArray EnsureItemArray(IntStringKey key) => table._ensure_item_array(ref key, this);

        public void RemoveKey(IntStringKey key) => this.Remove(key);

        #endregion
    }
}
