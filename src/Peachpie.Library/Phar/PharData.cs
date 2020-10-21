using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.Core;
using Pchp.Library.Spl;

namespace Pchp.Library.Phar
{
    /// <summary>
    /// The <see cref="PharData"/> class provides a high-level interface to accessing and creating non-executable tar and zip archives.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    [PhpExtension(PharExtension.ExtensionName)]
    public sealed class PharData /* : RecursiveDirectoryIterator*/
    {
        #region .ctor

        public PharData(string filename, int flags = 0, string alias = default, int fileformat = 0)
        {
            __construct(filename, flags, alias, fileformat);
        }

        public void __construct(string filename, int flags = 0, string alias = default, int fileformat = 0)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
