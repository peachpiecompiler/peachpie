using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PHP.Core
{
    #region Language Features Enum

    /// <summary>
    /// PHP language features supported by Phalanger.
    /// </summary>
    [Flags]
    public enum LanguageFeatures
    {
        /// <summary>
        /// Basic features - always present.
        /// </summary>
        Basic = 0,

        /// <summary>
        /// Allows using short open tags in the script.
        /// </summary>
        ShortOpenTags = 1,

        /// <summary>
        /// Allows using ASP tags.
        /// </summary>
        AspTags = 2,

        /// <summary>
        /// Enables PHP5 keywords such as <c>private</c>, <c>protected</c>, <c>public</c>, <c>clone</c>, <c>goto</c>, , etc.
        /// Enables namespaces.
        /// </summary>
        V5Keywords = 4,

        /// <summary>
        /// Enables primitive type keywords <c>bool</c>, <c>int</c>, <c>int64</c>, <c>double</c>, <c>string</c>,
        /// <c>object</c>, <c>resource</c>.
        /// </summary>
        TypeKeywords = 8,

        /// <summary>
        /// Enables Unicode escapes in strings (\U, \u, \C).
        /// </summary>
        UnicodeSemantics = 32,

        /// <summary>
        /// Allows to treat values of PHP types as CLR objects (e.g. $s = "string"; $s->GetHashCode()).
        /// </summary>
        ClrSemantics = 64,

        /// <summary>
        /// Enables PHP keywords that may be used in C# as class or namespace name, to be used in PHP code too.
        /// E.g. "List", "Array", "Abstract", ... would not be treated as syntax error when used as a <c>namespace_name_identifier</c> token.
        /// </summary>
        CSharpTypeNames = 128,

        /// <summary>
        /// Features enabled by default in the standard mode. Corresponds to the currently supported version of PHP.
        /// </summary>
        Default = Php5,

        /// <summary>
        /// Features enabled by default in the pure mode. Corresponds to the PHP/CLR language.
        /// </summary>
        PureModeDefault = PhpClr,

        Php4 = ShortOpenTags,
        Php5 = Php4 | V5Keywords,
        PhpClr = Php5 | UnicodeSemantics | TypeKeywords | ClrSemantics | AspTags | CSharpTypeNames
    }

    #endregion
}
