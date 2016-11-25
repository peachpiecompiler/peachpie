using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pchp.Library
{
    public static class Web
    {
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
            var webctx = ctx.HttpContext;
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
            var webctx = ctx.HttpContext;
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
    }
}
