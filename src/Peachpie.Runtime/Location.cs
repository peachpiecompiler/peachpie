using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Provides location in the source code, source file, containing line and column.
    /// </summary>
    public struct Location
    {
        /// <summary>
        /// Source file path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Containing line number.
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Containing solumn number.
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// Initializes location.
        /// </summary>
        /// <param name="path">Source file path.</param>
        /// <param name="line">Containing line number.</param>
        /// <param name="col">Column number.</param>
        public Location(string path, int line, int col)
        {
            Debug.Assert(path != null);
            Debug.Assert(line >= 0);
            Debug.Assert(col >= 0);

            this.Path = path;
            this.Line = line;
            this.Column = col;
        }
    }
}
