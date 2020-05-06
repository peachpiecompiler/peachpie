using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public string ToString(bool forcePreRelease)
        {
            if (LowerBound.HasValue || UpperBound.HasValue)
            {
                var value = new StringBuilder();
                var suffix = (forcePreRelease || (LowerBound.HasValue ? LowerBound.IsPreRelease : UpperBound.IsPreRelease))
                    ? "-*"
                    : "";

                if (!UpperBound.HasValue && !LowerBoundExclusive)
                {
                    // minimum version
                    return $"[{LowerBound.AnyToZero()}{suffix},]";
                }

                value.Append(LowerBoundExclusive ? '(' : '[');

                if (LowerBound.HasValue)
                {
                    value.Append(LowerBound.AnyToZero().ToString());
                    value.Append(suffix);

                    if (UpperBound.HasValue && !LowerBoundExclusive && LowerBound == UpperBound)
                    {
                        // exact [version]
                        value.Append(suffix.Length == 0 ? "]" : ",]");
                        return value.ToString();
                    }
                }

                value.Append(',');

                if (UpperBound.HasValue)
                {
                    // upper bound does not allow asteriks
                    Debug.Assert(!UpperBound.IsAnyMajor && !UpperBound.IsAnyMinor && !UpperBound.IsAnyBuild);

                    value.Append(UpperBound.ToString());
                }

                value.Append(UpperBoundExclusive ? ')' : ']');

                // [lower,upper]
                return value.ToString();
            }
            else
            {
                //
                return forcePreRelease ? "0.0.0-*" : "*";
            }
        }

        /// <summary>
        /// Gets corresponding package floating version string.
        /// </summary>
        public override string ToString() => ToString(forcePreRelease: false);
    }
}
