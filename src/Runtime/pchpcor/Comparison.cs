using System;
using System.Collections.Generic;
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
                case PhpTypeCode.Boolean: return (lx != 0 ? 2 : 1) - (y.Boolean ? 2 : 1);
                case PhpTypeCode.Double: return Compare((double)lx, y.Double);
                default:
                    throw new NotImplementedException($"compare(Long, {y.TypeCode})");
            }
            //if (y == null) return ((long)x == 0) ? 0 : 1; // obsolete: Math.Sign((int)x); // y == 0
            //if (y.GetType() == typeof(string)) return -CompareString((string)y, (long)x);
        }

        public static int Compare(double dx, PhpValue y)
        {
            switch (y.TypeCode)
            {
                case PhpTypeCode.Double: return Compare(dx, y.Double);
                case PhpTypeCode.Long: return Compare(dx, (double)y.Long);
                case PhpTypeCode.Boolean: return (dx != 0.0 ? 2 : 1) - (y.Boolean ? 2 : 1);
                default:
                    throw new NotImplementedException($"compare(Double, {y.TypeCode})");
            }
            //if (y == null) return ((double)x == 0.0) ? 0 : 1; // obsolete: CompareDouble((double)x,0.0); // y == 0.0
            //if (y.GetType() == typeof(string)) return -CompareString((string)y, (double)x);
        }

        public static int Compare(bool bx, PhpValue y)
            => (bx ? 2 : 1) - (y.ToBoolean() ? 2 : 1);
        
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
    }
}
