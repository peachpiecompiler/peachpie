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

        public PharData(string fname, int flags = 0, string alias = default, int format = 0)
        {
            __construct(fname, flags, alias, format);
        }

        public void __construct(string fname, int flags = 0, string alias = default, int format = 0)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
