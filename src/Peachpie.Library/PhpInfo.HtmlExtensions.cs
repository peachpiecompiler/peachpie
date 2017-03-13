using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library
{
    partial class PhpInfo
    {
        private static HtmlTagWriter Tag(this Context ctx, string tag, object attributes = null)
        {
            return new HtmlTagWriter(ctx, tag, attributes);
        }

        private static HtmlTagWriter Tag(this HtmlTagWriter tagWriter, string tag, object attributes = null)
        {
            return new HtmlTagWriter(tagWriter.Context, tag, attributes);
        }
    }
}
