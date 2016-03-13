using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Well known types used by runtime.
    /// </summary>
    public enum PhpTypeCode : int
    {
        Unknown = 0,

        Long,
        Double,
        Boolean,
        PhpNumber,
        PhpArray,
        String,
        ByteString,
        PhpStringBuilder,
        Void,
        Object,
        Closure,
        PhpValue,
        PhpAlias,
    }

    /// <summary>
    /// Helper class providing methods for <see cref="PhpTypeCode"/>.
    /// </summary>
    public static class PhpTypeCodes
    {

    }
}
