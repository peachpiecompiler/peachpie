// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Pchp.Library.PerlRegex
{
    [Flags]
    internal enum RegexOptions
    {
        None                    = 0x0000,
        IgnoreCase              = 0x0001, // "i"
        Multiline               = 0x0002, // "m"
        ExplicitCapture         = 0x0004, // "n"
        Compiled                = 0x0008, // "c"
        Singleline              = 0x0010, // "s"
        IgnorePatternWhitespace = 0x0020, // "x"
        RightToLeft             = 0x0040, // "r"

#if DEBUG
        Debug                   = 0x0080, // "d"
#endif

        ECMAScript              = 0x0100, // "e"
        CultureInvariant        = 0x0200,

        //
        // Perl regular expression specific options.
        //
        
        PCRE_CASELESS = IgnoreCase,         // i
        PCRE_MULTILINE = Multiline,         // m
        PCRE_DOTALL = Singleline,           // s
        PCRE_EXTENDED = IgnorePatternWhitespace,      // x
        PCRE_ANCHORED = 0x0400,             // A
        PCRE_DOLLAR_ENDONLY = 0x0800,       // D
        PCRE_UNGREEDY = 0x1000,             // U
        PCRE_UTF8 = 0x2000,                 // u
        PCRE_EXTRA = 0x3000,                // X

        /// <summary>
        /// Spend more time studying the pattern - ignoring.
        /// </summary>
        PCRE_S = 0x4000,                    // S

        ///// <summary>
        ///// Evaluate as PHP code.
        ///// Deprecated and removed.
        ///// </summary>
        //PREG_REPLACE_EVAL = 0x8000,        // e
    }
}
