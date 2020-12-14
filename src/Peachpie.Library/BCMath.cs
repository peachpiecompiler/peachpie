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

                Register(BCMathOptions.Scale, IniFlags.Supported | IniFlags.Local, GetSetOption, BCMath.ExtensionName);
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

        static PhpValue GetSetOption(Context ctx, IPhpConfigurationService config, string option, PhpValue value, IniAction action)
        {
            var local = config.Get<BCMathConfig>();
            if (local == null)
            {
                return PhpValue.Null;
            }

            if (string.Equals(option, BCMathOptions.Scale))
            {
                var oldvalue = local.Scale;
                if (action == IniAction.Set) local.Scale = Math.Max(0, (int)value);
                return oldvalue;
            }
            else
            {
                Debug.Fail("Option '" + option + "' is not currently supported.");
                return PhpValue.Null;
            }
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
                var digits = BigInteger
                    .DivRem(numerator, denominator, out var rem)
                    .ToString(NumberFormatInfo.InvariantInfo);

                result.Append(digits);

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

        ///// <summary>
        ///// Get modulus of an arbitrary precision number.
        ///// </summary>
        //TODO: public static string bcmod(Context ctx, string num1, string num2, int? scale = default) => ToString(Parse(num1) % Parse(num2), GetScale(ctx, scale));

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

        //TODO: function bcpowmod
    }
}
