using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Utilities;
using Pchp.Library.Spl;
using Rationals;

namespace Pchp.Library
{
    [PhpExtension("bcmath")]
    public static class BCMath
    {
        sealed class BCMathOptions
        {
            public const int DefaultScale = 0;

            /// <summary>
            /// <c>bcscale()</c> value.
            /// </summary>
            public int Scale { get; set; } = DefaultScale;
        }

        const int MaxScale = 20;

        static int GetCurrentScale(Context ctx)
        {
            return ctx.TryGetStatic<BCMathOptions>(out var options)
                ? options.Scale
                : BCMathOptions.DefaultScale;
        }

        static void SetCurrentScale(Context ctx, int scale)
        {
            ctx.GetStatic<BCMathOptions>().Scale = Math.Min(Math.Max(scale, 0), MaxScale);
        }

        static string ToString(Rational num, int? scale = default)
        {
            var remainingscale = scale.GetValueOrDefault();

            var numerator = BigInteger.Abs(num.Numerator);
            var denominator = BigInteger.Abs(num.Denominator);
            var hasdecimal = false;

            var result = StringBuilderUtilities.Pool.Get();

            if (num.Sign < 0)
            {
                result.Append('-');
            }

            for (; ; )
            {
                var digits = BigInteger
                    .DivRem(numerator, denominator, out var rem)
                    .ToString(NumberFormatInfo.InvariantInfo);

                result.Append(digits);

                if (remainingscale <= 0)
                {
                    break;
                }

                // .
                if (!hasdecimal)
                {
                    hasdecimal = true;
                    result.Append(NumberFormatInfo.InvariantInfo.NumberDecimalSeparator);
                }

                // done?
                if (rem.IsZero)
                {
                    result.Append('0', remainingscale);
                    break;
                }

                // next decimals
                numerator = rem * 10;
                remainingscale--;
            }

            //
            return StringBuilderUtilities.GetStringAndReturn(result);
        }

        static Rational Parse(string num)
        {
            if (!Rational.TryParseDecimal(num, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var value))
            {
                // Warning: bcmath function argument is not well-formed
                PhpException.InvalidArgument("num", Resources.Resources.bcmath_wrong_argument);

                value = Rational.Zero;
            }

            return value;
        }

        /// <summary>
        /// Add two arbitrary precision numbers.
        /// </summary>
        public static string bcadd(string num1, string num2, int? scale = default) => ToString(Parse(num1) + Parse(num2), scale);

        /// <summary>
        /// Subtract one arbitrary precision number from another.
        /// </summary>
        public static string bcsub(string num1, string num2, int? scale = default) => ToString(Parse(num1) - Parse(num2), scale);

        /// <summary>
        /// Compare two arbitrary precision numbers.
        /// </summary>
        public static int bccomp(string num1, string num2, int? scale = default) => Parse(num1).CompareTo(Parse(num2));

        /// <summary>
        /// Multiply two arbitrary precision numbers.
        /// </summary>
        public static string bcmul(string num1, string num2, int? scale = default) => ToString(Parse(num1) * Parse(num2), scale);

        /// <summary>
        /// Divide two arbitrary precision numbers.
        /// </summary>
        public static string bcdiv(string num1, string num2, int? scale = default) => ToString(Parse(num1) / Parse(num2), scale);

        ///// <summary>
        ///// Get modulus of an arbitrary precision number.
        ///// </summary>
        //TODO: public static string bcmod(string num1, string num2, int? scale = default) => ToString(Parse(num1) % Parse(num2), scale);

        /// <summary>
        /// Raise an arbitrary precision number to another.
        /// </summary>
        public static string bcpow(string num, string exponent, int? scale = default) => ToString(Rational.Pow(Parse(num), int.Parse(exponent)), scale);

        /// <summary>
        /// Get the square root of an arbitrary precision number.
        /// </summary>
        public static string bcsqrt(string num, int? scale = default) => ToString(Rational.RationalRoot(Parse(num), 2), scale);

        /// <summary>
        /// Set or get default scale parameter for all bc math functions.
        /// </summary>
        public static int bcscale(Context ctx, int? scale = default)
        {
            var oldscale = GetCurrentScale(ctx);
            if (scale.HasValue)
            {
                SetCurrentScale(ctx, scale.GetValueOrDefault());
            }
            return oldscale;
        }

        //TODO: function bcpowmod
    }
}
