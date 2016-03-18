using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// String builder providing fast concatenation and character replacements.
    /// Possibly consists of list of unicode and binary strings.
    /// </summary>
    public class PhpStringBuilder
    {
        #region Fields & Properties

        ///// <summary>
        ///// Bit mask of chunks containing <see cref="BinaryString"/> or <see cref="StringBuilder"/>, otherwise it is <see cref="string"/>.
        ///// </summary>
        //ulong _writable = 0;

        /// <summary>
        /// Max count of chunks.
        /// Adding chunks to full builder causes all chunks to be concatenated.
        /// </summary>
        const int MaxChunks = sizeof(ulong);

        #endregion

        #region Construction

        // from builder, binary, unicode

        #endregion

        #region Operations

        // Append
        // Prepend
        // this[] { get; set; }

        #endregion
    }
}
