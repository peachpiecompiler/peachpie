using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;

namespace Pchp.Library.Streams
{
    internal sealed class ZlibFilterFactory : IFilterFactory
    {
        public bool GetImplementedFilter(string name, bool instantiate, out PhpFilter instance, PhpValue parameters)
        {
            instance = null;

            switch (name)
            {
                case "zlib.deflate":
                    if (instantiate) instance = new DeflateFilter(-1, DeflateFilterMode.Normal);
                    return true;
                case "zlib.inflate":
                    if (instantiate) instance = new InflateFilter();
                    return true;
            }

            return false;
        }

        public string[] GetImplementedFilterNames()
        {
            return new string[] { "zlib.deflate", "zlib.inflate" };
        }
    }
}
