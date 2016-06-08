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
    public partial class PhpArray : PhpHashtable, IPhpConvertible, IPhpArrayOperators, IPhpComparable, IPhpEnumerable
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
        /// Creates a new instance of <see cref="PhpArray"/> with specified capacities for integer and string keys respectively.
        /// </summary>
        /// <param name="intCapacity"></param>
        /// <param name="stringCapacity"></param>
        public PhpArray(int intCapacity, int stringCapacity) : base(intCapacity + stringCapacity) { }

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
            PhpArray result = new PhpArray(values.Length, 0);
            foreach (object value in values)
                result.Add(value);
            return result;
        }

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

        #endregion

        #region Operators

        /// <summary>
        /// Creates copy of this instance using shared underlaying hashtable.
        /// </summary>
        public PhpArray DeepCopy() => new PhpArray(this);

        /// <summary>
        /// Gets PHP enumerator for this array.
        /// </summary>
        public new OrderedDictionary.Enumerator GetEnumerator() => new OrderedDictionary.Enumerator(this);

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
            //PhpException.Throw(PhpError.Notice, CoreResources.GetString("array_to_string_conversion"));
            
            return ToString(ctx);
        }

        public object ToClass()
        {
            return new stdClass()
            {
                __peach__runtimeFields = this.DeepCopy()
            };
        }

        #endregion

        #region IPhpComparable

        public int Compare(PhpValue obj)
        {
            switch (obj.TypeCode)
            {
                case PhpTypeCode.Object:
                    if (obj.Object == null) return Count;
                    break;

                case PhpTypeCode.Boolean:
                    return (Count > 0 ? 2 : 1) - (obj.Boolean ? 2 : 1);

                case PhpTypeCode.PhpArray:
                    // compare elements:
                    //bool incomparable;
                    //int result = CompareArrays(this, obj.Array, comparer, out incomparable);
                    //if (incomparable)
                    //{
                    //    //PhpException.Throw(PhpError.Warning, CoreResources.GetString("incomparable_arrays_compared"));
                    //    throw new ArgumentException("incomparable_arrays_compared");
                    //}
                    //return result;
                    throw new NotImplementedException();
            }

            //
            return 1;
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
            if (_intrinsicEnumerator != null)
                _intrinsicEnumerator.MoveFirst();
        }

        /// <summary>
        /// Creates an enumerator used in foreach statement.
        /// </summary>
        /// <param name="aliasedValues">Whether the values returned by enumerator are assigned by reference.</param>
        /// <returns>The dictionary enumerator.</returns>
        public IPhpEnumerator GetForeachEnumerator(bool aliasedValues) => aliasedValues
                ? new OrderedDictionary.Enumerator(this)            // when enumerating aliases, changes are reflected to the enumerator
                : new OrderedDictionary.ReadonlyEnumerator(this);   // when enumerating values, any upcoming changes to the array do not take effect to the enumerator

        /// <summary>
        /// Creates an enumerator used in foreach statement.
        /// </summary>
        /// <param name="aliasedValues">Whether the values returned by enumerator are assigned by reference.</param>
        /// <param name="caller">Type of the caller (ignored).</param>
        /// <returns>The dictionary enumerator.</returns>
        /// <remarks>Used for internal purposes only!</remarks>
        public IPhpEnumerator GetForeachEnumerator(bool aliasedValues, RuntimeTypeHandle caller) => GetForeachEnumerator(aliasedValues);

        #endregion

        #region IPhpArrayOperators

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

        public PhpArray EnsureItemArray(IntStringKey key) => table._ensure_item_array(ref key, this);

        public void RemoveKey(IntStringKey key) => this.Remove(key);

        #endregion
    }
}
