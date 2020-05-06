using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Devsense.PHP.Errors;

namespace Peachpie.NET.Sdk.Versioning
{
    /// <summary>
    /// Version expression operators.
    /// </summary>
    public enum Operation
    {
        /// <summary>
        /// Nop.
        /// </summary>
        None,

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
        /// ~A
        /// </summary>
        TildeVersionRange,

        /// <summary>
        /// ^A
        /// </summary>
        CaretVersionRange,

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
            public static ReadOnlySpan<char> Range => " - ".AsSpan();
            public static ReadOnlySpan<char> Or => "||".AsSpan();
            public const char Lt = '<';
            public static ReadOnlySpan<char> Lte => "<=".AsSpan();
            public const char Gt = '>';
            public static ReadOnlySpan<char> Gte => ">=".AsSpan();
            public static ReadOnlySpan<char> Ne => "!=".AsSpan();
            public const char TildeVersion = '~';
            public const char CaretVersion = '^';
            public static readonly Regex AndRegex = new Regex(@"[^|]+([,\s]+)[^|]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        /// <summary>
        /// Expression operation.
        /// </summary>
        public abstract Operation Operation { get; }

        /// <summary>
        /// Parses the version expression.
        /// </summary>
        public static bool TryParse(string value, out ComposerVersionExpression expression) => TryParse(value.AsSpan(), out expression);

        /// <summary>
        /// Parses the version expression.
        /// </summary>
        public static bool TryParse(ReadOnlySpan<char> value, out ComposerVersionExpression expression)
        {
            expression = default;
            value = value.Trim();

            // ranges
            var range = value.IndexOf(Tokens.Range); // regexp
            if (range > 0)
            {
                // A - B
                if (ComposerVersion.TryParse(value.Slice(0, range), out var fromversion) &&
                    ComposerVersion.TryParse(value.Slice(range + Tokens.Range.Length), out var toversion))
                {
                    expression = new RangeExpression { From = fromversion, To = toversion, };
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (range == 0)
            {
                // - B
                return false;
            }

            // AND, highest precedence
            var and = Tokens.AndRegex.Match(value.ToString());
            if (and.Success)
            {
                var cap = and.Groups[1];

                if (TryParse(value.Slice(0, cap.Index), out var leftexpr) &&
                    TryParse(value.Slice(cap.Index + cap.Length), out var rightexpr))
                {
                    expression = new AndExpression { Left = leftexpr, Right = rightexpr, };
                    return true;
                }
            }

            // OR
            var or = value.IndexOf(Tokens.Or);
            if (or > 0)
            {
                // A || B
                if (TryParse(value.Slice(0, or), out var left) &&
                    TryParse(value.Slice(or + Tokens.Or.Length), out var right))
                {
                    expression = new OrExpression { Left = left, Right = right, };
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (or == 0)
            {
                // invalid
                return false;
            }

            //
            if (value.IsEmpty)
            {
                return false;
            }

            // unary version ranges
            Operation op;

            if (value[0] == Tokens.CaretVersion)
            {
                op = Operation.CaretVersionRange;
                value = value.Slice(1);
            }
            else if (value[0] == Tokens.TildeVersion)
            {
                op = Operation.TildeVersionRange;
                value = value.Slice(1);
            }
            else if (value.StartsWith(Tokens.Lte))
            {
                op = Operation.LessThanOrEqual;
                value = value.Slice(Tokens.Lte.Length);
            }
            else if (value.StartsWith(Tokens.Gte))
            {
                op = Operation.GreaterThanOrEqual;
                value = value.Slice(Tokens.Gte.Length);
            }
            else if (value.StartsWith(Tokens.Ne))
            {
                op = Operation.NotEqual;
                value = value.Slice(Tokens.Ne.Length);
            }
            else if (value[0] == Tokens.Lt)
            {
                op = Operation.LessThan;
                value = value.Slice(1);
            }
            else if (value[0] == Tokens.Gt)
            {
                op = Operation.GreaterThan;
                value = value.Slice(1);
            }
            else
            {
                op = Operation.Exact;
            }

            if (ComposerVersion.TryParse(value, out var version))
            {
                if (op == Operation.Exact)
                {
                    expression = new ExactVersionExpression { Version = version };
                }
                else
                {
                    expression = new UnaryExpression(op) { Version = version, };
                }

                return true;
            }

            //
            return false;
        }

        /// <summary>
        /// Evaluates the expression to a corresponding PackageReference floating version.
        /// </summary>
        public abstract FloatingVersion Evaluate();
    }

    [DebuggerDisplay("{Operation} {Version}")]
    class UnaryExpression : ComposerVersionExpression
    {
        public override Operation Operation => _op;
        readonly Operation _op;

        public UnaryExpression(Operation op)
        {
            _op = op;
        }

        public ComposerVersion Version { get; set; }

        public override FloatingVersion Evaluate()
        {
            if (Version.IsAnyMajor)
            {
                // *
                return new FloatingVersion();
            }

            switch (Operation)
            {
                case Operation.TildeVersionRange:
                    return new FloatingVersion
                    {
                        LowerBound = Version.AnyToZero().WithStabilityFlag(Version.Stability ?? ComposerVersion.StabilityDev),
                        UpperBound = Version.IsAnyBuild
                            ? new ComposerVersion(Version.Major + 1, 0, 0) { Stability = Version.Stability ?? ComposerVersion.StabilityDev }
                            : new ComposerVersion(Version.Major, Version.Minor + 1, 0) { Stability = Version.Stability ?? ComposerVersion.StabilityDev },
                        UpperBoundExclusive = true,
                    };

                case Operation.CaretVersionRange:
                    return new FloatingVersion
                    {
                        LowerBound = Version.AnyToZero(),
                        UpperBound = Version.Major >= 1
                            ? new ComposerVersion(Version.Major + 1, 0, 0) { Stability = Version.Stability }
                            : Version.GetClosestHigher(),
                        UpperBoundExclusive = true,
                    };

                case Operation.LessThan:
                    return new FloatingVersion
                    {
                        UpperBound = Version.AnyToZero().WithStabilityFlag(Version.Stability ?? ComposerVersion.StabilityDev),
                        UpperBoundExclusive = true,
                    };

                case Operation.LessThanOrEqual:
                    return new FloatingVersion
                    {
                        UpperBound = Version.AnyToZero(),
                    };

                case Operation.NotEqual:
                // we can only have one range
                // gt
                case Operation.GreaterThan:
                    return new FloatingVersion
                    {
                        LowerBound = Version.AnyToZero(),
                        LowerBoundExclusive = true,
                    };

                case Operation.GreaterThanOrEqual:
                    return new FloatingVersion
                    {
                        LowerBound = Version.AnyToZero().WithStabilityFlag(Version.Stability ?? ComposerVersion.StabilityDev),
                    };

                default:
                    throw new InvalidOperationException(Operation.ToString());
            }
        }
    }

    [DebuggerDisplay("{Left} {Operation} {Right}")]
    abstract class BinaryExpression : ComposerVersionExpression
    {
        public ComposerVersionExpression Left { get; set; }

        public ComposerVersionExpression Right { get; set; }

        public override FloatingVersion Evaluate() => Evaluate(Left.Evaluate(), Right.Evaluate());

        protected abstract FloatingVersion Evaluate(FloatingVersion x, FloatingVersion y);
    }

    sealed class AndExpression : BinaryExpression
    {
        public override Operation Operation => Operation.And;

        protected override FloatingVersion Evaluate(FloatingVersion x, FloatingVersion y)
        {
            var result = new FloatingVersion();

            // LowerBound = Max(x.LowerBound, y.LowerBound)
            var lowerx = x.LowerBound.AnyToZero();
            var lowery = y.LowerBound.AnyToZero();

            var lowercomp = lowerx.CompareTo(lowery);
            if (lowercomp > 0 || (lowercomp == 0 && x.LowerBoundExclusive))
            {
                result.LowerBound = lowerx;
                result.LowerBoundExclusive = x.LowerBoundExclusive;
            }
            else
            {
                result.LowerBound = lowery;
                result.LowerBoundExclusive = y.LowerBoundExclusive;
            }

            // UpperBound = Min(x.UpperBound, y.UpperBound)
            var upperx = x.UpperBound.GetClosestHigher();
            var uppery = y.UpperBound.GetClosestHigher();

            var uppercomp = upperx.CompareTo(uppery);
            if (uppercomp < 0 || (uppercomp == 0 && x.UpperBoundExclusive))
            {
                result.UpperBound = x.UpperBound;
                result.UpperBoundExclusive = x.UpperBoundExclusive;
            }
            else
            {
                result.UpperBound = y.UpperBound;
                result.UpperBoundExclusive = y.UpperBoundExclusive;
            }

            //
            return result;
        }
    }

    sealed class OrExpression : BinaryExpression
    {
        public override Operation Operation => Operation.Or;

        protected override FloatingVersion Evaluate(FloatingVersion x, FloatingVersion y)
        {
            // cannot be translated,
            // so we'll just merge the ranges

            var result = new FloatingVersion();

            // LowerBound = Max(x.LowerBound, y.LowerBound)
            var lowerx = x.LowerBound.AnyToZero();
            var lowery = y.LowerBound.AnyToZero();

            var lowercomp = lowerx.CompareTo(lowery);
            if (lowercomp < 0 || (lowercomp == 0 && !x.LowerBoundExclusive))
            {
                result.LowerBound = lowerx;
                result.LowerBoundExclusive = x.LowerBoundExclusive;
            }
            else
            {
                result.LowerBound = lowery;
                result.LowerBoundExclusive = y.LowerBoundExclusive;
            }

            // UpperBound = Min(x.UpperBound, y.UpperBound)
            var upperx = x.UpperBound.GetClosestHigher();
            var uppery = y.UpperBound.GetClosestHigher();

            var uppercomp = upperx.CompareTo(uppery);
            if (uppercomp > 0 || (uppercomp == 0 && !x.UpperBoundExclusive))
            {
                result.UpperBound = x.UpperBound;
                result.UpperBoundExclusive = x.UpperBoundExclusive;
            }
            else
            {
                result.UpperBound = y.UpperBound;
                result.UpperBoundExclusive = y.UpperBoundExclusive;
            }

            //
            return result;
        }
    }

    [DebuggerDisplay("{Version}")]
    sealed class ExactVersionExpression : ComposerVersionExpression
    {
        public override Operation Operation => Operation.Exact;

        public ComposerVersion Version { get; set; }

        public override FloatingVersion Evaluate()
        {
            // has asterisks?
            if (Version.IsAnyMajor)
            {
                // *
                return new FloatingVersion();
            }

            if (Version.IsAnyMinor || Version.IsAnyBuild)
            {
                return new FloatingVersion
                {
                    LowerBound = Version.AnyToZero().WithStabilityFlag(Version.Stability ?? ComposerVersion.StabilityDev),
                    UpperBound = Version.GetClosestHigher().WithStabilityFlag(Version.Stability ?? ComposerVersion.StabilityDev),
                    UpperBoundExclusive = true,
                };
            }
            else
            {
                return new FloatingVersion
                {
                    LowerBound = Version,
                    UpperBound = Version,
                };
            }
        }
    }

    [DebuggerDisplay("{From} - {To}")]
    sealed class RangeExpression : ComposerVersionExpression
    {
        public override Operation Operation => Operation.Range;

        public ComposerVersion From { get; set; }

        public ComposerVersion To { get; set; }

        public override FloatingVersion Evaluate()
        {
            return new FloatingVersion
            {
                LowerBound = From.AnyToZero().WithStabilityFlag(From.Stability ?? ComposerVersion.StabilityDev),
                UpperBound = To.GetClosestHigher(),
                UpperBoundExclusive = true,
            };
        }
    }
}
