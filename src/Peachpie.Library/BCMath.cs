#nullable enable

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
using static Pchp.Library.StandardPhpOptions;

namespace Pchp.Library
{
    [PhpExtension(BCMath.ExtensionName, Registrator = typeof(Registrator))]
    public static class BCMath
    {
        const string ExtensionName = "bcmath";

        #region BCMathConfig, BCMathOptions

        sealed class Registrator
        {
            public Registrator()
            {
                Context.RegisterConfiguration(new BCMathConfig());

                Register<BCMathConfig>(BCMathOptions.Scale, BCMath.ExtensionName,
                    (local) => local.Scale,
                    (local, value) => local.Scale = Math.Max(0, (int)value));
            }
        }

        sealed class BCMathConfig : IPhpConfiguration
        {
            public string ExtensionName => BCMath.ExtensionName;

            /// <summary>
            /// <c>bcmath.scale()</c> option value.
            /// </summary>
            public int Scale { get; set; } = 0;

            public IPhpConfiguration Copy() => new BCMathConfig { Scale = Scale, };
        }

        struct BCMathOptions
        {
            public static string Scale => "bcmath.scale";
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Gets the scale to be used by function.
        /// </summary>
        static int GetScale(Context ctx, int? scale) => scale.HasValue ? scale.GetValueOrDefault() : GetCurrentScale(ctx);

        static int GetCurrentScale(Context ctx) => ctx.Configuration.Get<BCMathConfig>().Scale;

        static void SetCurrentScale(Context ctx, int scale) => ctx.Configuration.Get<BCMathConfig>().Scale = Math.Max(scale, 0);

        static string ToString(Rational num, int scale)
        {
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
                var number = BigInteger.DivRem(numerator, denominator, out var rem);
                if (number >= 10)
                {
                    var digits = number.ToString(NumberFormatInfo.InvariantInfo);
                    result.Append(digits);
                }
                else
                {
                    Debug.Assert(number >= 0);
                    result.Append((char)('0' + (int)number));
                }

                if (scale <= 0)
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
                    result.Append('0', scale);
                    break;
                }

                // next decimals
                numerator = rem * 10;
                scale--;
            }

            //
            return StringBuilderUtilities.GetStringAndReturn(result);
        }

        static Rational Trunc(this Rational num) => num.Sign >= 0 ? num.WholePart : (-(-num).WholePart);

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

        #endregion

        /// <summary>
        /// Add two arbitrary precision numbers.
        /// </summary>
        public static string bcadd(Context ctx, string num1, string num2, int? scale = default) => ToString(Parse(num1) + Parse(num2), GetScale(ctx, scale));

        /// <summary>
        /// Subtract one arbitrary precision number from another.
        /// </summary>
        public static string bcsub(Context ctx, string num1, string num2, int? scale = default) => ToString(Parse(num1) - Parse(num2), GetScale(ctx, scale));

        /// <summary>
        /// Compare two arbitrary precision numbers.
        /// </summary>
        public static int bccomp(Context ctx, string num1, string num2, int? scale = default) => Parse(num1).CompareTo(Parse(num2));

        /// <summary>
        /// Multiply two arbitrary precision numbers.
        /// </summary>
        public static string bcmul(Context ctx, string num1, string num2, int? scale = default) => ToString(Parse(num1) * Parse(num2), GetScale(ctx, scale));

        /// <summary>
        /// Divide two arbitrary precision numbers.
        /// </summary>
        public static string bcdiv(Context ctx, string num1, string num2, int? scale = default) => ToString(Parse(num1) / Parse(num2), GetScale(ctx, scale));

        /// <summary>
        /// Get modulus of an arbitrary precision number.
        /// </summary>
        /// <returns>
        /// Get the remainder of dividing <paramref name="num1"/> by <paramref name="num2"/>.
        /// Unless <paramref name="num2"/> is zero, the result has the same sign as <paramref name="num1"/>.
        /// </returns>
        public static string bcmod(Context ctx, string num1, string num2, int? scale = default)
        {
            var a = Parse(num1);
            var b = Parse(num2);

            if (b.IsZero)
            {
                throw new DivisionByZeroError("Modulo by zero");
                // return null; // PHP < 8
            }

            var result = bcmod(a, b);

            return ToString(result, GetScale(ctx, scale));
        }

        static Rational bcmod(Rational num, Rational mod)
        {
            if (mod.IsZero)
            {
                throw new DivisionByZeroError("Modulo by zero");
                // return null; // PHP < 8
            }

            if (num.IsZero)
            {
                return Rational.Zero;
            }
            else
            {
                return num - Trunc(num / mod) * mod;
            }
        }

        /// <summary>
        /// Raise an arbitrary precision number to another.
        /// </summary>
        public static string bcpow(Context ctx, string num, string exponent, int? scale = default) => ToString(Rational.Pow(Parse(num), int.Parse(exponent)), GetScale(ctx, scale));

        /// <summary>
        /// Get the square root of an arbitrary precision number.
        /// </summary>
        public static string bcsqrt(Context ctx, string num, int? scale = default) => ToString(Rational.RationalRoot(Parse(num), 2), GetScale(ctx, scale));

        /// <summary>
        /// Set or get default scale parameter for all bc math functions.
        /// </summary>
        public static int bcscale(Context ctx, int? scale = default)
        {
            var oldscale = GetCurrentScale(ctx);
            if (scale.HasValue)
            {
                var newscale = scale.GetValueOrDefault();
                if (newscale < 0)
                {
                    PhpException.InvalidArgument(nameof(scale));
                }

                SetCurrentScale(ctx, newscale);
            }
            return oldscale;
        }

        /// <summary>
        /// Raise an arbitrary precision number to another, reduced by a specified modulus.
        /// </summary>
        public static string bcpowmod(Context ctx, string num, string exponent, string modulus, int? scale = default)
        {
            var x = Parse(num);
            var mod = Parse(modulus);

            // bcmod(bcpow($x, $y), $mod)

            x = Rational.Pow(x, int.Parse(exponent));

            var result = bcmod(x, mod);

            return ToString(result, GetScale(ctx, scale));
        }
    }
}
