using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core.Dynamic;
using Pchp.Core.Reflection;
using Pchp.Core.Utilities;

namespace Pchp.Core
{
    /// <summary>
    /// Represents a PHP value.
    /// </summary>
    /// <remarks>
    /// Note, <c>default(PhpValue)</c> does not represent a valid state of the object.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly partial struct PhpValue : IPhpConvertible, IEquatable<PhpValue> // <T>
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
        readonly PhpTypeCode _type;  // CONSIDER: encode future flags into the int value

        /// <summary>
        /// A value type container.
        /// </summary>
        readonly ValueField _value;

        /// <summary>
        /// A reference type container.
        /// </summary>
        readonly ObjectField _obj;

        #endregion

        #region Properties

        /// <summary>
        /// Gets value indicating whether the value is a <c>NULL</c> or undefined.
        /// </summary>
        public bool IsNull => TypeCode switch
        {
            PhpTypeCode.Null => true,
            PhpTypeCode.Alias => Alias.Value.TypeCode == PhpTypeCode.Null,
            _ => false,
        };

        /// <summary>
        /// Gets value indicating whether the value is considered to be empty.
        /// </summary>
        public bool IsEmpty => ToBoolean() == false;

        /// <summary>
        /// The structure was not initialized.
        /// </summary>
        [Obsolete]
        public bool IsDefault => TypeCode == 0; // NULL

        /// <summary>INTERNAL. Checks if the value has been marked as invalid.</summary>
        internal bool IsInvalid => TypeCode == InvalidTypeCode; // CONSIDER: (TypeCode >= PhpTypeCode.Count)

        /// <summary>INTERNAL. Type code of an invalid value.</summary>
        internal static PhpTypeCode InvalidTypeCode => ~(PhpTypeCode)0;

        /// <summary>
        /// Gets value indicating the value is <c>FALSE</c> or <c>&amp;FALSE</c>.
        /// </summary>
        public bool IsFalse => IsBooleanImpl(out var b) && !b;

        /// <summary>
        /// Gets value indicating whether the value is an alias containing another value.
        /// </summary>
        public bool IsAlias => _type == PhpTypeCode.Alias;

        /// <summary>
        /// Gets value indicating the value represents an object.
        /// </summary>
        public bool IsObject => _type == PhpTypeCode.Object;

        /// <summary>
        /// Gets value indicating the value represents PHP array.
        /// </summary>
        public bool IsArray => _type == PhpTypeCode.PhpArray;

        /// <summary>
        /// Gets value indicating the value represents boolean.
        /// </summary>
        public bool IsBoolean => _type == PhpTypeCode.Boolean;

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
                    case PhpTypeCode.Long:
                    case PhpTypeCode.Double:
                    case PhpTypeCode.String:
                    case PhpTypeCode.MutableString:
                    case PhpTypeCode.Null:
                        return true;

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
        public PhpTypeCode TypeCode => _type;

        /// <summary>
        /// Explicit conversion to <see cref="bool"/>.
        /// </summary>
        public bool ToBoolean() => TypeCode switch
        {
            PhpTypeCode.Null => false,
            PhpTypeCode.Boolean => Boolean,
            PhpTypeCode.Long => Long != 0,
            PhpTypeCode.Double => Double != .0,
            PhpTypeCode.PhpArray => Array.Count != 0,
            PhpTypeCode.String => Convert.ToBoolean(String),
            PhpTypeCode.MutableString => MutableStringBlob.ToBoolean(),
            PhpTypeCode.Object => Convert.ToBoolean(Object),
            PhpTypeCode.Alias => Alias.Value.ToBoolean(),
            _ => false,
        };

        /// <summary>
        /// Explicit conversion to <see cref="long"/>.
        /// </summary>
        public long ToLong() => TypeCode switch
        {
            PhpTypeCode.Null => 0,
            PhpTypeCode.Boolean => Boolean ? 1L : 0L,
            PhpTypeCode.Long => Long,
            PhpTypeCode.Double => (long)Double,
            PhpTypeCode.PhpArray => (long)Array.Count,
            PhpTypeCode.String => Convert.StringToLongInteger(String),
            PhpTypeCode.MutableString => MutableString.ToLong(),
            PhpTypeCode.Object => Convert.ToLong(Object),
            PhpTypeCode.Alias => Alias.Value.ToLong(),
            _ => throw InvalidTypeCodeException(),
        };

        /// <summary>
        /// Explicit conversion to <see cref="double"/>.
        /// </summary>
        public double ToDouble() => TypeCode switch
        {
            PhpTypeCode.Null => 0.0,
            PhpTypeCode.Boolean => Boolean ? 1.0 : 0.0,
            PhpTypeCode.Long => (double)Long,
            PhpTypeCode.Double => Double,
            PhpTypeCode.PhpArray => (double)Array.Count,
            PhpTypeCode.String => Convert.StringToDouble(String),
            PhpTypeCode.MutableString => MutableString.ToDouble(),
            PhpTypeCode.Object => Convert.ToDouble(Object),
            PhpTypeCode.Alias => Alias.Value.ToDouble(),
            _ => throw InvalidTypeCodeException(),
        };

        public decimal ToDecimal() => (decimal)ToDouble();    // TODO: more precision when converting from string 

        public object ToClass()
        {
            switch (TypeCode)
            {
                case PhpTypeCode.Null:
                    return new stdClass();

                case PhpTypeCode.Boolean:
                case PhpTypeCode.Long:
                case PhpTypeCode.Double:
                case PhpTypeCode.String:
                case PhpTypeCode.MutableString:
                    return new stdClass(this);

                case PhpTypeCode.PhpArray:
                    return Array.ToObject();

                case PhpTypeCode.Object:
                    return (Object is IPhpConvertible conv) ? conv.ToClass() : Object;

                case PhpTypeCode.Alias:
                    return Alias.Value.ToClass();

                default:
                    throw InvalidTypeCodeException();
            }
        }

        public Convert.NumberInfo ToNumber(out PhpNumber number)
        {
            switch (TypeCode)
            {
                case PhpTypeCode.Null:
                    number = PhpNumber.Create(0L);
                    return Convert.NumberInfo.LongInteger;

                case PhpTypeCode.Boolean:
                    number = PhpNumber.Create(Boolean ? 1L : 0L);
                    return Convert.NumberInfo.IsNumber | Convert.NumberInfo.LongInteger;

                case PhpTypeCode.Long:
                    number = PhpNumber.Create(Long);
                    return Convert.NumberInfo.IsNumber | Convert.NumberInfo.LongInteger;

                case PhpTypeCode.Double:
                    number = PhpNumber.Create(Double);
                    return Convert.NumberInfo.IsNumber | Convert.NumberInfo.Double;

                case PhpTypeCode.String:
                    return Convert.ToNumber(String, out number);

                case PhpTypeCode.MutableString:
                    return MutableString.ToNumber(out number);

                case PhpTypeCode.PhpArray:
                    return ((IPhpConvertible)Array).ToNumber(out number);

                case PhpTypeCode.Object:
                    return Convert.ToNumber(Object, out number);

                case PhpTypeCode.Alias:
                    return Alias.Value.ToNumber(out number);

                default:
                    throw InvalidTypeCodeException();
            }
        }

        public string ToString(Context ctx) => TypeCode switch
        {
            PhpTypeCode.Null => string.Empty,
            PhpTypeCode.Boolean => Convert.ToString(Boolean),
            PhpTypeCode.Long => Long.ToString(),
            PhpTypeCode.Double => Convert.ToString(Double, ctx),
            PhpTypeCode.PhpArray => (string)Array,
            PhpTypeCode.String => String,
            PhpTypeCode.MutableString => MutableStringBlob.ToString(ctx.StringEncoding),
            PhpTypeCode.Object => Convert.ToString(Object, ctx),
            PhpTypeCode.Alias => Alias.Value.ToString(ctx),
            _ => throw InvalidTypeCodeException(),
        };

        public string ToStringOrThrow(Context ctx) => ToString(ctx);

        internal string ToStringUtf8() => TypeCode switch
        {
            PhpTypeCode.Null => string.Empty,
            PhpTypeCode.Boolean => Convert.ToString(Boolean),
            PhpTypeCode.Long => Long.ToString(),
            PhpTypeCode.Double => Convert.ToString(Double),
            PhpTypeCode.PhpArray => (string)Array,
            PhpTypeCode.String => String,
            PhpTypeCode.MutableString => MutableStringBlob.ToString(Encoding.UTF8),
            PhpTypeCode.Object => Object.ToString(),
            PhpTypeCode.Alias => Alias.Value.ToStringUtf8(),
            _ => throw InvalidTypeCodeException(),
        };

        #endregion

        #region Conversions

        public static implicit operator PhpValue(bool value) => Create(value);
        public static implicit operator PhpValue(int value) => Create(value);
        public static implicit operator PhpValue(uint value) => Create((long)value);
        public static implicit operator PhpValue(long value) => Create(value);
        public static implicit operator PhpValue(ulong value) => Create(value);
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
            if ((value.ToNumber(out var result) & Convert.NumberInfo.Unconvertible) != 0)
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
        public string AsString(Context ctx) => TypeCode == PhpTypeCode.Null ? null : ToString(ctx);

        /// <summary>
        /// Conversion to <see cref="int"/>.
        /// </summary>
        public int ToInt() => checked((int)ToLong());

        /// <summary>
        /// Gets underlaying class instance or <c>null</c>.
        /// </summary>
        public object AsObject() => TypeCode switch
        {
            PhpTypeCode.Object => Object,
            PhpTypeCode.Alias => Alias.Value.AsObject(),
            _ => null,
        };

        /// <summary>
        /// Explicit cast to object.
        /// Non-object values are wrapped to <see cref="stdClass"/>.
        /// </summary>
        /// <remarks>Alias to <see cref="ToClass"/></remarks>
        public object ToObject() => ToClass();

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
        public PhpArray/*!*/ToArray() => TypeCode switch
        {
            PhpTypeCode.Null => PhpArray.NewEmpty(),
            PhpTypeCode.Boolean => PhpArray.New(this),
            PhpTypeCode.Long => PhpArray.New(this),
            PhpTypeCode.Double => PhpArray.New(this),
            PhpTypeCode.PhpArray => Array.AsWritable(),
            PhpTypeCode.String => PhpArray.New(this),
            PhpTypeCode.MutableString => MutableString.ToArray(),
            PhpTypeCode.Object => Convert.ClassToArray(Object),
            PhpTypeCode.Alias => Alias.Value.ToArray(),
            _ => throw InvalidTypeCodeException(),
        };

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

        public static PhpValue operator ~(PhpValue x) => Operators.BitNot(in x);

        public static PhpValue operator &(PhpValue left, PhpValue right) => Operators.BitAnd(in left, in right);

        public static PhpValue operator |(PhpValue left, PhpValue right) => Operators.BitOr(in left, in right);

        public static PhpValue operator ^(PhpValue left, PhpValue right) => Operators.BitXor(in left, in right);

        /// <summary>
        /// Division of <paramref name="left"/> and <paramref name="right"/> according to PHP semantics.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>Quotient of <paramref name="left"/> and <paramref name="right"/>.</returns>
        public static PhpNumber operator /(PhpValue left, PhpValue right) => Operators.Div(in left, in right);

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

        public bool TryToIntStringKey(out IntStringKey key)
        {
            switch (TypeCode)
            {
                case PhpTypeCode.Null:
                    key = IntStringKey.EmptyStringKey;
                    return true;

                case PhpTypeCode.Boolean:
                    key = new IntStringKey(Boolean ? 1 : 0);
                    return true;

                case PhpTypeCode.Long:
                    key = new IntStringKey(Long);
                    return true;

                case PhpTypeCode.Double:
                    key = new IntStringKey((long)Double);
                    return true;

                case PhpTypeCode.String:
                    key = Convert.StringToArrayKey(String);
                    return true;

                case PhpTypeCode.MutableString:
                    key = Convert.StringToArrayKey(MutableStringBlob.ToString());
                    return true;

                case PhpTypeCode.PhpArray:
                case PhpTypeCode.Object:
                    key = default;
                    return false;

                case PhpTypeCode.Alias:
                    return Alias.Value.TryToIntStringKey(out key);

                default:
                    throw InvalidTypeCodeException();
            }
        }

        public IntStringKey ToIntStringKey()
        {
            if (TryToIntStringKey(out var iskey))
            {
                return iskey;
            }

            PhpException.IllegalOffsetType();
            return IntStringKey.EmptyStringKey;
        }

        /// <summary>
        /// Gets enumerator object used within foreach statement.
        /// </summary>
        public IPhpEnumerator GetForeachEnumerator(bool aliasedValues, RuntimeTypeHandle caller) => TypeCode switch
        {
            PhpTypeCode.PhpArray => Array.GetForeachEnumerator(aliasedValues),
            PhpTypeCode.Object => Operators.GetForeachEnumerator(Object, aliasedValues, caller),
            PhpTypeCode.Alias => Alias.Value.GetForeachEnumerator(aliasedValues, caller),
            _ => Operators.GetEmptyForeachEnumerator(),
        };

        /// <summary>
        /// Compares two value operands.
        /// </summary>
        /// <param name="right">The right operand.</param>
        /// <returns>Comparison result.
        /// Zero for equality,
        /// negative value for <c>this</c> &lt; <paramref name="right"/>,
        /// position value for <c>this</c> &gt; <paramref name="right"/>.</returns>
        public int Compare(PhpValue right) => TypeCode switch
        {
            PhpTypeCode.Null => Comparison.CompareNull(right),
            PhpTypeCode.Boolean => Comparison.Compare(Boolean, right),
            PhpTypeCode.Long => Comparison.Compare(Long, right),
            PhpTypeCode.Double => Comparison.Compare(Double, right),
            PhpTypeCode.PhpArray => Array.Compare(right),
            PhpTypeCode.String => Comparison.Compare(String, right),
            PhpTypeCode.MutableString => Comparison.Compare(MutableStringBlob.ToString(), right),
            PhpTypeCode.Object => Comparison.Compare(Object, right),
            PhpTypeCode.Alias => Alias.Value.Compare(right),
            _ => throw InvalidTypeCodeException(),
        };

        /// <summary>
        /// Performs strict comparison.
        /// </summary>
        /// <param name="right">The right operand.</param>
        /// <returns>The value determining operands are strictly equal.</returns>
        public bool StrictEquals(PhpValue right) => TypeCode switch
        {
            // TODO: compare TypeCode, then dereference eventually and then switch

            PhpTypeCode.Null => right.IsNull,
            PhpTypeCode.Boolean => right.IsBoolean(out var b) && b == Boolean,
            PhpTypeCode.Long => right.IsLong(out var l) && l == Long,
            PhpTypeCode.Double => right.IsDouble(out var d) && d == Double,
            PhpTypeCode.PhpArray => Array.StrictCompareEq(right.AsArray()),
            PhpTypeCode.String => right.IsString(out var s) && s == String,
            PhpTypeCode.MutableString => right.IsString(out var s) && s.Length == MutableStringBlob.Length && s == MutableStringBlob.ToString(),
            PhpTypeCode.Object => right.AsObject() == Object,
            PhpTypeCode.Alias => Alias.Value.StrictEquals(right),
            _ => throw InvalidTypeCodeException(),
        };

        /// <summary>
        /// Gets callable wrapper for the object dynamic invocation.
        /// </summary>
        public IPhpCallable AsCallable(RuntimeTypeHandle callerCtx = default, object callerObj = null) => TypeCode switch
        {
            PhpTypeCode.PhpArray => Convert.AsCallable(Array, callerCtx, callerObj),
            PhpTypeCode.String => PhpCallback.Create(String, callerCtx, callerObj),
            PhpTypeCode.MutableString => PhpCallback.Create(MutableStringBlob.ToString(), callerCtx, callerObj),
            PhpTypeCode.Object => Convert.ClassAsCallable(Object, callerCtx, callerObj),
            PhpTypeCode.Alias => Alias.Value.AsCallable(callerCtx, callerObj),
            _ => PhpCallback.CreateInvalid(),
        };

        /// <summary>
        /// Ensures the value is a class object.
        /// In case it isn't, creates stdClass according to PHP semantics.
        /// In case current value is empty, replaces current value with newly created stdClass.
        /// </summary>
        /// <returns>Non-null object.</returns>
        public static object EnsureObject(ref PhpValue value)
        {
            object result;

            switch (value.TypeCode)
            {
                case PhpTypeCode.Null:
                    // TODO: Err: Warning: Creating default object from empty value
                    value = FromClass(result = new stdClass());
                    break;

                case PhpTypeCode.Boolean:
                    result = new stdClass();   // empty class

                    // me is changed if me.Boolean == FALSE
                    if (value.Boolean == false) value = FromClass(result);
                    break;

                case PhpTypeCode.Long:
                case PhpTypeCode.Double:
                    // this is not changed
                    result = new stdClass(value);
                    break;

                case PhpTypeCode.PhpArray:
                    // me is not modified
                    result = value.Array.ToObject();
                    break;

                case PhpTypeCode.String:
                    result = new stdClass(value);

                    // me is changed if value is empty
                    if (value.String.Length == 0) value = FromClass(result);
                    break;

                case PhpTypeCode.MutableString:
                    result = new stdClass(value);

                    // me is changed if value is empty
                    if (value.MutableStringBlob.IsEmpty) value = FromClass(result);
                    break;

                case PhpTypeCode.Object:
                    result = value.Object;
                    break;

                case PhpTypeCode.Alias:
                    result = EnsureObject(ref value.Alias.Value);
                    break;

                default:
                    throw InvalidTypeCodeException();
            }

            //
            return result;
        }

        /// <summary>
        /// Ensures the value is a PHP array.
        /// In case it isn't, creates PhpArray according to PHP semantics.
        /// In case current value is empty, replaces current value with newly created array.
        /// </summary>
        /// <returns>PHP array instance. Canot be <c>null</c>.</returns>
        /// <remarks>Used for L-Values accessed as arrays (<code>$lvalue[] = rvalue</code>).</remarks>
        public static IPhpArray EnsureArray(ref PhpValue value)
        {
            PhpArray tmp;

            switch (value.TypeCode)
            {
                case PhpTypeCode.Null:
                    // TODO: Err: Warning: Creating default object from empty value
                    value = Create(tmp = new PhpArray());
                    return tmp;

                case PhpTypeCode.Boolean:
                    tmp = new PhpArray();   // empty class

                    // me is changed if me.Boolean == FALSE
                    if (value.Boolean == false) value = Create(tmp);
                    return tmp;

                case PhpTypeCode.Long:
                case PhpTypeCode.Double:
                    // this is not changed
                    return new PhpArray();

                case PhpTypeCode.PhpArray:
                    return value.Array;

                case PhpTypeCode.String:
                    // upgrade to mutable string
                    var str = new PhpString(value.String);
                    // ensure its internal blob
                    var arr = str.EnsureWritable();

                    // copy back new value
                    value = Create(str);
                    return arr;

                case PhpTypeCode.MutableString:
                    // ensure blob is lazily copied
                    if (value.MutableStringBlob.IsShared)
                    {
                        value = new PhpValue(value.MutableStringBlob.ReleaseOne());
                    }

                    //
                    return value.MutableStringBlob;

                case PhpTypeCode.Object:
                    return Operators.EnsureArray(value.Object);

                case PhpTypeCode.Alias:
                    return EnsureArray(ref value.Alias.Value);

                default:
                    throw InvalidTypeCodeException();
            }
        }

        /// <summary>
        /// Ensures the value as an alias.
        /// In case it isn't, the value is aliased.
        /// </summary>
        /// <returns>Non-null alias of the value.</returns>
        public static PhpAlias EnsureAlias(ref PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.PhpArray:
                    // ensure array is lazily copied
                    value.Array.EnsureWritable();
                    break;

                case PhpTypeCode.MutableString:
                    // ensure blob is lazily copied
                    if (value.MutableStringBlob.IsShared)
                    {
                        value = new PhpValue(value.MutableStringBlob.ReleaseOne());
                    }
                    break;

                case PhpTypeCode.Alias:
                    return value.Alias.AddRef();
            }

            // create alias to the value:
            var alias = new PhpAlias(value, 1);
            value = Create(alias);
            return alias;
        }

        /// <summary>
        /// Dereferences in case of an alias.
        /// </summary>
        /// <returns>Not aliased value.</returns>
        public PhpValue GetValue() => Object is PhpAlias alias ? alias.Value : this;

        /// <summary>
        /// Accesses the value as an array and gets item at given index.
        /// Gets <c>NULL</c> value in case the key is not found.
        /// </summary>
        public PhpValue GetArrayItem(PhpValue index, bool quiet = false) => Operators.GetItemValue(this, index, quiet);

        /// <summary>
        /// Creates a deep copy of PHP value.
        /// In case of scalars, the shallow copy is returned.
        /// In case of classes, the same reference is returned.
        /// In case of array or string, its copy is returned.
        /// In case of aliased value, the same alias is returned.
        /// </summary>
        public PhpValue DeepCopy()
        {
            switch (TypeCode)
            {
                case PhpTypeCode.Null:
                case PhpTypeCode.Boolean:
                case PhpTypeCode.Long:
                case PhpTypeCode.Double:
                    return this;

                case PhpTypeCode.PhpArray:
                    return Array.DeepCopy();

                case PhpTypeCode.String:
                    return this;

                case PhpTypeCode.MutableString:
                    return new PhpValue(MutableStringBlob.AddRef());

                case PhpTypeCode.Object:
                    return this;

                case PhpTypeCode.Alias:
                    return Alias.DeepCopy();

                default:
                    throw InvalidTypeCodeException();
            }
        }

        /// <summary>
        /// Outputs current value to <see cref="Context"/>.
        /// Handles byte (8bit) strings and allows for chunked text to be streamed without costly concatenation.
        /// </summary>
        public void Output(Context ctx)
        {
            switch (TypeCode)
            {
                case PhpTypeCode.Boolean: ctx.Echo(Boolean); break;
                case PhpTypeCode.Long: ctx.Echo(Long); break;
                case PhpTypeCode.Double: ctx.Echo(Double); break;
                case PhpTypeCode.PhpArray: ctx.Echo((string)Array); break;
                case PhpTypeCode.String: ctx.Echo(String); break;
                case PhpTypeCode.MutableString: MutableStringBlob.Output(ctx); break;
                case PhpTypeCode.Object: ctx.Echo(Convert.ToString(Object, ctx)); break;
                case PhpTypeCode.Alias: Alias.Value.Output(ctx); break;
            }
        }

        /// <summary>
        /// Gets underlaying value or object as <see cref="System.Object"/>.
        /// </summary>
        public object ToClr()
        {
            switch (TypeCode)
            {
                case PhpTypeCode.Null: return null;
                case PhpTypeCode.Boolean: return Boolean;
                case PhpTypeCode.Long: return Long;
                case PhpTypeCode.Double: return Double;
                case PhpTypeCode.PhpArray:
                case PhpTypeCode.String:
                case PhpTypeCode.Object: return Object;
                case PhpTypeCode.MutableString: return MutableStringBlob.ToString();
                case PhpTypeCode.Alias: return Alias.Value.ToClr();
                default:
                    throw InvalidTypeCodeException();
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

            if (type == typeof(PhpAlias) && IsAlias)
            {
                return Alias;
            }

            //if (type.IsNullable_T(out var nullable_t))
            //{
            //    throw new NotImplementedException();
            //}

            //
            throw new InvalidCastException($"{TypeCode} -> {type.FullName}");
        }

        /// <summary>
        /// Calls corresponding <c>Accept</c> method on visitor.
        /// </summary>
        public void Accept(PhpVariableVisitor visitor)
        {
            switch (TypeCode)
            {
                case PhpTypeCode.Null: visitor.AcceptNull(); break;
                case PhpTypeCode.Boolean: visitor.Accept(Boolean); break;
                case PhpTypeCode.Long: visitor.Accept(Long); break;
                case PhpTypeCode.Double: visitor.Accept(Double); break;
                case PhpTypeCode.PhpArray: visitor.Accept(Array); break;
                case PhpTypeCode.String: visitor.Accept(String); break;
                case PhpTypeCode.MutableString: visitor.Accept(MutableString); break;
                case PhpTypeCode.Object: visitor.AcceptObject(Object); break;
                case PhpTypeCode.Alias: visitor.Accept(Alias); break;
                default: throw InvalidTypeCodeException();
            }
        }

        /// <summary>
        /// Gets value converted to string using default configuration options.
        /// </summary>
        public override string ToString()
        {
            Debug.WriteLine("Use ToString(Context) instead!");
            return ToStringUtf8();
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
            switch (TypeCode)
            {
                case PhpTypeCode.String:
                    str = _obj.@string;
                    return true;

                case PhpTypeCode.MutableString:
                    str = _obj.blob.ToString();
                    return true;

                case PhpTypeCode.Alias:
                    return _obj.alias.Value.IsStringImpl(out str);

                default:
                    str = default;
                    return false;
            }
        }

        /// <summary>
        /// Checks the value is <c>string</c> or <c>&amp;string</c> constructed as <see cref="PhpString"/> (a mutable string).
        /// </summary>
        internal bool IsMutableStringImpl(out PhpString str)
        {
            switch (TypeCode)
            {
                case PhpTypeCode.MutableString:
                    str = MutableString;
                    return true;

                case PhpTypeCode.Alias:
                    return Alias.Value.IsMutableStringImpl(out str);

                default:
                    str = default;
                    return false;
            }
        }

        /// <summary>
        /// Checks the value is of type <c>string</c> (both unicode and single-byte) or an alias to a string.
        /// </summary>
        internal bool IsStringImpl()
        {
            return TypeCode switch
            {
                PhpTypeCode.String => true,
                PhpTypeCode.MutableString => true,
                PhpTypeCode.Alias => Alias.Value.IsStringImpl(),
                _ => false,
            };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Exception InvalidTypeCodeException() => new InvalidOperationException();

        #endregion

        #region IEquatable<PhpValue>

        public bool Equals(PhpValue other) => this.Compare(other) == 0;

        #endregion

        #region Construction

        /// <summary>
        /// Singleton of PhpValue representing <c>void</c>.
        /// </summary>
        [Obsolete("Use PhpValue.Null instead.")]
        public static readonly PhpValue Void = default;

        /// <summary>
        /// Singleton of PhpValue representing <c>null</c>.
        /// </summary>
        public static readonly PhpValue Null = default;

        /// <summary>
        /// PhpValue representing <c>false</c>.
        /// </summary>
        public static readonly PhpValue False = new PhpValue(false);

        /// <summary>
        /// PhpValue representing <c>true</c>.
        /// </summary>
        public static readonly PhpValue True = new PhpValue(true);

        private PhpValue(long value) : this(PhpTypeCode.Long)
        {
            _value.@long = value;
        }

        private PhpValue(double value) : this(PhpTypeCode.Double)
        {
            _value.@double = value;
        }

        private PhpValue(bool value) : this(PhpTypeCode.Boolean)
        {
            _value.@bool = value;
        }

        private PhpValue(PhpTypeCode type, object obj) : this(ReferenceEquals(obj, null) ? PhpTypeCode.Null : type)
        {
            _obj.@object = obj;
        }

        internal PhpValue(PhpString.Blob blob) : this(PhpTypeCode.MutableString)
        {
            Debug.Assert(blob != null);
            _obj.blob = blob;
        }

        internal PhpValue(string value) : this(PhpTypeCode.String)
        {
            Debug.Assert(value != null);
            _obj.@string = value;
        }

        internal PhpValue(PhpArray array) : this(PhpTypeCode.PhpArray)
        {
            Debug.Assert(array != null);
            _obj.array = array;
        }

        private PhpValue(PhpTypeCode type)
        {
            _type = type;
            _value = default;
            _obj = default;
        }

        /// <summary>
        /// INTERNAL.
        /// Creates an invalid value with the type code of <c>-1</c>.
        /// </summary>
        internal static PhpValue CreateInvalid() => new PhpValue(InvalidTypeCode);

        public static PhpValue Create(PhpNumber number) => number.ToPhpValue();

        public static PhpValue Create(long value) => new PhpValue(value);

        public static PhpValue Create(double value) => new PhpValue(value);

        public static PhpValue Create(int value) => new PhpValue(value);

        public static PhpValue Create(bool value) => value ? True : False;

        public static PhpValue Create(string value) => new PhpValue(PhpTypeCode.String, value);

        public static PhpValue Create(PhpString value) => PhpString.AsPhpValue(value);

        internal static PhpValue Create(PhpString.Blob blob) => new PhpValue(blob);

        internal static PhpValue Create(byte[] bytes) => new PhpValue(new PhpString.Blob(bytes));

        public static PhpValue Create(PhpArray value) => new PhpValue(PhpTypeCode.PhpArray, value);

        public static PhpValue Create(IPhpArray value) => value is PhpArray arr ? Create(arr) : FromClass(value);

        public static PhpValue Create(PhpAlias value) => new PhpValue(PhpTypeCode.Alias, value);

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

        /// <summary>
        /// Create <see cref="PhpValue"/> representation of <see cref="ulong"/>.
        /// The value will be converted either to <see cref="long"/> or juggles to <see cref="double"/> if it's larger than long.
        /// </summary>
        public static PhpValue Create(ulong value)
        {
            if (value <= long.MaxValue)
            {
                return Create((long)value);
            }
            else
            {
                return Create((double)value);
            }
        }

        public static PhpValue FromClass(object value)
        {
            Debug.Assert(!(value is int || value is long || value is bool || value is string || value is double || value is PhpAlias || value is PhpString || value is PhpArray));
            return new PhpValue(PhpTypeCode.Object, value);
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
                if (value.GetType() == typeof(IntStringKey)) return Create((IntStringKey)value);

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
