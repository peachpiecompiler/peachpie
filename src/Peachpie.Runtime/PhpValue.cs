using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
        struct ValueField
        {
            [FieldOffset(0)]
            public long Long;
            [FieldOffset(0)]
            public double Double;
            [FieldOffset(0)]
            public int Int;
            [FieldOffset(0)]
            public bool Bool;
        }

        #endregion

        #region Nested struct: ObjectField

        /// <summary>
        /// Union for reference types.
        /// </summary>
        //[StructLayout(LayoutKind.Explicit)]
        struct ObjectField
        {
            //[FieldOffset(0)]
            public object Obj;
            //[FieldOffset(0)]
            public string String => (string)Obj;
            //[FieldOffset(0)]
            public PhpArray Array => (PhpArray)Obj;
            //[FieldOffset(0)]
            public PhpAlias Alias => (PhpAlias)Obj;

            public override int GetHashCode() => (Obj != null) ? Obj.GetHashCode() : 0;
        }

        #endregion

        #region Fields

        /// <summary>
        /// The value type.
        /// </summary>
        TypeTable _type;

        /// <summary>
        /// A reference type container.
        /// </summary>
        ObjectField _obj;

        /// <summary>
        /// A value type container.
        /// </summary>
        ValueField _value;

        #endregion

        #region Properties

        /// <summary>
        /// Gets value indicating whether the value is a <c>NULL</c> or undefined.
        /// </summary>
        public bool IsNull => _type.IsNull;

        /// <summary>
        /// Gets value indicating whether the value is considered to be empty.
        /// </summary>
        public bool IsEmpty => _type.IsEmpty(ref this);

        /// <summary>
        /// Gets value indicating whether the value is set.
        /// </summary>
        public bool IsSet => (_type != null && TypeCode != PhpTypeCode.Undefined);

        /// <summary>
        /// Gets value indicating whether the value is an alias containing another value.
        /// </summary>
        public bool IsAlias => (TypeCode == PhpTypeCode.Alias);

        /// <summary>
        /// Gets value indicating the value represents an object.
        /// </summary>
        public bool IsObject => (TypeCode == PhpTypeCode.Object);

        /// <summary>
        /// Gets value indicating the value represents PHP array.
        /// </summary>
        public bool IsArray => (TypeCode == PhpTypeCode.PhpArray);

        /// <summary>
        /// Gets value indicating the value represents boolean.
        /// </summary>
        public bool IsBoolean => (TypeCode == PhpTypeCode.Boolean);

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
                    case PhpTypeCode.WritableString:
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
        public long Long { get { Debug.Assert(TypeCode == PhpTypeCode.Long); return _value.Long; } }

        /// <summary>
        /// Gets the double field of the value.
        /// Does not perform a conversion, expects the value is of type double.
        /// </summary>
        public double Double { get { Debug.Assert(TypeCode == PhpTypeCode.Double); return _value.Double; } }

        /// <summary>
        /// Gets the boolean field of the value.
        /// Does not perform a conversion, expects the value is of type boolean.
        /// </summary>
        public bool Boolean { get { Debug.Assert(TypeCode == PhpTypeCode.Boolean); return _value.Bool; } }

        /// <summary>
        /// Gets the object field of the value as string.
        /// Does not perform a conversion, expects the value is of type (readonly UTF16) string.
        /// </summary>
        public string String { get { Debug.Assert(_obj.Obj is string || _obj.Obj == null); return _obj.String; } }

        /// <summary>
        /// Gets the object field of the value as PHP writable string.
        /// Does not perform a conversion, expects the value is of type (writable UTF16 or single-byte) string.
        /// </summary>
        public PhpString WritableString { get { Debug.Assert(_obj.Obj is PhpString); return (PhpString)_obj.Obj; } }

        /// <summary>
        /// Gets underlaying reference object.
        /// </summary>
        public object Object { get { return _obj.Obj; } }

        /// <summary>
        /// Gets underlaying array object.
        /// </summary>
        public PhpArray Array { get { Debug.Assert(TypeCode == PhpTypeCode.PhpArray); return _obj.Array; } }

        /// <summary>
        /// Gets underlaying alias object.
        /// </summary>
        public PhpAlias Alias { get { Debug.Assert(_obj.Obj is PhpAlias); return _obj.Alias; } }

        #endregion

        #region IPhpConvertible

        /// <summary>
        /// Gets the underlaying value type.
        /// </summary>
        public PhpTypeCode TypeCode => _type.Type;

        public object ToClass() => _type.ToClass(ref this);

        public long ToLong() => _type.ToLong(ref this);

        public double ToDouble() => _type.ToDouble(ref this);

        public bool ToBoolean() => _type.ToBoolean(ref this);

        public Convert.NumberInfo ToNumber(out PhpNumber number) => _type.ToNumber(ref this, out number);

        public string ToString(Context ctx) => _type.ToString(ref this, ctx);

        public string ToStringOrThrow(Context ctx) => _type.ToStringOrThrow(ref this, ctx);

        #endregion

        #region Conversions

        public static explicit operator PhpValue(int value) => Create(value);
        public static explicit operator PhpValue(long value) => Create(value);
        public static explicit operator PhpValue(double value) => Create(value);
        public static explicit operator PhpValue(string value) => Create(value);
        public static explicit operator PhpValue(PhpArray value) => Create(value);
        public static explicit operator PhpValue(bool value) => Create(value);

        #endregion

        #region Operators

        public static bool operator ==(PhpValue left, PhpValue right) => left.Compare(right) == 0;

        public static bool operator !=(PhpValue left, PhpValue right) => left.Compare(right) != 0;

        public static bool operator <(PhpValue left, PhpValue right) => left.Compare(right) < 0;

        public static bool operator >(PhpValue left, PhpValue right) => left.Compare(right) > 0;

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

        public static explicit operator bool(PhpValue value) => value.ToBoolean();

        public static explicit operator long(PhpValue value) => value.ToLong();

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

        public static explicit operator PhpArray(PhpValue value) => value.AsArray();

        /// <summary>
        /// Accesses the value as an array and gets item at given index.
        /// Gets <c>void</c> value in case the key is not found.
        /// Raises PHP exception in case the value cannot be accessed as an array.
        /// </summary>
        public PhpValue this[IntStringKey key]
        {
            get { return GetArrayItem(key, false); }
        }

        public override bool Equals(object obj) => Equals((obj is PhpValue) ? (PhpValue)obj : FromClr(obj));

        public override int GetHashCode() => _obj.GetHashCode() ^ (int)_value.Long;

        public IntStringKey ToIntStringKey() => _type.ToIntStringKey(ref this);

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
        /// Gets underlaying class instance or <c>null</c>.
        /// </summary>
        public object AsObject() => _type.AsObject(ref this);

        /// <summary>
        /// Converts value to an array.
        /// </summary>
        public PhpArray ToArray() => _type.ToArray(ref this);

        /// <summary>
        /// Gets callable wrapper for the object dynamic invocation.
        /// </summary>
        /// <returns></returns>
        public IPhpCallable AsCallable() => _type.AsCallable(ref this);

        public object EnsureObject() => _type.EnsureObject(ref this);

        /// <summary>
        /// Converts underlaying value into <see cref="IPhpArray"/>.
        /// </summary>
        /// <returns>PHP array instance.</returns>
        /// <remarks>Used for L-Values accessed as arrays (<code>$lvalue[] = rvalue</code>).</remarks>
        public IPhpArray EnsureArray() => _type.EnsureArray(ref this);

        public PhpAlias EnsureAlias() => _type.EnsureAlias(ref this);

        /// <summary>
        /// Dereferences in case of an alias.
        /// </summary>
        /// <returns>Not aliased value.</returns>
        public PhpValue GetValue() => IsAlias ? Alias.Value : this;

        /// <summary>
        /// Accesses the value as an array and gets item at given index.
        /// Gets <c>void</c> value in case the key is not found.
        /// </summary>
        public PhpValue GetArrayItem(IntStringKey key, bool quiet = false) => _type.GetArrayItem(ref this, key, quiet);

        /// <summary>
        /// Accesses the value as an array and ensures the item at given index as alias.
        /// </summary>
        public PhpAlias EnsureItemAlias(IntStringKey key, bool quiet = false) => _type.EnsureItemAlias(ref this, key, quiet);

        /// <summary>
        /// Creates a deep copy of PHP value.
        /// In case of scalars, the shallow copy is returned.
        /// In case of classes or aliases, the same reference is returned.
        /// In case of array or string, its copy is returned.
        /// </summary>
        public PhpValue DeepCopy() => _type.DeepCopy(ref this);

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
                case PhpTypeCode.Int32: return _value.Int;
                case PhpTypeCode.Long: return Long;
                case PhpTypeCode.Object: return Object;
                case PhpTypeCode.PhpArray: return Array;
                case PhpTypeCode.String: return String;
                case PhpTypeCode.WritableString: return WritableString.ToString();
                case PhpTypeCode.Alias: return Alias.Value.ToClr();
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
            
            if (type == typeof(long)) return this.ToLong();
            if (type == typeof(int)) return (int)this.ToLong();
            if (type == typeof(double)) return this.ToDouble();
            if (type == typeof(bool)) return this.ToBoolean();
            if (type == typeof(PhpArray)) return this.ToArray();
            if (type == typeof(string)) return this.ToString();
            if (type == typeof(object)) return this.ToClass();

            if (this.Object != null && type.GetTypeInfo().IsAssignableFrom(this.Object.GetType()))
            {
                return this.Object;
            }
            
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
        public static readonly PhpValue Null = new PhpValue(new NullTable());

        /// <summary>
        /// PhpValue representing <c>false</c>.
        /// </summary>
        public static PhpValue False => PhpValue.Create(false);

        /// <summary>
        /// PhpValue representing <c>true</c>.
        /// </summary>
        public static PhpValue True => PhpValue.Create(true);

        private PhpValue(long value) : this()
        {
            _type = TypeTable.LongTable;
            _value.Long = value;
        }

        private PhpValue(double value) : this()
        {
            _type = TypeTable.DoubleTable;
            _value.Double = value;
        }

        private PhpValue(bool value) : this()
        {
            _type = TypeTable.BoolTable;
            _value.Bool = value;
        }

        private PhpValue(TypeTable type, object obj) : this()
        {
            _type = (obj != null) ? type : TypeTable.NullTable;
            _obj.Obj = obj;
        }

        private PhpValue(TypeTable type) : this()
        {
            _type = type;
            Debug.Assert(IsNull || !IsSet);
        }

        public static PhpValue Create(PhpNumber number)
            => (number.IsLong)
                 ? Create(number.Long)
                 : Create(number.Double);

        public static PhpValue Create(long value) => new PhpValue(value);

        public static PhpValue Create(double value) => new PhpValue(value);

        public static PhpValue Create(int value) => new PhpValue(value);

        public static PhpValue Create(bool value) => new PhpValue(value);

        public static PhpValue Create(string value) => new PhpValue(TypeTable.StringTable, value);

        public static PhpValue Create(PhpString value) => new PhpValue(TypeTable.WritableStringTable, value);

        public static PhpValue Create(PhpArray value) => new PhpValue(TypeTable.ArrayTable, value);

        public static PhpValue Create(PhpAlias value) => new PhpValue(TypeTable.AliasTable, value);

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
                if (value.GetType() == typeof(bool)) return Create((bool)value);
                if (value.GetType() == typeof(string)) return Create((string)value);
                if (value.GetType() == typeof(PhpString)) return Create((PhpString)value);
                if (value.GetType() == typeof(PhpAlias)) return Create((PhpAlias)value);
                if (value.GetType() == typeof(PhpArray)) return Create((PhpArray)value);
                if (value.GetType() == typeof(PhpValue)) return (PhpValue)value;

                //                
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
                result[i] = FromClass(values[i]);
            }

            //
            return result;
        }

        #endregion
    }
}
