using Pchp.Core;
using Pchp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.Threading;

namespace Pchp.Library
{
    [PhpExtension("standard")]
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

        public const int PHP_QUERY_RFC1738 = (int)PhpQueryRfc.RFC1738;
        public const int PHP_QUERY_RFC3986 = (int)PhpQueryRfc.RFC3986;

        enum PhpQueryRfc
        {
            RFC1738 = 1,
            RFC3986 = 2,
        }

        #endregion

        #region base64_decode, base64_encode

        [return: CastToFalse]
        public static PhpString base64_decode(string encoded_data, bool strict = false)
        {
            if (string.IsNullOrEmpty(encoded_data))
            {
                return default; // FALSE
            }

            try
            {
                return new PhpString(Base64Utils.FromBase64(encoded_data.AsSpan(), strict));
            }
            catch (FormatException)
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.invalid_base64_encoded_data);
                return default; // FALSE
            }
        }

        [return: CastToFalse]
        public static string base64_encode(Context ctx, PhpString data_to_encode)
        {
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
            url ??= string.Empty;

            if (url.Length == 0)
            {
                // empty URL results in following array to be returned:
                return new PhpArray(1)
                {
                    { "path", string.Empty },
                };
            }

            var match = ParseUrlMethods.ParseUrlRegEx.Match(url);

            if (match == null || !match.Success || match.Groups["port"].Value.Length > 5)   // not matching or port number too long
            {
                //PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_url", FileSystemUtils.StripPassword(url))); // PHP 5.3.3+ does not emit warning
                return null;
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

            // parse the port number,
            // invalid port number causes the function to return FALSE
            var port_int = port != null ? uint.Parse(port) : 0;
            if (port_int > ushort.MaxValue || port_int < 0)
            {
                return null;
            }

            var result = new PhpArray(8);

            const char neutralChar = '_';

            // store segments into the array (same order as it is in PHP)
            if (scheme != null) result["scheme"] = ParseUrlMethods.ReplaceControlCharset(scheme, neutralChar);
            if (host != null) result["host"] = ParseUrlMethods.ReplaceControlCharset(host, neutralChar);
            if (port != null) result["port"] = port_int;
            if (user != null) result["user"] = ParseUrlMethods.ReplaceControlCharset(user, neutralChar);
            if (pass != null) result["pass"] = ParseUrlMethods.ReplaceControlCharset(pass, neutralChar);
            if (path != null) result["path"] = ParseUrlMethods.ReplaceControlCharset(path, neutralChar);
            if (query != null) result["query"] = ParseUrlMethods.ReplaceControlCharset(query, neutralChar);
            if (fragment != null) result["fragment"] = ParseUrlMethods.ReplaceControlCharset(fragment, neutralChar);

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
        /// <returns>The URL component or <c>NULL</c> if the requested component is not parsed.</returns>
		public static PhpValue parse_url(string url, int component)
        {
            var item = PhpValue.Null;

            var array = parse_url(url);
            if (array != null)
            {
                if (component < 0)
                {
                    // negative {component} results in the whole array to be returned
                    item = array;
                }
                else
                {
                    switch (component)
                    {
                        case PHP_URL_FRAGMENT: item = array["fragment"]; break;
                        case PHP_URL_HOST: item = array["host"]; break;
                        case PHP_URL_PASS: item = array["pass"]; break;
                        case PHP_URL_PATH: item = array["path"]; break;
                        case PHP_URL_PORT: item = array["port"]; break;
                        case PHP_URL_QUERY: item = array["query"]; break;
                        case PHP_URL_SCHEME: item = array["scheme"]; break;
                        case PHP_URL_USER: item = array["user"]; break;

                        default:
                            //PhpException.Throw(PhpError.Warning, LibResources.GetString("arg_invalid_value", "component", component));                        
                            throw new ArgumentException(nameof(component));
                    }
                }
            }

            //
            return item;
        }

        /// <summary>
        /// Parses a string as if it were the query string passed via an URL.
        /// </summary>
        /// <param name="str">The string to parse.</param>
        /// <param name="result">The array to store the variable found in <paramref name="str"/> to.</param>
        public static void parse_str(string str, out PhpArray result)
        {
            result = new PhpArray();
            parse_str(result, str);
        }

        /// <summary>
        /// Parses a string as if it were the query string passed via an URL and sets variables in the
        /// current scope.
        /// </summary>
        /// <param name="locals">Array of local variables passed from runtime, will be filled with parsed variables. Must not be <c>null</c>.</param>
        /// <param name="str">The string to parse.</param>
        public static void parse_str([ImportValue(ImportValueAttribute.ValueSpec.Locals)] PhpArray locals, string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                UriUtils.ParseQuery(str, locals.AddVariable);
            }
        }

        #endregion

        #region setcookie, setrawcookie

        /// <summary>
        /// Sends a cookie with specified name, value and expiration timestamp.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="name">The name of the cookie to send.</param>
        /// <param name="value">The value of the cookie. The value will be <see cref="UrlEncode"/>d.</param>
        /// <param name="expire">The time (Unix timestamp) when the cookie expires.</param>
        /// <param name="path">The virtual path on server in which is the cookie valid.</param>
        /// <param name="domain">The domain where the cookie is valid.</param>
        /// <param name="secure">Whether to transmit the cookie securely (that is, over HTTPS only).</param>
        /// <param name="httponly">When TRUE the cookie will be made accessible only through the HTTP protocol.
        /// This means that the cookie won't be accessible by scripting languages, such as JavaScript.
        /// This setting can effectively help to reduce identity theft through XSS attacks
        /// (although it is not supported by all browsers).</param>
        /// <returns>Whether a cookie has been successfully send.</returns>
        public static bool setcookie(Context ctx, string name, string value = null, int expire = 0, string path = null, string domain = null, bool secure = false, bool httponly = false)
        {
            return SetCookieInternal(ctx, name, value, expire, path, domain, secure, httponly, false);
        }

        /// <summary>
        /// The same as <see cref="setcookie(Context, string, string, int, string, string, bool, bool)"/> except for that value is not <see cref="UrlEncode"/>d.
        /// </summary>
        public static bool setrawcookie(Context ctx, string name, string value = null, int expire = 0, string path = null, string domain = null, bool secure = false, bool httponly = false)
        {
            return SetCookieInternal(ctx, name, value, expire, path, domain, secure, httponly, true);
        }

        /// <summary>
        /// Internal version common for <see cref="setcookie"/> and <see cref="setrawcookie"/>.
        /// </summary>
        internal static bool SetCookieInternal(Context ctx, string name, string value, int expire, string path, string domain, bool secure, bool httponly, bool raw)
        {
            var httpctx = ctx.HttpPhpContext;
            if (httpctx == null)
            {
                return false;
            }

            DateTimeOffset? expires;
            if (expire > 0)
            {
                expires = new DateTimeOffset(DateTimeUtils.UnixTimeStampToUtc(expire).ToLocalTime());
            }
            else
            {
                expires = null;
            }

            httpctx.AddCookie(name, raw ? value : WebUtility.UrlEncode(value), expires, path ?? "/", domain, secure, httponly);

            return true;
        }

        #endregion

        #region header, header_remove

        ///// <summary>
        ///// Regular expression matching "HTTP/1.0 (StatusCode)".
        ///// </summary>
        //readonly static Lazy<Regex> s_header_regex_statuscode = new Lazy<Regex>(
        //    () => new Regex("[ ]*HTTP/[^ ]* ([0-9]{1,3}).*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled),
        //    LazyThreadSafetyMode.None);

        /// <summary>
        /// Checks the given string for `<code>[ ]*HTTP/[^ ]* ([0-9]{1,3}).*</code>`.
        /// </summary>
        /// <param name="header">Input string.</param>
        /// <param name="code">If matches, gets the HTTP status code</param>
        /// <returns></returns>
        static bool TryMatchHttpStatusHeader(ReadOnlySpan<char> header, out int code)
        {
            code = default;

            //var m = s_header_regex_statuscode.Value.Match(header);
            //if (m.Success)
            //{
            //    code = int.Parse(m.Groups[1].Value);
            //    return true;
            //}

            // HTTP/* 123.*
            const string prefix = "HTTP/";
            if (header.StartsWith(prefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                header = header.Slice(prefix.Length);
                var statusAt = header.IndexOf(' ');
                if (statusAt >= 0)
                {
                    header = header.Slice(statusAt + 1);
                    // naive int.TryParse(span, count);
                    for (int i = 0; i < header.Length && i < 3; i++)
                    {
                        var c = header[i];
                        var n = c - '0';
                        if (n >= 0 && n <= 9)   // digit
                        {
                            code = (code * 10) + n;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            //
            return code != 0;
        }

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
                PhpException.Throw(PhpError.Notice, Resources.Resources.headers_has_been_sent);
                return;
            }

            // response code is not forced => checks for initial HTTP/ and the status code in "str":
            var header = str.AsSpan().Trim();

            if (http_response_code <= 0)
            {
                if (TryMatchHttpStatusHeader(header, out var status))
                {
                    webctx.StatusCode = status;
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

            int i = header.IndexOf(':');
            if (i > 0)
            {
                var name = header.Slice(0, i).TrimEnd();
                if (name.Length != 0)
                {
                    var value = header.Slice(i + 1).TrimStart().ToString();
                    webctx.SetHeader(name.ToString(), value, append: !replace);
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
                try
                {
                    if (name != null)
                    {
                        webctx.RemoveHeader(name);
                    }
                    else
                    {
                        webctx.RemoveHeaders();
                    }
                }
                catch
                {
                    // can't remove, webctx.HeadersSent
                }

                // TODO: cookies, session
            }
        }

        #endregion

        #region http_response_code

        /// <summary>
        /// Get or Set the HTTP response code.
        /// </summary>
        public static int http_response_code(Context ctx, int response_code = 0)
        {
            var webctx = ctx.HttpPhpContext;
            if (webctx == null)
            {
                return -1; // TRUE|FALSE
            }

            //
            var code = webctx.StatusCode;
            if (response_code > 0)
            {
                webctx.StatusCode = response_code;
            }

            //
            return code;
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

            foreach (var x in webctx.GetHeaders())
            {
                if (string.IsNullOrEmpty(x.Value))
                {
                    list.Add(x.Key);
                }
                else
                {
                    list.Add(x.Key + ": " + x.Value);
                }
            }

            /*foreach (var x in context.Response.Cookies.AllKeys)
            {
                var cookie = context.Response.Cookies[x];
                list.Add("set-cookie: " + cookie.Name + "=" + cookie.Value);    // TODO: full cookie spec
            }*/

            // TODO: cookies, session

            return list;
        }

        #endregion

        #region header_register_callback

        /// <summary>
        /// Registers a function that will be called when PHP starts sending output.
        /// </summary>
        public static bool header_register_callback(Context ctx, IPhpCallable callback)
        {
            var webctx = ctx.HttpPhpContext;
            if (webctx == null || callback == null)
            {
                return false;
            }

            webctx.HeadersSending += () => { callback.Invoke(ctx); };

            return true;
        }

        #endregion

        #region http_build_query, get_browser

        static string UrlEncode(string value, PhpQueryRfc type)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            switch (type)
            {
                case PhpQueryRfc.RFC3986:
                    // NOTE: this is not correct,
                    // behavior depends on IRI configuration
                    // see https://docs.microsoft.com/en-us/dotnet/api/system.uri.escapeuristring#remarks
                    return Uri.EscapeUriString(value);

                case PhpQueryRfc.RFC1738:
                default:
                    // ' ' encoded as '+'
                    return WebUtility.UrlEncode(value);
            }
        }

        /// <summary>
        /// Generates a URL-encoded query string from the associative (or indexed) array provided. 
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="formData">
        /// The array form may be a simple one-dimensional structure, or an array of arrays
        /// (who in turn may contain other arrays). 
        /// </param>
        /// <param name="numericPrefix">
        /// If numeric indices are used in the base array and this parameter is provided,
        /// it will be prepended to the numeric index for elements in the base array only.
        /// This is meant to allow for legal variable names when the data is decoded by PHP
        /// or another CGI application later on.
        /// </param>
        /// <param name="argSeparator">
        /// arg_separator.output is used to separate arguments, unless this parameter is
        /// specified, and is then used. 
        /// </param>
        /// <param name="encType"></param>
        /// <returns>Returns a URL-encoded string </returns>
        public static string http_build_query(Context ctx, PhpValue formData, string numericPrefix = null, string argSeparator = null, int encType = PHP_QUERY_RFC1738)
        {
            return http_build_query(ctx, formData, numericPrefix, argSeparator ?? "&", (PhpQueryRfc)encType, null);
        }

        static string http_build_query(Context ctx, PhpValue formData, string numericPrefix, string argSeparator, PhpQueryRfc encType, string indexerPrefix)
        {
            var result = new StringBuilder(64);
            var first = true;

            var enumerator = formData.GetForeachEnumerator(false, default);
            while (enumerator.MoveNext())
            {
                var key = enumerator.CurrentKey;
                var value = enumerator.CurrentValue;

                // the query parameter name (key name)
                // the parameter name is URL encoded
                string keyName = key.IsLong(out var l)
                    ? UrlEncode(numericPrefix, encType) + l.ToString()
                    : UrlEncode(key.ToStringOrThrow(ctx), encType);

                if (indexerPrefix != null)
                {
                    keyName = indexerPrefix + "%5B" + keyName + "%5D";  // == prefix[key] (url encoded brackets)
                }

                // write the query element

                if (value.IsPhpArray(out var valueArray))
                {
                    // value is an array, emit query recursively, use current keyName as an array variable name

                    var queryStr = http_build_query(ctx, valueArray, null, argSeparator, encType, keyName);  // emit the query recursively
                    if (string.IsNullOrEmpty(queryStr) == false)
                    {
                        if (!first)
                        {
                            result.Append(argSeparator);
                        }

                        result.Append(queryStr);
                    }
                }
                else
                {
                    // simple value, emit query in a form of (key=value), URL encoded !

                    if (!first)
                    {
                        result.Append(argSeparator);
                    }

                    result.Append(keyName);
                    result.Append("=");
                    result.Append(UrlEncode(value.ToStringOrThrow(ctx), encType));    // == "keyName=keyValue"
                }

                // separator will be used in next loop
                first = false;
            }

            return result.ToString();
        }

        /// <summary>
        /// Attempts to determine the capabilities of the user's browser, by looking up the browser's information in the browscap.ini  file.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="user_agent">
        /// The User Agent to be analyzed. By default, the value of HTTP User-Agent header is used; however, you can alter this (i.e., look up another browser's info) by passing this parameter.
        /// You can bypass this parameter with a NULL value.
        /// </param>
        /// <param name="return_array">If set to TRUE, this function will return an array instead of an object . </param>
        /// <returns>
        ///  The information is returned in an object or an array which will contain various data elements representing,
        ///  for instance, the browser's major and minor version numbers and ID string; TRUE/FALSE  values for features
        ///  such as frames, JavaScript, and cookies; and so forth.
        ///  The cookies value simply means that the browser itself is capable of accepting cookies and does not mean
        ///  the user has enabled the browser to accept cookies or not. The only way to test if cookies are accepted is
        ///  to set one with setcookie(), reload, and check for the value. 
        /// </returns>
        public static PhpValue get_browser(Context ctx, string user_agent = null, bool return_array = false)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region get_headers

        /// <summary>
        /// Fetches all the headers sent by the server in response to an HTTP request.
        /// </summary>
        /// <param name="url">The target URL.</param>
        /// <param name="format">If the optional <paramref name="format"/> parameter is set to non-zero, <see cref="get_headers"/>() parses the response and sets the array's keys.</param>
        /// <param name="context">A valid context resource created with <see cref="Streams.PhpContexts.stream_context_create"/>().</param>
        /// <returns></returns>
        [return: CastToFalse]
        public static PhpArray get_headers(string url, int format = 0, PhpResource context = null)
        {
            var arr = new PhpArray();

            var streamcontext = Streams.StreamContext.GetValid(context, allowNull: true);

            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    var response = client.GetAsync(url, System.Net.Http.HttpCompletionOption.ResponseHeadersRead).Result;

                    arr.Add($"HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}");

                    foreach (var h in response.Headers)
                    {
                        var value = string.Join(", ", h.Value);

                        if (format == 0)
                        {
                            arr.Add(h.Key + ": " + value);
                        }
                        else
                        {
                            arr[h.Key] = value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
                return null;
            }

            return arr;
        }

        #endregion

        #region getallheaders, apache_request_headers

        /// <summary>
        /// Fetch all HTTP request headers.
        /// </summary>
        /// <returns>An associative array of all the HTTP headers in the current request, or FALSE on failure.</returns>
        [return: CastToFalse]
        public static PhpArray apache_request_headers(Context ctx) => getallheaders(ctx);

        /// <summary>
        /// Fetch all HTTP request headers.
        /// </summary>
        /// <returns>An associative array of all the HTTP headers in the current request, or FALSE on failure.</returns>
        [return: CastToFalse]
        public static PhpArray getallheaders(Context ctx)
        {
            var webctx = ctx.HttpPhpContext;
            if (webctx != null)
            {
                var headers = webctx.RequestHeaders;
                if (headers != null)
                {
                    var result = new PhpArray(16);

                    foreach (var h in headers)
                    {
                        result[h.Key] = string.Join(", ", h.Value) ?? string.Empty;
                    }

                    return result;
                }
            }

            return null;
        }

        #endregion

        #region rawurlencode, rawurldecode, urlencode, urldecode

        /// <summary>
        /// Decode URL-encoded strings
        /// </summary>
        /// <param name="str">The URL string (e.g. "hello%20from%20foo%40bar").</param>
        /// <returns>Decoded string (e.g. "hello from foo@bar")</returns>
        [return: NotNull]
        public static string rawurldecode(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            return WebUtility.UrlDecode(str.Replace("+", "%2B"));  // preserve '+'
        }

        /// <summary>
        /// Encodes a URL string keeping spaces in it. Spaces are encoded as '%20'.
        /// </summary>  
        /// <param name="str">The string to be encoded.</param>
        /// <returns>The encoded string.</returns>
        [return: NotNull]
        public static string rawurlencode(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            return UpperCaseEncodedChars(UrlEncode(str, PhpQueryRfc.RFC3986)); // ' ' => '%20'
        }

        /// <summary>
        /// Decodes a URL string.
        /// </summary>
        [return: NotNull]
        public static string urldecode(Context ctx, string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            return System.Web.HttpUtility.UrlDecode(str, ctx.StringEncoding);
        }

        /// <summary>
        /// Encodes a URL string. Spaces are encoded as '+'.
        /// </summary>  
        [return: NotNull]
        public static string urlencode(Context ctx, string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            return UpperCaseEncodedChars(System.Web.HttpUtility.UrlEncode(str, ctx.StringEncoding));    // ' ' => '+'
        }

        static string UpperCaseEncodedChars(string encoded)
        {
            char[] temp = encoded.ToCharArray();
            for (int i = 0; i < temp.Length; i++)
            {
                if (temp[i] == '%' && i < temp.Length - 2)
                {
                    temp[i + 1] = char.ToUpperInvariant(temp[i + 1]);
                    temp[i + 2] = char.ToUpperInvariant(temp[i + 2]);
                }
            }
            return new string(temp);
        }

        #endregion

        #region is_uploaded_file, move_uploaded_file

        /// <summary>
        /// Tells whether the file was uploaded via HTTP POST.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="path"></param>
        public static bool is_uploaded_file(Context ctx, string path)
        {
            return !string.IsNullOrEmpty(path) && ctx.IsTemporaryFile(path);
        }

        /// <summary>
        /// Moves an uploaded file to a new location.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="path">Temporary path of the uploaded file.</param>
        /// <param name="destination">Destination.</param>
        /// <returns>Value indicating the move succeeded.</returns>
        public static bool move_uploaded_file(Context ctx, string path, string destination)
        {
            if (path == null || !ctx.IsTemporaryFile(path))
            {
                return false;
            }

            // overwrite destination unconditionally:
            if (PhpPath.file_exists(ctx, destination))
            {
                PhpPath.unlink(ctx, destination);
            }

            // move temp file to destination:
            if (PhpPath.rename(ctx, path, destination))
            {
                ctx.RemoveTemporaryFile(path);
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region idn_to_ascii, idn_to_utf8

        public const int IDNA_DEFAULT = 0;
        public const int IDNA_ALLOW_UNASSIGNED = 1;
        public const int IDNA_USE_STD3_RULES = 2;

        public const int INTL_IDNA_VARIANT_2003 = 0;
        public const int INTL_IDNA_VARIANT_UTS46 = 1;

        /// <summary>
        /// Convert domain name to IDNA ASCII form.
        /// </summary>
        [return: CastToFalse]
        public static string idn_to_ascii(string domain, int options = IDNA_DEFAULT, int variant = INTL_IDNA_VARIANT_UTS46)
        {
            return new System.Globalization.IdnMapping().GetAscii(domain);
        }

        /// <summary>
        /// Convert domain name to IDNA ASCII form.
        /// </summary>
        //[return: CastToFalse]
        public static string idn_to_ascii(string domain, int options, int variant, ref PhpArray idna_info)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Convert domain name from IDNA ASCII to Unicode
        /// </summary>
        //[return: CastToFalse]
        public static string idn_to_utf8(string domain, int options = IDNA_DEFAULT, int variant = INTL_IDNA_VARIANT_UTS46)
        {
            return new System.Globalization.IdnMapping().GetUnicode(domain);
        }

        /// <summary>
        /// Convert domain name from IDNA ASCII to Unicode
        /// </summary>
        //[return: CastToFalse]
        public static string idn_to_utf8(string domain, int options, int variant, ref PhpArray idna_info)
        {
            throw new NotImplementedException();
        }


        #endregion

        #region set_time_limit, ignore_user_abort, connection_aborted, connection_status

        /// <summary>
        /// Sets the request time-out in seconds (configuration option "max_execution_time").
        /// No value is returned.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="seconds">The time-out setting for request.</param>
        public static bool set_time_limit(Context ctx, int seconds)
        {
            //ctx.ApplyExecutionTimeout(seconds);

            return false;
        }

        /// <summary>
        /// Get a value of a configuration option "ignore_user_abort".
        /// </summary>
        /// <returns>The current value of the option.</returns>
        public static bool ignore_user_abort(Context ctx)
        {
            return ctx.Configuration.Core.IgnoreUserAbort;
        }

        /// <summary>
        /// Sets a value of a configuration option "ignore_user_abort".
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="value">The new value of the option.</param>
        /// <returns>The previous value of the option.</returns>
        /// <exception cref="PhpException">Web request PHP context is not available (Warning).</exception>
        public static bool ignore_user_abort(Context ctx, bool value)
        {
            if (!ctx.IsWebApplication) return true;

            bool result = ctx.Configuration.Core.IgnoreUserAbort;
            ctx.Configuration.Core.IgnoreUserAbort = value;

            //// enables/disables disconnection tracking:
            //ctx.TrackClientDisconnection = !value;

            return result;
        }

        /// <summary>
        /// Returns connection status bitfield.
        /// </summary>
        public static int connection_status(Context ctx)
        {
            int result = CONNECTION_NORMAL;

            var http = ctx.HttpPhpContext;
            if (http != null)
            {
                //if (http.ExecutionTimedOut)
                //    result |= CONNECTION_TIMEOUT;

                if (!http.IsClientConnected)
                    result |= CONNECTION_ABORTED;
            }

            return result;
        }

        /// <summary>
        /// Check whether client disconnected.
        /// </summary>
        public static int connection_aborted(Context ctx)
        {
            var http = ctx.HttpPhpContext;
            if (http != null && !http.IsClientConnected)
            {
                return 1;
            }

            return 0;
        }

        #endregion
    }
}
