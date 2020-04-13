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

        /// <summary>
        /// Represents a big floating point number.
        /// </summary>
        public struct BigFloat
        {
            public BigInteger Sig { get; }

            public int Exp { get; }

            public static BigFloat Zero => new BigFloat(BigInteger.Zero, 0);

            static readonly BigInteger s_ten = new BigInteger(10);

            public bool IsZero => Sig.IsZero;

            /// <summary>
            /// Initializes the value.
            /// </summary>
            public BigFloat(BigInteger significant, int exponent)
            {
                Sig = significant;
                Exp = exponent;
            }

            static bool IsSign(char ch) => ch == '-' || ch == '+';

            static bool IsDot(char ch) => ch == '.';

            static int MinExp(BigFloat left, BigFloat right) => Math.Min(left.Exp, right.Exp);

            /// <summary>
            /// Gets new number where significant is adjusted so the number has desired exponent.
            /// </summary>
            BigFloat WithExp(int newExp)
            {
                var shift = this.Exp - newExp;
                if (shift != 0)
                {
                    // TODO: optimize

                    var sig = this.Sig;

                    while (shift > 0)
                    {
                        shift--;
                        sig *= s_ten;
                    }
                    while (shift < 0)
                    {
                        shift++;
                        sig /= s_ten;
                    }

                    return new BigFloat(sig, newExp);
                }

                return this;
            }

            public static bool TryParse(ReadOnlySpan<char> value, out BigFloat result)
            {
                if (value.Length == 0)
                {
                    result = Zero;
                    return false;
                }

                // parse number in format:
                // [+-]ddd[.]ddd

                int index = 0;
                var hasdigit = false;
                int dot = -1;

                // [+-]
                if (IsSign(value[index]))
                {
                    index++;
                }

                // digits
                while (index < value.Length && char.IsDigit(value[index]))
                {
                    hasdigit = true;
                    index++;
                }

                // dot
                if (index < value.Length && IsDot(value[index]))
                {
                    dot = index;
                    index++;
                }

                // digits
                while (index < value.Length && char.IsDigit(value[index]))
                {
                    hasdigit = true;
                    index++;
                }

                // parsed whole string ?
                if (index != value.Length || !hasdigit)
                {
                    result = Zero;
                    return false;
                }

                //

                string sig; // [+-]ddd
                int exp;    // 

                if (dot < 0)
                {
                    sig = value.ToString();
                    exp = 0;
                }
                else
                {
                    sig = value.Slice(0, dot).ToString() + value.Slice(dot + 1).ToString();
                    exp = dot + 1 - value.Length;
                }

                //

                if (sig.Length == 0)
                {
                    result = Zero;
                    return true;
                }

                if (BigInteger.TryParse(sig, out var bigint))
                {
                    if (bigint.IsZero)
                    {
                        result = Zero;
                    }
                    else
                    {
                        result = new BigFloat(bigint, exp);
                    }

                    return true;
                }

                //
                result = Zero;
                return false;
            }

            public static BigFloat operator +(BigFloat left, BigFloat right)
            {
                if (left.IsZero) return right;
                if (right.IsZero) return left;

                // common exp
                int exp = MinExp(left, right);

                // add sig
                return new BigFloat(left.WithExp(exp).Sig + right.WithExp(exp).Sig, exp);
            }

            public static BigFloat operator -(BigFloat left, BigFloat right)
            {
                if (left.IsZero) return right;
                if (right.IsZero) return left;

                // common exp
                int exp = MinExp(left, right);

                // add sig
                return new BigFloat(left.WithExp(exp).Sig - right.WithExp(exp).Sig, exp);
            }

            public static BigFloat operator *(BigFloat left, BigFloat right)
            {
                if (left.IsZero || right.IsZero) return Zero;

                return new BigFloat(left.Sig * right.Sig, left.Exp + right.Exp);
            }

            public static BigFloat operator /(BigFloat left, BigFloat right)
            {
                if (left.IsZero) return Zero;
                if (right.IsZero) throw new DivideByZeroException();

                throw new NotImplementedException();
            }

            public string ToInvariantString(int scale)
            {
                if (IsZero)
                {
                    return "0";
                }

                string result;

                if (Exp >= 0)
                {
                    result = Sig.ToString("D", CultureInfo.InvariantCulture);

                    // *10^E
                    result = result.PadRight(result.Length + Exp, '0');
                }
                else // if (Exp < 0)
                {
                    var digits = -Exp;

                    //if (digits < 100)
                    //{
                    //    result = Sig.ToString("D" + digits, CultureInfo.InvariantCulture);
                    //}
                    //else
                    {
                        result = Sig.ToString("D", CultureInfo.InvariantCulture);
                        result = result.PadLeft(digits, '0');
                    }

                    var dot = result.Length + Exp;
                    if (dot == 0)
                    {
                        result = "0." + result.PadLeft(-Exp, '0');
                    }
                    else
                    {
                        result = result.Substring(0, dot) + "." + result.Substring(dot);
                    }

                    // TODO: scale
                    // TODO: trim zeros
                }

                return result;
            }

            public override string ToString() => ToInvariantString(MaxScale);
        }

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

        //public static string bcadd(string left, string right, int? scale = default)
        //{
        //    if (!BigFloat.TryParse(left.AsSpan(), out var lnum) |
        //        !BigFloat.TryParse(right.AsSpan(), out var rnum))
        //    {
        //        // Warning: bcmath function argument is not well-formed
        //    }

        //    return (lnum + rnum).ToString();
        //}

        //function bcsub
        //function bcmul
        //function bcdiv
        //function bcmod
        //function bcpow
        //function bcsqrt
        //function bcscale
        //function bccomp
        //function bcpowmod
    }
}
