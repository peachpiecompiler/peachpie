#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Utilities;

namespace Peachpie.Library.Network
{
    [PhpExtension(CURLConstants.ExtensionName)]
    public static class CURLFunctions
    {
        /// <summary>
        /// Create a CURLFile object.
        /// </summary>
        [return: NotNull]
        public static CURLFile/*!*/curl_file_create(string filename, string? mimetype = null, string? postname = null) => new CURLFile(filename, mimetype, postname);

        [return: NotNull]
        public static CURLResource/*!*/curl_init(string? url = null) => new CURLResource() { Url = url };

        /// <summary>
        /// Close a cURL session.
        /// </summary>
        public static void curl_close(Context ctx, CURLResource resource)
        {
            if (resource != null)
            {
                if (resource.TryGetOption<CurlOption_CookieJar>(out var jar))
                {
                    jar.PrintCookies(ctx, resource);
                }

                //
                resource.Dispose();
            }
            else
            {
                PhpException.ArgumentNull(nameof(resource));
            }
        }

        /// <summary>
        /// Sets an option on the given cURL session handle.
        /// </summary>
        public static bool curl_setopt(
            [ImportValue(ImportValueAttribute.ValueSpec.CallerClass)]RuntimeTypeHandle callerCtx,
            CURLResource ch, int option, PhpValue value)
        {
            return ch.TrySetOption(option, value, callerCtx);
        }

        /// <summary>
        /// URL encodes the given string.
        /// </summary>
        public static string curl_escape(CURLResource ch, string str) => WebUtility.UrlEncode(str);

        /// <summary>
        /// Decodes the given URL encoded string.
        /// </summary>
        public static string curl_unescape(CURLResource ch, string str) => WebUtility.UrlDecode(str);

        /// <summary>
        /// Gets cURL version information.
        /// </summary>
        /// <param name="age">Ignored.
        /// Should be set to the version of this functionality by the time you write your program.
        /// This way, libcurl will always return a proper struct that your program understands, while programs
        /// in the future might get a different struct.
        /// <c>CURLVERSION_NOW</c> will be the most recent one for the library you have installed.</param>
        /// <returns>Array with version information.</returns>
        [return: NotNull]
        public static PhpArray curl_version(int age = CURLConstants.CURLVERSION_NOW)
        {
            // version_number       cURL 24 bit version number
            // version              cURL version number, as a string
            // ssl_version_number   OpenSSL 24 bit version number
            // ssl_version          OpenSSL version number, as a string
            // libz_version         zlib version number, as a string
            // host                 Information about the host where cURL was built
            // age
            // features             A bitmask of the CURL_VERSION_XXX constants
            // protocols            An array of protocols names supported by cURL

            var fakever = CURLConstants.FakeCurlVersion;

            return new PhpArray(9)
            {
                {"version_number", (fakever.Major << 16) | (fakever.Minor << 8) | (fakever.Build) },
                {"age", CURLConstants.CURLVERSION_NOW},
                {"features", CURLConstants.CURL_VERSION_HTTP2|CURLConstants.CURL_VERSION_IPV6|CURLConstants.CURL_VERSION_KERBEROS4|CURLConstants.CURL_VERSION_LIBZ|CURLConstants.CURL_VERSION_SSL},
                {"ssl_version_number", 0}, // always 0
                {"version", fakever.ToString()},
                {"host", "dotnet"},
                {"ssl_version", ""},
                {"libz_version", "1"},
                {"protocols", new PhpArray(){ "http", "https", "ftp" } },
            };
        }

        /// <summary>
        /// Set multiple options for a cURL transfer.
        /// </summary>
        public static bool curl_setopt_array(
            [ImportValue(ImportValueAttribute.ValueSpec.CallerClass)]RuntimeTypeHandle callerCtx,
            CURLResource ch, PhpArray options)
        {
            if (ch == null || !ch.IsValid)
            {
                PhpException.ArgumentNull(nameof(ch));
                return false;
            }

            if (options != null)
            {
                var enumerator = options.GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    var key = enumerator.CurrentKey;

                    if (key.IsInteger && ch.TrySetOption(key.Integer, enumerator.CurrentValue, callerCtx))
                    {
                        // ok
                        continue;
                    }
                    else
                    {
                        // stop on first fail and return FALSE
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Return the last error number.
        /// </summary>
        public static int curl_errno(CURLResource ch)
            => (int)(ch?.Result != null ? ch.Result.ErrorCode : CurlErrors.CURLE_OK);

        /// <summary>
        /// Return a string containing the last error for the current session.
        /// </summary>
        public static string curl_error(CURLResource ch)
        {
            if (ch != null && ch.Result != null)
            {
                var err = ch.Result.ErrorCode;
                if (err != CurlErrors.CURLE_OK)
                {
                    return ch.Result.ErrorMessage ?? err.ToString(); // TODO: default error messages in resources
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Get information regarding a specific transfer.
        /// </summary>
        public static PhpValue curl_getinfo(CURLResource ch, int opt = 0)
        {
            if (ch == null)
            {
                PhpException.ArgumentNull(nameof(ch));
                return PhpValue.Null;
            }

            var r = ch.Result ?? CURLResponse.Empty;

            switch (opt)
            {
                case 0:
                    // array of all information
                    var arr = new PhpArray(38)
                    {
                        { "url", ch.Url ?? string.Empty },
                        { "content_type", r.ContentType },
                        { "http_code", (long)r.StatusCode },
                        { "header_size", r.HeaderSize },
                        { "filetime", r.LastModifiedTimeStamp },
                        { "total_time", r.TotalTime.TotalSeconds },
                        { "download_content_length", r.ContentLength },
                        { "redirect_url", ch.FollowLocation && r.ResponseUri != null ? r.ResponseUri.AbsoluteUri : string.Empty },
                        //{ "http_version", CURL_HTTP_VERSION_*** }
                        //{ "protocol", CURLPROTO_*** },
                        //{ "scheme", STRING },
                    };

                    if (ch.RequestHeaders != null)
                    {
                        arr["request_header"] = ch.RequestHeaders;
                    }

                    return arr;
                case CURLConstants.CURLINFO_EFFECTIVE_URL:
                    return ch.Url ?? string.Empty;
                case CURLConstants.CURLINFO_REDIRECT_URL:
                    return (ch.FollowLocation && r.ResponseUri != null ? r.ResponseUri.AbsoluteUri : string.Empty);
                case CURLConstants.CURLINFO_HTTP_CODE:
                    return (int)r.StatusCode;
                case CURLConstants.CURLINFO_FILETIME:
                    return r.LastModifiedTimeStamp;
                case CURLConstants.CURLINFO_CONTENT_TYPE:
                    return r.ContentType;
                case CURLConstants.CURLINFO_CONTENT_LENGTH_DOWNLOAD:
                    return r.ContentLength;
                case CURLConstants.CURLINFO_TOTAL_TIME:
                    return r.TotalTime.TotalSeconds;
                case CURLConstants.CURLINFO_PRIVATE:
                    return Operators.IsSet(r.Private) ? r.Private.DeepCopy() : PhpValue.False;
                case CURLConstants.CURLINFO_COOKIELIST:
                    return ((ch.CookieContainer != null && ch.Result != null) ? CreateCookiePhpArray(ch.Result.Cookies) : PhpArray.Empty);
                case CURLConstants.CURLINFO_HEADER_SIZE:
                    return r.HeaderSize;
                case CURLConstants.CURLINFO_HEADER_OUT:
                    return r.RequestHeaders ?? PhpValue.False;
                default:
                    PhpException.ArgumentValueNotSupported(nameof(opt), opt);
                    return PhpValue.False;
            }
        }

        internal static IEnumerable<string> CookiesToNetscapeStyle(CookieCollection cookies)
        {
            if (cookies == null || cookies.Count == 0)
            {
                yield break;
            }

            foreach (Cookie c in cookies)
            {
                string prefix = c.HttpOnly ? "#HttpOnly_" : "";
                string subdomainAccess = "TRUE";                    // Simplified
                string secure = c.Secure.ToString().ToUpperInvariant();
                long expires = (c.Expires.Ticks == 0) ? 0 : DateTimeUtils.UtcToUnixTimeStamp(c.Expires);
                yield return $"{prefix}{c.Domain}\t{subdomainAccess}\t{c.Path}\t{secure}\t{expires}\t{c.Name}\t{c.Value}";
            }
        }

        static PhpArray CreateCookiePhpArray(CookieCollection cookies)
        {
            return new PhpArray(CookiesToNetscapeStyle(cookies));
        }

        static void AddCookies(CookieCollection from, CookieContainer container)
        {
            if (from != null)
            {
                container?.Add(from);
            }
        }

        static Uri? TryCreateUri(CURLResource ch)
        {
            var url = ch.Url;
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            //
            if (url.IndexOf("://", StringComparison.Ordinal) == -1)
            {
                url = string.Concat(ch.DefaultSheme, "://", url);
            }

            // TODO: implicit port

            //
            Uri.TryCreate(url, UriKind.Absolute, out Uri result);
            return result;
        }

        static async Task<CURLResponse> ProcessHttpResponseTask(Context ctx, CURLResource ch, Task<WebResponse> responseTask)
        {
            try
            {
                using (var response = (HttpWebResponse)responseTask.Result)
                {
                    return new CURLResponse(await ProcessResponse(ctx, ch, response), response, ch);
                }
            }
            catch (AggregateException agEx)
            {
                var ex = agEx.InnerException;

                ch.VerboseOutput(ex.ToString());

                if (ex is WebException webEx)
                {
                    // TODO: ch.FailOnError ?

                    var exception = webEx.InnerException ?? webEx;

                    switch (webEx.Status)
                    {
                        case WebExceptionStatus.ProtocolError:
                            // actually ok, 301, 500, etc .. process the response:
                            return new CURLResponse(await ProcessResponse(ctx, ch, (HttpWebResponse)webEx.Response), (HttpWebResponse)webEx.Response, ch);

                        case WebExceptionStatus.Timeout:
                            return CURLResponse.CreateError(CurlErrors.CURLE_OPERATION_TIMEDOUT, exception);
                        case WebExceptionStatus.TrustFailure:
                            return CURLResponse.CreateError(CurlErrors.CURLE_SSL_CACERT, exception);
                        default:
                            return CURLResponse.CreateError(CurlErrors.CURLE_COULDNT_CONNECT, exception);
                    }
                }
                else if (ex is ProtocolViolationException)
                {
                    return CURLResponse.CreateError(CurlErrors.CURLE_FAILED_INIT, ex);
                }
                else if (ex is CryptographicException)
                {
                    return CURLResponse.CreateError(CurlErrors.CURLE_SSL_CERTPROBLEM, ex);
                }
                else
                {
                    throw ex;
                }
            }
        }

        static readonly IWebProxy s_DefaultProxy = new WebProxy();

        static Task<WebResponse> ExecHttpRequestInternalAsync(Context ctx, CURLResource ch, Uri uri)
        {
            var req = WebRequest.CreateHttp(uri);

            // setup request:

            Debug.Assert(ch.Method != null, "Method == null");

            req.Method = ch.Method;
            req.AllowAutoRedirect = ch.FollowLocation && ch.MaxRedirects != 0;
            req.Timeout = ch.Timeout <= 0 ? System.Threading.Timeout.Infinite : ch.Timeout;
            req.ContinueTimeout = ch.ContinueTimeout;
            req.Accept = "*/*";    // default value
            if (req.AllowAutoRedirect)
            {
                // equal or less than 0 will cause exception
                req.MaximumAutomaticRedirections = ch.MaxRedirects < 0 ? int.MaxValue : ch.MaxRedirects;
            }
            if (ch.CookieContainer != null)
            {
                if (ch.Result != null)
                {
                    // pass cookies from previous response to the request
                    AddCookies(ch.Result.Cookies, ch.CookieContainer);
                }
                req.CookieContainer = ch.CookieContainer;
            }
            //req.AutomaticDecompression = (DecompressionMethods)~0; // NOTICE: this nullify response Content-Length and Content-Encoding
            if (ch.CookieHeader != null) TryAddCookieHeader(req, ch.CookieHeader);
            if (ch.Username != null) req.Credentials = new NetworkCredential(ch.Username, ch.Password ?? string.Empty);
            // TODO: certificate
            if (!string.IsNullOrEmpty(ch.ProxyType) && !string.IsNullOrEmpty(ch.ProxyHost))
            {
                req.Proxy = new WebProxy($"{ch.ProxyType}://{ch.ProxyHost}:{ch.ProxyPort}")
                {
                    Credentials = string.IsNullOrEmpty(ch.ProxyUsername)
                        ? null
                        : new NetworkCredential(ch.ProxyUsername, ch.ProxyPassword ?? string.Empty)
                };
            }
            else
            {
                // by default, curl does not go through system proxy
                req.Proxy = s_DefaultProxy;
            }

            // 
            ch.ApplyOptions(ctx, req);

            // make request:

            // GET, HEAD
            if (string.Equals(ch.Method, WebRequestMethods.Http.Get, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ch.Method, WebRequestMethods.Http.Head, StringComparison.OrdinalIgnoreCase))
            {
                // nothing to do
            }
            // POST
            else if (string.Equals(ch.Method, WebRequestMethods.Http.Post, StringComparison.OrdinalIgnoreCase))
            {
                ProcessPost(ctx, req, ch);
            }
            // PUT
            else if (string.Equals(ch.Method, WebRequestMethods.Http.Put, StringComparison.OrdinalIgnoreCase))
            {
                ProcessPut(req, ch);
            }
            // DELETE, or custom method
            else
            {
                // custom method, nothing to do
            }

            //
            if (ch.StoreRequestHeaders)
            {
                ch.RequestHeaders = HttpHeaders.HeaderString(req); // and restore it when constructing CURLResponse
            }

            //
            return req.GetResponseAsync();
        }

        static void ProcessPut(HttpWebRequest req, CURLResource ch)
        {
            var fs = ch.ProcessingRequest.Stream;
            if (fs != null)
            {
                // req.ContentLength = bytes.Length;

                using (var stream = req.GetRequestStream())
                {
                    fs.RawStream.CopyTo(stream);
                }
            }
        }

        static void ProcessPost(Context ctx, HttpWebRequest req, CURLResource ch)
        {
            byte[] bytes;

            if (ch.PostFields.IsPhpArray(out var arr) && arr != null)
            {
                string boundary = "----------" + DateTime.UtcNow.Ticks.ToString();
                string contentType = "multipart/form-data; boundary=" + boundary;

                bytes = GetMultipartFormData(ctx, arr, boundary);

                req.ContentType = contentType;
            }
            else
            {
                bytes = ch.PostFields.ToBytes(ctx);

                if (string.IsNullOrEmpty(req.ContentType))
                {
                    req.ContentType = "application/x-www-form-urlencoded";
                }
            }

            req.ContentLength = bytes.Length;

            using (var stream = req.GetRequestStream())
            {
                stream.Write(bytes);
            }
        }

        static byte[] GetMultipartFormData(Context ctx, PhpArray postParameters, string boundary)
        {
            var encoding = Encoding.ASCII;
            var formDataStream = new MemoryStream();
            bool needsCLRF = false;

            var param = postParameters.GetFastEnumerator();
            while (param.MoveNext())
            {
                // Skip it on the first parameter, add it to subsequent parameters.
                if (needsCLRF)
                {
                    formDataStream.Write(encoding.GetBytes("\r\n"));
                }

                needsCLRF = true;

                if (param.CurrentValue.AsObject() is CURLFile fileToUpload)
                {
                    // Add just the first part of this param, since we will write the file data directly to the Stream
                    string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n",
                        boundary,
                        param.CurrentKey.ToString(),
                        string.IsNullOrEmpty(fileToUpload.postname) ? fileToUpload.name : fileToUpload.postname,
                        string.IsNullOrEmpty(fileToUpload.mime) ? "application/octet-stream" : fileToUpload.mime);

                    formDataStream.Write(encoding.GetBytes(header));

                    // Write the file data directly to the Stream, rather than serializing it to a string.
                    formDataStream.Write(File.ReadAllBytes(fileToUpload.name));
                }
                else
                {
                    string postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n",
                        boundary,
                        param.CurrentKey.ToString());

                    formDataStream.Write(encoding.GetBytes(postData));
                    formDataStream.Write(param.CurrentValue.ToBytes(ctx));
                }
            }

            // Add the end of the request.  Start with a newline
            string footer = "\r\n--" + boundary + "--\r\n";
            formDataStream.Write(encoding.GetBytes(footer));

            // Dump the Stream into a byte[]
            return formDataStream.ToArray();
        }

        /// <summary>
        /// Add the Cookie header if not present.
        /// </summary>
        static void TryAddCookieHeader(WebRequest req, string value)
        {
            if (req.Headers.Get(HttpRequestHeader.Cookie.ToString()) == null)
            {
                req.Headers.Add(HttpRequestHeader.Cookie, value);
            }
        }

        static async Task<PhpValue> ProcessResponse(Context ctx, CURLResource ch, HttpWebResponse response)
        {
            // in case we are returning the response value
            var returnstream = ch.ProcessingResponse.Method == ProcessMethodEnum.RETURN
                ? new MemoryStream()
                : null;

            // handle headers
            if (!ch.ProcessingHeaders.IsEmpty)
            {
                var statusHeaders = HttpHeaders.StatusHeader(response) + HttpHeaders.HeaderSeparator; // HTTP/1.1 xxx xxx\r\n
                Stream? outputHeadersStream = null;

                switch (ch.ProcessingHeaders.Method)
                {
                    case ProcessMethodEnum.RETURN:
                    case ProcessMethodEnum.STDOUT:
                        outputHeadersStream = (returnstream ?? ctx.OutputStream);
                        goto default;
                    case ProcessMethodEnum.FILE:
                        outputHeadersStream = ch.ProcessingHeaders.Stream.RawStream;
                        goto default;
                    case ProcessMethodEnum.USER:
                        // pass headers one by one,
                        // in original implementation we should pass them as they are read from socket:

                        ch.ProcessingHeaders.User.Invoke(ctx, new[] {
                            PhpValue.FromClass(ch),
                            PhpValue.Create(statusHeaders)
                        });

                        for (int i = 0; i < response.Headers.Count; i++)
                        {
                            var key = response.Headers.GetKey(i);
                            var value = response.Headers.Get(i);

                            if (key == null || key.Length != 0)
                            {
                                // header
                                ch.ProcessingHeaders.User.Invoke(ctx, new[] {
                                    PhpValue.FromClr(ch),
                                    PhpValue.Create(key + ": " + value + HttpHeaders.HeaderSeparator),
                                });
                            }
                        }

                        // \r\n
                        ch.ProcessingHeaders.User.Invoke(ctx, new[] {
                            PhpValue.FromClr(ch),
                            PhpValue.Create(HttpHeaders.HeaderSeparator)
                        });

                        break;
                    default:
                        if (outputHeadersStream != null)
                        {
                            await outputHeadersStream.WriteAsync(Encoding.ASCII.GetBytes(statusHeaders));
                            await outputHeadersStream.WriteAsync(response.Headers.ToByteArray());
                        }
                        else
                        {
                            Debug.Fail("Unexpected ProcessingHeaders " + ch.ProcessingHeaders.Method);
                        }
                        break;
                }
            }

            var stream = response.GetResponseStream();

            // gzip decode if necessary
            if (response.ContentEncoding == "gzip") // TODO: // && ch.AcceptEncoding.Contains("gzip") ??
            {
                ch.VerboseOutput("Decompressing the output stream using GZipStream.");
                stream = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: false);
            }

            // read into output stream:
            switch (ch.ProcessingResponse.Method)
            {
                case ProcessMethodEnum.STDOUT: await stream.CopyToAsync(ctx.OutputStream); break;
                case ProcessMethodEnum.RETURN: stream.CopyTo(returnstream); break;
                case ProcessMethodEnum.FILE: await stream.CopyToAsync(ch.ProcessingResponse.Stream.RawStream); break;
                case ProcessMethodEnum.USER:
                    if (response.ContentLength != 0)
                    {
                        // preallocate a buffer to read to,
                        // this should be according to PHP's behavior and slightly more effective than memory stream
                        byte[] buffer = new byte[ch.BufferSize > 0 ? ch.BufferSize : 2048];
                        int bufferread;

                        while ((bufferread = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ch.ProcessingResponse.User.Invoke(ctx, new[]
                            {
                                PhpValue.FromClr(ch),
                                PhpValue.Create(new PhpString(buffer.AsSpan(0, bufferread).ToArray())), // clone the array and pass to function
                            });
                        }
                    }
                    break;
                case ProcessMethodEnum.IGNORE: break;
            }

            //
            stream.Dispose();

            //
            if (response.ResponseUri != null)
            {
                ch.Url = response.ResponseUri.AbsoluteUri;
            }

            return (returnstream != null)
                ? PhpValue.Create(new PhpString(returnstream.ToArray()))
                : PhpValue.True;
        }

        static bool IsProtocol(CURLResource ch, Uri uri, string scheme, int proto)
        {
            return
                string.Equals(uri.Scheme, scheme, StringComparison.OrdinalIgnoreCase) &&
                (ch.Protocols & proto) != 0;
        }

        static void StartRequestExecution(Context ctx, CURLResource ch)
        {
            ch.StartTime = DateTime.UtcNow;

            var uri = TryCreateUri(ch);
            if (uri == null)
            {
                ch.VerboseOutput("Cannot create URI for '" + ch.Url + "'.");
                ch.Result = CURLResponse.CreateError(CurlErrors.CURLE_URL_MALFORMAT);
            }
            else if (
                IsProtocol(ch, uri, "http", CURLConstants.CURLPROTO_HTTP) ||
                IsProtocol(ch, uri, "https", CURLConstants.CURLPROTO_HTTPS))
            {
                ch.VerboseOutput("Initiating HTTP(S) request.");

                ch.ResponseTask = ExecHttpRequestInternalAsync(ctx, ch, uri);
                ch.Result = null;
            }
            else
            {
                ch.VerboseOutput("The protocol '" + uri.Scheme + "' is not supported.");

                ch.Result = CURLResponse.CreateError(CurlErrors.CURLE_UNSUPPORTED_PROTOCOL);
            }
        }

        static void EndRequestExecution(Context ctx, CURLResource ch)
        {
            if (ch.ResponseTask != null)
            {
                ch.Result = ProcessHttpResponseTask(ctx, ch, ch.ResponseTask).GetAwaiter().GetResult();
                ch.ResponseTask = null;
            }

            ch.Result.TotalTime = (DateTime.UtcNow - ch.StartTime);

            if (ch.TryGetOption<CurlOption_Private>(out var opt_private))
            {
                ch.Result.Private = opt_private.OptionValue;
            }
        }

        /// <summary>
        /// Perform a cURL session.
        /// </summary>
        public static PhpValue curl_exec(Context ctx, CURLResource ch)
        {
            StartRequestExecution(ctx, ch);
            EndRequestExecution(ctx, ch);

            return ch.Result.ExecValue;
        }

        /// <summary>
        /// Return the content of a cURL handle if <see cref="CURLConstants.CURLOPT_RETURNTRANSFER"/> is set.
        /// </summary>
        public static PhpValue curl_multi_getcontent(CURLResource ch)
        {
            if (ch.ProcessingResponse.Method == ProcessMethodEnum.RETURN && ch.Result != null && Operators.IsSet(ch.Result.ExecValue))
            {
                return ch.Result.ExecValue;
            }
            else
            {
                return PhpValue.Null;
            }
        }

        /// <summary>
        /// Return a new cURL multi handle.
        /// </summary>
        [return: NotNull]
        public static CURLMultiResource/*!*/curl_multi_init() => new CURLMultiResource();

        /// <summary>
        /// Close a set of cURL handles.
        /// </summary>
        public static void curl_multi_close(CURLMultiResource mh) => mh?.Dispose();

        /// <summary>
        /// Add a normal cURL handle to a cURL multi handle.
        /// </summary>
        public static int curl_multi_add_handle(CURLMultiResource mh, CURLResource ch) => (int)mh.TryAddHandle(ch);

        /// <summary>
        /// Remove a multi handle from a set of cURL handles
        /// </summary>
        /// <remarks>
        /// Removes a given <paramref name="ch"/> handle from the given <paramref name="mh"/> handle.
        /// When the <paramref name="ch"/> handle has been removed, it is again perfectly legal to run
        /// <see cref="curl_exec(Context, CURLResource)"/> on this handle. Removing the <paramref name="ch"/>
        /// handle while being used, will effectively halt the transfer in progress involving that handle.
        /// </remarks>
        public static int curl_multi_remove_handle(CURLMultiResource mh, CURLResource ch)
        {
            if (mh.Handles.Remove(ch) && ch.ResponseTask != null)
            {
                // We will simply remove the only reference to the ongoing request and let the framework either
                // finish it or cancel it
                ch.ResponseTask = null;
            }

            return CURLConstants.CURLM_OK;
        }

        /// <summary>
        /// Run the sub-connections of the current cURL handle.
        /// </summary>
        public static int curl_multi_exec(Context ctx, CURLMultiResource mh, out int still_running)
        {
            int runningCount = 0;

            foreach (var handle in mh.Handles)
            {
                if (handle.ResponseTask != null)
                {
                    if (handle.ResponseTask.IsCompleted)
                    {
                        EndRequestExecution(ctx, handle);
                        mh.AddResultMessage(handle);
                    }
                    else
                    {
                        runningCount++;
                    }
                }
                else if (handle.Result == null)
                {
                    StartRequestExecution(ctx, handle);
                    runningCount++;
                }
            }

            still_running = runningCount;
            return CURLConstants.CURLM_OK;
        }

        /// <summary>
        /// Get information about the current transfers.
        /// </summary>
        [return: CastToFalse]
        public static PhpArray? curl_multi_info_read(CURLMultiResource mh) => curl_multi_info_read(mh, out _);

        /// <summary>
        /// Get information about the current transfers.
        /// </summary>
        [return: CastToFalse]
        public static PhpArray? curl_multi_info_read(CURLMultiResource mh, out int msgs_in_queue)
        {
            if (mh.MessageQueue.Count == 0)
            {
                msgs_in_queue = 0;
                return null;
            }
            else
            {
                var msg = mh.MessageQueue.Dequeue();
                msgs_in_queue = mh.MessageQueue.Count;
                return msg;
            }
        }

        /// <summary>
        /// Wait for activity on any curl_multi connection.
        /// </summary>
        public static int curl_multi_select(CURLMultiResource mh, float timeout = 1.0f)
        {
            var tasks = mh.Handles
                .Select(h => h.ResponseTask)
                .Where(t => t != null)
                .ToArray();

            if (tasks.Length == 0)
            {
                return 0;
            }

            // Already completed and not yet processed by curl_multi_exec -> no waiting
            int finished = tasks.Count(t => t.IsCompleted);
            if (finished > 0 || timeout == 0.0f)
            {
                return finished;
            }

            Task.WaitAny(tasks, TimeSpan.FromSeconds(timeout));
            return tasks.Count(t => t.IsCompleted);
        }

        /// <summary>
        /// Return the last multi curl error number.
        /// </summary>
        public static int curl_multi_errno(CURLMultiResource mh) => (int)mh.LastError;

        /// <summary>
        /// Return string describing error code.
        /// </summary>
        public static string curl_multi_strerror(CurlMultiErrors errornum) => CURLConstants.GetErrorString(errornum);

        /// <summary>
        /// Set an option for the cURL multi handle.
        /// </summary>
        public static bool curl_multi_setopt(CURLMultiResource mh, int option, PhpValue value)
        {
            // We keep the responsibility of multiple request handling completely on .NET framework
            PhpException.FunctionNotSupported(nameof(curl_multi_setopt));
            return false;
        }
    }
}
