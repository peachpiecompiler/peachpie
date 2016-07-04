using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    partial class Context
    {
        /// <summary>
        /// Encoding used to convert between unicode strings and binary strings.
        /// </summary>
        public virtual Encoding StringEncoding => Encoding.UTF8;

        /// <summary>
        /// Gets number format used for converting <see cref="double"/> to <see cref="string"/>.
        /// </summary>
        public virtual NumberFormatInfo NumberFormat => NumberFormatInfo.InvariantInfo;
    }
}
