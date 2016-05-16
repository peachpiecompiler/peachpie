using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// PHP comparison semantic.
    /// </summary>
    public static class Comparison
    {
        public static bool Clt(long lx, double dy) => (double)lx < dy;
        public static bool Cgt(long lx, double dy) => (double)lx > dy;
        public static bool Ceq(long lx, double dy) => (double)lx == dy;

        public static bool Ceq(long lx, bool by) => (lx != 0) == by;

        public static int Compare(long lx, PhpValue y)
        {
            switch (y.TypeCode)
            {
                case PhpTypeCode.Long: return Compare(lx, y.Long);
                case PhpTypeCode.Boolean: return Compare(lx != 0, y.Boolean);
                case PhpTypeCode.Double: return Compare((double)lx, y.Double);
                case PhpTypeCode.String: return -Compare(y.String, lx);
                default:
                    throw new NotImplementedException($"compare(Long, {y.TypeCode})");
            }
            //if (y == null) return ((long)x == 0) ? 0 : 1; // obsolete: Math.Sign((int)x); // y == 0
        }

        public static int Compare(double dx, PhpValue y)
        {
            switch (y.TypeCode)
            {
                case PhpTypeCode.Double: return Compare(dx, y.Double);
                case PhpTypeCode.Long: return Compare(dx, (double)y.Long);
                case PhpTypeCode.Boolean: return Compare(dx != 0.0, y.Boolean);
                case PhpTypeCode.String: return -Compare(y.String, dx);
                default:
                    throw new NotImplementedException($"compare(Double, {y.TypeCode})");
            }
            //if (y == null) return ((double)x == 0.0) ? 0 : 1; // obsolete: CompareDouble((double)x,0.0); // y == 0.0
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
                default:
                    throw new NotImplementedException($"compare(String, {y.TypeCode})");
            }
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
    }
}
