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

                if (_type == PhpTypeCode.String || _type == PhpTypeCode.BinaryString || _type == PhpTypeCode.PhpStringBuilder)
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
            throw new NotImplementedException();
        }

        public double ToDouble()
        {
            throw new NotImplementedException();
        }

        public bool ToBoolean()
        {
            throw new NotImplementedException();
        }

        public Convert.NumberInfo ToNumber(out PhpNumber number)
        {
            throw new NotImplementedException();
        }

        public string ToString(Context ctx)
        {
            throw new NotImplementedException();
        }

        public string ToStringOrThrow(Context ctx)
        {
            throw new NotImplementedException();
        }
        
        #endregion

        #region Construction

        public static PhpValue Create(PhpNumber number)
        {
            //return (number.IsLong)
            //     ? Create(number.Long)
            //     : Create(number.Double);

            return new PhpValue()
            {
                _type = number.TypeCode,    // Long || Double
                _long = number._long // _long = Long <=> _double = Double
            };
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

        public static PhpValue Create(bool value)
        {
            return new PhpValue() { _type = PhpTypeCode.Boolean, _bool = value };
        }

        public static PhpValue CreateNull()
        {
            return new PhpValue() { _type = PhpTypeCode.Object };
        }

        #endregion
    }
}
