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
        public bool LowerBoundInclusive { get; set; }

        /// <summary></summary>
        public ComposerVersion UpperBound { get; set; }

        /// <summary></summary>
        public bool UpperBoundInclusive { get; set; }

        // And
        // Or
    }
}
