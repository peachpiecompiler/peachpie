using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    [StructLayout(LayoutKind.Explicit)]
    public struct PhpNumber
    {
        #region Fields

        /// <summary>
        /// Number type.
        /// Valid values are <see cref="PhpTypeCode.Long"/> and <see cref="PhpTypeCode.Double"/>.
        /// </summary>
        [FieldOffset(0)]
        public PhpTypeCode TypeCode;

        [FieldOffset(4)]
        public long Long;

        [FieldOffset(4)]
        public double Double;

        #endregion

        #region Properties

        /// <summary>
        /// Gets value indicating the number is a floating point number.
        /// </summary>
        public bool IsDouble => TypeCode == PhpTypeCode.Double;

        /// <summary>
        /// Gets value indicating the number is an integer.
        /// </summary>
        public bool IsLong => TypeCode == PhpTypeCode.Long;

        #endregion

        #region Construction

        public static PhpNumber Create(int value) => Create((long)value);

        public static PhpNumber Create(long value)
        {
            return new PhpNumber() { TypeCode = PhpTypeCode.Long, Long = value };
        }

        public static PhpNumber Create(double value)
        {
            return new PhpNumber() { TypeCode = PhpTypeCode.Double, Double = value };
        }

        #endregion
    }
}
