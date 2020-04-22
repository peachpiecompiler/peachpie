#nullable enable

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Pchp.Core;

namespace Pchp.Library
{
    [PhpExtension("filter")]
    public static class Filter
    {
        #region Constants

        [PhpHidden]
        public enum FilterInput
        {
            Post = 0,
            Get = 1,
            Cookie = 2,
            Env = 4,
            Server = 5,
            Session = 6,
            Request = 99
        }

        public const int INPUT_POST = (int)FilterInput.Post;
        public const int INPUT_GET = (int)FilterInput.Get;
        public const int INPUT_COOKIE = (int)FilterInput.Cookie;
        public const int INPUT_ENV = (int)FilterInput.Env;
        public const int INPUT_SERVER = (int)FilterInput.Server;
        public const int INPUT_SESSION = (int)FilterInput.Session;
        public const int INPUT_REQUEST = (int)FilterInput.Request;

        /// <summary>
        /// Other filter ids.
        /// </summary>
        enum FilterIds
        {
            /// <summary>
            /// Flag used to require scalar as input
            /// </summary>
            FILTER_REQUIRE_SCALAR = 33554432,

            /// <summary>
            /// Require an array as input.
            /// </summary>
            FILTER_REQUIRE_ARRAY = 16777216,

            /// <summary>
            /// Always returns an array.
            /// </summary>
            FILTER_FORCE_ARRAY = 67108864,

            /// <summary>
            /// Use NULL instead of FALSE on failure.
            /// </summary>
            FILTER_NULL_ON_FAILURE = 134217728,

            /// <summary>
            /// ID of "callback" filter.
            /// </summary>
            FILTER_CALLBACK = 1024,
        }

        public const int FILTER_REQUIRE_SCALAR = (int)FilterIds.FILTER_REQUIRE_SCALAR;
        public const int FILTER_REQUIRE_ARRAY = (int)FilterIds.FILTER_REQUIRE_ARRAY;
        public const int FILTER_FORCE_ARRAY = (int)FilterIds.FILTER_FORCE_ARRAY;
        public const int FILTER_NULL_ON_FAILURE = (int)FilterIds.FILTER_NULL_ON_FAILURE;
        public const int FILTER_CALLBACK = (int)FilterIds.FILTER_CALLBACK;

        /// <summary>
        /// Validation filters.
        /// </summary>
        enum FilterValidate : int
        {
            /// <summary>
            /// ID of "int" filter.
            /// </summary>
            INT = 257,

            /// <summary>
            /// ID of "boolean" filter.
            /// </summary>
            BOOLEAN = 258,

            /// <summary>
            /// ID of "float" filter.
            /// </summary>
            FLOAT = 259,

            /// <summary>
            /// ID of "validate_regexp" filter.
            /// </summary>
            REGEXP = 272,

            /// <summary>
            /// ID of "validate_url" filter.
            /// </summary>
            URL = 273,

            /// <summary>
            /// ID of "validate_email" filter.
            /// </summary>
            EMAIL = 274,

            /// <summary>
            /// ID of "validate_ip" filter.
            /// </summary>
            IP = 275,

            /// <summary>
            /// Validates value as MAC address.
            /// </summary>
            MAC = 276,

            /// <summary>
            /// Validates whether the domain name label lengths are valid.
            /// Validates domain names against RFC 1034, RFC 1035, RFC 952, RFC 1123, RFC 2732, RFC 2181, and RFC 1123.
            /// Optional flag <see cref="FILTER_FLAG_HOSTNAME"/> adds ability to specifically validate hostnames (they must start with an alphanumeric character and contain only alphanumerics or hyphens).
            /// </summary>
            DOMAIN = 277,
        }

        public const int FILTER_VALIDATE_INT = (int)FilterValidate.INT;
        public const int FILTER_VALIDATE_BOOLEAN = (int)FilterValidate.BOOLEAN;
        public const int FILTER_VALIDATE_FLOAT = (int)FilterValidate.FLOAT;
        public const int FILTER_VALIDATE_REGEXP = (int)FilterValidate.REGEXP;
        public const int FILTER_VALIDATE_URL = (int)FilterValidate.URL;
        public const int FILTER_VALIDATE_EMAIL = (int)FilterValidate.EMAIL;
        public const int FILTER_VALIDATE_IP = (int)FilterValidate.IP;
        public const int FILTER_VALIDATE_MAC = (int)FilterValidate.MAC;
        public const int FILTER_VALIDATE_DOMAIN = (int)FilterValidate.DOMAIN;

        /// <summary>
        /// Sanitize filters.
        /// </summary>
        enum FilterSanitize : int
        {
            /// <summary>
            /// ID of default ("string") filter.
            /// </summary>
            FILTER_DEFAULT = FILTER_UNSAFE_RAW, // alias of FILTER_UNSAFE_RAW

            /// <summary>
            /// ID of "string" filter.
            /// </summary>
            STRING = 513,

            /// <summary>
            /// ID of "stripped" filter.
            /// </summary>
            STRIPPED = STRING,   // alias of FILTER_SANITIZE_STRING

            /// <summary>
            /// ID of "encoded" filter.
            /// </summary>
            ENCODED = 514,

            /// <summary>
            /// ID of "special_chars" filter.
            /// </summary>
            SPECIAL_CHARS = 515,

            /// <summary>
            /// ID of "unsafe_raw" filter.
            /// </summary>
            FILTER_UNSAFE_RAW = 516,

            /// <summary>
            /// ID of "email" filter.
            /// Remove all characters except letters, digits and !#$%&amp;'*+-/=?^_`{|}~@.[].
            /// </summary>
            EMAIL = 517,

            /// <summary>
            /// ID of "url" filter.
            /// </summary>
            URL = 518,

            /// <summary>
            /// ID of "number_int" filter.
            /// </summary>
            NUMBER_INT = 519,

            /// <summary>
            /// ID of "number_float" filter.
            /// </summary>
            NUMBER_FLOAT = 520,

            /// <summary>
            /// ID of "magic_quotes" filter.
            /// </summary>
            MAGIC_QUOTES = 521,

            /// <summary>
            /// Equivalent to calling htmlspecialchars() with ENT_QUOTES set.
            /// </summary>
            FULL_SPECIAL_CHARS = 522,

            /// <summary>
            /// add_slashes filter.
            /// </summary>
            ADD_SLASHES = 523,
        }

        public const int FILTER_SANITIZE_STRING = (int)FilterSanitize.STRING;
        public const int FILTER_SANITIZE_STRIPPED = (int)FilterSanitize.STRIPPED;
        public const int FILTER_SANITIZE_ENCODED = (int)FilterSanitize.ENCODED;
        public const int FILTER_SANITIZE_SPECIAL_CHARS = (int)FilterSanitize.SPECIAL_CHARS;
        public const int FILTER_UNSAFE_RAW = (int)FilterSanitize.FILTER_UNSAFE_RAW;
        public const int FILTER_DEFAULT = (int)FilterSanitize.FILTER_DEFAULT;
        public const int FILTER_SANITIZE_EMAIL = (int)FilterSanitize.EMAIL;
        public const int FILTER_SANITIZE_URL = (int)FilterSanitize.URL;
        public const int FILTER_SANITIZE_NUMBER_INT = (int)FilterSanitize.NUMBER_INT;
        public const int FILTER_SANITIZE_NUMBER_FLOAT = (int)FilterSanitize.NUMBER_FLOAT;
        public const int FILTER_SANITIZE_MAGIC_QUOTES = (int)FilterSanitize.MAGIC_QUOTES;
        public const int FILTER_SANITIZE_FULL_SPECIAL_CHARS = (int)FilterSanitize.FULL_SPECIAL_CHARS;
        public const int FILTER_SANITIZE_ADD_SLASHES = (int)FilterSanitize.ADD_SLASHES;

        [Flags]
        public enum FilterFlag : int
        {
            /// <summary>
            /// No flags.
            /// </summary>
            NONE = 0,

            /// <summary>
            /// Allow octal notation (0[0-7]+) in "int" filter.
            /// </summary>
            ALLOW_OCTAL = 1,

            /// <summary>
            /// Allow hex notation (0x[0-9a-fA-F]+) in "int" filter.
            /// </summary>
            ALLOW_HEX = 2,

            /// <summary>
            /// Strip characters with ASCII value less than 32.
            /// </summary>
            STRIP_LOW = 4,

            /// <summary>
            /// Strip characters with ASCII value greater than 127.
            /// </summary>
            STRIP_HIGH = 8,

            /// <summary>
            /// Encode characters with ASCII value less than 32.
            /// </summary>
            ENCODE_LOW = 16,

            /// <summary>
            /// Encode characters with ASCII value greater than 127.
            /// </summary>
            ENCODE_HIGH = 32,

            /// <summary>
            /// Encode &amp;.
            /// </summary>
            ENCODE_AMP = 64,

            /// <summary>
            /// Don't encode ' and ".
            /// </summary>
            NO_ENCODE_QUOTES = 128,

            /// <summary>
            /// ?
            /// </summary>
            EMPTY_STRING_NULL = 256,

            /// <summary>
            /// ?
            /// </summary>
            STRIP_BACKTICK = 512,

            /// <summary>
            /// Allow fractional part in "number_float" filter.
            /// </summary>
            ALLOW_FRACTION = 4096,

            /// <summary>
            /// Allow thousand separator (,) in "number_float" filter.
            /// </summary>
            ALLOW_THOUSAND = 8192,

            /// <summary>
            /// Allow scientific notation (e, E) in "number_float" filter.
            /// </summary>
            ALLOW_SCIENTIFIC = 16384,

            /// <summary>
            /// Require scheme in "validate_url" filter.
            /// </summary>
            SCHEME_REQUIRED = 65536,

            /// <summary>
            /// Require host in "validate_url" filter.
            /// </summary>
            HOST_REQUIRED = 131072,

            /// <summary>
            /// Require path in "validate_url" filter.
            /// </summary>
            PATH_REQUIRED = 262144,

            /// <summary>
            /// Require query in "validate_url" filter.
            /// </summary>
            QUERY_REQUIRED = 524288,

            /// <summary>
            /// Allow only IPv4 address in "validate_ip" filter.
            /// </summary>
            IPV4 = 1048576,

            /// <summary>
            /// Adds ability to specifically validate hostnames
            /// (they must start with an alphanumberic character and contain only alphanumerics or hyphens).
            /// </summary>
            HOSTNAME = 1048576, // yes the same as IPV4

            /// <summary>
            /// Accepts Unicode characters in the local part in "validate_email" filter.
            /// </summary>
            EMAIL_UNICODE = 1048576, // same as HOSTNAME

            /// <summary>
            /// Allow only IPv6 address in "validate_ip" filter.
            /// </summary>
            IPV6 = 2097152,

            /// <summary>
            /// Deny reserved addresses in "validate_ip" filter.
            /// </summary>
            NO_RES_RANGE = 4194304,

            /// <summary>
            /// Deny private addresses in "validate_ip" filter.
            /// </summary>
            NO_PRIV_RANGE = 8388608
        }

        /// <summary>
        /// No flags.
        /// </summary>
        public const int FILTER_FLAG_NONE = (int)FilterFlag.NONE;

        /// <summary>
        /// Allow octal notation (0[0-7]+) in "int" filter.
        /// </summary>
        public const int FILTER_FLAG_ALLOW_OCTAL = (int)FilterFlag.ALLOW_OCTAL;

        /// <summary>
        /// Allow hex notation (0x[0-9a-fA-F]+) in "int" filter.
        /// </summary>
        public const int FILTER_FLAG_ALLOW_HEX = (int)FilterFlag.ALLOW_HEX;

        /// <summary>
        /// Strip characters with ASCII value less than 32.
        /// </summary>
        public const int FILTER_FLAG_STRIP_LOW = (int)FilterFlag.STRIP_LOW;

        /// <summary>
        /// Strip characters with ASCII value greater than 127.
        /// </summary>
        public const int FILTER_FLAG_STRIP_HIGH = (int)FilterFlag.STRIP_HIGH;

        /// <summary>
        /// Encode characters with ASCII value less than 32.
        /// </summary>
        public const int FILTER_FLAG_ENCODE_LOW = (int)FilterFlag.ENCODE_LOW;

        /// <summary>
        /// Encode characters with ASCII value greater than 127.
        /// </summary>
        public const int FILTER_FLAG_ENCODE_HIGH = (int)FilterFlag.ENCODE_HIGH;

        /// <summary>
        /// Encode &amp;.
        /// </summary>
        public const int FILTER_FLAG_ENCODE_AMP = (int)FilterFlag.ENCODE_AMP;

        /// <summary>
        /// Don't encode ' and ".
        /// </summary>
        public const int FILTER_FLAG_NO_ENCODE_QUOTES = (int)FilterFlag.NO_ENCODE_QUOTES;

        /// <summary>
        /// ?
        /// </summary>
        public const int FILTER_FLAG_EMPTY_STRING_NULL = (int)FilterFlag.EMPTY_STRING_NULL;

        /// <summary>
        /// Removes backtick characters from the string, anywhere.
        /// </summary>
        public const int FILTER_FLAG_STRIP_BACKTICK = (int)FilterFlag.STRIP_BACKTICK;

        /// <summary>
        /// Allow fractional part in "number_float" filter.
        /// </summary>
        public const int FILTER_FLAG_ALLOW_FRACTION = (int)FilterFlag.ALLOW_FRACTION;

        /// <summary>
        /// Allow thousand separator (,) in "number_float" filter.
        /// </summary>
        public const int FILTER_FLAG_ALLOW_THOUSAND = (int)FilterFlag.ALLOW_THOUSAND;

        /// <summary>
        /// Allow scientific notation (e, E) in "number_float" filter.
        /// </summary>
        public const int FILTER_FLAG_ALLOW_SCIENTIFIC = (int)FilterFlag.ALLOW_SCIENTIFIC;

        /// <summary>
        /// Require scheme in "validate_url" filter.
        /// </summary>
        public const int FILTER_FLAG_SCHEME_REQUIRED = (int)FilterFlag.SCHEME_REQUIRED;

        /// <summary>
        /// Require host in "validate_url" filter.
        /// </summary>
        public const int FILTER_FLAG_HOST_REQUIRED = (int)FilterFlag.HOST_REQUIRED;

        /// <summary>
        /// Require path in "validate_url" filter.
        /// </summary>
        public const int FILTER_FLAG_PATH_REQUIRED = (int)FilterFlag.PATH_REQUIRED;

        /// <summary>
        /// Require query in "validate_url" filter.
        /// </summary>
        public const int FILTER_FLAG_QUERY_REQUIRED = (int)FilterFlag.QUERY_REQUIRED;

        /// <summary>
        /// Adds ability to specifically validate hostnames
        /// (they must start with an alphanumberic character and contain only alphanumerics or hyphens).
        /// </summary>
        public const int FILTER_FLAG_HOSTNAME = (int)FilterFlag.HOSTNAME;

        /// <summary>
        /// Accepts Unicode characters in the local part in "validate_email" filter.
        /// </summary>
        public const int FILTER_FLAG_EMAIL_UNICODE = (int)FilterFlag.EMAIL_UNICODE;

        /// <summary>
        /// Allow only IPv4 address in "validate_ip" filter.
        /// </summary>
        public const int FILTER_FLAG_IPV4 = (int)FilterFlag.IPV4;

        /// <summary>
        /// Allow only IPv6 address in "validate_ip" filter.
        /// </summary>
        public const int FILTER_FLAG_IPV6 = (int)FilterFlag.IPV6;

        /// <summary>
        /// Deny reserved addresses in "validate_ip" filter.
        /// </summary>
        public const int FILTER_FLAG_NO_RES_RANGE = (int)FilterFlag.NO_RES_RANGE;

        /// <summary>
        /// Deny private addresses in "validate_ip" filter.
        /// </summary>
        public const int FILTER_FLAG_NO_PRIV_RANGE = (int)FilterFlag.NO_PRIV_RANGE;

        #endregion

        #region (NS) filter_input_array, filter_var_array, filter_id, filter_list

        public static PhpValue filter_input_array(int type) => filter_input_array(type, PhpValue.Null);

        /// <summary>
        /// Gets external variables and optionally filters them.
        /// </summary>
        public static PhpValue filter_input_array(int type, PhpValue definition, bool add_empty = true)
        {
            PhpException.FunctionNotSupported("filter_input_array");
            return PhpValue.False;
        }

        /// <summary>
        /// Returns the filter ID belonging to a named filter.
        /// </summary>
        [return: CastToFalse]
        public static int filter_id(string filtername)
        {
            PhpException.FunctionNotSupported("filter_id");
            return -1;
        }

        /// <summary>
        /// Returns a list of all supported filters.
        /// </summary>
        public static PhpArray/*!*/filter_list()
        {
            PhpException.FunctionNotSupported("filter_list");
            return new PhpArray();
        }

        public static PhpValue filter_var_array(PhpArray data) => filter_var_array(data, PhpValue.Null);

        /// <summary>
        /// Gets multiple variables and optionally filters them.
        /// </summary>
        public static PhpValue filter_var_array(PhpArray data, object definition)
        {
            PhpException.FunctionNotSupported("filter_list");
            return PhpValue.False;
        }

        #endregion

        #region filter_input

        /// <summary>
        /// Gets a specific external variable by name and optionally filters it.
        /// </summary>
        public static PhpValue filter_input(Context/*!*/context, FilterInput type, string variable_name, int filter = (int)FilterSanitize.FILTER_DEFAULT, PhpValue options = default)
        {
            var arrayobj = GetArrayByInput(context, type);
            PhpValue value;
            if (arrayobj == null || !arrayobj.TryGetValue(variable_name, out value))
            {
                return PhpValue.Null;
            }
            else
            {
                return filter_var(context, value, filter, options);
            }
        }

        #endregion

        #region filter_var, filter_has_var

        /// <summary>
        /// Checks if variable of specified type exists
        /// </summary>
        public static bool filter_has_var(Context/*!*/context, FilterInput type, string variable_name)
        {
            var arrayobj = GetArrayByInput(context, type);
            if (arrayobj != null)
                return arrayobj.ContainsKey(variable_name);
            else
                return false;
        }

        /// <summary>
        /// Returns <see cref="PhpArray"/> containing required input.
        /// </summary>
        /// <param name="context">Current runtime <see cref="Context"/>.</param>
        /// <param name="type"><see cref="FilterInput"/> value.</param>
        /// <returns>An instance of <see cref="PhpArray"/> or <c>null</c> if there is no such input.</returns>
        private static PhpArray? GetArrayByInput(Context/*!*/context, FilterInput type)
        {
            switch (type)
            {
                case FilterInput.Get:
                    return context.Get;
                case FilterInput.Post:
                    return context.Post;
                case FilterInput.Server:
                    return context.Server;
                case FilterInput.Request:
                    return context.Request;
                case FilterInput.Env:
                    return context.Env;
                case FilterInput.Cookie:
                    return context.Cookie;
                case FilterInput.Session:
                    return context.Session;
                default:
                    return null;
            }
        }

        private static string StripBacktickIfSet(string value, FilterFlag flags)
        {
            if ((flags & FilterFlag.STRIP_BACKTICK) == 0 || string.IsNullOrEmpty(value))
            {
                return value;
            }
            else
            {
                return value.Replace("`", "");
            }
        }

        private static string SanitizeString(string value, FilterFlag flags)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            value = Strings.strip_tags(value);

            //FILTER_FLAG_STRIP_LOW

            //FILTER_FLAG_STRIP_HIGH

            //FILTER_FLAG_STRIP_BACKTICK
            value = StripBacktickIfSet(value, flags);

            //FILTER_FLAG_ENCODE_LOW

            //FILTER_FLAG_ENCODE_HIGH            

            //FILTER_FLAG_NO_ENCODE_QUOTES + FILTER_FLAG_ENCODE_AMP
            if ((flags & FilterFlag.NO_ENCODE_QUOTES) == 0)
            {
                value = Strings.HtmlSpecialCharsEncode(value, 0, value.Length,
                    quoteStyle: Strings.QuoteStyle.BothQuotes,
                    charSet: null,
                    keepExisting: (flags & FilterFlag.ENCODE_AMP) == 0); // sanitize `&` properly
            }
            //FILTER_FLAG_ENCODE_AMP
            else if ((flags & FilterFlag.ENCODE_AMP) != 0)
            {
                value = value.Replace("&", "&#38;");
            }

            //
            return value;
        }

        /// <summary>
        /// Filters a variable with a specified filter.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="variable">Value to filter.</param>
        /// <param name="filter">The ID of the filter to apply.</param>
        /// <param name="options">Associative array of options or bitwise disjunction of flags. If filter accepts options, flags can be provided in "flags" field of array. For the "callback" filter, callback type should be passed. The callback must accept one argument, the value to be filtered, and return the value after filtering/sanitizing it.</param>
        /// <returns>Returns the filtered data, or <c>false</c> if the filter fails.</returns>
        public static PhpValue filter_var(Context ctx, PhpValue variable, int filter = FILTER_DEFAULT, PhpValue options = default)
        {
            var @default = PhpValue.False; // a default value
            PhpArray? options_arr;
            long flags = 0;
            long l; // tmp

            // process options

            if (Operators.IsSet(options))
            {
                options_arr = options.AsArray();
                if (options_arr != null)
                {
                    // [flags]
                    if (options_arr.TryGetValue("flags", out var flagsval))
                    {
                        flagsval.IsLong(out flags);
                    }

                    // [options] => { "min_range" => ??, "default" => ?? }
                    if (options_arr.TryGetValue("options", out var optionsval) && optionsval.IsPhpArray(out var opts_arr))
                    {
                        // [default]
                        if (opts_arr != null && opts_arr.TryGetValue("default", out var defaultval))
                        {
                            @default = defaultval;
                        }
                    }
                }
                else
                {
                    options.IsLong(out flags);
                }
            }
            else
            {
                options_arr = null;
            }

            switch (filter)
            {
                //
                // SANITIZE
                //

                case (int)FilterSanitize.FILTER_DEFAULT:
                    return (PhpValue)variable.ToString(ctx);

                //case (int)FilterSanitize.STRIPPED: // alias to "string" filter
                case (int)FilterSanitize.STRING:

                    if (variable.IsPhpArray(out _) || variable.AsObject() is PhpResource)
                    {
                        return false;
                    }

                    return SanitizeString(variable.ToString(ctx), (FilterFlag)flags);

                case (int)FilterSanitize.ENCODED:
                    return System.Web.HttpUtility.UrlEncode(StripBacktickIfSet(variable.ToString(ctx), (FilterFlag)flags));

                case (int)FilterSanitize.EMAIL:
                    // Remove all characters except letters, digits and !#$%&'*+-/=?^_`{|}~@.[].
                    return (PhpValue)FilterSanitizeString(variable.ToString(ctx), (c) =>
                            (int)c <= 0x7f && (Char.IsLetterOrDigit(c) ||
                            c == '!' || c == '#' || c == '$' || c == '%' || c == '&' || c == '\'' ||
                            c == '*' || c == '+' || c == '-' || c == '/' || c == '=' || c == '!' ||
                            c == '?' || c == '^' || c == '_' || c == '`' || c == '{' || c == '|' ||
                            c == '}' || c == '~' || c == '@' || c == '.' || c == '[' || c == ']'));

                case (int)FilterSanitize.FULL_SPECIAL_CHARS:
                    return Strings.htmlspecialchars(
                        StripBacktickIfSet(variable.ToString(ctx), (FilterFlag)flags),
                        (flags & (long)FilterFlag.NO_ENCODE_QUOTES) != 0 ? Strings.QuoteStyle.NoQuotes : Strings.QuoteStyle.BothQuotes);

                case (int)FilterSanitize.MAGIC_QUOTES: // -->
                case (int)FilterSanitize.ADD_SLASHES:
                    return StringUtils.AddCSlashes(variable.ToString(ctx), true, true, true);

                //
                // VALIDATE
                //

                case (int)FilterValidate.URL:

                    // TODO: protocol may be ommited, try to add "http://" if fails

                    if (Uri.TryCreate(variable.ToString(ctx), UriKind.Absolute, out var uri))
                    {
                        if (uri.IsFile && !uri.OriginalString.StartsWith(uri.Scheme, StringComparison.OrdinalIgnoreCase))
                        {
                            // quick check the file:// was just added on linux impl. of Uri.Parse
                            return @default;
                        }

                        if (flags != 0)
                        {
                            // CONSIDER: rather use `Web.parse_url()` ...
                            var uriflags = (FilterFlag)flags;
                            //if ((uriflags & FilterFlag.PATH_REQUIRED) == FilterFlag.PATH_REQUIRED && ...)
                        }

                        return uri.AbsoluteUri;
                    }
                    return @default;

                case (int)FilterValidate.EMAIL:
                    {
                        return variable.IsString(out var str) && RegexUtilities.IsValidEmail(str)
                            ? (PhpValue)str
                            : @default;
                    }
                case (int)FilterValidate.IP:
                    if (System.Net.IPAddress.TryParse(variable.ToString(ctx), out var addr))
                    {
                        if (flags != 0)
                        {
                            // validate flags:
                            if ((addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 && (flags & (int)FilterFlag.IPV6) == 0) ||
                                (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && (flags & (int)FilterFlag.IPV4) == 0))
                            {
                                return @default;
                            }

                            if ((flags & (int)FilterFlag.NO_PRIV_RANGE) == (int)FilterFlag.NO_PRIV_RANGE)
                            {
                                /*
                                 * Fails validation for the IPv4 ranges: 10.0.0.0/8, 172.16.0.0/12 and 192.168.0.0/16.
                                 * Fails validation for the IPv6 addresses starting with FD or FC.
                                 */
                                throw new NotImplementedException();
                            }

                            if ((flags & (int)FilterFlag.NO_PRIV_RANGE) == (int)FilterFlag.NO_RES_RANGE)
                            {
                                /*
                                 * Fails validation for IPv4 ranges: 0.0.0.0/8, 169.254.0.0/16, 127.0.0.0/8 and 240.0.0.0/4.
                                 * Fails validation for IPv6 ranges: ::1/128, ::/128, ::ffff:0:0/96 and fe80::/10.
                                 */
                                throw new NotImplementedException();
                            }
                        }

                        return addr.ToString();
                    }
                    else
                    {
                        return @default;
                    }

                case (int)FilterValidate.INT:
                    {
                        // TODO: switch:

                        if (variable.IsLong(out l))
                        {
                            // ok
                        }
                        else if (variable.IsString(out var str) && long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out l))
                        {
                            // ok
                        }
                        else if (variable.IsBoolean(out var b))
                        {
                            l = b ? 1 : 0;
                        }
                        else if (variable.IsDouble(out var d))
                        {
                            l = (long)d;
                        }
                        else
                        {
                            // null
                            // array
                            // invalid string
                            // object
                            return @default;
                        }

                        //

                        if (Operators.IsSet(options))
                        {
                            PhpException.ArgumentValueNotSupported("options", "!null");
                        }

                        // TODO: options: min_range, max_range

                        return l;
                    }
                case (int)FilterValidate.BOOLEAN:
                    {
                        if (variable.IsBoolean(out var b))
                        {
                            return b;
                        }

                        var varstr = variable.ToString(ctx);

                        // TRUE for "1", "true", "on" and "yes".

                        if (varstr.EqualsOrdinalIgnoreCase("1") ||
                            varstr.EqualsOrdinalIgnoreCase("true") ||
                            varstr.EqualsOrdinalIgnoreCase("on") ||
                            varstr.EqualsOrdinalIgnoreCase("yes"))
                        {
                            return PhpValue.True;
                        }

                        //
                        if ((flags & FILTER_NULL_ON_FAILURE) == FILTER_NULL_ON_FAILURE)
                        {
                            // FALSE is for "0", "false", "off", "no", and "",
                            // NULL for all non-boolean values

                            if (varstr.Length == 0 ||
                                varstr.EqualsOrdinalIgnoreCase("0") ||
                                varstr.EqualsOrdinalIgnoreCase("false") ||
                                varstr.EqualsOrdinalIgnoreCase("off"))
                            {
                                return PhpValue.False;
                            }
                            else
                            {
                                return PhpValue.Null;
                            }
                        }
                        else
                        {
                            // FALSE otherwise
                            return PhpValue.False;
                        }
                    }
                case (int)FilterValidate.REGEXP:
                    {
                        // options = options['regexp']
                        if (options_arr != null &&
                            options_arr.TryGetValue("regexp", out var regexpval))
                        {
                            if (PCRE.preg_match(regexpval.ToString(ctx), variable.ToString(ctx)) > 0)
                            {
                                return variable;
                            }
                        }
                        else
                        {
                            PhpException.InvalidArgument("options", string.Format(Resources.LibResources.option_missing, "regexp"));
                        }

                        return @default;
                    }

                case FILTER_CALLBACK:
                    // options = ['options' => $callback]
                    if (options_arr != null &&
                        options_arr.TryGetValue("options", out var callbackvar))
                    {
                        return callbackvar.AsCallable().Invoke(ctx, variable);
                    }

                    return @default;

                case FILTER_VALIDATE_MAC:
                    //try
                    //{
                    var value = variable.ToString(ctx);
                    //var macaddr = System.Net.NetworkInformation.PhysicalAddress.Parse(value).GetAddressBytes(); // only validates "dash" format
                    //if (macaddr.Length == 6)
                    //{
                    //    return value;
                    //}
                    if (RegexUtilities.IsValidMacAddress(value))
                    {
                        return value;
                    }

                    //}
                    //catch
                    //{
                    //}

                    return @default;

                case FILTER_VALIDATE_DOMAIN:
                    value = variable.ToString(ctx);
                    return Uri.CheckHostName(value) != UriHostNameType.Unknown
                        ? value
                        : @default;

                default:
                    PhpException.ArgumentValueNotSupported(nameof(filter), filter);
                    break;
            }

            return PhpValue.False;
        }

        #endregion

        #region Helper filter methods

        private static class RegexUtilities
        {
            //static readonly Lazy<Regex> ValidEmailRegex = new Lazy<Regex>(() => new Regex(
            //        @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
            //        @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$",
            //        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled),
            //    System.Threading.LazyThreadSafetyMode.None);

            //static readonly Lazy<Regex> ValidEmailUnicodeRegex = new Lazy<Regex>(() => new Regex(
            //        @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=(?:[0-9a-z]|[^\x20-\x7E]))@))" +
            //        @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
            //        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled),
            //    System.Threading.LazyThreadSafetyMode.None);

            static readonly Lazy<Regex> s_lazyMacAddressRegex = new Lazy<Regex>(
                () => new Regex(
                    @"^(?:[0-9a-fA-F]{2}:){5}[0-9a-fA-F]{2}|(?:[0-9a-fA-F]{2}-){5}[0-9a-fA-F]{2}|(?:[0-9a-fA-F]{2}){5}[0-9a-fA-F]{2}|(?:[0-9a-fA-F]{4}.){2}[0-9a-fA-F]{4}$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled),
                System.Threading.LazyThreadSafetyMode.None);

            public static bool IsValidMacAddress(string physicaladdress) => !string.IsNullOrEmpty(physicaladdress) && s_lazyMacAddressRegex.Value.IsMatch(physicaladdress);

            public static bool IsValidEmail(string strIn)
            {
                if (string.IsNullOrEmpty(strIn) || strIn.Length > 320)
                {
                    return false;
                }

                // Use IdnMapping class to convert Unicode domain names.
                try
                {
                    strIn = Regex.Replace(strIn, @"(@)(.+)$", match => DomainMapper(match));
                }
                catch (ArgumentException)
                {
                    return false;
                }

                // use MailAddress parser:
                try
                {
                    var addr = new System.Net.Mail.MailAddress(strIn);
                    var valid = addr.Address == strIn;

                    return valid;
                }
                catch
                {
                    return false;
                }

                //// Return true if strIn is in valid e-mail format.
                //return ValidEmailRegex.Value.IsMatch(strIn);
            }

            static string DomainMapper(Match match)
            {
                string domainName = match.Groups[2].Value;

                // IdnMapping class with default property values.
                var idn = new IdnMapping();
                domainName = idn.GetAscii(domainName);

                return match.Groups[1].Value + domainName;
            }
        }

        /// <summary>
        /// Remove all characters not valid by given <paramref name="predicate"/>.
        /// </summary>
        private static string FilterSanitizeString(string str, Predicate<char>/*!*/predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (str == null)
            {
                return string.Empty;
            }

            StringBuilder? newstr = null;

            int from = 0;
            for (int i = 0; i < str.Length; i++)
            {
                // "remove" not allowed characters:
                if (!predicate(str[i]))
                {
                    if (newstr == null)
                    {
                        newstr = StringBuilderUtilities.Pool.Get();
                    }

                    newstr.Append(str, from, i - from);
                    from = i + 1;
                }
            }

            if (newstr == null)
            {
                // all characters matched predicate:
                return str;
            }
            else
            {
                // finalize the string:
                if (from < str.Length)
                {
                    newstr.Append(str, from, str.Length - from);
                }

                return StringBuilderUtilities.GetStringAndReturn(newstr);
            }
        }

        #endregion
    }
}
