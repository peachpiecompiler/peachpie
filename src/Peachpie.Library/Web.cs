using Pchp.Core;
using Pchp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pchp.Library
{
    public static class Web
    {
        #region Constants

        public const int PHP_URL_SCHEME = 0;
        public const int PHP_URL_HOST = 1;
        public const int PHP_URL_PORT = 2;
        public const int PHP_URL_USER = 3;
        public const int PHP_URL_PASS = 4;
        public const int PHP_URL_PATH = 5;
        public const int PHP_URL_QUERY = 6;
        public const int PHP_URL_FRAGMENT = 7;

        public const int CONNECTION_NORMAL = 0;
        public const int CONNECTION_ABORTED = 1;
        public const int CONNECTION_TIMEOUT = 2;

        #endregion

        #region base64_decode, base64_encode

        [return: CastToFalse]
        public static PhpString base64_decode(string encoded_data, bool strict = false)
        {
            if (encoded_data == null)
            {
                return null;
            }

            try
            {
                return new PhpString(System.Convert.FromBase64String(encoded_data));
            }
            catch (FormatException)
            {
                // TODO: Err
                //PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_base64_encoded_data"));
                throw new ArgumentException();
            }
        }

        [return: CastToFalse]
        public static string base64_encode(Context ctx, PhpString data_to_encode)
        {
            if (data_to_encode == null)
            {
                return null;
            }

            return System.Convert.ToBase64String(data_to_encode.ToBytes(ctx.StringEncoding));
        }

        #endregion

        #region parse_url, parse_str

        #region Helper parse_url() methods

        internal static class ParseUrlMethods
        {
            /// <summary>
            /// Regular expression for parsing URLs (via parse_url())
            /// </summary>
            public static Regex ParseUrlRegEx
            {
                get
                {
                    return
                        (_parseUrlRegEx) ??
                        (_parseUrlRegEx = new Regex(@"^((?<scheme>[^:]+):(?<scheme_separator>/{0,2}))?((?<user>[^:@/?#\[\]]*)(:(?<pass>[^@/?#\[\]]*))?@)?(?<host>([^/:?#\[\]]+)|(\[[^\[\]]+\]))?(:(?<port>[0-9]*))?(?<path>/[^\?#]*)?(\?(?<query>[^#]+)?)?(#(?<fragment>.*))?$",
                            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase));
                }
            }
            private static Regex _parseUrlRegEx = null;

            /// <summary>
            /// Determines matched group value or null if the group was not matched.
            /// </summary>
            /// <param name="g"></param>
            /// <returns></returns>
            public static string MatchedString(Group/*!*/g)
            {
                Debug.Assert(g != null);

                return (g.Success && g.Value.Length > 0) ? g.Value : null;
            }

            /// <summary>
            /// Replace all the occurrences of control characters (see iscntrl() C++ function) with the specified character.
            /// </summary>
            public static string ReplaceControlCharset(string/*!*/str, char newChar)
            {
                Debug.Assert(str != null);

                StringBuilder sb = null;
                int last = 0;
                for (int i = 0; i < str.Length; i++)
                {
                    if (char.IsControl(str[i]))
                    {
                        if (sb == null) sb = new StringBuilder(str.Length);
                        sb.Append(str, last, i - last);
                        sb.Append(newChar);
                        last = i + 1;
                    }
                }

                if (sb != null)
                {
                    sb.Append(str, last, str.Length - last);
                    return sb.ToString();
                }
                else
                {
                    return str;
                }
            }
        }

        #endregion

        /// <summary>
		/// Parses an URL and returns its components.
		/// </summary>
		/// <param name="url">
		/// The URL string with format 
        /// <c>{scheme}://{user}:{pass}@{host}:{port}{path}?{query}#{fragment}</c>
		/// or <c>{schema}:{path}?{query}#{fragment}</c>.
		/// </param>
        /// <returns>
		/// An array which keys are names of components (stated in URL string format in curly braces, e.g."schema")
		/// and values are components themselves.
		/// </returns>
        [return: CastToFalse]
        public static PhpArray parse_url(string url)
        {
            var match = ParseUrlMethods.ParseUrlRegEx.Match(url ?? string.Empty);

            if (match == null || !match.Success || match.Groups["port"].Value.Length > 5)   // not matching or port number too long
            {
                //PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_url", FileSystemUtils.StripPassword(url)));
                //return null;
                // TODO: Err
                throw new ArgumentException();
            }

            string scheme = ParseUrlMethods.MatchedString(match.Groups["scheme"]);
            string user = ParseUrlMethods.MatchedString(match.Groups["user"]);
            string pass = ParseUrlMethods.MatchedString(match.Groups["pass"]);
            string host = ParseUrlMethods.MatchedString(match.Groups["host"]);
            string port = ParseUrlMethods.MatchedString(match.Groups["port"]);
            string path = ParseUrlMethods.MatchedString(match.Groups["path"]);
            string query = ParseUrlMethods.MatchedString(match.Groups["query"]);
            string fragment = ParseUrlMethods.MatchedString(match.Groups["fragment"]);

            string scheme_separator = match.Groups["scheme_separator"].Value;   // cannot be null

            int tmp;

            // some exceptions
            if (host != null && scheme != null && scheme_separator.Length == 0 && int.TryParse(host, out tmp))
            {   // domain:port/path
                port = host;
                host = scheme;
                scheme = null;
            }
            else if (scheme_separator.Length != 2 && host != null)
            {   // mailto:user@host
                // st:xx/zzz
                // mydomain.com/path
                // mydomain.com:port/path

                // dismiss user and pass
                if (user != null || pass != null)
                {
                    if (pass != null) user = user + ":" + pass;
                    host = user + "@" + host;

                    user = null;
                    pass = null;
                }

                // dismiss port
                if (port != null)
                {
                    host += ":" + port;
                    port = null;
                }

                // everything as a path
                path = scheme_separator + host + path;
                host = null;
            }

            PhpArray result = new PhpArray(0, 8);

            const char neutralChar = '_';

            // store segments into the array (same order as it is in PHP)
            if (scheme != null) result["scheme"] = (PhpValue)ParseUrlMethods.ReplaceControlCharset(scheme, neutralChar);
            if (host != null) result["host"] = (PhpValue)ParseUrlMethods.ReplaceControlCharset(host, neutralChar);
            if (port != null) result["port"] = (PhpValue)unchecked((ushort)uint.Parse(port)); // PHP overflows in this way
            if (user != null) result["user"] = (PhpValue)ParseUrlMethods.ReplaceControlCharset(user, neutralChar);
            if (pass != null) result["pass"] = (PhpValue)ParseUrlMethods.ReplaceControlCharset(pass, neutralChar);
            if (path != null) result["path"] = (PhpValue)ParseUrlMethods.ReplaceControlCharset(path, neutralChar);
            if (query != null) result["query"] = (PhpValue)ParseUrlMethods.ReplaceControlCharset(query, neutralChar);
            if (fragment != null) result["fragment"] = (PhpValue)ParseUrlMethods.ReplaceControlCharset(fragment, neutralChar);

            return result;
        }

        /// <summary>
		/// Parses an URL and returns its components.
		/// </summary>
		/// <param name="url">
		/// The URL string with format 
        /// <c>{scheme}://{user}:{pass}@{host}:{port}{path}?{query}#{fragment}</c>
		/// or <c>{schema}:{path}?{query}#{fragment}</c>.
		/// </param>
        /// <param name="component">Specify one of PHP_URL_SCHEME, PHP_URL_HOST, PHP_URL_PORT, PHP_URL_USER, PHP_URL_PASS, PHP_URL_PATH, PHP_URL_QUERY or PHP_URL_FRAGMENT to retrieve just a specific URL component as a string (except when PHP_URL_PORT is given, in which case the return value will be an integer).</param>
		public static string parse_url(string url, int component)
        {
            var array = parse_url(url);
            if (array != null)
            {
                switch (component)
                {
                    case PHP_URL_FRAGMENT: return array["fragment"].AsString();
                    case PHP_URL_HOST: return array["host"].AsString();
                    case PHP_URL_PASS: return array["pass"].AsString();
                    case PHP_URL_PATH: return array["path"].AsString();
                    case PHP_URL_PORT: return array["port"].AsString(); // might be null
                    case PHP_URL_QUERY: return array["query"].AsString();
                    case PHP_URL_SCHEME: return array["scheme"].AsString();
                    case PHP_URL_USER: return array["user"].AsString();

                    default:
                        //PhpException.Throw(PhpError.Warning, LibResources.GetString("arg:invalid_value", "component", component));                        
                        throw new ArgumentException(nameof(component));
                }
            }

            //
            return null;
        }

        /// <summary>
        /// Parses a string as if it were the query string passed via an URL.
        /// </summary>
        /// <param name="str">The string to parse.</param>
        /// <param name="result">The array to store the variable found in <paramref name="str"/> to.</param>
        public static void parse_str(string str, out PhpArray result)
        {
            parse_str(result = new PhpArray(), str);
        }

        /// <summary>
        /// Parses a string as if it were the query string passed via an URL and sets variables in the
        /// current scope.
        /// </summary>
        /// <param name="locals">Array of local variables passed from runtime, will be filled with parsed variables. Must not be <c>null</c>.</param>
        /// <param name="str">The string to parse.</param>
        public static void parse_str([ImportLocals]PhpArray locals, string str)
        {
            Debug.Assert(locals != null);

            if (!string.IsNullOrEmpty(str))
            {
                UriUtils.ParseQuery(str, locals.AddVariable);
            }
        }

        #endregion

        #region header, header_remove

        /// <summary>
        /// Regullar expression matching "HTTP/1.0 (StatusCode)".
        /// </summary>
        readonly static Regex _header_regex_statuscode = new Regex("[ ]*HTTP/[^ ]* ([0-9]{1,3}).*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
		/// Adds a specified header to the current response.
		/// </summary>
        /// <param name="ctx">Runtime context.</param>
		/// <param name="str">The header to be added.</param>
		/// <param name="replace">Whether the header should be replaced if there is already one with the same name. 
		/// Replacement not supported (ignored since 5.1.2)</param>
		/// <param name="http_response_code">Sets the response status code.</param>
		/// <remarks>
		/// <para>
		/// If <paramref name="http_response_code"/> is positive than the response status code is set to this value.
		/// Otherwise, if <paramref name="str"/> has format "{spaces}HTTP/{no spaces} {response code}{whatever}" 
		/// then the response code is set to the {responce code} and the method returns.
		/// </para>
		/// <para>
		/// If <paramref name="str"/> has format "{name}:{value}" then the respective header is set (both name and value 
		/// are trimmed) and an appropriate action associated with this header by ASP.NET is performed.
		/// </para>
		/// <para>This function prevents more than one header to be sent at once as 
		/// a protection against header injection attacks (which means that header is always replaced).
		/// </para>
		/// </remarks>
		public static void header(Context ctx, string str, bool replace = true, int http_response_code = 0)
        {
            var webctx = ctx.HttpPhpContext;
            if (webctx == null || string.IsNullOrEmpty(str) || webctx.HeadersSent)
            {
                return;
            }

            // response code is not forced => checks for initial HTTP/ and the status code in "str":  
            if (http_response_code <= 0)
            {
                var m = _header_regex_statuscode.Match(str);
                if (m.Success)
                {
                    webctx.StatusCode = int.Parse(m.Groups[1].Value);
                    return;
                }
            }
            else
            {
                // sets response status code:
                webctx.StatusCode = http_response_code;
            }

            // adds a header if it has a correct form (i.e. "name: value"):
            // store header in collection associated with current context - headers can be
            // replaced and are flushed automatically (in BeforeHeadersSent event :-)) on IIS Classic Mode.
            int i = str.IndexOf(':');
            if (i > 0)
            {
                string name = str.Substring(0, i).Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    webctx.SetHeader(name, str.Substring(i + 1).Trim());
                }
            }
        }

        /// <summary>
        /// Removes an HTTP header previously set using header().
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="name">The header name to be removed or <c>null</c> to remove all headers.
        /// Note: This parameter is case-insensitive. 
        /// </param>
        /// <remarks>Caution: This function will remove all headers set by PHP, including cookies, session and the X-Powered-By headers.</remarks>
        public static void header_remove(Context ctx, string name = null)
        {
            var webctx = ctx.HttpPhpContext;
            if (webctx != null)
            {
                if (name != null)
                {
                    webctx.RemoveHeader(name);
                }
                else
                {
                    webctx.RemoveHeaders();
                }

                // TODO: cookies, session
            }
        }

        #endregion

        #region headers_sent, headers_list

        /// <summary>
        /// Checks whether all headers has been sent.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <returns>Whether headers has already been sent.</returns>
        public static bool headers_sent(Context ctx)
        {
            var webctx = ctx.HttpPhpContext;
            return webctx != null && webctx.HeadersSent;
        }

        /// <summary>
        /// Checks whether all headers has been sent.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="file">The name of a source file which has sent headers or an empty string 
        /// headers has not been sent yet. Not supported.</param>
        /// <returns>Whether headers has already been sent.</returns>
        /// <exception cref="PhpException">Web server variables are not available (Warning).</exception>
        /// <exception cref="PhpException">Function is not supported in this version (Warning).</exception>
        public static bool headers_sent(Context ctx, out string file)
        {
            // TODO: Err // PhpException.FunctionNotSupported();
            file = string.Empty;
            return headers_sent(ctx);
        }

        /// <summary>
        /// Checks whether all headers has been sent.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="file">The name of a source file which has sent headers or an empty string  if
        /// headers has not been sent yet. Not supported.</param>
        /// <param name="line">The line in a source file where headers has been sent or 0 if 
        /// headers has not been sent yet. Not supported.</param>
        /// <returns>Whether headers has already been sent.</returns>
        public static bool headers_sent(Context ctx, out string file, out long line)
        {
            // TODO: Err // PhpException.FunctionNotSupported();
            file = string.Empty;
            line = 0;
            return headers_sent(ctx);
        }

        /// <summary>
        /// headers_list() will return a list of headers to be sent to the browser / client.
        /// To determine whether or not these headers have been sent yet, use headers_sent(). 
        /// </summary>
        public static PhpArray headers_list(Context ctx)
        {
            var webctx = ctx.HttpPhpContext;
            if (webctx == null)
            {
                return null;
            }

            var list = new PhpArray();

            //foreach (var x in ScriptContext.CurrentContext.Headers)
            //{
            //    list.Add(x.Key + ": " + x.Value);
            //}

            /*foreach (var x in context.Response.Cookies.AllKeys)
            {
                var cookie = context.Response.Cookies[x];
                list.Add("set-cookie: " + cookie.Name + "=" + cookie.Value);    // TODO: full cookie spec
            }*/

            // TODO: cookies, session

            return list;
        }

        #endregion
    }
}
