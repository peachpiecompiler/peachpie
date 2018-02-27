using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;
using Pchp.Library.Streams;

namespace Peachpie.Library.Network
{
    /// <summary>
    /// CURL resource.
    /// </summary>
    public sealed class CURLResource : PhpResource
    {
        #region Properties

        public string Url { get; set; }

        public bool FollowLocation { get; set; } = false;

        public int MaxRedirects { get; set; } = 50;

        /// <summary>
        /// The contents of the "User-Agent: " header to be used in a HTTP request.
        /// </summary>
        public string UserAgent { get; set; }

        public string Referer { get; set; }

        /// <summary>
        /// TRUE to do a regular HTTP POST. This POST is the normal application/x-www-form-urlencoded kind, most commonly used by HTML forms.
        /// </summary>
        public bool IsPost { get; set; } = false;

        ///// <summary>
        ///// The full data to post in a HTTP "POST" operation.
        ///// This parameter can either be passed as a urlencoded string like 'para1=val1&para2=val2&...' or as an array with the field name as key and field data as value.
        ///// If value is an array, the Content-Type header will be set to multipart/form-data.
        ///// </summary>
        //public string PostFields { get; set; }

        public bool ReturnTransfer { get; set; } = false;

        /// <summary>
        /// <c>true</c> to include the header in the output.
        /// Default is <c>false</c>.
        /// </summary>
        public bool OutputHeader { get; set; } = false;

        /// <summary>
        /// The file that the transfer should be written to.
        /// </summary>
        public PhpStream OutputTransfer { get; set; }

        /// <summary>
        /// The file that the transfer should be read from when uploading.
        /// </summary>
        public PhpStream InputTransfer { get; set; }

        #endregion

        public CURLResource() : base(CURLConstants.CurlResourceName)
        {
        }

        protected override void FreeManaged()
        {
            base.FreeManaged();
        }
    }
}
