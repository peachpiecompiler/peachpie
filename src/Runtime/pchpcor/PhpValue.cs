using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Represents a non-aliased PHP value.
    /// </summary>
    [DebuggerDisplay("{_type} ({GetDebuggerValue,nq})")]
    [StructLayout(LayoutKind.Explicit)]
    public struct PhpValue : IPhpConvertible // <T>
    {
        #region GetDebuggerValue

        string GetDebuggerValue
        {
            get
            {
                var str = ToString();

                if (_type == PhpTypeCode.String || _type == PhpTypeCode.WritableString)
                    str = $"'{str}'";

                return str;
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The value type.
        /// </summary>
        [FieldOffset(0)]
        PhpTypeCode _type;
        [FieldOffset(4)]
        long _long;
        [FieldOffset(4)]
        double _double;
        [FieldOffset(4)]
        bool _bool;
        [FieldOffset(16)]
        object _obj;

        #endregion

        #region Properties

        public PhpTypeCode TypeCode => _type;

        /// <summary>
        /// Gets value indicating whether the value is a <c>NULL</c>.
        /// </summary>
        public bool IsNull => object.ReferenceEquals(_obj, null) && TypeCode == PhpTypeCode.Object;

        /// <summary>
        /// Gets the long field of the value.
        /// Does not perform a conversion, expects the value is of type long.
        /// </summary>
        public long Long { get { Debug.Assert(_type == PhpTypeCode.Long); return _long; } }

        /// <summary>
        /// Gets the double field of the value.
        /// Does not perform a conversion, expects the value is of type double.
        /// </summary>
        public double Double { get { Debug.Assert(_type == PhpTypeCode.Double); return _double; } }

        /// <summary>
        /// Gets the boolean field of the value.
        /// Does not perform a conversion, expects the value is of type boolean.
        /// </summary>
        public bool Boolean { get { Debug.Assert(_type == PhpTypeCode.Boolean); return _bool; } }

        /// <summary>
        /// Gets the object field of the value as string.
        /// Does not perform a conversion, expects the value is of type (readonly UTF16) string.
        /// </summary>
        public string String { get { Debug.Assert(_type == PhpTypeCode.String && _obj != null); return (string)_obj; } }

        /// <summary>
        /// Gets underlaying reference object.
        /// </summary>
        public object Object { get { Debug.Assert(_type == PhpTypeCode.Object); return _obj; } }

        /// <summary>
        /// Gets underaying alias object.
        /// </summary>
        public PhpAlias Alias { get { Debug.Assert(_type == PhpTypeCode.Alias); return (PhpAlias)_obj; } }

        #endregion

        #region Operators

        public object ToClass(Context ctx)
        {
            switch (_type)
            {
                case PhpTypeCode.Object:
                    if (_obj == null) // new stdClass()
                        throw new NotImplementedException(); // // new stdClass(){ $scalar = VALUE }
                    //if (_obj is IPhpConvertible)
                    //    return ((IPhpConvertible)_obj).ToClass(ctx);
                    return _obj;
                case PhpTypeCode.Alias:
                    return this.Alias.ToClass(ctx);
                case PhpTypeCode.Long:
                    throw new NotImplementedException(); // // new stdClass(){ $scalar = VALUE }
                case PhpTypeCode.Boolean:
                    throw new NotImplementedException(); // // new stdClass(){ $scalar = VALUE }
                case PhpTypeCode.Double:
                    throw new NotImplementedException(); // // new stdClass(){ $scalar = VALUE }
                case PhpTypeCode.String:
                    throw new NotImplementedException(); // // new stdClass(){ $scalar = VALUE }
            }

            throw new ArgumentException();
        }

        public long ToLong()
        {
            switch (_type)
            {
                case PhpTypeCode.Long:
                    return _long;
                case PhpTypeCode.Boolean:
                    return _bool ? 1 : 0;
                case PhpTypeCode.Double:
                    return (long)_double;
                case PhpTypeCode.String:
                    return Convert.StringToLongInteger((string)_obj);
                case PhpTypeCode.Object:
                    if (_obj == null) return 0;
                    if (_obj is IPhpConvertible) return ((IPhpConvertible)_obj).ToLong();
                    throw new NotImplementedException();
                case PhpTypeCode.Alias:
                    return this.Alias.ToLong();
            }

            throw new ArgumentException();
        }

        public double ToDouble()
        {
            switch (_type)
            {
                case PhpTypeCode.Double:
                    return _double;
                case PhpTypeCode.Long:
                    return (double)_long;
                case PhpTypeCode.Boolean:
                    return _bool ? 1.0 : 0.0;
                case PhpTypeCode.String:
                    return Convert.StringToDouble((string)_obj);
                case PhpTypeCode.Object:
                    if (_obj == null) return 0.0;
                    if (_obj is IPhpConvertible) return ((IPhpConvertible)_obj).ToDouble();
                    throw new NotImplementedException();
                case PhpTypeCode.Alias:
                    return this.Alias.ToDouble();
            }

            throw new ArgumentException();
        }

        public bool ToBoolean()
        {
            switch (_type)
            {
                case PhpTypeCode.Boolean:
                    return _bool;
                case PhpTypeCode.Long:
                    return _long != 0;
                case PhpTypeCode.Double:
                    return _double != 0.0;
                case PhpTypeCode.String:
                    return Convert.ToBoolean((string)_obj);
                case PhpTypeCode.Object:
                    if (_obj == null) return false;
                    if (_obj is IPhpConvertible) return ((IPhpConvertible)_obj).ToBoolean();
                    throw new NotImplementedException();
                case PhpTypeCode.Alias:
                    return this.Alias.ToBoolean();
            }

            throw new ArgumentException();
        }

        public Convert.NumberInfo ToNumber(out PhpNumber number)
        {
            switch (_type)
            {
                case PhpTypeCode.Long:
                    number = PhpNumber.Create(_long);
                    return Convert.NumberInfo.IsNumber | Convert.NumberInfo.LongInteger;
                case PhpTypeCode.Double:
                    number = PhpNumber.Create(_double);
                    return Convert.NumberInfo.IsNumber | Convert.NumberInfo.Double;
                case PhpTypeCode.Boolean:
                    number = PhpNumber.Create(_bool ? 1L : 0L);
                    return Convert.NumberInfo.IsNumber | Convert.NumberInfo.LongInteger;
                case PhpTypeCode.String:
                    return Convert.ToNumber((string)_obj, out number);
                case PhpTypeCode.Object:
                    if (_obj == null)
                    {
                        number = PhpNumber.Create(0L);
                        return Convert.NumberInfo.Unconvertible;
                    }
                    if (_obj is IPhpConvertible)
                    {
                        return ((IPhpConvertible)_obj).ToNumber(out number);
                    }
                    throw new NotImplementedException();
                case PhpTypeCode.Alias:
                    return this.Alias.ToNumber(out number);
            }

            throw new ArgumentException();
        }

        public string ToString(Context ctx)
        {
            switch (_type)
            {
                case PhpTypeCode.String:
                    return (string)_obj;
                case PhpTypeCode.Double:
                    return Convert.ToString(_double, ctx);
                case PhpTypeCode.Long:
                    return _long.ToString();
                case PhpTypeCode.Boolean:
                    return Convert.ToString(_bool);
                case PhpTypeCode.Object:
                    if (_obj == null) return string.Empty;
                    if (_obj is IPhpConvertible) return ((IPhpConvertible)_obj).ToString(ctx);
                    throw new NotImplementedException();
                case PhpTypeCode.Alias:
                    return this.Alias.ToString(ctx);
            }

            throw new ArgumentException();
        }

        public string ToStringOrThrow(Context ctx)
        {
            return ToString(ctx);   // TODO: or throw
        }

        #endregion

        #region Construction

        private PhpValue(long value) : this()
        {
            _type = PhpTypeCode.Long;
            _long = value;
        }

        private PhpValue(double value) : this()
        {
            _type = PhpTypeCode.Double;
            _double = value;
        }

        private PhpValue(bool value) : this()
        {
            _type = PhpTypeCode.Boolean;
            _bool = value;
        }

        private PhpValue(PhpTypeCode code, object obj) : this()
        {
            _type = code;
            _obj = obj;
        }

        public static PhpValue Create(PhpNumber number)
            => (number.IsLong)
                 ? Create(number.Long)
                 : Create(number.Double);

        public static PhpValue Create(long value) => new PhpValue(value);

        public static PhpValue Create(double value) => new PhpValue(value);

        public static PhpValue Create(int value) => new PhpValue((long)value);

        public static PhpValue Create(bool value) => new PhpValue(value);

        public static PhpValue CreateNull() => new PhpValue(PhpTypeCode.Object, null);

        public static PhpValue CreateVoid() => new PhpValue();

        public static PhpValue Create(string value) => new PhpValue((value != null) ? PhpTypeCode.String : PhpTypeCode.Object, value);

        public static PhpValue Create(PhpString value) => new PhpValue((value != null) ? PhpTypeCode.WritableString : PhpTypeCode.Object, value);

        public static PhpValue Create(PhpAlias value) => new PhpValue((value != null) ? PhpTypeCode.Alias : PhpTypeCode.Object, value);

        public static PhpValue FromClass(object value)
        {
            Debug.Assert(!(value is int || value is long || value is bool || value is string || value is double || value is PhpAlias));
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
                if (value.GetType() == typeof(bool)) return Create((bool)value);
                if (value.GetType() == typeof(string)) return Create((string)value);
                if (value.GetType() == typeof(PhpString)) return Create((PhpString)value);

                //
                throw new ArgumentException();
            }
            else
            {
                return CreateNull();
            }
        }

        #endregion
    }
}
