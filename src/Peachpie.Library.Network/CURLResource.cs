using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Utilities;
using Pchp.Library.Streams;

namespace Peachpie.Library.Network
{
    /// <summary>
    /// CURL resource.
    /// </summary>
    public sealed class CURLResource : PhpResource
    {
        #region Flags

        /// <summary>
        /// Various boolean properties.
        /// </summary>
        [Flags]
        enum Flags
        {
            StoreRequestHeaders = 1,
            Verbose = 2,
            FailOnError = 4,
            FollowLocation = 8,
            SafeUpload = 16,
        }

        #endregion

        #region Properties

        readonly Dictionary<Type, ICurlOption> _options = new Dictionary<Type, ICurlOption>();

        /// <summary>
        /// Various options whichs value is x^2 can be stored here as a flag.
        /// </summary>
        Flags _flags;

        /// <summary><c>CURLINFO_HEADER_OUT</c> option.</summary>
        public bool StoreRequestHeaders
        {
            get => (_flags & Flags.StoreRequestHeaders) != 0;
            set
            {
                if (value) _flags |= Flags.StoreRequestHeaders;
                else _flags &= ~Flags.StoreRequestHeaders;
            }
        }

        /// <summary>
        /// Whether to enable verbose output to STDERR (or <see cref="VerboseOutput"/>).
        /// </summary>
        public bool Verbose
        {
            get => (_flags & Flags.Verbose) != 0;
            set
            {
                if (value) _flags |= Flags.Verbose;
                else _flags &= ~Flags.Verbose;
            }
        }

        public string Url { get; set; }

        public string DefaultSheme { get; set; } = "http";

        public bool FollowLocation
        {
            get => (_flags & Flags.FollowLocation) != 0;
            set
            {
                if (value) _flags |= Flags.FollowLocation;
                else _flags &= ~Flags.FollowLocation;
            }
        }

        // default is -1 for an infinite number of redirects in curl
        public int MaxRedirects { get; set; } = -1;

        /// <summary>
        /// The maximum number of milliseconds to allow cURL functions to execute.
        /// </summary>
        public int Timeout { get; set; } = 0; // default of curl is 0 which means it never times out during transfer

        /// <summary>
        /// Gets or sets a timeout, in milliseconds, to wait until the 100-Continue is received from the server.
        /// </summary>
        public int ContinueTimeout { get; set; } = 1000; // libcurl default

        /// <summary>If set, specifies the size of internal buffer used for read when passing response content to user's function.</summary>
        public int BufferSize { get; set; } = 2048;

        /// <summary>
        /// Alternative output for <see cref="Verbose"/>.
        /// </summary>
        public PhpStream VerboseOutput { get; set; }

        /// <summary>
        /// <c>TRUE</c> to fail verbosely if the HTTP code returned is greater than or equal to 400.
        /// The default behavior is to return the page normally, ignoring the code.	
        /// <see cref="CURLConstants.CURLOPT_FAILONERROR"/> flag.
        /// </summary>
        public bool FailOnError
        {
            get => (_flags & Flags.FailOnError) != 0;
            set
            {
                if (value) _flags |= Flags.FailOnError;
                else _flags &= ~Flags.FailOnError;
            }
        }

        /// <summary>
        /// <see cref="CURLConstants.CURLOPT_SAFE_UPLOAD"/> option.
        /// </summary>
        public bool SafeUpload
        {
            get => (_flags & Flags.SafeUpload) != 0;
            set
            {
                if (value) _flags |= Flags.SafeUpload;
                else _flags &= ~Flags.SafeUpload;
            }
        }

        public string Method { get; set; } = WebRequestMethods.Http.Get;

        /// <summary>
        /// The full data to post in a HTTP "POST" operation.
        /// This parameter can either be passed as a urlencoded string like 'para1=val1&amp;para2=val2&amp;...' or as an array with the field name as key and field data as value.
        /// If value is an array, the Content-Type header will be set to multipart/form-data.
        /// </summary>
        public PhpValue PostFields { get; set; } = default;

        /// <summary>
        /// The value of the Cookie header.
        /// Ignored if already present in <see cref="Headers"/>.
        /// </summary>
        public string CookieHeader { get; set; }

        /// <summary>
        /// As long as <see cref="CURLConstants.CURLOPT_COOKIEFILE"/> is set (regardless of the value, even
        /// null suffices), the cookies retrieved from the server are recorded.
        /// Otherwise this reference is <c>null</c>.
        /// </summary>
        public CookieContainer CookieContainer { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string ProxyType { get; set; } = "http";

        public string ProxyHost { get; set; }

        public int ProxyPort { get; set; } = 1080;

        public string ProxyUsername { get; set; }

        public string ProxyPassword { get; set; }

        /// <summary>
        /// Specify how to process headers.
        /// WARN: if <see cref="ProcessingResponse"/> is RETURN => STDOUT means RETURN.
        /// </summary>
        public ProcessMethod ProcessingHeaders = ProcessMethod.Ignore;

        /// <summary>
        /// Specify how to process content.
        /// </summary>
        public ProcessMethod ProcessingResponse = ProcessMethod.StdOut;

        /// <summary>
        /// Specify how to process request stream.
        /// </summary>
        public ProcessMethod ProcessingRequest = new ProcessMethod() { Method = ProcessMethodEnum.FILE };

        /// <summary>
        /// Bit mask of enabled protocols. All by default.
        /// </summary>
        internal int Protocols { get; set; } = CURLConstants.CURLPROTO_ALL;

        #endregion

        internal DateTime StartTime { get; set; }

        /// <summary>
        /// Optional.
        /// Request headers sent including the leading line (GET / HTTP) and trailing newline (\n\n).
        /// </summary>
        internal string RequestHeaders { get; set; }

        /// <summary>
        /// Ongoing request handled by the framework. Must be set to null after being processed.
        /// </summary>
        internal Task<WebResponse> ResponseTask { get; set; }

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
            this.ProcessingHeaders = ProcessMethod.Ignore;
            this.ProcessingResponse = ProcessMethod.StdOut;
            this.ProcessingRequest = new ProcessMethod() { Method = ProcessMethodEnum.FILE };
            this.PostFields = default;
            this.VerboseOutput = null;

            this._options.Clear();

            //
            base.FreeManaged();
        }

        /// <summary>
        /// Sets cURL option.
        /// </summary>
        internal void SetOption<TOption>(TOption option) where TOption : ICurlOption
        {
            _options[typeof(TOption)] = option;
        }

        /// <summary>
        /// Gets option value.
        /// </summary>
        internal bool TryGetOption<TOption>(out TOption option) where TOption : ICurlOption
        {
            if (_options.TryGetValue(typeof(TOption), out var x))
            {
                option = (TOption)x;
                return true;
            }

            option = default;
            return false;
        }

        internal bool RemoveOption<TOption>() where TOption : ICurlOption
        {
            return _options.Remove(typeof(TOption));
        }

        /// <summary>
        /// Gets enumeration of set of additional options.
        /// </summary>
        internal IEnumerable<ICurlOption>/*!*/Options => _options.Values;

        /// <summary>
        /// Applies all the options to the request.
        /// </summary>
        internal void ApplyOptions(Context ctx,  WebRequest request)
        {
            foreach (var option in this.Options)
            {
                option.Apply(ctx, request);
            }
        }
    }

    #region ProcessMethod, ProcessMethodEnum

    /// <summary>
    /// How to process the data (headers, read, write).
    /// </summary>
    public enum ProcessMethodEnum
    {
        /// <summary>
        /// Data will be written to the output.
        /// </summary>
        STDOUT = 0,

        /// <summary>
        /// Data will be wrtten to (file) stream. See <see cref="ProcessMethod.Stream"/>.
        /// </summary>
        FILE = 1,

        /// <summary>
        /// Data will be passed to a user function. See <see cref="ProcessMethod.User"/>.
        /// </summary>
        USER = 2,

        ///// <summary>
        ///// Data will be passed from <see cref="ProcessMethod.Stream"/> if provided.
        ///// </summary>
        //DIRECT = 3,

        /// <summary>
        /// Data will be returned from `exec` as string.
        /// </summary>
        RETURN = 4,

        /// <summary>
        /// Data are ignored.
        /// </summary>
        IGNORE = 7,
    }

    /// <summary>
    /// Specifies how to process data (headers, read, write).
    /// </summary>
    public struct ProcessMethod
    {
        public static ProcessMethod StdOut => new ProcessMethod(ProcessMethodEnum.STDOUT);
        public static ProcessMethod Return => new ProcessMethod(ProcessMethodEnum.RETURN);
        public static ProcessMethod Ignore => new ProcessMethod(ProcessMethodEnum.IGNORE);

        public ProcessMethod(ProcessMethodEnum method) { Method = method; Stream = null; User = null; }
        public ProcessMethod(PhpStream stream) : this(ProcessMethodEnum.FILE) { Debug.Assert(stream != null); Stream = stream; }
        public ProcessMethod(IPhpCallable user) : this(ProcessMethodEnum.USER) { Debug.Assert(user != null); User = user; }

        public ProcessMethodEnum Method;
        public PhpStream Stream;
        public IPhpCallable User;

        /// <summary>Whether there is no routine to be called.</summary>
        public bool IsEmpty => Method == ProcessMethodEnum.IGNORE;
    }

    #endregion

    sealed class CURLResponse
    {
        /// <summary>
        /// Gets empty response with all the values zero (response of not executed request).
        /// </summary>
        public static CURLResponse Empty => new CURLResponse();

        /// <summary>
        /// Error code number if exception happened.
        /// </summary>
        public CurlErrors ErrorCode { get; set; } = CurlErrors.CURLE_OK;

        /// <summary>
        /// Optional. Error message.
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Gets value indicating the request errored.
        /// </summary>
        public bool HasError => ErrorCode != CURLConstants.CURLE_OK;

        public Uri ResponseUri { get; }

        public HttpStatusCode StatusCode { get; }

        public DateTime? LastModified
        {
            get
            {
                if (DateTime.TryParse(Headers?[HttpRequestHeader.LastModified], out var dt))
                {
                    return dt;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets <c>lastmodified</c> header as a Unix time stamp.
        /// Gets <c>-1</c> if header is not specified.
        /// </summary>
        public long LastModifiedTimeStamp
        {
            get
            {
                var date = this.LastModified;
                if (date.HasValue)
                {
                    return DateTimeUtils.UtcToUnixTimeStamp(date.Value);
                }
                else
                {
                    return -1;
                }
            }
        }

        /// <summary>
        /// Content length of download, read from Content-Length: field.
        /// If not specified, gets <c>-1</c>.
        /// </summary>
        public long ContentLength => Headers != null && long.TryParse(Headers[HttpRequestHeader.ContentLength], out var length) ? length : -1;

        public string ContentType => (Headers != null) ? Headers[HttpRequestHeader.ContentType] : null;

        public string StatusHeader { get; set; } = string.Empty;

        public int HeaderSize =>
            (string.IsNullOrEmpty(StatusHeader) ? 0 : (StatusHeader.Length + HttpHeaders.HeaderSeparator.Length)) +
            ((Headers != null && Headers.Count != 0) ? Headers.ToByteArray().Length : 0);

        public WebHeaderCollection Headers { get; }

        public string RequestHeaders { get; }

        public CookieCollection Cookies { get; }

        public TimeSpan TotalTime { get; set; }

        /// <summary>
        /// Private data set to the requesting handle.
        /// </summary>
        public PhpValue Private { get; set; }

        public PhpValue ExecValue { get; }

        public static CURLResponse CreateError(CurlErrors errcode, Exception ex = null) =>
            new CURLResponse(PhpValue.False)
            {
                ErrorCode = errcode,
                ErrorMessage = ex?.Message,
            };

        public CURLResponse(PhpValue execvalue, HttpWebResponse response = null, CURLResource ch = null)
        {
            this.ExecValue = execvalue;

            if (response != null)
            {
                this.ResponseUri = response.ResponseUri;
                this.StatusCode = response.StatusCode;
                this.Headers = response.Headers;
                this.StatusHeader = HttpHeaders.StatusHeader(response);
                this.Cookies = response.Cookies;
            }

            if (ch != null)
            {
                this.RequestHeaders = ch.RequestHeaders;
            }
        }

        private CURLResponse()
        {
            // everything to default, not exected request
        }
    }
}
