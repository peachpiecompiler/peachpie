using System;
using System.Collections.Generic;
using System.Net;
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

        public string DefaultSheme { get; set; } = "http";

        public bool FollowLocation { get; set; } = false;

        public int MaxRedirects { get; set; } = 50;

        /// <summary>
        /// The contents of the "User-Agent: " header to be used in a HTTP request.
        /// </summary>
        public string UserAgent { get; set; }

        public string Referer { get; set; }

        public string Method { get; set; } = WebRequestMethods.Http.Get;

        /// <summary>
        /// The full data to post in a HTTP "POST" operation.
        /// This parameter can either be passed as a urlencoded string like 'para1=val1&amp;para2=val2&amp;...' or as an array with the field name as key and field data as value.
        /// If value is an array, the Content-Type header will be set to multipart/form-data.
        /// </summary>
        public PhpValue PostFields { get; set; } = PhpValue.Void;

        /// <summary>
        /// Headers to be send with the request.
        /// Keys of the array are ignored, values are in form of <c>header-name: value</c>
        /// </summary>
        public PhpArray Headers { get; set; }

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
        /// The file that the transfer should be read from when uploading using <c>PUT</c> method.
        /// </summary>
        public PhpStream PutStream { get; set; }

        #endregion

        /// <summary>
        /// Response after the execution.
        /// </summary>
        internal CURLResponse Result { get; set; }

        public CURLResource() : base(CURLConstants.CurlResourceName)
        {
        }

        protected override void FreeManaged()
        {
            // clear references
            this.Result = null;
            this.OutputTransfer = null;
            this.PutStream = null;
            this.Headers = null;
            this.PostFields = PhpValue.Void;

            //
            base.FreeManaged();
        }
    }

    sealed class CURLResponse
    {
        readonly HttpWebResponse _responseObj;

        /// <summary>
        /// Error code number if exception happened.
        /// </summary>
        public int ErrorCode { get; set; } = CURLConstants.CURLE_OK;

        /// <summary>
        /// Optional. Error message.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets value indicating the request errored.
        /// </summary>
        public bool HasError => ErrorCode != CURLConstants.CURLE_OK;

        public Uri ResponseUri => _responseObj.ResponseUri;

        public int StatusCode => (int)_responseObj.StatusCode;

        public DateTime LastModified => _responseObj.LastModified;

        public string ContentType => _responseObj.ContentType;

        public int HeaderSize => _responseObj.Headers.ToByteArray().Length;

        public TimeSpan TotalTime { get; set; }

        public PhpValue ExecValue { get; }

        public static CURLResponse CreateError(int errcode, Exception ex = null) => new CURLResponse(PhpValue.False) { ErrorCode = errcode, ErrorMessage = ex?.Message };

        public CURLResponse(PhpValue execvalue, WebResponse responseObj = null)
        {
            this.ExecValue = execvalue;
            _responseObj = (HttpWebResponse)responseObj;
        }
    }
}
