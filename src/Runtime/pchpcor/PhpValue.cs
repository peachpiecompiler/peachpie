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
        /// Value type.
        /// </summary>
        [FieldOffset(0)]
        PhpTypeCode _type;

        [FieldOffset(4)]
        object _obj;

        [FieldOffset(8)]
        long _long;

        [FieldOffset(8)]
        double _double;

        [FieldOffset(8)]
        bool _bool;

        #endregion

        #region Properties

        public PhpTypeCode TypeCode => _type;

        /// <summary>
        /// Gets value indicating whether the value is a <c>NULL</c>.
        /// </summary>
        public bool IsNull => object.ReferenceEquals(_obj, null) && TypeCode == PhpTypeCode.Object;

        #endregion

        #region Operators

        public object ToObject()
        {
            throw new NotImplementedException();
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
            }

            throw new ArgumentException();
        }

        public string ToStringOrThrow(Context ctx)
        {
            return ToString(ctx);   // TODO: or throw
        }

        #endregion

        #region Construction

        public static PhpValue Create(PhpNumber number)
        {
            return (number.IsLong)
                 ? Create(number.Long)
                 : Create(number.Double);

            // TODO: does following work on all platforms?
            //return new PhpValue()
            //{
            //    _type = number.TypeCode,    // Long || Double
            //    _long = number._long // _long = Long <=> _double = Double
            //};
        }

        public static PhpValue Create(long value)
        {
            return new PhpValue() { _type = PhpTypeCode.Long, _long = value };
        }

        public static PhpValue Create(double value)
        {
            return new PhpValue() { _type = PhpTypeCode.Double, _double = value };
        }

        public static PhpValue Create(int value) => Create((long)value);

        public static PhpValue Create(bool value) => new PhpValue() { _type = PhpTypeCode.Boolean, _bool = value };

        public static PhpValue CreateNull() => new PhpValue() { _type = PhpTypeCode.Object };

        public static PhpValue Create(string value) => new PhpValue() { _type = (value != null) ? PhpTypeCode.String : PhpTypeCode.Object, _obj = value };

        public static PhpValue Create(PhpString value) => new PhpValue() { _type = (value != null) ? PhpTypeCode.WritableString : PhpTypeCode.Object, _obj = value };

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
