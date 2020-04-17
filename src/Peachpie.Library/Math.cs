using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pchp.Library
{
    /// <summary>
	/// Implements PHP mathematical functions and constants.
	/// </summary>
	/// <threadsafety static="true"/>
	[PhpExtension("standard")]
    public static class PhpMath
    {
        #region Per-request Random Number Generators

        // since 7.1: rand() aliases to mt_rand() and corr.
        /// <summary>
        /// Gets an initialized random number generator associated with the current thread.
        /// </summary>
        internal static Random Generator => MTGenerator;
        //readonly static ThreadLocal<Random> _generator = new ThreadLocal<Random>(
        //    () => new Random(unchecked((int)System.DateTime.UtcNow.ToFileTimeUtc())));

        /// <summary>
		/// Gets an initialized Mersenne Twister random number generator associated with the current thread.
		/// </summary>
		internal static MersenneTwister MTGenerator => _mtGenerator.Value;   // lazily creates the value using the factory method once
        readonly static ThreadLocal<MersenneTwister> _mtGenerator = new ThreadLocal<MersenneTwister>(
            () => new MersenneTwister(unchecked((uint)System.DateTime.UtcNow.ToFileTimeUtc())));

        #endregion

        #region Constants

        public const double M_PI = Math.PI;
        public const double M_E = Math.E;
        public const double M_LOG2E = 1.4426950408889634074;
        public const double M_LOG10E = 0.43429448190325182765;
        public const double M_LN2 = 0.69314718055994530942;
        public const double M_LN10 = 2.30258509299404568402;
        public const double M_PI_2 = 1.57079632679489661923;
        public const double M_PI_4 = 0.78539816339744830962;
        public const double M_1_PI = 0.31830988618379067154;
        public const double M_2_PI = 0.63661977236758134308;
        public const double M_SQRTPI = 1.77245385090551602729;
        public const double M_2_SQRTPI = 1.12837916709551257390;
        /// <summary>sqrt(2)</summary>
        public const double M_SQRT2 = 1.41421356237309504880;
        public const double M_SQRT3 = 1.73205080756887729352;
        public const double M_SQRT1_2 = 0.70710678118654752440;
        public const double M_LNPI = 1.14472988584940017414;
        public const double M_EULER = 0.57721566490153286061;
        public const double NAN = double.NaN;
        public const double INF = double.PositiveInfinity;

        public const int MT_RAND_MT19937 = 0;
        public const int MT_RAND_PHP = 1;

        #endregion

        #region Absolutize Range

        /// <summary>
        /// Absolutizes range specified by an offset and a length relatively to a dimension of an array.
        /// </summary>
        /// <param name="count">The number of items in array. Should be non-negative.</param>
        /// <param name="offset">
        /// The offset of the range relative to the beginning (if non-negative) or the end of the array (if negative).
        /// If the offset underflows or overflows the length is shortened appropriately.
        /// </param>
        /// <param name="length">
        /// The length of the range if non-negative. Otherwise, its absolute value is the number of items
        /// which will not be included in the range from the end of the array. In the latter case 
        /// the range ends with the |<paramref name="length"/>|-th item from the end of the array (counting from zero).
        /// </param>
        /// <remarks>
        /// Ensures that <c>[offset,offset + length]</c> is subrange of <c>[0,count]</c>.
        /// </remarks>
        /// <returns>Value indicating whether the offset is within a valid range, otherwise the caller should return <c>FALSE</c>.</returns>
        internal static bool AbsolutizeRange(ref int offset, ref int length, int count)
        {
            Debug.Assert(count >= 0);

            // prevents overflows:
            if (offset >= count || count == 0)
            {
                length = 0;

                if (offset == count)
                {
                    return true;
                }
                else
                {
                    offset = count;
                    return false;
                }
            }

            // negative offset => offset is relative to the end of the string:
            if (offset < 0)
            {
                offset += count;
                if (offset < 0)
                {
                    offset = 0;
                }
            }

            Debug.Assert(offset >= 0 && offset < count);

            if (length < 0)
            {
                // there is count-offset items from offset to the end of array,
                // the last |length| items is taken away:
                length = count - offset + length;
                if (length < 0) length = 0;
            }
            else if ((long)offset + length > count)
            {
                // interval ends on the end of array:
                length = count - offset;
            }

            Debug.Assert(length >= 0 && offset + length <= count);

            return true;
        }

        #endregion

        #region rand, srand, getrandmax, uniqid, lcg_value, random_int, random_bytes

        /// <summary>
        /// Seed the random number generator. No return value.
        /// </summary>
        public static void srand() => mt_srand();

        /// <summary>
        /// Seed the random number generator. No return value.
        /// </summary>
        /// <param name="seed">Optional seed value.</param>
        public static void srand(int seed) => mt_srand(seed);

        /// <summary>
        /// Show largest possible random value.
        /// </summary>
        /// <returns>The largest possible random value returned by rand().</returns>
		public static int getrandmax() => mt_getrandmax();

        /// <summary>
        /// Generate a random integer.
        /// </summary>
        /// <returns>A pseudo random value between 0 and getrandmax(), inclusive.</returns>
        public static int rand() => mt_rand();

        /// <summary>
        /// Generate a random integer.
        /// </summary>
        /// <param name="min">The lowest value to return.</param>
        /// <param name="max">The highest value to return.</param>
        /// <returns>A pseudo random value between min and max, inclusive. </returns>
        public static int rand(int min, int max) => mt_rand(min, max);

        /// <summary>
        /// Generate a unique ID.
        /// Gets a prefixed unique identifier based on the current time in microseconds. 
        /// </summary>
        /// <returns>Returns the unique identifier, as a string.</returns>
        public static string uniqid()
        {
            return uniqid(null, false);
        }

        /// <summary>
        /// Generate a unique ID.
        /// Gets a prefixed unique identifier based on the current time in microseconds. 
        /// </summary>
        /// <param name="prefix">Can be useful, for instance, if you generate identifiers simultaneously on several hosts that might happen to generate the identifier at the same microsecond.
        /// With an empty prefix , the returned string will be 13 characters long.
        /// </param>
        /// <returns>Returns the unique identifier, as a string.</returns>
        public static string uniqid(string prefix)
        {
            return uniqid(prefix, false);
        }

        /// <summary>
        /// Generate a unique ID.
        /// </summary>
        /// <remarks>
        /// With an empty prefix, the returned string will be 13 characters long. If more_entropy is TRUE, it will be 23 characters.
        /// </remarks>
        /// <param name="prefix">Use the specified prefix.</param>
        /// <param name="more_entropy">Use LCG to generate a random postfix.</param>
        /// <returns>A pseudo-random string composed from the given prefix, current time and a random postfix.</returns>
        public static string uniqid(string prefix, bool more_entropy)
        {
            // Note that Ticks specify time in 100nanoseconds but it is raised each 100144 
            // ticks which is around 10 times a second (the same for Milliseconds).
            string ticks = string.Format("{0:X}", System.DateTime.UtcNow.Ticks + MTGenerator.Next());

            ticks = ticks.Substring(ticks.Length - 13);
            if (prefix == null) prefix = "";
            if (more_entropy)
            {
                string rnd = lcg_value().ToString();
                rnd = rnd.Substring(2, 8);
                return string.Format("{0}{1}.{2}", prefix, ticks, rnd);
            }
            else return string.Format("{0}{1}", prefix, ticks);
        }

        /// <summary>
        /// Generates a pseudo-random number using linear congruential generator in the range of (0,1).
        /// </summary>
        /// <remarks>
        /// This method uses the Framwork <see cref="rand()"/> generator
        /// which may or may not be the same generator as the PHP one (L(CG(2^31 - 85),CG(2^31 - 249))).
        /// </remarks>
        /// <returns></returns>
        public static double lcg_value() => MTGenerator.NextDouble();

        /// <summary>
        /// Generates cryptographically secure pseudo-random integers.
        /// </summary>
        /// <param name="min">The lowest value to be returned, which must be <see cref="Environment.PHP_INT_MIN"/> or higher.</param>
        /// <param name="max">The highest value to be returned, which must be less than or equal to <see cref="Environment.PHP_INT_MAX"/>.</param>
        /// <returns>Returns a cryptographically secure random integer in the range <paramref name="min"/> to <paramref name="max"/>, inclusive.</returns>
        public static long random_int(long min, long max)
        {
            if (max < min)
            {
                throw new ArgumentOutOfRangeException();
            }

            // TODO: use mcrypt, int64
            return rand((int)min, (int)max);
        }

        /// <summary>
        /// Generates cryptographically secure pseudo-random bytes.
        /// </summary>
        public static PhpString random_bytes(int length)
        {
            if (length <= 0)
            {
                throw new Spl.Error(string.Format(Resources.LibResources.arg_negative_or_zero, nameof(length)));
            }

            var bytes = new byte[length];

            // TODO: System.Security.Cryptography.RNGCryptoServiceProvider
            System.Security.Cryptography.RandomNumberGenerator.Create().GetBytes(bytes);

            return new PhpString(bytes);
        }

        #endregion

        #region mt_getrandmax, mt_rand, mt_srand

        public static int mt_getrandmax()
        {
            return Int32.MaxValue;
        }

        public static int mt_rand()
        {
            return MTGenerator.Next();
        }

        public static int mt_rand(int min, int max)
        {
            return (min < max) ? MTGenerator.Next(min, max) : MTGenerator.Next(max, min);
        }

        /// <summary>
        /// <see cref="mt_srand(int, MtMode)"/> mode.
        /// </summary>
        public enum MtMode : int
        {
            MT19937 = MT_RAND_MT19937,
            PHP = MT_RAND_PHP,
        }

        /// <summary>
        /// Seed the better random number generator.
        /// No return value.
        /// </summary>
        public static void mt_srand()
        {
            mt_srand(unchecked((int)System.DateTime.UtcNow.Ticks));
        }

        /// <summary>
        /// Seed the better random number generator.
        /// No return value.
        /// </summary>
        /// <param name="seed">Optional seed value.</param>
        public static void mt_srand(int seed)
        {
            MTGenerator.Seed(unchecked((uint)seed));
        }

        /// <summary>
        /// Seed the better random number generator.
        /// No return value.
        /// </summary>
        /// <param name="seed">Optional seed value.</param>
        /// <param name="mode">Seed algorithm implementation.</param>
        public static void mt_srand(int seed, MtMode mode = MtMode.MT19937)
        {
            if (mode != MtMode.MT19937)
            {
                PhpException.ArgumentValueNotSupported(nameof(mode), mode.ToString());
            }

            mt_srand(seed);
        }

        #endregion

        #region is_nan,is_finite,is_infinite

        public static bool is_nan(double x)
        {
            return Double.IsNaN(x);
        }

        public static bool is_finite(double x)
        {
            return !Double.IsInfinity(x);
        }

        public static bool is_infinite(double x)
        {
            return Double.IsInfinity(x);
        }

        #endregion

        #region decbin, bindec, decoct, octdec, dechex, hexdec, base_convert

        /// <summary>
        /// Converts the given number to int64 (if the number is whole and fits into the int64's range).
        /// </summary>
        /// <param name="number">The number.</param>
        /// <returns><c>long</c> representation of number if possible, otherwise a <c>double</c> representation.</returns>
        private static PhpNumber ConvertToLong(double number)
        {
            if ((Math.Round(number) == number) && (number <= long.MaxValue) && (number >= long.MinValue))
            {
                return PhpNumber.Create((long)number);
            }
            return PhpNumber.Create(number);
        }

        /// <summary>
        /// Converts the lowest 32 bits of the given number to a binary string.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string decbin(double number)
        {
            // Trim the number to the lower 32 binary digits.
            uint temp = unchecked((uint)number);
            return DoubleToBase(temp, 2) ?? "0";
        }

        ///// <summary>
        ///// Converts the lowest 32 bits of the given number to a binary string.
        ///// </summary>
        ///// <param name="number"></param>
        ///// <returns></returns>
        //public static string decbin_unicode(double number)
        //{
        //    // Trim the number to the lower 32 binary digits.
        //    uint temp = unchecked((uint)number);
        //    return DoubleToBaseUnicode(temp, 2);
        //}

        /// <summary>
        /// Returns the decimal equivalent of the binary number represented by the binary_string argument.
        /// bindec() converts a binary number to an integer or, if needed for size reasons, double.
        /// </summary>
        /// <param name="str">The binary string to convert.</param>
        /// <returns>The decimal value of <paramref name="str"/>.</returns>
        public static PhpNumber bindec(string str)
        {
            if (str == null)
                return PhpNumber.Default;

            return ConvertToLong(BaseToDouble(str, 2));
        }


        //[ImplementsFunction("bindec_unicode")]
        //public static object bindec_unicode(string str)
        //{
        //    if (str == null) return 0;
        //    return ConvertToInt(BaseToDoubleUnicode(str, 2));
        //}

        /// <summary>
        /// Returns a string containing an octal representation of the given number argument.
        /// </summary>
        /// <param name="number">Decimal value to convert.</param>
        /// <returns>Octal string representation of <paramref name="number"/>.</returns>
        public static string decoct(int number)
        {
            return System.Convert.ToString(number, 8);
        }

        /// <summary>
        /// Returns the decimal equivalent of the octal number represented by the <paramref name="str"/> argument.
        /// </summary>
        /// <param name="str">The octal string to convert.</param>
        /// <returns>The decimal representation of <paramref name="str"/>.</returns>
        public static PhpNumber octdec(string str)
        {
            if (str == null)
                return PhpNumber.Default;

            return ConvertToLong(BaseToDouble(str, 8));
        }

        //public static object octdec_unicode(string str)
        //{
        //    if (str == null) return 0;
        //    return ConvertToInt(BaseToDoubleUnicode(str, 8));
        //}

        /// <summary>
        /// Returns a string containing a hexadecimal representation of the given number argument.
        /// </summary>
        /// <param name="number">Decimal value to convert.</param>
        /// <returns>Hexadecimal string representation of <paramref name="number"/>.</returns>
        public static string dechex(long number)
        {
            return System.Convert.ToString(number, 16);
        }

        //public static string dechex_unicode(int number)
        //{
        //    return System.Convert.ToString(number, 16);
        //}

        /// <summary>
        /// Hexadecimal to decimal.
        /// Returns the decimal equivalent of the hexadecimal number represented by the hex_string argument. hexdec() converts a hexadecimal string to a decimal number.
        /// hexdec() will ignore any non-hexadecimal characters it encounters.
        /// </summary>
        /// <param name="str">The hexadecimal string to convert.</param>
        /// <returns>The decimal representation of <paramref name="str"/>.</returns>
        public static PhpNumber hexdec(string str)
        {
            if (str == null)
                return PhpNumber.Default;

            return ConvertToLong(BaseToDouble(str, 16));
        }

        //public static object hexdec_unicode(string str)
        //{
        //    if (str == null) return 0;
        //    return ConvertToInt(BaseToDoubleUnicode(str, 16));
        //}

        private static double BaseToDouble(string number, int fromBase)
        {
            if (number == null)
            {
                PhpException.ArgumentNull(nameof(number));
                return 0.0;
            }

            if (fromBase < 2 || fromBase > 36)
            {
                PhpException.InvalidArgument(nameof(fromBase), Resources.Resources.arg_out_of_bounds);
                return 0.0;
            }

            double fnum = 0;
            for (int i = 0; i < number.Length; i++)
            {
                int digit = Core.Convert.AlphaNumericToDigit(number[i]);
                if (digit < fromBase)
                {
                    fnum = fnum * fromBase + digit;
                }
                else
                {
                    // Warning ?
                }
            }

            return fnum;
        }

        //private static double BaseToDoubleUnicode(string number, int fromBase)
        //{
        //    if (number == null)
        //    {
        //        throw new NotImplementedException();
        //        //PhpException.ArgumentNull("number");
        //        //return 0.0;
        //    }

        //    if (fromBase < 2 || fromBase > 36)
        //    {
        //        throw new NotImplementedException();
        //        //PhpException.InvalidArgument("toBase", LibResources.GetString("arg_out_of_bounds"));
        //        //return 0.0;
        //    }

        //    double fnum = 0;
        //    for (int i = 0; i < number.Length; i++)
        //    {
        //        int digit = Pchp.Core.Convert.AlphaNumericToDigit(number[i]);
        //        if (digit < fromBase)
        //            fnum = fnum * fromBase + digit;
        //    }

        //    return fnum;
        //}

        private const string digitsUnicode = "0123456789abcdefghijklmnopqrstuvwxyz";
        private static byte[] digits = new byte[] {(byte)'0',(byte)'1',(byte)'2',(byte)'3',(byte)'4',(byte)'5',(byte)'6',(byte)'7',(byte)'8',(byte)'9',
            (byte)'a',(byte)'b',(byte)'c',(byte)'d',(byte)'e',(byte)'f',(byte)'g',(byte)'h',(byte)'i',(byte)'j',(byte)'k',(byte)'l',(byte)'m',(byte)'n',
            (byte)'o',(byte)'p',(byte)'q',(byte)'r',(byte)'s',(byte)'t',(byte)'u',(byte)'v',(byte)'w',(byte)'x',(byte)'y',(byte)'z' };

        private static string DoubleToBase(double number, int toBase)
        {
            if (toBase < 2 || toBase > 36)
            {
                PhpException.InvalidArgument(nameof(toBase), Resources.LibResources.arg_out_of_bounds);
                return null; // FALSE
            }

            // Don't try to convert infinity or NaN:
            if (double.IsInfinity(number) || double.IsNaN(number))
            {
                PhpException.InvalidArgument(nameof(number), Resources.LibResources.arg_out_of_bounds);
                return null; // FALSE
            }

            double fvalue = Math.Floor(number); /* floor it just in case */
            if (Math.Abs(fvalue) < 1)
            {
                return "0";
            }

            var sb = StringBuilderUtilities.Pool.Get();

            while (Math.Abs(fvalue) >= 1)
            {
                double mod = fmod(fvalue, toBase);
                int i = (int)mod;
                char c = digitsUnicode[i];
                //sb.Append(digits[(int) fmod(fvalue, toBase)]);
                sb.Append(c);
                fvalue /= toBase;
            }

            return Core.Utilities.StringUtils.Reverse(StringBuilderUtilities.GetStringAndReturn(sb));
        }

        /// <summary>
        /// Convert a number between arbitrary bases.
        /// Returns a string containing number represented in base tobase. The base in which number is given is specified in <paramref name="fromBase"/>. Both <paramref name="fromBase"/> and <paramref name="toBase"/> have to be between 2 and 36, inclusive. Digits in numbers with a base higher than 10 will be represented with the letters a-z, with a meaning 10, b meaning 11 and z meaning 35.
        /// </summary>
        /// <param name="number">The number to convert</param>
        /// <param name="fromBase">The base <paramref name="number"/> is in.</param>
        /// <param name="toBase">The base to convert <paramref name="number"/> to</param>
        /// <returns><paramref name="number"/> converted to base <paramref name="toBase"/>.</returns>
		[return: CastToFalse]
        public static string base_convert(string number, int fromBase, int toBase)
        {
            if (number == null)
            {
                return "0";
            }

            var value = BaseToDouble(number, fromBase);
            return DoubleToBase(value, toBase);
        }

        #endregion

        #region deg2rad, pi, cos, sin, tan, acos, asin, atan, atan2

        /// <summary>
        /// Degrees to radians.
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public static double deg2rad(double degrees)
        {
            return degrees / 180 * Math.PI;
        }

        /// <summary>
        /// Radians to degrees.
        /// </summary>
        /// <param name="radians"></param>
        /// <returns></returns>
        public static double rad2deg(double radians)
        {
            return radians / Math.PI * 180;
        }

        /// <summary>
        /// Returns an approximation of pi.
        /// </summary>
        /// <returns>The value of pi as <c>double</c>.</returns>
		public static double pi()
        {
            return Math.PI;
        }

        /// <summary>
        /// Returns the arc cosine of arg in radians.
        /// acos() is the complementary function of cos(), which means that <paramref name="x"/>==cos(acos(<paramref name="x"/>)) for every value of a that is within acos()' range.
        /// </summary>
        /// <param name="x">The argument to process.</param>
        /// <returns>The arc cosine of <paramref name="x"/> in radians.</returns>
		public static double acos(double x)
        {
            return Math.Acos(x);
        }

        /// <summary>
        /// Returns the arc sine of arg in radians. asin() is the complementary function of sin(), which means that <paramref name="x"/>==sin(asin(<paramref name="x"/>)) for every value of a that is within asin()'s range.
        /// </summary>
        /// <param name="x">The argument to process.</param>
        /// <returns>The arc sine of <paramref name="x"/> in radians.</returns>
		public static double asin(double x)
        {
            return Math.Asin(x);
        }

        public static double atan(double x)
        {
            return Math.Atan(x);
        }

        public static double atan2(double y, double x)
        {
            double rv = Math.Atan(y / x);
            if (x < 0)
            {
                return ((rv > 0) ? -Math.PI : Math.PI) + rv;
            }
            else return rv;
        }

        public static double cos(double x)
        {
            return Math.Cos(x);
        }

        public static double sin(double x)
        {
            return Math.Sin(x);
        }

        public static double tan(double x)
        {
            return Math.Tan(x);
        }

        #endregion

        #region cosh, sinh, tanh, acosh, asinh, atanh

        public static double cosh(double x)
        {
            return Math.Cosh(x);
        }

        public static double sinh(double x)
        {
            return Math.Sinh(x);
        }

        public static double tanh(double x)
        {
            return Math.Tanh(x);
        }

        public static double acosh(double x)
        {
            return Math.Log(x + Math.Sqrt(x * x - 1));
        }

        public static double asinh(double x)
        {
            return Math.Log(x + Math.Sqrt(x * x + 1));
        }

        public static double atanh(double x)
        {
            return Math.Log((1 + x) / (1 - x)) / 2;
        }

        #endregion

        #region exp, expm1, log, log10, log1p, pow, sqrt, hypot

        /// <summary>
        /// Returns <c>e</c> raised to the power of <paramref name="x"/>.
        /// </summary>
        public static double exp(double x)
        {
            return Math.Exp(x);
        }

        /// <summary>
        /// expm1() returns the equivalent to 'exp(arg) - 1' computed in a way that is accurate even
        /// if the value of arg is near zero, a case where 'exp (arg) - 1' would be inaccurate due to
        /// subtraction of two numbers that are nearly equal. 
        /// </summary>
        /// <param name="x">The argument to process </param>
        public static double expm1(double x)
        {
            return Math.Exp(x) - 1.0;   // TODO: implement exp(x)-1 for x near to zero
        }

        /// <summary>
        /// Returns the base-10 logarithm of <paramref name="x"/>.
        /// </summary>
        public static double log10(double x)
        {
            return Math.Log10(x);
        }

        public static double log(double x)
        {
            return Math.Log(x);
        }

        /// <summary>
        /// If the optional <paramref name="logBase"/> parameter is specified, log() returns log(<paramref name="logBase"/>) <paramref name="x"/>, otherwise log() returns the natural logarithm of <paramref name="x"/>.
        /// </summary>
        public static double log(double x, double logBase)
        {
            return Math.Log(x, logBase);
        }

        /// <summary>
        /// log1p() returns log(1 + number) computed in a way that is accurate even when the value
        /// of number is close to zero. log()  might only return log(1) in this case due to lack of precision. 
        /// </summary>
        /// <param name="x">The argument to process </param>
        /// <returns></returns>
		public static double log1p(double x)
        {
            return Math.Log(x + 1.0);   // TODO: implement log(x+1) for x near to zero
        }

        /// <summary>
        /// Returns <paramref name="base"/> raised to the power of <paramref name="exp"/>.
        /// </summary>
        public static PhpNumber pow(PhpNumber @base, PhpNumber exp) => PhpNumber.Pow(@base, exp);

        //public static PhpNumber pow(PhpNumber @base, PhpNumber exp)
        //{
        //    if (@base.IsLong && exp.IsLong && exp.Long >= 0)
        //    {
        //        // integer base, non-negative integer exp  //

        //        return pow(@base.Long, exp.Long);
        //    }

        //    double dexp = exp.ToDouble();
        //    double dbase = @base.ToDouble();

        //    if (dbase < 0)
        //    {
        //        // cannot rount to integer:
        //        if (Math.Ceiling(dexp) > dexp)
        //            return Double.NaN;

        //        double result = Math.Pow(-dbase, dexp);
        //        return (Math.IEEERemainder(Math.Abs(dexp), 2.0) < 1.0) ? result : -result;
        //    }

        //    if (dexp < 0)
        //        return 1 / Math.Pow(dbase, -dexp);
        //    else
        //        return Math.Pow(dbase, dexp);
        //}

        //private static PhpNumber pow(long lbase, long lexp)
        //{
        //    Debug.Assert(lexp >= 0);

        //    long l1 = 1, l2 = lbase;

        //    if (lexp == 0) // anything powered by 0 is 1
        //    {
        //        return PhpNumber.Create(1);
        //    }

        //    if (lbase == 0) // 0^(anything except 0) is 0
        //    {
        //        return PhpNumber.Create(0);
        //    }

        //    try
        //    {
        //        while (lexp >= 1)
        //        {
        //            if ((lexp & 1) != 0)
        //            {
        //                l1 *= l2;
        //                lexp--;
        //            }
        //            else
        //            {
        //                l2 *= l2;
        //                lexp /= 2;
        //            }
        //        }
        //    }
        //    catch (ArithmeticException)
        //    {
        //        return PhpNumber.Create((double)l1 * Math.Pow(l2, lexp));
        //    }

        //    // able to do it with longs
        //    return PhpNumber.Create(l1);
        //}

        public static double sqrt(double x)
        {
            return Math.Sqrt(x);
        }

        public static double hypot(double x, double y)
        {
            return Math.Sqrt(x * x + y * y);
        }

        #endregion

        #region  ceil, floor, round, abs, fmod, max, min, intdiv

        /// <summary>
        /// Returns the next highest integer value by rounding up <paramref name="x"/> if necessary.
        /// </summary>
        /// <param name="x">The value to round.</param>
        /// <returns><paramref name="x"/> rounded up to the next highest integer. The return value of ceil() is still of type <c>double</c> as the value range of double is usually bigger than that of integer.</returns>
        public static double ceil(double x)
        {
            return Math.Ceiling(x);
        }

        /// <summary>
        /// Returns the next lowest integer value by rounding down <paramref name="x"/> if necessary.
        /// </summary>
        /// <param name="x">The numeric value to round.</param>
        /// <returns><paramref name="x"/> rounded to the next lowest integer. The return value of floor() is still of type <c>double</c> because the value range of double is usually bigger than that of integer.</returns>
		public static double floor(double x)
        {
            return Math.Floor(x);
        }

        /// <summary>
        /// Rounds a float.
        /// </summary>
        /// <param name="x">The value to round.</param>
        /// <returns>The rounded value.</returns>
		public static double round(double x)
        {
            return RoundInternal(x, RoundMode.HalfUp);
        }

        /// <summary>
        /// Rounds a float.
        /// </summary>
        /// <param name="x">The value to round.</param>
        /// <param name="precision">The optional number of decimal digits to round to. Can be less than zero to ommit digits at the end. Default is <c>0</c>.</param>
        /// <returns>The rounded value.</returns>
        public static double round(double x, int precision /*= 0*/)
        {
            return round(x, precision, RoundMode.HalfUp);
        }

        /// <summary>
        /// <c>$mode</c> parameter for <see cref="round(double,int,RoundMode)"/> function.
        /// </summary>
        public enum RoundMode : int
        {
            /// <summary>
            /// When a number is halfway between two others, it is rounded away from zero.
            /// </summary>
            HalfUp = 1,

            /// <summary>
            /// When a number is halfway between two others, it is rounded to the zero.
            /// </summary>
            HalfDown = 2,

            /// <summary>
            /// When a number is halfway between two others, it is rounded toward the nearest even number.
            /// </summary>
            HalfEven = 3,

            /// <summary>
            /// When a number is halfway between two others, it is rounded toward the nearest odd number.
            /// </summary>
            HalfOdd = 4,
        }

        public const int PHP_ROUND_HALF_UP = (int)RoundMode.HalfUp;
        public const int PHP_ROUND_HALF_DOWN = (int)RoundMode.HalfDown;
        public const int PHP_ROUND_HALF_EVEN = (int)RoundMode.HalfEven;
        public const int PHP_ROUND_HALF_ODD = (int)RoundMode.HalfOdd;

        #region Round Helpers

        /// <summary>
        /// Returns precise value of 10^<paramref name="power"/>.
        /// </summary>
        private static double Power10Value(int power)
        {
            switch (power)
            {
                case -15: return .000000000000001;
                case -14: return .00000000000001;
                case -13: return .0000000000001;
                case -12: return .000000000001;
                case -11: return .00000000001;
                case -10: return .0000000001;
                case -9: return .000000001;
                case -8: return .00000001;
                case -7: return .0000001;
                case -6: return .000001;
                case -5: return .00001;
                case -4: return .0001;
                case -3: return .001;
                case -2: return .01;
                case -1: return .1;
                case 0: return 1.0;
                case 1: return 10.0;
                case 2: return 100.0;
                case 3: return 1000.0;
                case 4: return 10000.0;
                case 5: return 100000.0;
                case 6: return 1000000.0;
                case 7: return 10000000.0;
                case 8: return 100000000.0;
                case 9: return 1000000000.0;
                case 10: return 10000000000.0;
                case 11: return 100000000000.0;
                case 12: return 1000000000000.0;
                case 13: return 10000000000000.0;
                case 14: return 100000000000000.0;
                case 15: return 1000000000000000.0;
                default: return Math.Pow(10.0, (double)power);
            }
        }

        private static double RoundInternal(double value, RoundMode mode)
        {
            double tmp_value;

            if (value >= 0.0)
            {
                tmp_value = Math.Floor(value + 0.5);
                if (mode != RoundMode.HalfUp)
                {
                    if ((mode == RoundMode.HalfDown && value == (-0.5 + tmp_value)) ||
                        (mode == RoundMode.HalfEven && value == (0.5 + 2 * Math.Floor(tmp_value * .5))) ||
                        (mode == RoundMode.HalfOdd && value == (0.5 + 2 * Math.Floor(tmp_value * .5) - 1.0)))
                    {
                        tmp_value = tmp_value - 1.0;
                    }
                }
            }
            else
            {
                tmp_value = Math.Ceiling(value - 0.5);
                if (mode != RoundMode.HalfUp)
                {
                    if ((mode == RoundMode.HalfDown && value == (0.5 + tmp_value)) ||
                        (mode == RoundMode.HalfEven && value == (-0.5 + 2 * Math.Ceiling(tmp_value * .5))) ||
                        (mode == RoundMode.HalfOdd && value == (-0.5 + 2 * Math.Ceiling(tmp_value * .5) + 1.0)))
                    {
                        tmp_value = tmp_value + 1.0;
                    }
                }
            }

            return tmp_value;
        }

        private static readonly double[] _Log10AbsValues = new[]
        {
            1e-8, 1e-7, 1e-6, 1e-5, 1e-4, 1e-3, 1e-2, 1e-1,
            1e0,  1e1,  1e2,  1e3,  1e4,  1e5,  1e6,  1e7,
            1e8,  1e9,  1e10, 1e11, 1e12, 1e13, 1e14, 1e15,
            1e16, 1e17, 1e18, 1e19, 1e20, 1e21, 1e22, 1e23
        };

        private static int _Log10Abs(double value)
        {
            value = Math.Abs(value);

            if (value < 1e-8 || value > 1e23)
            {
                return (int)Math.Floor(Math.Log10(value));
            }
            else
            {
                var values = _Log10AbsValues;

                /* Do a binary search with 5 steps */
                var result = 16;
                if (value < values[result])
                    result -= 8;
                else
                    result += 8;

                if (value < values[result])
                    result -= 4;
                else
                    result += 4;

                if (value < values[result])
                    result -= 2;
                else
                    result += 2;

                if (value < values[result])
                    result -= 1;
                else
                    result += 1;

                if (value < values[result])
                    result -= 1;

                result -= 8;

                //
                return result;
            }
        }

        #endregion

        /// <summary>
        /// Rounds a float.
        /// </summary>
        /// <param name="x">The value to round.</param>
        /// <param name="precision">The optional number of decimal digits to round to. Can be less than zero to ommit digits at the end. Default is <c>0</c>.</param>
        /// <param name="mode">One of PHP_ROUND_HALF_UP, PHP_ROUND_HALF_DOWN, PHP_ROUND_HALF_EVEN, or PHP_ROUND_HALF_ODD. Default is <c>PHP_ROUND_HALF_UP</c>.</param>
        /// <returns>The rounded value.</returns>
        public static double round(double x, int precision = 0, RoundMode mode = RoundMode.HalfUp)
        {
            if (Double.IsInfinity(x) || Double.IsNaN(x) || x == default(double))
                return x;

            if (precision == 0)
            {
                return RoundInternal(x, mode);
            }
            else
            {
                if (precision > 23 || precision < -23)
                    return x;

                //
                // Following code is taken from math.c to avoid incorrect .NET rounding
                //

                var precision_places = 14 - _Log10Abs(x);

                var f1 = Power10Value(precision);
                double tmp_value;

                /* If the decimal precision guaranteed by FP arithmetic is higher than
                   the requested places BUT is small enough to make sure a non-zero value
                   is returned, pre-round the result to the precision */
                if (precision_places > precision && precision_places - precision < 15)
                {
                    var f2 = Power10Value(precision_places);
                    tmp_value = x * f2;
                    /* preround the result (tmp_value will always be something * 1e14,
                       thus never larger than 1e15 here) */
                    tmp_value = RoundInternal(tmp_value, mode);
                    /* now correctly move the decimal point */
                    f2 = Power10Value(Math.Abs(precision - precision_places));
                    /* because places < precision_places */
                    tmp_value = tmp_value / f2;
                }
                else
                {
                    /* adjust the value */
                    tmp_value = x * f1;
                    /* This value is beyond our precision, so rounding it is pointless */
                    if (Math.Abs(tmp_value) >= 1e15)
                        return x;
                }

                /* round the temp value */
                tmp_value = RoundInternal(tmp_value, mode);

                /* see if it makes sense to use simple division to round the value */
                //if (precision < 23 && precision > -23)
                {
                    tmp_value = tmp_value / f1;
                }
                //else
                //{
                //    /* Simple division can't be used since that will cause wrong results.
                //       Instead, the number is converted to a string and back again using
                //       strtod(). strtod() will return the nearest possible FP value for
                //       that string. */

                //    /* 40 Bytes should be more than enough for this format string. The
                //       float won't be larger than 1e15 anyway. But just in case, use
                //       snprintf() and make sure the buffer is zero-terminated */
                //    char buf[40];
                //    snprintf(buf, 39, "%15fe%d", tmp_value, -places);
                //    buf[39] = '\0';
                //    tmp_value = zend_strtod(buf, NULL);
                //    /* couldn't convert to string and back */
                //    if (!zend_finite(tmp_value) || zend_isnan(tmp_value)) {
                //        tmp_value = value;
                //    }
                //}

                return tmp_value;
            }
        }

        /// <summary>
        /// Returns the absolute value of <paramref name="x"/>.
        /// </summary>
        /// <param name="x">The numeric value to process.</param>
        public static PhpNumber abs(PhpNumber x)
        {
            return x.IsLong
                ? abs(x.Long)
                : PhpNumber.Create(Math.Abs(x.Double));
        }

        public static double abs(double x)
        {
            return Math.Abs(x);
        }

        public static PhpNumber abs(long lx)
        {
            if (lx == long.MinValue)
                return PhpNumber.Create(-(double)lx);
            else
                return PhpNumber.Create(Math.Abs(lx));
        }

        /// <summary>
        /// Returns the floating point remainder (modulo) of the division of the arguments.
        /// </summary>
        /// <param name="x">The dividend.</param>
        /// <param name="y">The divisor.</param>
        /// <returns>The floating point remainder of <paramref name="x"/>/<paramref name="y"/>.</returns>
		public static double fmod(double x, double y)
        {
            y = Math.Abs(y);
            double rem = Math.IEEERemainder(Math.Abs(x), y);
            if (rem < 0) rem += y;
            return (x >= 0) ? rem : -rem;
        }

        /// <summary>
        /// Find highest value.
        /// </summary>
        public static PhpValue max(PhpArray array) => FindExtreme(array.Values, true);

        /// <summary>
        /// Find lowest value.
        /// </summary>
        public static PhpValue min(PhpArray array) => FindExtreme(array.Values, false);

        /// <summary>
        /// Find highest value.
        /// </summary>
        public static long max(long a, long b) => Math.Max(a, b);

        /// <summary>
        /// Find lowest value.
        /// </summary>
        public static long min(long a, long b) => Math.Min(a, b);

        /// <summary>
        /// Find highest value.
        /// If the first and only parameter is an array, max() returns the highest value in that array. If at least two parameters are provided, max() returns the biggest of these values.
        /// </summary>
        /// <param name="numbers">An array containing the values or values separately.</param>
        /// <returns>max() returns the numerically highest of the parameter values. If multiple values can be considered of the same size, the one that is listed first will be returned.
        /// When max() is given multiple arrays, the longest array is returned. If all the arrays have the same length, max() will use lexicographic ordering to find the return value.
        /// When given a string it will be cast as an integer when comparing.</returns>
		public static PhpValue max(params PhpValue[] numbers) => GetExtreme(numbers, true);

        /// <summary>
        /// Find lowest value.
        /// If the first and only parameter is an array, min() returns the lowest value in that array. If at least two parameters are provided, min() returns the smallest of these values.
        /// </summary>
        /// <param name="numbers">An array containing the values or values separately.</param>
        /// <returns>min() returns the numerically lowest of the parameter values.</returns>
		public static PhpValue min(params PhpValue[] numbers) => GetExtreme(numbers, false);

        internal static PhpValue GetExtreme(PhpValue[] numbers, bool maximum)
        {
            if (numbers.Length == 1)
            {
                var arr = numbers[0].AsArray();
                if (arr != null)
                {
                    return FindExtreme(arr.Values, maximum);
                }
            }

            //
            return FindExtreme(numbers, maximum);
        }

        internal static PhpValue FindExtreme(IEnumerable<PhpValue> array, bool maximum)
        {
            Debug.Assert(array != null);

            PhpValue ex;

            var enumerator = array.GetEnumerator();
            if (enumerator.MoveNext())
            {
                ex = enumerator.Current.GetValue();

                int fact = maximum ? 1 : -1;

                while (enumerator.MoveNext())
                {
                    if (Comparison.Compare(enumerator.Current, ex) * fact > 0)
                    {
                        ex = enumerator.Current.GetValue();
                    }
                }
            }
            else
            {
                ex = PhpValue.Null;
            }

            enumerator.Dispose();

            //
            return ex;
        }

        /// <summary>
        /// Returns the integer quotient of the <paramref name="dividend"/> of dividend by <paramref name="divisor"/>.
        /// </summary>
        /// <param name="dividend">Number to be divided.</param>
        /// <param name="divisor">Number which divides the <paramref name="dividend"/>.</param>
        public static long intdiv(long dividend, long divisor) => dividend / divisor;

        #endregion
    }
}
