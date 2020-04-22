using System;
using System.Collections.Generic;
using System.Text;

namespace Peachpie.NET.Sdk.Versioning
{
    /// <summary>
    /// Version expression operators.
    /// </summary>
    public enum Operation
    {
        /// <summary>
        /// Exact version.
        /// </summary>
        Exact,

        /// <summary>
        /// Logical and: ` ` or `,`.
        /// </summary>
        And,

        /// <summary>
        /// Logical or: `||`.
        /// </summary>
        Or,

        /// <summary>
        /// Version range: `A - B`
        /// </summary>
        Range,

        /// <summary>
        /// `&gt;`
        /// </summary>
        GreaterThan,

        /// <summary>
        /// `&gt;=`
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// `&lt;`
        /// </summary>
        LessThan,

        /// <summary>
        /// `&lt;=`
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// `!=`
        /// </summary>
        NotEqual,
    }

    /// <summary>
    /// Version constraint expression.
    /// </summary>
    public abstract class ComposerVersionExpression
    {
        struct Tokens
        {
            public const char Wildcard = '*';
            public const char VersionSeparator = '.';
            public const char StabilitySeparator = '-';
            public const string Range = " - ";
            public readonly static char[] And = new[] { ' ', ',' };
            public const string Or = "||";
            public const char Lt = '<';
            public const string Lte = "<=";
            public const char Gt = '>';
            public const string Gte = ">=";
            public const string Ne = "!=";
        }

        /// <summary>
        /// Expression operation.
        /// </summary>
        public abstract Operation Operation { get; }

        /// <summary>
        /// Parses the version expression.
        /// </summary>
        public static bool TryParse(string value, out ComposerVersionExpression expression)
        {
            throw new NotImplementedException();
        }
    }
}
