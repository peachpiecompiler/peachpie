using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Defines comparison methods which are used to compare PHP.NET types.
    /// </summary>
    public interface IPhpComparable
    {
        /// <summary>
        /// Compares the current instance with another object using default comparer.
        /// </summary>
        /// <param name="obj">Object to compare with.</param>
        /// <returns>Negative, Positive or Zero.</returns>
        /// <exception cref="ArgumentException">Incomparable objects have been compared.</exception>
		int Compare(PhpValue obj);
    }

    /// <summary>
    /// PHP comparison semantic.
    /// </summary>
    public static class Comparison
    {
        public static bool Clt(long lx, double dy) => (double)lx < dy;
        public static bool Cgt(long lx, double dy) => (double)lx > dy;
        public static bool Ceq(long lx, double dy) => (double)lx == dy;

        public static bool Ceq(long lx, bool by) => (lx != 0) == by;
        public static bool Ceq(long lx, string sy) => Equals(sy, lx);
        public static bool Ceq(double dx, string sy) => Equals(sy, dx);
        public static bool Ceq(string sx, long ly) => Equals(sx, ly);
        public static bool Ceq(string sx, double dy) => Equals(sx, dy);
        public static bool Ceq(string sx, bool by) => Convert.ToBoolean(sx) == by;

        public static int Compare(long lx, PhpValue y)
        {
            switch (y.TypeCode)
            {
                case PhpTypeCode.Long: return Compare(lx, y.Long);
                case PhpTypeCode.Boolean: return Compare(lx != 0, y.Boolean);
                case PhpTypeCode.Double: return Compare((double)lx, y.Double);
                case PhpTypeCode.String: return -Compare(y.String, lx);
                case PhpTypeCode.WritableString: return -Compare(y.WritableString.ToString(), lx);
                case PhpTypeCode.Alias: return Compare(lx, y.Alias.Value);
                case PhpTypeCode.Undefined: return (lx == 0) ? 0 : 1;
                case PhpTypeCode.Object:
                    if (y.Object == null) return (lx == 0) ? 0 : 1;
                    break;
            }

            throw new NotImplementedException($"compare(Long, {y.TypeCode})");
        }

        public static int Compare(double dx, PhpValue y)
        {
            switch (y.TypeCode)
            {
                case PhpTypeCode.Double: return Compare(dx, y.Double);
                case PhpTypeCode.Long: return Compare(dx, (double)y.Long);
                case PhpTypeCode.Boolean: return Compare(dx != 0.0, y.Boolean);
                case PhpTypeCode.String: return -Compare(y.String, dx);
                case PhpTypeCode.WritableString: return -Compare(y.WritableString.ToString(), dx);
                case PhpTypeCode.Alias: return Compare(dx, y.Alias.Value);
                case PhpTypeCode.Undefined: return (dx == 0.0) ? 0 : 1;
                case PhpTypeCode.Object:
                    if (y.Object == null) return (dx == 0.0) ? 0 : 1;
                    break;
            }

            throw new NotImplementedException($"compare(Double, {y.TypeCode})");
        }

        public static int Compare(bool bx, PhpValue y) => Compare(bx, y.ToBoolean());

        public static int Compare(bool bx, bool by) => (bx ? 2 : 1) - (by ? 2 : 1);

        public static int Compare(string sx, PhpValue y)
        {
            switch (y.TypeCode)
            {
                case PhpTypeCode.Double: return Compare(sx, y.Double);
                case PhpTypeCode.Long: return Compare(sx, y.Long);
                case PhpTypeCode.Boolean: return Compare(Convert.ToBoolean(sx), y.Boolean);
                case PhpTypeCode.String: return Compare(sx, y.String);
                case PhpTypeCode.WritableString: return Compare(sx, y.WritableString.ToString());
                case PhpTypeCode.Alias: return Compare(sx, y.Alias.Value);
                case PhpTypeCode.Undefined: return (sx.Length == 0) ? 0 : 1;
                case PhpTypeCode.Object:
                    if (y.Object == null) return (sx.Length == 0) ? 0 : 1;
                    break;
            }

            throw new NotImplementedException($"compare(String, {y.TypeCode})");
        }

        public static int CompareNull(PhpValue y)
        {
            // TODO: PhpValue.IsEmpty ? 0 : -1

            switch (y.TypeCode)
            {
                case PhpTypeCode.Boolean: return y.Boolean ? -1 : 0;
                case PhpTypeCode.Long: return y.Long == 0 ? 0 : -1;
                case PhpTypeCode.Double: return y.Double == 0 ? 0 : -1;
                case PhpTypeCode.String: return y.String.Length == 0 ? 0 : -1;
                case PhpTypeCode.WritableString: return y.WritableString.Length == 0 ? 0 : -1;
                case PhpTypeCode.Alias: return CompareNull(y.Alias.Value);
                case PhpTypeCode.Object:
                    if (y.Object == null) return 0;
                    break;
                case PhpTypeCode.Undefined: return 0;
            }

            throw new NotImplementedException($"compare(null, {y.TypeCode})");
        }

        public static int Compare(PhpNumber x, PhpValue y) => x.IsLong ? Compare(x.Long, y) : Compare(x.Double, y);

        public static int Compare(PhpValue x, PhpValue y) => x.Compare(y);

        /// <summary>
		/// Compares two long integer values.
		/// </summary>
		/// <returns>(+1,0,-1)</returns>
        public static int Compare(long x, long y) => (x > y) ? +1 : (x < y ? -1 : 0);

        /// <summary>
		/// Compares two double values.
		/// </summary>
		/// <returns>(+1,0,-1)</returns>
		/// <remarks>We cannot used <see cref="Math.Sign"/> on <c>x - y</c> since the result can be NaN.</remarks>
        public static int Compare(double x, double y) => (x > y) ? +1 : (x < y ? -1 : 0);

        /// <summary>
		/// Compares string in a manner of PHP. 
		/// </summary>
		/// <remarks>Note that this comparison is not transitive (e.g. {"2","10","10a"} leads to a contradiction).</remarks>
        public static int Compare(string/*!*/sx, string/*!*/sy)
        {
            Debug.Assert(sx != null);
            Debug.Assert(sy != null);

            long lx, ly;
            double dx, dy;
            Convert.NumberInfo info_x, info_y;

            info_x = Convert.StringToNumber(sx, out lx, out dx);

            // an operand is not entirely convertable to numbers => string comparison is performed:
            if ((info_x & Convert.NumberInfo.IsNumber) == 0)
                return string.CompareOrdinal(sx, sy);

            info_y = Convert.StringToNumber(sy, out ly, out dy);

            // an operand is not entirely convertable to numbers => string comparison is performed:
            if ((info_y & Convert.NumberInfo.IsNumber) == 0)
                return string.CompareOrdinal(sx, sy);

            // numeric comparison
            return (((info_x | info_y) & Convert.NumberInfo.Double) != 0)
                ? Compare(dx, dy)   // at least one operand has been converted to double:
                : Compare(lx, ly);  // compare integers:
        }

        /// <summary>
		/// Compares a <see cref="string"/> with <see cref="long"/>.
		/// </summary>
        public static int Compare(string/*!*/sx, long ly)
        {
            Debug.Assert(sx != null);

            double dx;
            long lx;

            switch (Convert.StringToNumber(sx, out lx, out dx) & Convert.NumberInfo.TypeMask)
            {
                case Convert.NumberInfo.Double: return Compare(dx, (double)ly);
                case Convert.NumberInfo.LongInteger: return Compare(lx, ly);
                default: Debug.Assert(false); throw null;
            }
        }

        /// <summary>
        /// Compares a <see cref="string"/> with <see cref="double"/>.
        /// </summary>
        public static int Compare(string/*!*/sx, double dy)
        {
            Debug.Assert(sx != null);

            double dx;
            long lx;

            switch (Convert.StringToNumber(sx, out lx, out dx) & Convert.NumberInfo.TypeMask)
            {
                case Convert.NumberInfo.Double: return Compare(dx, dy);
                case Convert.NumberInfo.LongInteger: return Compare((double)lx, dy);
                default: Debug.Assert(false); throw null;
            }
        }

        /// <summary>
		/// Compares two objects for equality in a manner of the PHP regular comparison.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="ly">The second object.</param>
		/// <returns>Whether the values of operands are the same.</returns>
        public static bool Equals(string/*!*/ x, long ly)
        {
            Debug.Assert(x != null);

            double dx;
            long lx;

            switch (Convert.StringToNumber(x, out lx, out dx) & Convert.NumberInfo.TypeMask)
            {
                case Convert.NumberInfo.Double: return dx == ly;
                case Convert.NumberInfo.LongInteger: return lx == ly;
                default: Debug.Assert(false); throw null;
            }
        }

        /// <summary>
		/// Compares two objects for equality in a manner of the PHP regular comparison.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="dy">The second object.</param>
		/// <returns>Whether the values of operands are the same.</returns>
        public static bool Equals(string/*!*/ x, double dy)
        {
            Debug.Assert(x != null);

            double dx;
            long lx;

            switch (Convert.StringToNumber(x, out lx, out dx) & Convert.NumberInfo.TypeMask)
            {
                case Convert.NumberInfo.Double: return dx == dy;
                case Convert.NumberInfo.LongInteger: return lx == dy;
                default: Debug.Assert(false); throw null;
            }
        }
    }

    /// <summary>
    /// PHP strict comparison semantic.
    /// </summary>
    public static class StrictComparison
    {
        public static bool Ceq(bool bx, PhpValue y) => y.TypeCode == PhpTypeCode.Boolean && bx == y.Boolean;
        public static bool Ceq(long lx, PhpValue y) => y.TypeCode == PhpTypeCode.Long && lx == y.Long;
        public static bool Ceq(double dx, PhpValue y) => y.TypeCode == PhpTypeCode.Double && dx == y.Double;

        public static bool Ceq(PhpValue x, bool by) => x.TypeCode == PhpTypeCode.Boolean && by == x.Boolean;

        public static bool Ceq(PhpValue x, PhpValue y) => x.StrictEquals(y);
    }
}
