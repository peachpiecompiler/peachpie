using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Text;
using Pchp.Core;

namespace Peachpie.Library.Network
{
    [PhpExtension(CURLConstants.ExtensionName)]
    public static class CURLFunctions
    {
        /// <summary>
        /// Create a CURLFile object.
        /// </summary>
        [return: NotNull]
        public static CURLFile/*!*/curl_file_create(string filename, string mimetype = null, string postname = null) => new CURLFile(filename, mimetype, postname);

        [return: NotNull]
        public static CURLResource/*!*/curl_init(string url = null) => new CURLResource() { Url = url };

        /// <summary>
        /// Close a cURL session.
        /// </summary>
        public static void curl_close(CURLResource resource) => resource?.Dispose();

        /// <summary>
        /// Sets an option on the given cURL session handle.
        /// </summary>
        public static bool curl_setopt(CURLResource ch, int option, PhpValue value) => ch.TrySetOption(option, value);

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
        public static bool curl_setopt_array(CURLResource ch, PhpArray options)
        {
            if (ch == null || !ch.IsValid)
            {
                return false;
            }

            if (options != null)
            {
                var enumerator = options.GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    if (!enumerator.CurrentKey.IsInteger ||
                        !ch.TrySetOption(enumerator.CurrentKey.Integer, enumerator.CurrentValue))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        static void Write(this Stream stream, byte[] bytes) => stream.Write(bytes, 0, bytes.Length);

        static Uri TryCreateUri(CURLResource ch)
        {
            var url = ch.Url;
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            //
            if (url.IndexOf("://") == -1)
            {
                url = string.Concat(ch.DefaultSheme, "://", url);
            }

            // TODO: implicit port

            //
            Uri.TryCreate(url, UriKind.Absolute, out Uri result);
            return result;
        }

        static CURLResponse ExecHttpRequest(Context ctx, CURLResource ch, Uri uri)
        {
            var req = WebRequest.CreateHttp(uri);

            // setup request:

            Debug.Assert(ch.Method != null, "Method == null");

            req.Method = ch.Method;
            req.AllowAutoRedirect = ch.FollowLocation;
            req.MaximumAutomaticRedirections = ch.MaxRedirects;
            if (ch.UserAgent != null) req.UserAgent = ch.UserAgent;
            if (ch.Referer != null) req.Referer = ch.Referer;
            if (ch.Headers != null) AddHeaders(req, ch.Headers);
            // TODO: cookies
            // TODO: certificate
            // TODO: credentials
            // TODO: proxy

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

            // process response:

            using (var response = (HttpWebResponse)req.GetResponse())    // NOTE: GetResponse() internally throws an exception, ignore it
            {
                return new CURLHttpResponse() { ExecValue = ProcessResponse(ctx, ch, response) };
            }
        }

        static void ProcessPut(HttpWebRequest req, CURLResource ch)
        {
            // req.ContentLength = bytes.Length;

            using (var stream = req.GetRequestStream())
            {
                ch.PutStream.RawStream.CopyTo(stream);
            }
        }

        static void ProcessPost(Context ctx, HttpWebRequest req, CURLResource ch)
        {
            byte[] bytes;

            var arr = ch.PostFields.AsArray();
            if (arr != null)
            {
                string boundary = "----------" + DateTime.UtcNow.Ticks.ToString();
                string contentType = "multipart/form-data; boundary=" + boundary;

                bytes = GetMultipartFormData(ctx, arr, boundary);

                req.ContentType = contentType;
            }
            else
            {
                bytes = ch.PostFields.ToBytes(ctx);
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

        static void AddHeaders(HttpWebRequest req, PhpArray headers)
        {
            var enumerator = headers.GetFastEnumerator();
            while (enumerator.MoveNext())
            {
                var header = enumerator.CurrentValue.AsString();
                if (header != null)
                {
                    req.Headers.Add(header);
                }
            }
        }

        static PhpValue ProcessResponse(Context ctx, CURLResource ch, WebResponse response)
        {
            var stream = response.GetResponseStream();

            // figure out the output stream:

            MemoryStream returnstream = null;   // in case we are returning the response value

            var outputstream =
                (ch.OutputTransfer != null)
                    ? ch.OutputTransfer.RawStream
                    : ch.ReturnTransfer
                        ? (returnstream = new MemoryStream())
                        : ctx.OutputStream;

            Debug.Assert(outputstream != null);

            // read into output stream:

            if (ch.OutputHeader)
            {
                var headers = response.Headers.ToByteArray();
                outputstream.Write(headers, 0, headers.Length);
            }

            stream.CopyTo(outputstream);

            //

            return (returnstream != null)
                ? PhpValue.Create(new PhpString(returnstream.ToArray()))
                : PhpValue.True;
        }

        /// <summary>
        /// Perform a cURL session.
        /// </summary>
        public static PhpValue curl_exec(Context ctx, CURLResource ch)
        {
            var uri = TryCreateUri(ch);
            if (uri == null) return PhpValue.False;

            //

            if (string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                ch.Response = ExecHttpRequest(ctx, ch, uri);
            }
            else
            {
                return PhpValue.False;
            }

            //

            return ch.Response.ExecValue;
        }
    }
}
