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

        void SetHeader(string name, string value);

        void RemoveHeader(string name);

        void RemoveHeaders();

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
        void AddCookie(string name, string value, DateTimeOffset? expires, string path = "/", string domain = null, bool secure = false, bool httpOnly = false);

        /// <summary>
        /// Flushes the response stream of the HTTP server.
        /// </summary>
        void Flush();
    }
}
