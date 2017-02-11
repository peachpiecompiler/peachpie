using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library.Json
{
    internal struct Position
    {
        public int Start, Length;

        public Position(int p) : this(p, 0) { }
        public Position(int p, int length) { Start = p; Length = length; }
    }
}
