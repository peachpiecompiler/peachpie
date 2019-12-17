using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core.Dynamic;

namespace Pchp.Core
{
    /// <summary>
    /// Represents a PHP value.
    /// </summary>
    /// <remarks>
    /// Note, <c>default(PhpValue)</c> does not represent a valid state of the object.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]   // {_type} has to be first for performance reasons.
    public partial struct PhpValue : IPhpConvertible, IEquatable<PhpValue> // <T>
    {
        #region Nested struct: ValueField

        /// <summary>
        /// Union for possible value types.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        [DebuggerNonUserCode]
        struct ValueField
        {
            [FieldOffset(0)]
            public bool @bool; // NOTE: must be first field, having bool as the last field confuses .NET debugger and converts the entire struct to `0` or `1` // https://github.com/peachpiecompiler/peachpie/issues/249 // if still causes issues, remove this field and use Long only

            [FieldOffset(0)]
            public long @long;

            [FieldOffset(0)]
            public double @double;
        }

        #endregion

        #region ObjectField

        /// <summary>
        /// Union for possible reference types.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        [DebuggerNonUserCode]
        struct ObjectField
        {
            [FieldOffset(0)]
            public object @object;

            [FieldOffset(0)]
            public string @string;

            [FieldOffset(0)]
            public PhpString.Blob blob;

            [FieldOffset(0)]
            public PhpArray array;

            [FieldOffset(0)]
            public PhpAlias alias;
        }

        #endregion

        #region Fields

        /// <summary>
        /// The value type.
        /// </summary>
        TypeTable _type;

        /// <summary>
        /// A value type container.
        /// </summary>
        readonly ValueField _value;

        /// <summary>
        /// A reference type container.
        /// </summary>
        ObjectField _obj;

        #endregion

        #region Properties

        /// <summary>
        /// Gets value indicating whether the value is a <c>NULL</c> or undefined.
        /// </summary>
        public bool IsNull => _type.IsNull(ref this);

        /// <summary>
        /// Gets value indicating whether the value is considered to be empty.
        /// </summary>
        public bool IsEmpty => _type.IsEmpty(ref this);

        /// <summary>
        /// The structure was not initialized.
        /// </summary>
        public bool IsDefault => ReferenceEquals(_type, null);

        /// <summary>
        /// Gets value indicating whether the value is set.
        /// </summary>
        public bool IsSet => !IsDefault && !ReferenceEquals(_type, Void._type);

        /// <summary>
        /// Gets value indicating the value is <c>FALSE</c> or <c>&amp;FALSE</c>.
        /// </summary>
        public bool IsFalse => IsBooleanImpl(out var b) && !b;

        /// <summary>
        /// Gets value indicating whether the value is an alias containing another value.
        /// </summary>
        public bool IsAlias => ReferenceEquals(_type, TypeTable.AliasTable);

        /// <summary>
        /// Gets value indicating the value represents an object.
        /// </summary>
        public bool IsObject => ReferenceEquals(_type, TypeTable.ClassTable);

        /// <summary>
        /// Gets value indicating the value represents PHP array.
        /// </summary>
        public bool IsArray => ReferenceEquals(_type, TypeTable.ArrayTable);

        /// <summary>
        /// Gets value indicating the value represents boolean.
        /// </summary>
        public bool IsBoolean => ReferenceEquals(_type, TypeTable.BoolTable);

        /// <summary>
        /// Gets value indicating this variable after dereferencing is a scalar variable.
        /// </summary>
        public bool IsScalar
        {
            get
            {
                switch (TypeCode)
                {
                    case PhpTypeCode.Boolean:
                    case PhpTypeCode.Int32:
                    case PhpTypeCode.Long:
                    case PhpTypeCode.Double:
                    case PhpTypeCode.String:
                    case PhpTypeCode.MutableString:
                    case PhpTypeCode.Null:
                        return true;

                    case PhpTypeCode.Object:
                        return this.Object == null; // Note: will be handled by PhpTypeCode.Null

                    case PhpTypeCode.Alias:
                        return Alias.Value.IsScalar;

                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Gets the long field of the value.
        /// Does not perform a conversion, expects the value is of type long.
        /// </summary>
        public long Long { get { Debug.Assert(TypeCode == PhpTypeCode.Long); return _value.@long; } }

        /// <summary>
        /// Gets the double field of the value.
        /// Does not perform a conversion, expects the value is of type double.
        /// </summary>
        public double Double { get { Debug.Assert(TypeCode == PhpTypeCode.Double); return _value.@double; } }

        /// <summary>
        /// Gets the boolean field of the value.
        /// Does not perform a conversion, expects the value is of type boolean.
        /// </summary>
        public bool Boolean { get { Debug.Assert(TypeCode == PhpTypeCode.Boolean); return _value.@bool; } }

        /// <summary>
        /// Gets the object field of the value as string.
        /// Does not perform a conversion, expects the value is of type (readonly UTF16) string.
        /// </summary>
        public string String { get { Debug.Assert(_obj.@object is string || _obj.@object == null); return _obj.@string; } }

        /// <summary>
        /// Gets underlaying <see cref="PhpString.Blob"/> object.
        /// </summary>
        internal PhpString.Blob MutableStringBlob
        {
            get
            {
                Debug.Assert(_obj.@object is PhpString.Blob);
                return _obj.blob;
            }
            set
            {
                _obj.@object = value ?? throw new ArgumentNullException();
                _type = TypeTable.MutableStringTable;
            }
        }

        /// <summary>
        /// Gets the object field of the value as PHP writable string.
        /// Does not perform a conversion, expects the value is of type (writable UTF16 or single-byte) string.
        /// </summary>
        public PhpString MutableString { get { return new PhpString(MutableStringBlob); } }

        /// <summary>
        /// Gets underlaying reference object.
        /// </summary>
        public object Object { get { return _obj.@object; } }

        /// <summary>
        /// Gets underlaying array object.
        /// </summary>
        public PhpArray Array { get { Debug.Assert(_obj.@object is PhpArray); return _obj.array; } }

        /// <summary>
        /// Gets underlaying alias object.
        /// </summary>
        public PhpAlias Alias { get { Debug.Assert(_obj.@object is PhpAlias); return _obj.alias; } }

        #endregion

        #region IPhpConvertible

        /// <summary>
        /// Gets the underlaying value type.
        /// </summary>
        public PhpTypeCode TypeCode => _type.Type;

        public object ToClass() => _type.ToClass(ref this);

        public long ToLong() => _type.ToLong(ref this);

        public double ToDouble() => _type.ToDouble(ref this);

        public decimal ToDecimal() => (decimal)_type.ToDouble(ref this);    // TODO: more precision when converting from string 

        public bool ToBoolean() => _type.ToBoolean(ref this);

        public Convert.NumberInfo ToNumber(out PhpNumber number) => _type.ToNumber(ref this, out number);

        public string ToString(Context ctx) => _type.ToString(ref this, ctx);

        public string ToStringOrThrow(Context ctx) => _type.ToStringOrThrow(ref this, ctx);

        #endregion

        #region Conversions

        public static implicit operator PhpValue(bool value) => Create(value);
        public static implicit operator PhpValue(int value) => Create(value);
        public static implicit operator PhpValue(long value) => Create(value);
        public static implicit operator PhpValue(double value) => Create(value);
        public static implicit operator PhpValue(PhpNumber value) => Create(value);
        public static implicit operator PhpValue(IntStringKey value) => Create(value);
        public static implicit operator PhpValue(string value) => Create(value);
        public static implicit operator PhpValue(byte[] value) => Create(value);
        public static implicit operator PhpValue(PhpArray value) => Create(value);
        public static implicit operator PhpValue(Delegate value) => FromClass(value);

        public static implicit operator bool(PhpValue value) => value.ToBoolean();

        public static explicit operator long(PhpValue value) => value.ToLong();

        public static explicit operator ushort(PhpValue value) => checked((ushort)(long)value);

        public static explicit operator int(PhpValue value) => checked((int)(long)value);

        public static explicit operator uint(PhpValue value) => checked((uint)(long)value);

        public static explicit operator double(PhpValue value) => value.ToDouble();

        public static explicit operator PhpNumber(PhpValue value)
        {
            PhpNumber result;
            if ((value.ToNumber(out result) & Convert.NumberInfo.Unconvertible) != 0)
            {
                // TODO: ErrCode
                throw new InvalidCastException();
            }

            return result;
        }

        public static explicit operator PhpArray(PhpValue value) => value.ToArray();

        public static explicit operator DateTime(PhpValue value) => value.ToDateTime();

        /// <summary>
        /// Implicit conversion to string,
        /// preserves <c>null</c>,
        /// throws if conversion is not possible.</summary>
        public string AsString(Context ctx) => _type.AsString(ref this, ctx);

        /// <summary>
        /// Conversion to <see cref="int"/>.
        /// </summary>
        public int ToInt() => (int)this;

        /// <summary>
        /// Implicit conversion to <see cref="long"/>.
        /// Throws <c>TypeError</c> in case the implicit conversion cannot be done.
        /// </summary>
        /// <returns></returns>
        public long ToLongOrThrow() => _type.ToLongOrThrow(ref this);

        /// <summary>
        /// Gets underlaying class instance or <c>null</c>.
        /// </summary>
        public object AsObject() => _type.AsObject(ref this);

        /// <summary>
        /// Casts the value to object instance.
        /// Non-object values are wrapped to <see cref="stdClass"/>.
        /// </summary>
        public object ToObject() => _type.ToClass(ref this);

        /// <summary>
        /// Converts value to <see cref="PhpArray"/>.
        /// 
        /// Value is converted according to PHP semantic:
        /// - array is returned as it is.
        /// - null is converted to an empty array.
        /// - scalars are converted to a new array containing a single item.
        /// - object is converted to a new array containing the object's properties.
        /// 
        /// This method cannot return a <c>null</c> reference.
        /// </summary>
        public PhpArray/*!*/ToArray() => _type.ToArray(ref this);

        /// <summary>
        /// Wraps the value into <see cref="PhpAlias"/>,
        /// if value already contains the aliased value, it is returned as it is.
        /// </summary>
        public PhpAlias/*!*/AsPhpAlias() => _obj.@object as PhpAlias ?? new PhpAlias(this);

        #endregion

        #region Operators

        public static bool operator ==(PhpValue left, PhpValue right) => left.Compare(right) == 0;

        public static bool operator !=(PhpValue left, PhpValue right) => left.Compare(right) != 0;

        public static bool operator <(PhpValue left, PhpValue right) => left.Compare(right) < 0;

        public static bool operator >(PhpValue left, PhpValue right) => left.Compare(right) > 0;

        public static bool operator ==(string left, PhpValue right) => Comparison.Ceq(left, right);

        public static bool operator !=(string left, PhpValue right) => !(left == right);

        public static bool operator ==(PhpValue left, string right) => right == left;

        public static bool operator !=(PhpValue left, string right) => right != left;

        public static PhpValue operator ~(PhpValue x) => Operators.BitNot(ref x);

        public static PhpValue operator &(PhpValue left, PhpValue right) => Operators.BitAnd(ref left, ref right);

        public static PhpValue operator |(PhpValue left, PhpValue right) => Operators.BitOr(ref left, ref right);

        public static PhpValue operator ^(PhpValue left, PhpValue right) => Operators.BitXor(ref left, ref right);

        /// <summary>
        /// Division of <paramref name="left"/> and <paramref name="right"/> according to PHP semantics.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>Quotient of <paramref name="left"/> and <paramref name="right"/>.</returns>
        public static PhpNumber operator /(PhpValue left, PhpValue right) => Operators.Div(ref left, ref right);

        public static PhpNumber operator *(PhpValue left, PhpValue right) => PhpNumber.Multiply(left, right);

        public static PhpNumber operator /(long lx, PhpValue y)
        {
            PhpNumber ny;
            if ((y.ToNumber(out ny) & Convert.NumberInfo.IsPhpArray) != 0)
            {
                //PhpException.UnsupportedOperandTypes();
                //return PhpNumber.Create(0.0);
                throw new NotImplementedException();     // PhpException
            }

            return lx / ny;
        }

        public static double operator /(double dx, PhpValue y)
        {
            PhpNumber ny;
            if ((y.ToNumber(out ny) & Convert.NumberInfo.IsPhpArray) != 0)
            {
                //PhpException.UnsupportedOperandTypes();
                //return PhpNumber.Create(0.0);
                throw new NotImplementedException();     // PhpException
            }

            return dx / ny;
        }

        /// <summary>
        /// Accesses the value as an array and gets item at given index.
        /// Gets <c>void</c> value in case the key is not found.
        /// Raises PHP exception in case the value cannot be accessed as an array.
        /// </summary>
        public PhpValue this[IntStringKey key]
        {
            get { return GetArrayItem(Create(key), false); }
        }

        public override bool Equals(object obj) => Equals((obj is PhpValue) ? (PhpValue)obj : FromClr(obj));

        public override int GetHashCode() => _obj.@object != null ? _obj.@object.GetHashCode() : (int)_value.@long;

        public bool TryToIntStringKey(out IntStringKey key) => _type.TryToIntStringKey(ref this, out key);

        public IntStringKey ToIntStringKey()
        {
            if (TryToIntStringKey(out var iskey))
            {
                return iskey;
            }

            PhpException.Throw(PhpError.Warning, Resources.ErrResources.illegal_offset_type);
            return IntStringKey.EmptyStringKey;
        }

        /// <summary>
        /// Gets enumerator object used within foreach statement.
        /// </summary>
        public IPhpEnumerator GetForeachEnumerator(bool aliasedValues, RuntimeTypeHandle caller) => _type.GetForeachEnumerator(ref this, aliasedValues, caller);

        /// <summary>
        /// Compares two value operands.
        /// </summary>
        /// <param name="right">The right operand.</param>
        /// <returns>Comparison result.
        /// Zero for equality,
        /// negative value for <c>this</c> &lt; <paramref name="right"/>,
        /// position value for <c>this</c> &gt; <paramref name="right"/>.</returns>
        public int Compare(PhpValue right) => _type.Compare(ref this, right);

        /// <summary>
        /// Performs strict comparison.
        /// </summary>
        /// <param name="right">The right operand.</param>
        /// <returns>The value determining operands are strictly equal.</returns>
        public bool StrictEquals(PhpValue right) => _type.StrictEquals(ref this, right);

        /// <summary>
        /// Passes the value as strictly typed <c>array</c>.
        /// 
        /// Gets the underlaying <see cref="PhpArray"/> or throws an exception.
        /// The <c>NULL</c> is returned as it is.        /// 
        /// Anything else than <c>NULL</c> or <see cref="PhpArray"/> causes an exception.
        /// </summary>
        /// <returns><see cref="PhpArray"/> instance or a<c>null</c> reference.</returns>
        /// <exception cref="Exception">(PHP's TypeError) Value is neither <see cref="PhpArray"/> or <c>null</c>.</exception>
        public PhpArray ToArrayOrThrow() => _type.ArrayOrThrow(ref this);

        /// <summary>
        /// Gets callable wrapper for the object dynamic invocation.
        /// </summary>
        public IPhpCallable AsCallable(RuntimeTypeHandle callerCtx = default(RuntimeTypeHandle), object callerObj = null) => _type.AsCallable(ref this, callerCtx, callerObj);

        public object EnsureObject() => _type.EnsureObject(ref this);

        /// <summary>
        /// Converts underlaying value into <see cref="IPhpArray"/>.
        /// </summary>
        /// <returns>PHP array instance.</returns>
        /// <remarks>Used for L-Values accessed as arrays (<code>$lvalue[] = rvalue</code>).</remarks>
        public IPhpArray EnsureArray() => _type.EnsureArray(ref this);

        public PhpAlias EnsureAlias() => _type.EnsureAlias(ref this);

        /// <summary>
        /// Gets <see cref="IPhpArray"/> instance providing access to the value with array operators.
        /// Returns <c>null</c> if underlaying value does provide array access.
        /// </summary>
        public IPhpArray GetArrayAccess() => _type.GetArrayAccess(ref this);

        /// <summary>
        /// Dereferences in case of an alias.
        /// </summary>
        /// <returns>Not aliased value.</returns>
        public PhpValue GetValue() => Object is PhpAlias alias ? alias.Value : this;

        /// <summary>
        /// Accesses the value as an array and gets item at given index.
        /// Gets <c>void</c> value in case the key is not found.
        /// </summary>
        public PhpValue GetArrayItem(PhpValue index, bool quiet = false) => _type.GetArrayItem(ref this, index, quiet);

        /// <summary>
        /// Accesses the value as an array and ensures the item at given index as alias.
        /// </summary>
        public PhpAlias EnsureItemAlias(PhpValue index, bool quiet = false) => _type.EnsureItemAlias(ref this, index, quiet);

        /// <summary>
        /// Creates a deep copy of PHP value.
        /// In case of scalars, the shallow copy is returned.
        /// In case of classes or aliases, the same reference is returned.
        /// In case of array or string, its copy is returned.
        /// </summary>
        public PhpValue DeepCopy() => _type.DeepCopy(ref this);

        /// <summary>
        /// Deep copies the value in-place.
        /// Called when this has been passed by value and inplace dereferencing and copying is necessary.
        /// </summary>
        [DebuggerNonUserCode, DebuggerStepThrough]
        public void PassValue()
        {
            if (_type != null)
            {
                // make copy if applicable
                _type.PassValue(ref this);
            }
            else
            {
                // ensure the value is not default(PhpValue)
                _type = Null._type;
            }
        }

        /// <summary>
        /// Outputs current value to <see cref="Context"/>.
        /// Handles byte (8bit) strings and allows for chunked text to be streamed without costly concatenation.
        /// </summary>
        public void Output(Context ctx) => _type.Output(ref this, ctx);

        /// <summary>
        /// Gets underlaying value or object as <see cref="System.Object"/>.
        /// </summary>
        public object ToClr()
        {
            switch (this.TypeCode)
            {
                case PhpTypeCode.Boolean: return Boolean;
                case PhpTypeCode.Double: return Double;
                case PhpTypeCode.Int32: return (int)Long;
                case PhpTypeCode.Long: return Long;
                case PhpTypeCode.PhpArray:
                case PhpTypeCode.String:
                case PhpTypeCode.Object: return Object;
                case PhpTypeCode.MutableString: return MutableStringBlob.ToString();
                case PhpTypeCode.Alias: return Alias.Value.ToClr();
                case PhpTypeCode.Undefined:
                case PhpTypeCode.Null: return null;
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// Implicitly converts this value to <paramref name="type"/>.
        /// </summary>
        /// <param name="type">Target type.</param>
        /// <returns>Converted value.</returns>
        /// <exception cref="InvalidCastException">The value cannot be converted to specified <paramref name="type"/>.</exception>
        public object ToClr(Type type)
        {
            if (type == typeof(PhpValue)) return this;
            if (type == typeof(PhpAlias)) return this.EnsureAlias();

            if (type == typeof(long)) return (long)this;
            if (type == typeof(int)) return (int)(long)this;
            if (type == typeof(uint)) return (uint)(long)this;
            if (type == typeof(double)) return this.ToDouble();
            if (type == typeof(float)) return (float)this.ToDouble();
            if (type == typeof(bool)) return this.ToBoolean();
            if (type == typeof(PhpArray)) return this.ToArray();
            if (type == typeof(string)) return this.ToString();
            if (type == typeof(object)) return this.ToClass();

            if (this.Object != null && type.IsAssignableFrom(this.Object.GetType()))
            {
                return this.Object;
            }

            //if (type.IsNullable_T(out var nullable_t))
            //{
            //    throw new NotImplementedException();
            //}

            //
            throw new InvalidCastException($"{this.TypeCode} -> {type.FullName}");
        }

        /// <summary>
        /// Calls corresponding <c>Accept</c> method on visitor.
        /// </summary>
        public void Accept(PhpVariableVisitor visitor) => _type.Accept(ref this, visitor);

        /// <summary>
        /// Gets value converted to string using default configuration options.
        /// </summary>
        public override string ToString()
        {
            Debug.WriteLine("Use ToString(Context) instead!");
            return _type.ToStringQuiet(ref this);
        }

        /// <summary>
        /// Implements <c>foreach</c> over <see cref="PhpValue"/>.
        /// Gets the enumerator object allowing to iterate through PHP values, arrays and iterators.
        /// </summary>
        public IEnumerator<KeyValuePair<PhpValue, PhpValue>> GetEnumerator() => this.GetForeachEnumerator(false, default(RuntimeTypeHandle));

        #endregion

        #region Helpers

        /// <summary>
        /// Checks the value is of type <c>bool</c> or <c>&amp;bool</c> and gets its value.
        /// </summary>
        internal bool IsBooleanImpl(out bool b)
        {
            if (IsBoolean)
            {
                b = _value.@bool;
                return true;
            }
            else if (IsAlias)
            {
                return _obj.alias.Value.IsBoolean(out b);
            }
            else
            {
                b = default;
                return false;
            }
        }

        /// <summary>
        /// Checks the value is of type <c>string</c> or <c>&amp;string</c> and gets its value.
        /// Single-byte strings are decoded using <c>UTF-8</c>.
        /// </summary>
        internal bool IsStringImpl(out string str)
        {
            if (ReferenceEquals(_type, TypeTable.StringTable))
            {
                str = _obj.@string;
                return true;
            }
            else if (ReferenceEquals(_type, TypeTable.MutableStringTable))
            {
                str = _obj.blob.ToString(Encoding.UTF8);
                return true;
            }
            else if (IsAlias)
            {
                return _obj.alias.Value.IsStringImpl(out str);
            }
            else
            {
                str = default;
                return false;
            }
        }

        /// <summary>
        /// Checks the value is of type <c>string</c> (both unicode and single-byte) or an alias to a string.
        /// </summary>
        internal bool IsStringImpl()
        {
            return ReferenceEquals(_type, TypeTable.StringTable) ||
                   ReferenceEquals(_type, TypeTable.MutableStringTable) ||
                   (IsAlias && _obj.alias.Value.IsStringImpl());
        }

        /// <summary>
        /// Converts current value to <see cref="PhpString"/> and gets mutable access to the value.
        /// </summary>
        /// <returns>Object on which edit operations can be performed. Cannot be <c>null</c>.</returns>
        internal PhpString.Blob EnsureWritableString()
        {
            if (ReferenceEquals(_type, TypeTable.MutableStringTable))
            {
                var blob = MutableStringBlob;
                if (blob.IsShared)
                {
                    MutableStringBlob = blob = blob.ReleaseOne();
                }
                return blob;
            }
            else if (IsNull)
            {
                return (MutableStringBlob = new PhpString.Blob());
            }
            else if (ReferenceEquals(_type, TypeTable.StringTable))
            {
                return (MutableStringBlob = new PhpString.Blob(this.String));
            }
            else if (IsAlias)
            {
                return _obj.alias.Value.EnsureWritableString();
            }
            else
            {
                return (MutableStringBlob = new PhpString.Blob(_type.ToStringQuiet(ref this)));
            }
        }

        #endregion

        #region IEquatable<PhpValue>

        public bool Equals(PhpValue other) => this.Compare(other) == 0;

        #endregion

        #region Construction

        /// <summary>
        /// Singleton of PhpValue representing <c>void</c>.
        /// </summary>
        public static readonly PhpValue Void = new PhpValue(new VoidTable());

        /// <summary>
        /// Singleton of PhpValue representing <c>null</c>.
        /// </summary>
        public static readonly PhpValue Null = new PhpValue(TypeTable.NullTable);

        /// <summary>
        /// PhpValue representing <c>false</c>.
        /// </summary>
        public static readonly PhpValue False = new PhpValue(false);

        /// <summary>
        /// PhpValue representing <c>true</c>.
        /// </summary>
        public static readonly PhpValue True = new PhpValue(true);

        private PhpValue(long value) : this(TypeTable.LongTable)
        {
            _value.@long = value;
        }

        private PhpValue(double value) : this(TypeTable.DoubleTable)
        {
            _value.@double = value;
        }

        private PhpValue(bool value) : this(TypeTable.BoolTable)
        {
            _value.@bool = value;
        }

        private PhpValue(TypeTable type, object obj) : this(ReferenceEquals(obj, null) ? TypeTable.NullTable : type)
        {
            _obj.@object = obj;
        }

        private PhpValue(PhpString.Blob blob) : this(TypeTable.MutableStringTable)
        {
            Debug.Assert(blob != null);
            _obj.blob = blob;
        }

        internal PhpValue(string value) : this(TypeTable.StringTable)
        {
            Debug.Assert(value != null);
            _obj.@string = value;
        }

        private PhpValue(PhpArray array) : this(TypeTable.ArrayTable)
        {
            Debug.Assert(array != null);
            _obj.array = array;
        }

        private PhpValue(TypeTable type)
        {
            Debug.Assert(type != null);

            _type = type;
            _value = default;
            _obj = default;
        }

        public static PhpValue Create(PhpNumber number) => number.ToPhpValue();

        public static PhpValue Create(long value) => new PhpValue(value);

        public static PhpValue Create(double value) => new PhpValue(value);

        public static PhpValue Create(int value) => new PhpValue(value);

        public static PhpValue Create(bool value) => value ? True : False;

        public static PhpValue Create(string value) => new PhpValue(TypeTable.StringTable, value);

        public static PhpValue Create(PhpString value) => PhpString.AsPhpValue(value);

        internal static PhpValue Create(PhpString.Blob blob) => new PhpValue(blob);

        internal static PhpValue Create(byte[] bytes) => new PhpValue(new PhpString.Blob(bytes));

        public static PhpValue Create(PhpArray value) => new PhpValue(TypeTable.ArrayTable, value);

        public static PhpValue Create(IPhpArray value) => value is PhpArray arr ? Create(arr) : FromClass(value);

        public static PhpValue Create(PhpAlias value) => new PhpValue(TypeTable.AliasTable, value);

        /// <summary>
        /// Creates <see cref="PhpValue"/> from <see cref="Nullable{T}"/>.
        /// In case <see cref="Nullable{T}.HasValue"/> is <c>false</c>, a <see cref="PhpValue.False"/> is returned.
        /// </summary>
        /// <typeparam name="T">Nullable type argument.</typeparam>
        /// <param name="value">Original value to convert from.</param>
        /// <returns><see cref="PhpValue"/> containing value of given nullable, or <c>FALSE</c> if nullable has no value.</returns>
        public static PhpValue Create<T>(T? value) where T : struct => value.HasValue ? FromClr(value.GetValueOrDefault()) : PhpValue.False;

        /// <summary>
        /// Creates value containing new <see cref="PhpAlias"/> pointing to <c>NULL</c> value.
        /// </summary>
        public static PhpValue CreateAlias() => CreateAlias(Null);

        /// <summary>
        /// Creates value containing new <see cref="PhpAlias"/>.
        /// </summary>
        public static PhpValue CreateAlias(PhpValue value) => Create(new PhpAlias(value));

        public static PhpValue Create(IntStringKey value) => value.IsInteger ? Create(value.Integer) : Create(value.String);

        public static PhpValue FromClass(object value)
        {
            Debug.Assert(!(value is int || value is long || value is bool || value is string || value is double || value is PhpAlias || value is PhpString || value is PhpArray));
            return new PhpValue(TypeTable.ClassTable, value);
        }

        /// <summary>
        /// Implicitly converts a CLR type to PHP type.
        /// </summary>
        public static PhpValue FromClr(object value)
        {
            // implicit conversion from CLR types to PHP types
            if (value != null)
            {
                if (value.GetType() == typeof(int)) return Create((int)value);
                if (value.GetType() == typeof(long)) return Create((long)value);
                if (value.GetType() == typeof(double)) return Create((double)value);
                if (value.GetType() == typeof(float)) return Create((double)(float)value);
                if (value.GetType() == typeof(bool)) return Create((bool)value);
                if (value.GetType() == typeof(string)) return Create((string)value);
                if (value.GetType() == typeof(PhpString)) return Create((PhpString)value);
                if (value.GetType() == typeof(PhpAlias)) return Create((PhpAlias)value);
                if (value.GetType() == typeof(PhpArray)) return Create((PhpArray)value);
                if (value.GetType() == typeof(PhpValue)) return (PhpValue)value;
                if (value.GetType() == typeof(PhpNumber)) return Create((PhpNumber)value);
                if (value.GetType() == typeof(uint)) return Create((long)(uint)value);
                if (value.GetType() == typeof(byte[])) return Create(new PhpString((byte[])value));

                // object        
                return FromClass(value);
            }
            else
            {
                return Null;
            }
        }

        /// <summary>
        /// Implicitly converts a CLR type to PHP type.
        /// </summary>
        public static PhpValue FromClr(PhpValue value) => value;

        /// <summary>
        /// Converts an array of CLR values to PHP values.
        /// </summary>
        public static PhpValue[] FromClr(params object[] values)
        {
            if (values == null || values.Length == 0)
            {
                return Utilities.ArrayUtils.EmptyValues;
            }

            //
            var result = new PhpValue[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                result[i] = FromClr(values[i]);
            }

            //
            return result;
        }

        #endregion
    }
}
