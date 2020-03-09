#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    [DebuggerDisplay("{GetDebuggerValue,nq}", Type = "{GetDebuggerType,nq}")]
    [DebuggerNonUserCode, DebuggerStepThrough]
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct PhpNumber : IComparable<PhpNumber>, IComparable<long>, IComparable<double>, IEquatable<PhpNumber>, IPhpConvertible
    {
        #region nested enum: NumberType

        enum NumberType
        {
            @long = 0,
            @double = 1,
        }

        #endregion

        #region Fields

        /// <summary>
        /// Number type.
        /// </summary>
        [FieldOffset(0)]
        readonly NumberType _type;

        [FieldOffset(4)]
        readonly long _long;

        [FieldOffset(4)]
        readonly double _double;

        #endregion

        #region Properties

        /// <summary>
        /// Gets value indicating the number is a floating point number.
        /// </summary>
        public bool IsDouble => !IsLong;

        /// <summary>
        /// Gets value indicating the number is an integer.
        /// </summary>
        public bool IsLong => _type == NumberType.@long;

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

        #region Debug

        string GetDebuggerValue { get { return IsLong ? _long.ToString() : _double.ToString(CultureInfo.InvariantCulture); } }

        string GetDebuggerType { get { return IsLong ? PhpVariable.TypeNameInteger : PhpVariable.TypeNameDouble; } }

        #endregion

        #region Operators

        #region Equality, Comparison

        /// <summary>
        /// Value equality comparison, operand must contain the same value as this.
        /// </summary>
        public bool Equals(PhpNumber other) => other._long == _long && other._type == _type;    // <=> tmp.Double == Double

        /// <summary>
        /// Gets non strict comparison with another number.
        /// </summary>
        public int CompareTo(PhpNumber other)
        {
            if (_type == other._type)
            {
                return (_type == NumberType.@long)
                    ? _long.CompareTo(other._long)
                    : _double.CompareTo(other._double);
            }
            else
            {
                return (_type == NumberType.@long)
                    ? ((double)_long).CompareTo(other._double)
                    : _double.CompareTo((double)other._long);
            }
        }

        /// <summary>
        /// Gets non strict comparison with another number.
        /// </summary>
        public int CompareTo(double dx)
        {
            return ToDouble().CompareTo(dx);
        }

        /// <summary>
        /// Gets non strict comparison with another number.
        /// </summary>
        public int CompareTo(long lx)
        {
            return IsLong ? _long.CompareTo(lx) : _double.CompareTo((double)lx);
        }

        public static bool operator <(PhpNumber x, PhpNumber y)
        {
            return x.CompareTo(y) < 0;
        }

        public static bool operator >(PhpNumber x, PhpNumber y)
        {
            return x.CompareTo(y) > 0;
        }

        public static bool operator <(PhpNumber x, double dy)
        {
            return x.ToDouble() < dy;
        }

        public static bool operator >(PhpNumber x, double dy)
        {
            return x.ToDouble() > dy;
        }

        public static bool operator <(PhpNumber x, long ly)
        {
            return x.IsLong ? (x._long < ly) : (x._double < (double)ly);
        }

        public static bool operator >(PhpNumber x, long ly)
        {
            return x.IsLong ? (x._long > ly) : (x._double > (double)ly);
        }

        public static bool operator <(long lx, PhpNumber y)
        {
            return y.IsLong ? (lx < y._long) : ((double)lx < y._double);
        }

        public static bool operator >(long lx, PhpNumber y)
        {
            return y.IsLong ? (lx > y._long) : ((double)lx > y._double);
        }

        /// <summary>
        /// Non strict equality operator.
        /// </summary>
        public static bool operator ==(PhpNumber a, PhpValue b) => Comparison.Compare(a, b) == 0;

        /// <summary>
        /// Non strict inequality operator.
        /// </summary>
        public static bool operator !=(PhpNumber a, PhpValue b) => Comparison.Compare(a, b) != 0;

        /// <summary>
        /// Non strict equality operator.
        /// </summary>
        public static bool operator ==(PhpNumber a, PhpNumber b)
        {
            if (a._type == b._type)
            {
                return (a._type == NumberType.@long)
                    ? a._long == b._long
                    : a._double == b._double;
            }
            else
            {
                return (a._type == NumberType.@long)
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
        /// Non strict equality operator.
        /// </summary>
        public static bool operator ==(PhpNumber x, long y)
            => x.IsLong ? x.Long == y : x.Double == y;

        /// <summary>
        /// Non strict equality operator.
        /// </summary>
        public static bool operator !=(PhpNumber x, long y)
            => !(x == y);

        /// <summary>
        /// Non strict equality operator.
        /// </summary>
        public static bool operator ==(long lx, PhpNumber y)
            => y.IsLong ? lx == y.Long : lx == y.Double;

        /// <summary>
        /// Non strict equality operator.
        /// </summary>
        public static bool operator !=(long lx, PhpNumber y)
            => !(lx == y);

        /// <summary>
        /// Non strict equality operator.
        /// </summary>
        public static bool operator ==(PhpNumber x, double y)
            => x.ToDouble() == y;

        /// <summary>
        /// Non strict equality operator.
        /// </summary>
        public static bool operator !=(PhpNumber x, double y)
            => !(x == y);

        #endregion

        #region Add

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
            return Add(x._long, y._long);
        }

        /// <summary>
        /// Implements <c>+</c> operator on numbers.
        /// </summary>
        /// <param name="x">First operand.</param>
        /// <param name="dy">Second operand.</param>
        /// <returns></returns>
        public static double operator +(PhpNumber x, double dy)
        {
            // at least one operand is a double:
            return x.ToDouble() + dy;
        }

        /// <summary>
        /// Implements <c>+</c> operator on numbers.
        /// </summary>
        /// <param name="x">First operand.</param>
        /// <param name="ly">Second operand.</param>
        /// <returns></returns>
        public static PhpNumber operator +(PhpNumber x, long ly)
        {
            // at least one operand is a double:
            if (x.IsDouble)
            {
                // DOUBLE + LONG
                return Create(x._double + (double)ly);
            }

            // LONG + LONG
            Debug.Assert(x.IsLong);
            return Add(x._long, ly);
        }

        /// <summary>
        /// Implements <c>+</c> operator on numbers.
        /// </summary>
        /// <param name="dx">First operand.</param>
        /// <param name="y">Second operand.</param>
        /// <returns></returns>
        public static double operator +(double dx, PhpNumber y)
        {
            // at least one operand is a double:
            // DOUBLE + DOUBLE|LONG
            return dx + y.ToDouble();
        }

        /// <summary>
        /// Implements <c>+</c> operator on numbers.
        /// </summary>
        /// <param name="lx">First operand.</param>
        /// <param name="y">Second operand.</param>
        /// <returns></returns>
        public static PhpNumber operator +(long lx, PhpNumber y)
        {
            // at least one operand is a double:
            if (y.IsDouble)
            {
                // LONG + DOUBLE
                return Create((double)lx + y._double);
            }

            // LONG + LONG
            Debug.Assert(y.IsLong);
            return Add(lx, y._long);
        }

        /// <summary>
        /// Implements <c>+</c> operator on numbers.
        /// </summary>
        public static PhpNumber operator +(PhpValue x, PhpNumber y)
        {
            PhpNumber x_number;

            var x_info = x.ToNumber(out x_number);
            if ((x_info & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                throw new NotImplementedException();
            }

            return x_number + y;
        }

        /// <summary>
        /// Implements <c>+</c> operator on numbers.
        /// </summary>
        public static PhpNumber operator +(PhpNumber x, PhpValue y)
        {
            PhpNumber y_number;

            var y_info = y.ToNumber(out y_number);
            if ((y_info & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                throw new NotImplementedException();    // TODO: PhpException; return 0
            }

            return x + y_number;
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
        public static PhpNumber Add(PhpValue x, long y)
        {
            PhpNumber x_number;

            var x_info = x.ToNumber(out x_number);
            if ((x_info & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                throw new ArgumentException();
            }

            return x_number + y;
        }

        /// <summary>
        /// Implements <c>+</c> operator on numbers.
        /// </summary>
        public static double Add(PhpValue x, double y)
        {
            PhpNumber x_number;

            var x_info = x.ToNumber(out x_number);
            if ((x_info & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                throw new ArgumentException();
            }

            return x_number.ToDouble() + y;
        }

        /// <summary>
        /// Implements <c>+</c> operator on numbers.
        /// </summary>
        public static PhpNumber Add(long x, PhpValue y)
        {
            PhpNumber number;
            if ((y.ToNumber(out number) & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                //PhpException.UnsupportedOperandTypes();
                //return 0.0;
                throw new ArgumentException();  // TODO: ErrCode & return 0
            }

            //
            return x + number;
        }

        /// <summary>
        /// Implements <c>+</c> operator on numbers.
        /// </summary>
        public static double Add(double x, PhpValue y)
        {
            PhpNumber number;
            if ((y.ToNumber(out number) & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                //PhpException.UnsupportedOperandTypes();
                //return 0.0;
                throw new ArgumentException();  // TODO: ErrCode & return 0
            }

            //
            return x + number.ToDouble();
        }

        /// <summary>
        /// Implements <c>+</c> operator on values.
        /// </summary>
        public static PhpValue Add(PhpValue x, PhpValue y)
        {
            PhpNumber xnumber, ynumber;

            // converts x and y to numbers:
            if (((x.ToNumber(out xnumber) | y.ToNumber(out ynumber)) & (Convert.NumberInfo.IsPhpArray | Convert.NumberInfo.Unconvertible)) != 0)
            {
                return AddNonNumbers(ref x, ref y);
            }

            //
            return (xnumber + ynumber).ToPhpValue();
        }

        /// <summary>
        /// Implements <c>+</c> operator for unconvertible values or arrays.
        /// </summary>
        static PhpValue AddNonNumbers(ref PhpValue x, ref PhpValue y)
        {
            var arr_x = x.AsArray();
            var arr_y = y.AsArray();

            if (arr_x != null && arr_y != null)
            {
                return (PhpValue)PhpArray.Union(arr_x, arr_y);
            }

            //PhpException.UnsupportedOperandTypes();
            //return 0;
            throw new ArgumentException();  // TODO: ErrCode & return 0
        }

        /// <summary>
        /// Implements <c>+</c> operator on values.
        /// </summary>
        public static PhpArray Add(PhpValue x, PhpArray y)
        {
            var arr = x.AsArray();  // array or &array
            if (arr != null)
            {
                return PhpArray.Union(arr, y);
            }

            //PhpException.UnsupportedOperandTypes();
            //return 0;
            throw new ArgumentException();  // TODO: ErrCode & return 0
        }

        /// <summary>
        /// Implements <c>+</c> operator on values.
        /// </summary>
        public static PhpArray Add(PhpArray x, PhpValue y)
        {
            var arr = y.AsArray();  // array or &array
            if (arr != null)
            {
                return PhpArray.Union(x, arr);
            }

            //PhpException.UnsupportedOperandTypes();
            //return 0;
            throw new ArgumentException();  // TODO: ErrCode & return 0
        }

        #endregion

        #region Sub

        /// <summary>
        /// Implements <c>-</c> operator on numbers.
        /// </summary>
        /// <param name="x">First operand.</param>
        /// <param name="y">Second operand.</param>
        /// <returns></returns>
        public static PhpNumber operator -(PhpNumber x, PhpNumber y)
        {
            // at least one operand is convertible to a double:
            if (x.IsDouble) // double - number
                return Create(x._double - y.ToDouble());

            if (y.IsDouble) // long - double
                return Create((double)x._long - y._double);

            // long - long
            Debug.Assert(x.IsLong && y.IsLong);
            return Sub(x._long, y._long);
        }

        /// <summary>
        /// Implements <c>-</c> operator on numbers.
        /// </summary>
        /// <param name="x">First operand.</param>
        /// <param name="ly">Second operand.</param>
        /// <returns></returns>
        public static PhpNumber operator -(PhpNumber x, long ly)
        {
            // at least one operand is convertible to a double:
            if (x.IsDouble) // double - number
                return Create(x._double - (double)ly);

            // long - long
            Debug.Assert(x.IsLong);
            return Sub(x._long, ly);
        }

        /// <summary>
        /// Implements <c>-</c> operator on numbers.
        /// </summary>
        /// <param name="lx">First operand.</param>
        /// <param name="y">Second operand.</param>
        /// <returns></returns>
        public static PhpNumber operator -(long lx, PhpNumber y)
        {
            if (y.IsDouble) // long - double
                return Create((double)lx - y._double);

            // long - long
            Debug.Assert(y.IsLong);
            return Sub(lx, y._long);
        }

        /// <summary>
        /// Subtract operator.
        /// </summary>
        public static PhpNumber Sub(long lx, long ly)
        {
            long rl = unchecked(lx - ly);

            if ((lx & Operators.LONG_SIGN_MASK) != (rl & Operators.LONG_SIGN_MASK) &&   // result has different sign than x
                (lx & Operators.LONG_SIGN_MASK) != (ly & Operators.LONG_SIGN_MASK)      // x and y have the same sign                
                )
            {
                // overflow:
                return Create((double)lx - (double)ly);
            }
            else
            {
                return Create(rl);
            }
        }

        /// <summary>
        /// Subtract operator.
        /// </summary>
        public static double Sub(long lx, double dy)
        {
            return (double)lx - dy;
        }

        /// <summary>
        /// Subtract operator.
        /// </summary>
        public static double Sub(PhpNumber x, double dy)
        {
            return x.ToDouble() - dy;
        }

        /// <summary>
        /// Subtract operator.
        /// </summary>
        public static PhpNumber Sub(PhpValue x, long ly)
        {
            PhpNumber x_number;

            var x_info = x.ToNumber(out x_number);
            if ((x_info & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                throw new ArgumentException(); // return 0
            }

            //
            return x_number - ly;
        }

        /// <summary>
        /// Subtract operator.
        /// </summary>
        public static double Sub(PhpValue x, double dy)
        {
            PhpNumber x_number;

            var x_info = x.ToNumber(out x_number);
            if ((x_info & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                throw new ArgumentException();  // return 0
            }

            //
            return x_number.ToDouble() - dy;
        }

        /// <summary>
        /// Subtract operator.
        /// </summary>
        public static double Sub(double dx, PhpValue y)
        {
            PhpNumber y_number;

            var y_info = y.ToNumber(out y_number);
            if ((y_info & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                throw new ArgumentException();  // return 0
            }

            //
            return dx - y_number.ToDouble();
        }

        /// <summary>
        /// Subtract operator.
        /// </summary>
        public static PhpNumber Sub(PhpValue x, PhpNumber y)
        {
            PhpNumber x_number;

            var x_info = x.ToNumber(out x_number);
            if ((x_info & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                throw new ArgumentException();  // return 0
            }

            //
            return x_number - y;
        }

        /// <summary>
        /// Implements <c>-</c> operator on numbers.
        /// </summary>
        public static PhpNumber Sub(PhpNumber x, PhpValue y)
        {
            PhpNumber y_number;

            var y_info = y.ToNumber(out y_number);
            if ((y_info & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                throw new NotImplementedException();    // TODO: PhpException; return 0
            }

            return x - y_number;
        }

        /// <summary>
        /// Subtract operator.
        /// </summary>
        public static PhpNumber Sub(PhpValue x, PhpValue y)
        {
            PhpNumber xnumber, ynumber;

            // converts x and y to numbers:
            if (((x.ToNumber(out xnumber) | y.ToNumber(out ynumber)) & (Convert.NumberInfo.IsPhpArray | Convert.NumberInfo.Unconvertible)) != 0)
            {
                throw new ArgumentException();
            }

            //
            return xnumber - ynumber;
        }

        /// <summary>
        /// Subtract operator.
        /// </summary>
        public static PhpNumber Sub(long lx, PhpValue y)
        {
            PhpNumber ynumber;

            // converts x and y to numbers:
            if ((y.ToNumber(out ynumber) & (Convert.NumberInfo.IsPhpArray | Convert.NumberInfo.Unconvertible)) != 0)
            {
                throw new ArgumentException();
            }

            //
            return lx - ynumber;
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
        /// Unary minus operator.
        /// </summary>
        public static PhpNumber operator -(PhpNumber x)
        {
            if (x.IsDouble)
            {
                return Create(-x._double);
            }
            else
            {
                return Minus(x._long);
            }
        }

        #endregion

        #region Div

        /// <summary>
        /// Divide operator.
        /// </summary>
        public static PhpNumber operator /(PhpNumber x, PhpNumber y)
        {
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
        public static PhpNumber operator /(PhpNumber x, long ly)
        {
            if (x.IsDouble) // double / long
            {
                return Create(x._double / (double)ly);
            }

            // long / long
            var r = x._long % ly;

            return (r == 0)
                ? Create(x._long / ly)
                : Create((double)x._long / (double)ly);
        }

        /// <summary>
        /// Divide operator.
        /// </summary>
        public static double operator /(PhpNumber x, double dy)
        {
            return x.ToDouble() / dy;
        }

        /// <summary>
        /// Divide operator.
        /// </summary>
        public static PhpNumber operator /(PhpNumber x, PhpValue y)
        {
            PhpNumber ynumber;
            if ((y.ToNumber(out ynumber) & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                //PhpException.UnsupportedOperandTypes();
                //return 0.0;
                throw new ArgumentException();
            }

            //
            return x / ynumber;
        }

        /// <summary>
        /// Divide operator.
        /// </summary>
        public static double operator /(double dx, PhpNumber y)
        {
            return dx / y.ToDouble();
        }

        /// <summary>
        /// Divide operator.
        /// </summary>
        public static PhpNumber operator /(long lx, PhpNumber y)
        {
            if (y.IsDouble) // long / double
                return Create((double)lx / y._double);

            // long / long
            var r = lx % y._long;

            return (r == 0)
                ? Create(lx / y._long)
                : Create((double)lx / (double)y._long);
        }

        #endregion

        #region Mul

        /// <summary>
        /// Multiplies two long numbers with eventual overflow to double.
        /// </summary>
        public static PhpNumber Multiply(long lx, long ly)
        {
            long lr = unchecked(lx * ly);
            double dr = (double)lx * (double)ly;
            double delta = (double)lr - dr;

            // check for overflow
            if ((dr + delta) != dr)
                return Create(dr);
            else
                return Create(lr);

            //try
            //{
            //    return Create(checked(x._long * y._long));
            //}
            //catch (OverflowException)
            //{
            //    // we need double
            //    return Create((double)x._long * (double)y._long);
            //}
        }

        /// <summary>
        /// Multiply operator.
        /// </summary>
        public static double Multiply(long lx, double dy)
        {
            return (double)lx * dy;
        }

        /// <summary>
        /// Mul operator.
        /// </summary>
        public static PhpNumber operator *(PhpNumber x, PhpNumber y)
        {
            // double * number
            if (x.IsDouble)
                return Create(x._double * y.ToDouble());

            // long * double
            if (y.IsDouble)
                return Create((double)x._long * y._double);

            // long * long
            return Multiply(x._long, y._long);
        }

        /// <summary>
        /// Mul operator.
        /// </summary>
        public static PhpNumber operator *(long lx, PhpNumber y)
        {
            // long * double
            if (y.IsDouble)
                return Create((double)lx * y._double);

            // long * long
            return Multiply(lx, y._long);
        }

        /// <summary>
        /// Mul operator.
        /// </summary>
        public static PhpNumber operator *(PhpNumber x, long ly)
        {
            // double * number
            if (x.IsDouble)
                return Create(x._double * (double)ly);

            // long * long
            return Multiply(x._long, ly);
        }

        /// <summary>
        /// Mul operator.
        /// </summary>
        public static double operator *(PhpNumber x, double dy)
        {
            return x.ToDouble() * dy;
        }

        /// <summary>
        /// Mul operator.
        /// </summary>
        public static PhpNumber operator *(PhpNumber x, PhpValue y)
        {
            PhpNumber ynumber;
            if ((y.ToNumber(out ynumber) & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                //PhpException.UnsupportedOperandTypes();
                //return 0.0;
                throw new ArgumentException();
            }

            //
            return x * ynumber;
        }

        /// <summary>
        /// Mul operator.
        /// </summary>
        public static PhpNumber operator *(PhpValue x, PhpNumber y)
        {
            PhpNumber xnumber;
            if ((x.ToNumber(out xnumber) & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                //PhpException.UnsupportedOperandTypes();
                //return 0.0;
                throw new ArgumentException();
            }

            //
            return xnumber * y;
        }

        /// <summary>
        /// Multiply operator.
        /// </summary>
        public static PhpNumber Multiply(long lx, PhpValue y)
        {
            PhpNumber ynumber;
            if ((y.ToNumber(out ynumber) & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                //PhpException.UnsupportedOperandTypes();
                //return 0.0;
                throw new ArgumentException();
            }

            //
            return lx * ynumber;
        }

        /// <summary>
        /// Multiply operator.
        /// </summary>
        public static double Multiply(double dx, PhpValue y)
        {
            PhpNumber ynumber;
            if ((y.ToNumber(out ynumber) & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                //PhpException.UnsupportedOperandTypes();
                //return 0.0;
                throw new ArgumentException();
            }

            //
            return dx * ynumber.ToDouble();
        }

        /// <summary>
        /// Multiply operator.
        /// </summary>
        public static PhpNumber Multiply(PhpValue x, PhpValue y)
        {
            PhpNumber xnumber, ynumber;
            if (((x.ToNumber(out xnumber) | y.ToNumber(out ynumber)) & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                //PhpException.UnsupportedOperandTypes();
                //return 0.0;
                throw new ArgumentException();
            }

            //
            return xnumber * ynumber;
        }

        /// <summary>
        /// Multiply operator.
        /// </summary>
        public static PhpNumber Multiply(PhpValue x, long ly)
        {
            PhpNumber xnumber;
            if (((x.ToNumber(out xnumber)) & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                //PhpException.UnsupportedOperandTypes();
                //return 0.0;
                throw new ArgumentException();
            }

            //
            return xnumber * ly;
        }

        /// <summary>
        /// Multiply operator.
        /// </summary>
        public static double Multiply(PhpValue x, double dy)
        {
            PhpNumber xnumber;
            if (((x.ToNumber(out xnumber)) & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                //PhpException.UnsupportedOperandTypes();
                //return 0.0;
                throw new ArgumentException();
            }

            //
            return xnumber.ToDouble() * dy;
        }

        #endregion

        #region Pow

        //Pow_value_value = ct.PhpNumber.Method("Pow", ct.PhpValue, ct.PhpValue);
        public static PhpNumber Pow(PhpValue x, PhpValue y)
        {
            PhpNumber xnumber, ynumber;
            if (((x.ToNumber(out xnumber) | y.ToNumber(out ynumber)) & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                throw new ArgumentException();  // TODO: ErrCode and return 0;
            }

            //
            return Pow(ref xnumber, ref ynumber);
        }

        //Pow_number_number = ct.PhpNumber.Method("Pow", ct.PhpNumber, ct.PhpNumber);
        public static PhpNumber Pow(PhpNumber x, PhpNumber y) => Pow(ref x, ref y);

        static PhpNumber Pow(ref PhpNumber x, ref PhpNumber y)
        {
            if (x.IsDouble) return Create(Pow(x.Double, y.ToDouble()));
            if (y.IsDouble) return Create(Pow(x.ToDouble(), y.Double));

            // long ** long
            return Pow(x.Long, y.Long);
        }

        //Pow_long_long = ct.PhpNumber.Method("Pow", ct.Long, ct.Long);
        public static PhpNumber Pow(long lbase, long lexp)
        {
            if (lexp >= 0)
            {
                if (lexp == 0) // anything powered by 0 is 1
                {
                    return Create(1);
                }

                if (lbase == 0) // 0^(anything except 0) is 0
                {
                    return Create(0);
                }

                long l1 = 1;    // long result if possible
                long l2 = lbase;

                try
                {
                    while (lexp >= 1)
                    {
                        if ((lexp & 1) != 0)
                        {
                            l1 *= l2;
                            lexp--;
                        }
                        else
                        {
                            l2 *= l2;
                            lexp /= 2;
                        }
                    }
                }
                catch (ArithmeticException)
                {
                    return Create((double)l1 * Math.Pow(l2, lexp));
                }

                // able to do it with longs
                return Create(l1);
            }

            //
            return Create(Math.Pow((double)lbase, (double)lexp));
        }

        //Pow_long_number = ct.PhpNumber.Method("Pow", ct.Long, ct.PhpNumber);
        public static PhpNumber Pow(long lx, PhpNumber y)
        {
            if (y.IsDouble) return Create(Pow((double)lx, y.Double));
            return Pow(lx, y.Long);
        }

        //Pow_long_value = ct.PhpNumber.Method("Pow", ct.Long, ct.PhpValue);
        public static PhpNumber Pow(long lx, PhpValue y)
        {
            PhpNumber ynumber;
            if ((y.ToNumber(out ynumber) & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                throw new ArgumentException();  // TODO: ErrCode and return 0.0;
            }

            return Pow(lx, ynumber);
        }

        //Pow_double_double = ct.PhpNumber.Method("Pow", ct.Double, ct.Double);
        public static double Pow(double dx, double dy) => Math.Pow(dx, dy);

        //Pow_long_double = ct.PhpNumber.Method("Pow", ct.Long, ct.Double);
        public static double Pow(long lx, double dy) => Math.Pow((double)lx, dy);

        //Pow_number_double = ct.PhpNumber.Method("Pow", ct.PhpNumber, ct.Double);
        public static double Pow(PhpNumber x, double dy) => Math.Pow(x.ToDouble(), dy);

        // Pow_number_value = ct.PhpNumber.Method("Pow", ct.PhpNumber, ct.PhpValue);
        public static PhpNumber Pow(PhpNumber x, PhpValue y)
        {
            PhpNumber ynumber;
            if ((y.ToNumber(out ynumber) & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                throw new ArgumentException();  // TODO: ErrCode and return 0.0;
            }

            return Pow(ref x, ref ynumber);
        }

        //Pow_double_value = ct.PhpNumber.Method("Pow", ct.Double, ct.PhpValue);
        public static double Pow(double dx, PhpValue y)
        {
            PhpNumber ynumber;
            if ((y.ToNumber(out ynumber) & (Convert.NumberInfo.Unconvertible | Convert.NumberInfo.IsPhpArray)) != 0)
            {
                throw new ArgumentException();  // TODO: ErrCode and return 0.0;
            }

            //
            return Pow(dx, ynumber.ToDouble());
        }

        #endregion

        #region Mod

        /// <summary>Gets remainder of two numbers.</summary>
        public static long Mod(PhpValue x, PhpValue y)
        {
            PhpNumber nx, ny;
            var info = x.ToNumber(out nx) | y.ToNumber(out ny);
            if ((info & Convert.NumberInfo.IsPhpArray) != 0)
            {
                throw new ArgumentException();
                // TODO: Err UnsupportedOperandTypes
            }

            return Mod(nx.ToLong(), ny.ToLong());
        }

        /// <summary>Gets remainder of two numbers.</summary>
        public static long Mod(PhpValue x, long ly)
        {
            PhpNumber nx;
            var info = x.ToNumber(out nx);
            if ((info & Convert.NumberInfo.IsPhpArray) != 0)
            {
                throw new ArgumentException();
                // TODO: Err UnsupportedOperandTypes
            }

            return Mod(nx.ToLong(), ly);
        }

        /// <summary>Gets remainder of two numbers.</summary>
        public static long Mod(long lx, PhpValue y)
        {
            PhpNumber ny;
            var info = y.ToNumber(out ny);
            if ((info & Convert.NumberInfo.IsPhpArray) != 0)
            {
                throw new ArgumentException();
                // TODO: Err UnsupportedOperandTypes
            }

            return Mod(lx, ny.ToLong());
        }

        /// <summary>Gets remainder of two numbers.</summary>
        public static long Mod(long lx, long ly)
        {
            // TODO: Err check ly != 0 to report PHP warning

            return lx % ly;
        }

        #endregion

        #region Conversion

        /// <summary>
        /// Converts given number to <see cref="long"/>.
        /// </summary>
        public static implicit operator long(PhpNumber value) => value.ToLong();

        /// <summary>
        /// Converts given number to <see cref="bool"/>.
        /// </summary>
        public static implicit operator bool(PhpNumber value) => value.ToBoolean();

        /// <summary>
        /// Converts given number to <see cref="double"/>.
        /// </summary>
        public static implicit operator double(PhpNumber value) => value.ToDouble();

        public static implicit operator PhpNumber(long value) => Create(value);

        public static implicit operator PhpNumber(double value) => Create(value);

        #endregion

        #region Operators

        public bool IsEmpty() => /*this == default;*/ _long == 0L; // => _double == 0

        public stdClass ToObject() => new stdClass(ToPhpValue());

        public PhpValue ToPhpValue() => IsLong ? PhpValue.Create(_long) : PhpValue.Create(_double);

        #endregion

        #endregion

        #region Construction

        /// <summary>
        /// Integer zero number.
        /// </summary>
        public readonly static PhpNumber Default = new PhpNumber(0L);

        private PhpNumber(long value) : this()
        {
            _type = NumberType.@long;
            _long = value;
        }

        private PhpNumber(double value) : this()
        {
            _type = NumberType.@double;
            _double = value;
        }

        public static PhpNumber Create(long value) => new PhpNumber(value);

        public static PhpNumber Create(double value) => new PhpNumber(value);

        #endregion

        #region Object

        public override int GetHashCode() => unchecked((int)_long);

        public override bool Equals(object obj) => obj is PhpNumber num && Equals(num);

        #endregion

        #region IPhpConvertible

        public double ToDouble()
        {
            return IsDouble ? _double : (double)_long;
        }

        public long ToLong()
        {
            return IsLong ? _long : (long)_double;
        }

        public bool ToBoolean()
        {
            return _long != 0L;  // (Double == 0.0 <=> Long == 0L)
        }

        public Convert.NumberInfo ToNumber(out PhpNumber number)
        {
            number = this;

            return IsLong
                ? Convert.NumberInfo.IsNumber | Convert.NumberInfo.LongInteger
                : Convert.NumberInfo.IsNumber | Convert.NumberInfo.Double;
        }

        /// <summary>
        /// Gets string representation of the number.
        /// </summary>
        public string ToString(Context ctx)
        {
            return IsLong ? _long.ToString() : Convert.ToString(_double, ctx);
        }

        public object ToClass() => ToObject();

        public PhpArray ToArray() => PhpArray.New(ToPhpValue());

        #endregion
    }
}
