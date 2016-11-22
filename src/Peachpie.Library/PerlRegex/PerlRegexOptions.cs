using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library.PerlRegex
{
    /// <summary>
    /// Perl regular expression specific options.
    /// </summary>
    [Flags]
    internal enum PerlRegexOptions
    {
        None = 0,

        PCRE_CASELESS = 1,      // i
        PCRE_MULTILINE = 2,     // m
        PCRE_DOTALL = 4,        // s
        PCRE_EXTENDED = 8,      // x
        PCRE_ANCHORED = 16,     // A
        PCRE_DOLLAR_ENDONLY = 32,   // D
        PCRE_UNGREEDY = 64,     // U
        PCRE_UTF8 = 128,        // u
        PCRE_EXTRA = 256,       // X

        /// <summary>
        /// Spend more time studying the pattern - ignoring.
        /// </summary>
        PCRE_S = 512,           // S

        ///// <summary>
        ///// Evaluate as PHP code.
        ///// Deprecated and removed.
        ///// </summary>
        //PREG_REPLACE_EVAL = 4096,        // e

        /// <summary>
        /// An unknown option.
        /// </summary>
        Unknown = 8192,
    }
}
