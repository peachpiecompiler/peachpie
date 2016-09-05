using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Utilities
{
    static class PathUtils
    {
        public const char DirectorySeparator = '\\';
        public const char AltDirectorySeparator = '/';

        public static bool IsDirectorySeparator(this char ch) => ch == DirectorySeparator || ch == AltDirectorySeparator;
    }
}
