using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Core.Dynamic
{
    /// <summary>
    /// Conversion value used for overload resolution.
    /// </summary>
    [Flags]
    public enum ConversionCost : ushort
    {
        /// <summary>
        /// No conversion is needed. Best case.
        /// </summary>
        Pass = 0,

        /// <summary>
        /// The operation is costly but the value is kept without loosing precision.
        /// </summary>
        PassCostly = 1,

        /// <summary>
        /// Conversion using implicit cast without loosing precision.
        /// </summary>
        ImplicitCast = 2,

        /// <summary>
        /// Conversion using explicit cast that may loose precision.
        /// </summary>
        LoosingPrecision = 4,

        /// <summary>
        /// Conversion is possible but the value is lost and warning should be generated.
        /// </summary>
        Warning = 8,

        /// <summary>
        /// Implicit value will be used, argument is missing and parameter is optional.
        /// </summary>
        DefaultValue = 16,

        /// <summary>
        /// Too many arguments provided. Arguments will be omitted.
        /// </summary>
        TooManyArgs = 32,

        /// <summary>
        /// Missing mandatory arguments, default values will be used instead.
        /// </summary>
        MissingArgs = 64,

        /// <summary>
        /// Conversion does not exist.
        /// </summary>
        NoConversion = 128,

        /// <summary>
        /// Unspecified error.
        /// </summary>
        Error = 256,
    }
}
