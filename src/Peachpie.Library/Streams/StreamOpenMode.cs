using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library.Streams
{
    /// <summary>
	/// Shortcuts for the short overload of PhpStream.Open
	/// </summary>
	[Flags]
    public enum StreamOpenMode
    {
        /// <summary>Open for reading</summary>
        Read = ReadText,
        /// <summary>Open for writing</summary>
        Write = WriteText,
        /// <summary>Open for reading (text mode)</summary>
        ReadText = 0,
        /// <summary>Open for writing (text mode)</summary>
        WriteText = 1,
        /// <summary>Open for reading (binary mode)</summary>
        ReadBinary = 2,
        /// <summary>Open for writing (binary mode)</summary>
        WriteBinary = 3
    }
}
