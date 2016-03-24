using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// String builder providing fast concatenation and character replacements.
    /// </summary>
    public class PhpString : IPhpConvertible
    {
        #region Fields & Properties



        #endregion

        #region Construction

        /// <summary>
        /// Initializes empty php string.
        /// </summary>
        /// <param name="capacity">Expected capacity hint.</param>
        public PhpString(int capacity)
        {

        }

        // from builder, binary, unicode, concatenation

        #endregion

        #region Operations

        public void Append(string value) => Add(value);

        public virtual void Add(string value)
        {
            throw new NotImplementedException();
        }

        // Append
        // Prepend
        // this[] { get; set; }

        #endregion

        #region IPhpConvertible

        public PhpTypeCode TypeCode => PhpTypeCode.WritableString;

        public bool ToBoolean()
        {
            throw new NotImplementedException();
        }

        public double ToDouble()
        {
            throw new NotImplementedException();
        }

        public long ToLong()
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
    }
}
