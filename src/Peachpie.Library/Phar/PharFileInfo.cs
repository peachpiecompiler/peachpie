using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.Core;
using Pchp.Library.Spl;

namespace Pchp.Library.Phar
{
    /// <summary>
    /// The <see cref="PharFileInfo"/> class provides a high-level interface to the contents and attributes of a single file within a phar archive.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    [PhpExtension(PharExtension.ExtensionName)]
    public sealed class PharFileInfo : SplFileInfo
    {
        public PharFileInfo(Context ctx, string filename) : base(ctx, filename)
        {
            throw new NotImplementedException();
        }
    }
}
