using System;
using System.Collections.Generic;
using System.Text;

namespace Peachpie.NET.Sdk.Versioning
{
    /// <summary>
    /// Package reference floating version.
    /// </summary>
    public class FloatingVersion
    {
        /// <summary></summary>
        public ComposerVersion LowerBound { get; set; }

        /// <summary></summary>
        public bool LowerBoundExclusive { get; set; }

        /// <summary></summary>
        public ComposerVersion UpperBound { get; set; }

        /// <summary></summary>
        public bool UpperBoundExclusive { get; set; }

        /// <summary>
        /// Gets corresponding package floating version string.
        /// </summary>
        public override string ToString()
        {
            if (LowerBound.HasValue || UpperBound.HasValue)
            {
                var value = new StringBuilder();

                if (!UpperBound.HasValue && !LowerBoundExclusive)
                {
                    // version
                    return $"[{LowerBound},]";// + (LowerBound.Stability != null ? "-*" : "");
                }

                value.Append(LowerBoundExclusive ? '(' : '[');

                if (LowerBound.HasValue)
                {
                    value.Append(LowerBound.ToString());
                    if (LowerBound.Stability != null)
                    {
                        if (LowerBound.PartsCount == 0) value.Append('*');
                        //value.Append("-*");
                    }

                    if (UpperBound.HasValue && !LowerBoundExclusive && LowerBound == UpperBound)
                    {
                        // [version]
                        value.Append("]");
                        return value.ToString();
                    }
                }

                value.Append(',');

                if (UpperBound.HasValue)
                {
                    value.Append(UpperBound.ToString());

                    if (UpperBound.Stability != null)
                    {
                        if (UpperBound.PartsCount == 0) value.Append('*');
                        //value.Append("-*");
                    }
                }

                value.Append(UpperBoundExclusive ? ')' : ']');

                // [lower,upper]
                return value.ToString();
            }
            else
            {
                return "*";
            }
        }
    }
}
