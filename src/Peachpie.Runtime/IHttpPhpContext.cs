using System;
using System.Collections.Generic;
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
    }
}
