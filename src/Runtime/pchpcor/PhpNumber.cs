using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    [DebuggerDisplay("{TypeCode} ({GetDebuggerValue,nq})")]
    [StructLayout(LayoutKind.Explicit)]
    public struct PhpNumber : IComparable<PhpNumber>, IPhpConvertible
    {
        #region Fields

        /// <summary>
        /// Number type.
        /// Valid values are <see cref="PhpTypeCode.Long"/> and <see cref="PhpTypeCode.Double"/>.
        /// </summary>
        [FieldOffset(0)]
        PhpTypeCode _typeCode;

        [FieldOffset(4)]
        public long Long;

        [FieldOffset(4)]
        public double Double;

        #endregion

        #region Properties

        /// <summary>
        /// Gets value indicating the number is a floating point number.
        /// </summary>
        public bool IsDouble => _typeCode == PhpTypeCode.Double;

        /// <summary>
        /// Gets value indicating the number is an integer.
        /// </summary>
        public bool IsLong => _typeCode == PhpTypeCode.Long;

        #endregion

        string GetDebuggerValue
        {
            get { return IsLong ? Long.ToString() : Double.ToString(); }
        }

        void AssertTypeCode()
        {
            Debug.Assert(_typeCode == PhpTypeCode.Long || _typeCode == PhpTypeCode.Double);
        }

        #region Operators

        /// <summary>
        /// Gets non strict comparison with another number.
        /// </summary>
        public int CompareTo(PhpNumber other)
        {
            this.AssertTypeCode();
            other.AssertTypeCode();

            //
            if (_typeCode == other._typeCode)
            {
                return (_typeCode == PhpTypeCode.Long)
                    ? Long.CompareTo(other.Long)
                    : Double.CompareTo(other.Double);
            }
            else
            {
                return (_typeCode == PhpTypeCode.Long)
                    ? ((double)Long).CompareTo(other.Double)
                    : Double.CompareTo((double)other.Long);
            }
        }

        /// <summary>
        /// Non strict equality operator.
        /// </summary>
        public static bool operator ==(PhpNumber a, PhpNumber b)
        {
            a.AssertTypeCode();
            b.AssertTypeCode();

            //
            if (a._typeCode == b._typeCode)
            {
                return (a._typeCode == PhpTypeCode.Long)
                    ? a.Long == b.Long
                    : a.Double == b.Double;
            }
            else
            {
                return (a._typeCode == PhpTypeCode.Long)
                    ? (double)a.Long == b.Double
                    : a.Double == (double)b.Long;
            }
        }

        /// <summary>
        /// Non strict inequality operator.
        /// </summary>
        public static bool operator !=(PhpNumber a, PhpNumber b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Implements <c>+</c> operator on numbers.
        /// </summary>
        /// <param name="x">First operand.</param>
        /// <param name="y">Second operand.</param>
        /// <returns></returns>
        public static PhpNumber operator +(PhpNumber x, PhpNumber y)
        {
            // at least one operand is a double:
            if (x.IsDouble)
            {
                // DOUBLE + DOUBLE|LONG
                return Create(x.Double + y.ToDouble());
            }

            if (y.IsDouble)
            {
                // LONG + DOUBLE
                Debug.Assert(x.IsLong);
                return Create((double)x.Long + y.Double);
            }

            // LONG + LONG
            Debug.Assert(x.IsLong && y.IsLong);
            long rl = unchecked(x.Long + y.Long);

            if ((x.Long & Operators.LONG_SIGN_MASK) != (rl & Operators.LONG_SIGN_MASK) &&       // result has different sign than x
                (x.Long & Operators.LONG_SIGN_MASK) == (y.Long & Operators.LONG_SIGN_MASK)      // x and y have the same sign                
                )
            {
                // overflow to double:
                return Create((double)x.Long + (double)y.Long);
            }
            else
            {
                //// int to long overflow check
                //int il = unchecked((int)rl);
                //if (il == rl)
                //    return il;

                // long:
                return Create(rl);  // note: most common case
            }
        }

        /// <summary>
        /// Implements <c>-</c> operator on numbers.
        /// </summary>
        /// <param name="x">First operand.</param>
        /// <param name="y">Second operand.</param>
        /// <returns></returns>
        public static PhpNumber operator -(PhpNumber x, PhpNumber y)
        {
            // at least one operand is a double:
            if (x.IsDouble)
            {
                // DOUBLE - DOUBLE
                return Create(x.Double - y.ToDouble());
            }

            if (y.IsDouble)
            {
                // LONG - DOUBLE
                Debug.Assert(x.IsLong);
                return Create((double)x.Long - y.Double);
            }

            // LONG - LONG
            Debug.Assert(x.IsLong && y.IsLong);
            long rl = unchecked(x.Long - y.Long);

            if ((x.Long & Operators.LONG_SIGN_MASK) != (rl & Operators.LONG_SIGN_MASK) &&   // result has different sign than x
                (x.Long & Operators.LONG_SIGN_MASK) != (y.Long & Operators.LONG_SIGN_MASK)      // x and y have the same sign                
                )
            {
                // overflow:
                return Create((double)x.Long - (double)y.Long);
            }
            else
            {
                //// int to long overflow check
                //int il = unchecked((int)rl);
                //if (il == rl)
                //    return il;

                // long:
                return Create(rl);
            }
        }

        #endregion

        #region Construction

        public static PhpNumber Create(long value)
        {
            return new PhpNumber() { _typeCode = PhpTypeCode.Long, Long = value };
        }

        public static PhpNumber Create(double value)
        {
            return new PhpNumber() { _typeCode = PhpTypeCode.Double, Double = value };
        }

        #endregion

        #region Object

        public override int GetHashCode()
        {
            return unchecked((int)Long);
        }

        public override bool Equals(object obj)
        {
            PhpNumber tmp;
            return obj is PhpNumber
                && (tmp = (PhpNumber)obj).TypeCode == TypeCode
                && tmp.Long == Long;    // <=> tmp.Double == Double
        }

        #endregion

        #region IPhpConvertible

        /// <summary>
        /// The PHP value type.
        /// </summary>
        public PhpTypeCode TypeCode
        {
            get
            {
                AssertTypeCode();
                return _typeCode;
            }
        }

        public double ToDouble()
        {
            AssertTypeCode();

            return (_typeCode == PhpTypeCode.Long) ? (double)Long : Double;
        }

        public long ToLong()
        {
            AssertTypeCode();

            return (_typeCode == PhpTypeCode.Long) ? Long : (long)Double;
        }

        public bool ToBoolean()
        {
            AssertTypeCode();

            return Long != 0L;  // (Double == 0.0 <=> Long == 0L)
        }

        public Convert.NumberInfo ToNumber(out PhpNumber number)
        {
            AssertTypeCode();

            number = this;

            return (_typeCode == PhpTypeCode.Long)
                ? Convert.NumberInfo.IsNumber | Convert.NumberInfo.LongInteger
                : Convert.NumberInfo.IsNumber | Convert.NumberInfo.Double;
        }

        /// <summary>
        /// Gets string representation of the number.
        /// </summary>
        public string ToString(Context ctx)
        {
            AssertTypeCode();

            return IsLong ? Long.ToString() : Double.ToString();    // TODO: Double conversion must respect ctx culture
        }

        public string ToStringOrThrow(Context ctx)
        {
            return ToString(ctx);
        }

        #endregion
    }
}
