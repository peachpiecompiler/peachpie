using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Well known types.
    /// </summary>
    public enum PhpTypeCode : int
    {
        Unknown = 0,

        Long,
        Double,
        Boolean,
        PhpArray,
        String,
        ByteString,
        PhpStringBuilder,
        Void,
        Object,
        Closure,
    }

    /// <summary>
    /// Helper class providing methods for <see cref="PhpTypeCode"/>.
    /// </summary>
    public static class PhpTypeCodes
    {

    }
}
