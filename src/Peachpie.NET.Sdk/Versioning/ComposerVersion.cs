using System;
using System.Collections.Generic;
using System.Text;

namespace Peachpie.NET.Sdk.Versioning
{
    /// <summary>
    /// Single composer version.
    /// </summary>
    public struct ComposerVersion
    {
        /// <summary>
        /// Version corresponding to <c>"*"</c>.
        /// </summary>
        public static ComposerVersion Any => new ComposerVersion { Major = -1, Minor = -1, Build = -1, };

        /// <summary>
        /// Major version component/
        /// Value of <c>-1</c> represents not specified version component.
        /// </summary>
        public int Major { get; set; }

        /// <summary>
        /// Minor version component.
        /// Value of <c>-1</c> represents not specified version component.
        /// </summary>
        public int Minor { get; set; }

        /// <summary>
        /// Build number component.
        /// Value of <c>-1</c> represents not specified version component.
        /// </summary>
        public int Build { get; set; }

        /// <summary>
        /// Stability flag.
        /// </summary>
        public string Stability { get; set; }

        /// <summary>
        /// What parts of the version are specified.
        /// </summary>
        public int PartsCount => Major < 0 ? 0 : Minor < 0 ? 1 : Build < 0 ? 2 : 3;

        /// <summary>
        /// Gets value indicating the value is specified.
        /// </summary>
        public bool HasValue => Major != 0 || Minor != 0 || Build != 0 || Stability != null;

        /// <summary>Returns string representation of the version.</summary>
        public override string ToString()
        {
            return PartsCount switch
            {
                0 => "",
                1 => $"{Major}",
                2 => $"{Major}.{Minor}",
                3 => $"{Major}.{Minor}.{Build}",
                _ => throw new ArgumentException(),
            };
        }
    }
}
