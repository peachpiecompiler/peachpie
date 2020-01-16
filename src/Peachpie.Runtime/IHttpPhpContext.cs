#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Interface providing web features.
    /// </summary>
    public interface IHttpPhpContext
    {
        #region headers

        /// <summary>Gets value indicating HTTP headers were already sent.</summary>
        bool HeadersSent { get; }

        void SetHeader(string name, string value, bool append = false);

        void RemoveHeader(string name);

        void RemoveHeaders();

        /// <summary>Enumerates HTTP headers in current response.</summary>
        IEnumerable<KeyValuePair<string, string>> GetHeaders();

        /// <summary>Gets or sets current cache-control header.</summary>
        string CacheControl { get; set; }

        /// <summary>
        /// Event fired before headers are sent.
        /// </summary>
        event Action HeadersSending;

        #endregion

        #region request

        /// <summary>
        /// Gets the current request headers.
        /// </summary>
        IEnumerable<KeyValuePair<string, IEnumerable<string>>> RequestHeaders { get; }

        #endregion

        /// <summary>
        /// Gets or sets HTTP response status code.
        /// </summary>
        int StatusCode { get; set; }

        /// <summary>
        /// Stream with contents of the incoming HTTP entity body.
        /// </summary>
        Stream InputStream { get; }

        /// <summary>
        /// Adds a cookie into the response.
        /// </summary>
        void AddCookie(string name, string value, DateTimeOffset? expires, string path = "/", string? domain = null, bool secure = false, bool httpOnly = false);

        /// <summary>
        /// Flushes the response stream of the HTTP server.
        /// </summary>
        void Flush();

        /// <summary>
        /// Gets max request size (upload size, post size) in bytes.
        /// Gets <c>-1</c> if limit is not set.
        /// </summary>
        long MaxRequestSize { get; }

        /// <summary>
        /// Whether the underlaying connection is alive.
        /// </summary>
        bool IsClientConnected { get; }

        #region session

        /// <summary>
        /// Gets or sets session handler for current context.
        /// </summary>
        PhpSessionHandler SessionHandler { get; set; }

        /// <summary>
        /// Gets or sets session state.
        /// </summary>
        PhpSessionState SessionState { get; set; }

        #endregion
    }
}
