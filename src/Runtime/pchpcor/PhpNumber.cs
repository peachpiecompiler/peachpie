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
        internal long _long;

        [FieldOffset(4)]
        double _double;

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

        /// <summary>
        /// Gets the long field of the number.
        /// Does not perform a conversion, expects the number is of type long.
        /// </summary>
        public long Long
        {
            get
            {
                Debug.Assert(IsLong);
                return _long;
            }
        }

        /// <summary>
        /// Gets the double field of the number.
        /// Does not perform a conversion, expects the number is of type double.
        /// </summary>
        public double Double
        {
            get
            {
                Debug.Assert(IsDouble);
                return _double;
            }
        }

        #endregion

        string GetDebuggerValue
        {
            get { return IsLong ? _long.ToString() : _double.ToString(); }
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
                    ? _long.CompareTo(other._long)
                    : _double.CompareTo(other._double);
            }
            else
            {
                return (_typeCode == PhpTypeCode.Long)
                    ? ((double)_long).CompareTo(other._double)
                    : _double.CompareTo((double)other._long);
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
                    ? a._long == b._long
                    : a._double == b._double;
            }
            else
            {
                return (a._typeCode == PhpTypeCode.Long)
                    ? (double)a._long == b._double
                    : a._double == (double)b._long;
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
                return Create(x._double + y.ToDouble());
            }

            if (y.IsDouble)
            {
                // LONG + DOUBLE
                Debug.Assert(x.IsLong);
                return Create((double)x._long + y._double);
            }

            // LONG + LONG
            Debug.Assert(x.IsLong && y.IsLong);
            long rl = unchecked(x._long + y._long);

            if ((x._long & Operators.LONG_SIGN_MASK) != (rl & Operators.LONG_SIGN_MASK) &&       // result has different sign than x
                (x._long & Operators.LONG_SIGN_MASK) == (y._long & Operators.LONG_SIGN_MASK)      // x and y have the same sign                
                )
            {
                // overflow to double:
                return Create((double)x._long + (double)y._long);
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
        /// Implements <c>+</c> operator on numbers.
        /// </summary>
        /// <param name="x">First operand.</param>
        /// <param name="y">Second operand.</param>
        /// <returns></returns>
        public static PhpNumber operator +(PhpNumber x, double y)
        {
            // at least one operand is a double:
            return Create(Add(x, y));
        }

        /// <summary>
        /// Implements <c>+</c> operator on numbers.
        /// </summary>
        /// <param name="x">First operand.</param>
        /// <param name="y">Second operand.</param>
        /// <returns></returns>
        public static PhpNumber operator +(PhpNumber x, long y)
        {
            // at least one operand is a double:
            if (x.IsDouble)
            {
                // DOUBLE + LONG
                return Create(x._double + (double)y);
            }

            // LONG + LONG
            Debug.Assert(x.IsLong);
            long rl = unchecked(x._long + y);

            if ((x._long & Operators.LONG_SIGN_MASK) != (rl & Operators.LONG_SIGN_MASK) &&       // result has different sign than x
                (x._long & Operators.LONG_SIGN_MASK) == (y & Operators.LONG_SIGN_MASK)      // x and y have the same sign                
                )
            {
                // overflow to double:
                return Create((double)x._long + (double)y);
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
        /// Implements <c>+</c> operator on numbers.
        /// </summary>
        /// <param name="x">First operand.</param>
        /// <param name="y">Second operand.</param>
        /// <returns></returns>
        public static PhpNumber operator +(double x, PhpNumber y)
        {
            // at least one operand is a double:
            // DOUBLE + DOUBLE|LONG
            return Create(Add(x, y));
        }

        /// <summary>
        /// Implements <c>+</c> operator on numbers.
        /// </summary>
        /// <param name="x">First operand.</param>
        /// <param name="y">Second operand.</param>
        /// <returns></returns>
        public static PhpNumber operator +(long x, PhpNumber y)
        {
            // at least one operand is a double:
            if (y.IsDouble)
            {
                // LONG + DOUBLE
                return Create((double)x + y._double);
            }

            // LONG + LONG
            Debug.Assert(y.IsLong);
            long rl = unchecked(x + y._long);

            if ((x & Operators.LONG_SIGN_MASK) != (rl & Operators.LONG_SIGN_MASK) &&       // result has different sign than x
                (x & Operators.LONG_SIGN_MASK) == (y._long & Operators.LONG_SIGN_MASK)      // x and y have the same sign                
                )
            {
                // overflow to double:
                return Create((double)x + (double)y._long);
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
        /// Implements <c>+</c> operator on numbers.
        /// </summary>
        /// <param name="x">First operand.</param>
        /// <param name="y">Second operand.</param>
        /// <returns></returns>
        public static PhpNumber Add(long x, long y)
        {
            // LONG + LONG
            long rl = unchecked(x + y);

            if ((x & Operators.LONG_SIGN_MASK) != (rl & Operators.LONG_SIGN_MASK) &&    // result has different sign than x
                (x & Operators.LONG_SIGN_MASK) == (y & Operators.LONG_SIGN_MASK)        // x and y have the same sign                
                )
            {
                // overflow to double:
                return PhpNumber.Create((double)x + (double)y);
            }
            else
            {
                //// int to long overflow check
                //int il = unchecked((int)rl);
                //if (il == rl)
                //    return il;

                // long:
                return PhpNumber.Create(rl);  // note: most common case
            }
        }

        /// <summary>
        /// Implements <c>+</c> operator on numbers.
        /// </summary>
        public static double Add(long x, double y)
        {
            return (double)x + y;
        }

        /// <summary>
        /// Implements <c>+</c> operator on numbers.
        /// </summary>
        public static double Add(PhpNumber x, double y)
        {
            return x.ToDouble() + y;
        }

        /// <summary>
        /// Implements <c>+</c> operator on numbers.
        /// </summary>
        public static double Add(double x, PhpNumber y)
        {
            return x + y.ToDouble();
        }

        /// <summary>
        /// Unary minus operator for int64.
        /// </summary>
        public static PhpNumber Minus(long lx)
        {
            if (lx == long.MinValue)
                return Create(-(double)long.MinValue);

            return Create(-lx);
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
                return Create(x._double - y.ToDouble());
            }

            if (y.IsDouble)
            {
                // LONG - DOUBLE
                Debug.Assert(x.IsLong);
                return Create((double)x._long - y._double);
            }

            // LONG - LONG
            Debug.Assert(x.IsLong && y.IsLong);
            long rl = unchecked(x._long - y._long);

            if ((x._long & Operators.LONG_SIGN_MASK) != (rl & Operators.LONG_SIGN_MASK) &&   // result has different sign than x
                (x._long & Operators.LONG_SIGN_MASK) != (y._long & Operators.LONG_SIGN_MASK)  // x and y have the same sign                
                )
            {
                // overflow:
                return Create((double)x._long - (double)y._long);
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

        /// <summary>
        /// Unary minus operator.
        /// </summary>
        public static PhpNumber operator - (PhpNumber x)
        {
            x.AssertTypeCode();

            if (x.IsDouble)
            {
                return Create(-x._double);
            }
            else
            {
                return Minus(x._long);
            }
        }

        /// <summary>
        /// Divide operator.
        /// </summary>
        public static PhpNumber operator / (PhpNumber x, PhpNumber y)
        {
            x.AssertTypeCode();
            y.AssertTypeCode();

            if (x.IsDouble) // double / number
                return Create(x._double / y.ToDouble());

            if (y.IsDouble) // long / double
                return Create((double)x._long / y._double);

            // long / long
            var r = x._long % y._long;

            return (r == 0)
                ? Create(x._long / y._long)
                : Create((double)x._long / (double)y._long);
        }

        /// <summary>
        /// Divide operator.
        /// </summary>
        public static PhpNumber operator /(long lx, PhpNumber y)
        {
            y.AssertTypeCode();

            if (y.IsDouble) // long / double
                return Create((double)lx / y._double);

            // long / long
            var r = lx % y._long;

            return (r == 0)
                ? Create(lx / y._long)
                : Create((double)lx / (double)y._long);
        }

        #endregion

        #region Construction

        public static PhpNumber Create(long value)
        {
            return new PhpNumber() { _typeCode = PhpTypeCode.Long, _long = value };
        }

        public static PhpNumber Create(double value)
        {
            return new PhpNumber() { _typeCode = PhpTypeCode.Double, _double = value };
        }

        #endregion

        #region Object

        public override int GetHashCode()
        {
            return unchecked((int)_long);
        }

        public override bool Equals(object obj)
        {
            PhpNumber tmp;
            return obj is PhpNumber
                && (tmp = (PhpNumber)obj).TypeCode == TypeCode
                && tmp._long == _long;    // <=> tmp.Double == Double
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

            return (_typeCode == PhpTypeCode.Long) ? (double)_long : _double;
        }

        public long ToLong()
        {
            AssertTypeCode();

            return (_typeCode == PhpTypeCode.Long) ? _long : (long)_double;
        }

        public bool ToBoolean()
        {
            AssertTypeCode();

            return _long != 0L;  // (Double == 0.0 <=> Long == 0L)
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

            return IsLong ? _long.ToString() : _double.ToString();    // TODO: Double conversion must respect ctx culture
        }

        public string ToStringOrThrow(Context ctx)
        {
            return ToString(ctx);
        }

        #endregion
    }
}
