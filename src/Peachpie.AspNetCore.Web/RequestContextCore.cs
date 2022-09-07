﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Pchp.Core;
using Pchp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Peachpie.AspNetCore.Web.Session;
using System.Xml.Schema;

namespace Peachpie.AspNetCore.Web
{
    /// <summary>
    /// Runtime context for ASP.NET Core request.
    /// </summary>
    sealed class RequestContextCore : Context, IHttpPhpContext
    {
        /// <summary>Debug display string.</summary>
        protected override string DebugDisplay => $"{_httpctx.Request.Path.Value}{_httpctx.Request.QueryString.Value}";

        #region IHttpPhpContext

        /// <summary>Gets value indicating HTTP headers were already sent.</summary>
        bool IHttpPhpContext.HeadersSent
        {
            get { return _httpctx.Response.HasStarted; }
        }

        void IHttpPhpContext.SetHeader(string name, string value, bool append)
        {
            if (name.EqualsOrdinalIgnoreCase("content-length"))
            {
                // ignore content-length header, it is set correctly by middleware, using actual encoding
                return;
            }

            // specific cases:
            if (name.EqualsOrdinalIgnoreCase("location"))
            {
                _httpctx.Response.StatusCode = (int)System.Net.HttpStatusCode.Redirect; // 302
            }

            //
            var stringValue = new StringValues(value);

            if (append) // || name.EqualsOrdinalIgnoreCase("set-cookie")
            {
                // headers that can have multiple values:
                _httpctx.Response.Headers.Append(name, stringValue);
            }
            else
            {
                // replace semantic
                _httpctx.Response.Headers[name] = stringValue;
            }
        }

        void IHttpPhpContext.RemoveHeader(string name) { _httpctx.Response.Headers.Remove(name); }

        void IHttpPhpContext.RemoveHeaders() { _httpctx.Response.Headers.Clear(); }

        /// <summary>Enumerates HTTP headers in current response.</summary>
        IEnumerable<KeyValuePair<string, string>> IHttpPhpContext.GetHeaders()
        {
            return _httpctx.Response.Headers.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value.ToString()));
        }

        IEnumerable<KeyValuePair<string, IEnumerable<string>>> IHttpPhpContext.RequestHeaders
        {
            get
            {
                return _httpctx.Request.Headers.Select(header => new KeyValuePair<string, IEnumerable<string>>(header.Key, header.Value));
            }
        }

        public string CacheControl
        {
            get => _httpctx.Response.Headers["cache-control"];
            set => _httpctx.Response.Headers["cache-control"] = new StringValues(value);
        }

        public event Action HeadersSending
        {
            add
            {
                if (_headersSending == null)
                {
                    _httpctx.Response.OnStarting(() =>
                    {
                        _headersSending?.Invoke();
                        return Task.CompletedTask;
                    });
                }

                _headersSending += value;
            }
            remove
            {
                _headersSending -= value;
            }
        }
        Action _headersSending;

        /// <summary>
        /// Gets or sets HTTP response status code.
        /// </summary>
        public int StatusCode
        {
            get { return _httpctx.Response.StatusCode; }
            set { _httpctx.Response.StatusCode = value; }
        }

        /// <summary>
        /// Stream with contents of the incoming HTTP entity body.
        /// </summary>
        Stream IHttpPhpContext.InputStream => _httpctx.Request.Body;

        void IHttpPhpContext.AddCookie(string name, string value, DateTimeOffset? expires, string path, string domain, bool secure, bool httpOnly, string samesite)
        {
            var cookie = new CookieOptions
            {
                Expires = expires,
                Path = path,
                Domain = string.IsNullOrEmpty(domain) ? null : domain,  // IE, Edge: cookie with the empty domain was not passed to request
                Secure = secure,
                HttpOnly = httpOnly,
            };

            if (HttpContextHelpers.TryParseSameSite(samesite, out var samesitemode))
            {
                cookie.SameSite = samesitemode;
            }

            _httpctx.Response.Cookies.Append(name, value ?? string.Empty, cookie);
        }

        void IHttpPhpContext.Flush(bool endRequest)
        {
            _httpctx.Response.Body.Flush();

            if (endRequest)
            {
                // reset underlying output stream without disabling Output Buffering
                InitOutput(null, enableOutputBuffering: IsOutputBuffered);

                // signal to continue request pipeline
                RequestCompletionSource?.TrySetResult(RequestCompletionReason.ForceEnd);
            }
        }

        /// <summary>
        /// Gets max request size (upload size, post size) in bytes.
        /// Gets <c>0</c> if limit is not set.
        /// </summary>
        public long MaxRequestSize
        {
            get
            {
                var maxsize = _httpctx.Features.Get<IHttpMaxRequestBodySizeFeature>()?.MaxRequestBodySize;

                return maxsize ?? 30_000_000;
            }
        }

        /// <summary>
        /// Whether the underlaying connection is alive.
        /// </summary>
        public bool IsClientConnected => !_httpctx.RequestAborted.IsCancellationRequested;

        /// <summary>
        /// Gets or sets session handler for current context.
        /// </summary>
        PhpSessionHandler IHttpPhpContext.SessionHandler
        {
            get => _sessionhandler ?? AspNetCoreSessionHandler.Default;
            set
            {
                if (_sessionhandler != null && _sessionhandler != value)
                {
                    _sessionhandler.CloseSession(this, this, abandon: true);
                }

                _sessionhandler = value;
            }
        }
        PhpSessionHandler _sessionhandler;

        /// <summary>
        /// Gets or sets session state.
        /// </summary>
        PhpSessionState IHttpPhpContext.SessionState { get; set; }

        #endregion

        #region Request Lifecycle

        public static ScriptInfo ResolveScript(HttpRequest req, out string path_info)
        {
            // The default document.
            const string DefaultDocument = "/index.php";
            const char UrlSeparator = '/';

            var req_path = req.Path.Value.AsSpan();
            var path = req_path.TrimEnd(UrlSeparator);
            var script = ScriptInfo.Empty;

            for (int level = 0; ; level++)
            {
                // /a/b/file.php
                if (PathUtils.GetExtension(path).Length != 0 && (script = ScriptsMap.GetDeclaredScript(path)).IsValid)
                {
                    break;
                }

                // default document
                if (level == 0 && (script = ScriptsMap.GetDeclaredScript(path.ToString() + DefaultDocument)).IsValid) // TODO: NETSTANDARD2.1: string.concat
                {
                    break;
                }

                // ""
                // "/a"
                // "/a/b"
                var slash = path.LastIndexOf(UrlSeparator);
                if (slash >= 0)
                {
                    path = path.Slice(0, slash);
                }
                else
                {
                    path = ReadOnlySpan<char>.Empty;
                    break;
                }
            }

            //
            path_info = path.Length < req_path.Length && req_path != "/".AsSpan()
                ? req_path.Slice(path.Length).ToString()
                : null;

            return script;
        }

        /// <summary>
        /// Event signaling the request processing has been finished or cancelled.
        /// </summary>
        /// <remarks>
        /// End may occur when request finishes its processing or when event explicitly requested by user's code (See <see cref="IHttpPhpContext.Flush(bool)"/>).
        /// </remarks>
        public TaskCompletionSource<RequestCompletionReason> RequestCompletionSource { get; } = new TaskCompletionSource<RequestCompletionReason>();

        /// <summary>
        /// Internal timer used to signalize the request has timeouted.
        /// </summary>
        private Timer _requestLimitTimer = null;

        /// <summary>
        /// Set the time limit of the request, from now. Any pending time limit will be cancelled.
        /// After the specified time span, <see cref="RequestCompletionSource"/> will be signaled with the state <see cref="RequestCompletionReason.Timeout"/>.
        /// </summary>
        /// <param name="span">
        /// Time span of the time limit.
        /// Use <see cref="Timeout.InfiniteTimeSpan"/> (or <c>-1</c> milliseconds) to cancel the pending time limit.
        /// </param>
        internal void TrySetTimeLimit(TimeSpan span)
        {
            if (_requestLimitTimer == null)
            {
                if (span != Timeout.InfiniteTimeSpan)
                {
                    _requestLimitTimer = new Timer(
                        state =>
                        {
                            var self = (RequestContextCore)state;
                            self.RequestCompletionSource.TrySetResult(RequestCompletionReason.Timeout);
                        },
                        this, span, Timeout.InfiniteTimeSpan);
                }
            }
            else
            {
                if (span != Timeout.InfiniteTimeSpan)
                {
                    _requestLimitTimer.Change(span, Timeout.InfiniteTimeSpan);
                }
                else
                {
                    _requestLimitTimer.Dispose();
                    _requestLimitTimer = null;
                }
            }
        }

        /// <inheritdoc/>
        public override void ApplyExecutionTimeout(TimeSpan span) => TrySetTimeLimit(span);

        /// <summary>
        /// Performs the request lifecycle, invokes given entry script and cleanups the context.
        /// </summary>
        /// <param name="script">Entry script.</param>
        /// <param name="path_info">The <c>PATH_INFO</c> component.</param>
        public void ProcessScript(ScriptInfo script, string path_info = null)
        {
            Debug.Assert(script.IsValid);

            // set additional $_SERVER items
            AddServerScriptItems(script, path_info);

            // remember the initial script file
            this.MainScriptFile = script;

            // main script exception handler
            try
            {
                // autoload files specified in composer
                this.AutoloadFiles();

                // the main script
                script.Evaluate(this, this.Globals, null);
            }
            catch (ScriptDiedException died)
            {
                died.ProcessStatus(this);
            }
            catch (Exception exception)
            {
                // log
                PhpException.NotifiyOnError(PhpError.Error, exception.ToString());

                //
                if (!OnUnhandledException(exception))
                {
                    throw;
                }
            }
        }

        void AddServerScriptItems(ScriptInfo script, string path_info)
        {
            var array = this.Server;

            var script_name = "/" + script.Path.Replace('\\', '/');  // address of the script;

            array[CommonPhpArrayKeys.SCRIPT_NAME] = script_name;
            array[CommonPhpArrayKeys.SCRIPT_FILENAME] = RootPath + CurrentPlatform.NormalizeSlashes("/" + script.Path);
            array[CommonPhpArrayKeys.PHP_SELF] = script_name + path_info;

            if (path_info != null)
            {
                array[CommonPhpArrayKeys.PATH_INFO] = path_info;
            }
        }

        /// <summary>
        /// Disposes request resources.
        /// </summary>
        public override void Dispose()
        {
            if (_requestLimitTimer != null)
            {
                _requestLimitTimer.Dispose();
                _requestLimitTimer = null;
            }

            base.Dispose();
        }

        #endregion

        #region Echo

        //// underlying http stream requires async IO
        //// https://github.com/aspnet/Announcements/issues/342
        //public override void Echo(byte[] value)
        //{
        //    OutputStream.WriteAsync(value).GetAwaiter().GetResult();
        //}

        #endregion

        public override IHttpPhpContext HttpPhpContext => this;

        public override Encoding StringEncoding => _encoding;
        readonly Encoding _encoding;

        /// <summary>
        /// Gets server type interface name.
        /// </summary>
        public override string ServerApi => "isapi";

        /// <summary>
        /// Name of the server software as it appears in <c>$_SERVER[SERVER_SOFTWARE]</c> variable.
        /// </summary>
        public static string ServerSoftware => "ASP.NET Core Server";

        /// <summary>
        /// Informational string exposing technology powering the web request and version.
        /// </summary>
        static readonly string s_XPoweredBy = $"PeachPie {ContextExtensions.GetRuntimeInformationalVersion()}";

        static string DefaultContentType => "text/html; charset=UTF-8";

        /// <summary>
        /// Unique key of item within <see cref="HttpContext.Items"/> associated with this <see cref="Context"/>.
        /// </summary>
        static object HttpContextItemKey => typeof(Context);

        /// <summary>
        /// Reference to current <see cref="HttpContext"/>.
        /// Cannot be <c>null</c>.
        /// </summary>
        public HttpContext HttpContext => _httpctx;
        readonly HttpContext _httpctx;

        public RequestContextCore(HttpContext httpcontext, string rootPath, Encoding encoding)
            : base(httpcontext.RequestServices)
        {
            Debug.Assert(httpcontext != null);
            Debug.Assert(encoding != null);

            _httpctx = httpcontext;
            _encoding = encoding;

            httpcontext.Items[HttpContextItemKey] = this;

            // enable synchronous IO until we make everything async
            // https://github.com/aspnet/Announcements/issues/342
            var bodyControl = httpcontext.Features.Get<IHttpBodyControlFeature>();
            if (bodyControl != null)
            {
                bodyControl.AllowSynchronousIO = true;
            }

            //
            this.RootPath = rootPath;

            this.InitOutput(httpcontext.Response.Body, new SynchronizedTextWriter(httpcontext.Response, encoding));
            this.InitSuperglobals();

            this.SetupHeaders();
        }

        /// <summary>
        /// Gets (non disposed) context associated to given <see cref="HttpContext"/>.
        /// </summary>
        internal static Context TryGetFromHttpContext(HttpContext httpctx)
        {
            if (httpctx != null && httpctx.Items.TryGetValue(HttpContextItemKey, out object obj) && obj is Context ctx && !ctx.IsDisposed)
            {
                return ctx;
            }

            return null;
        }

        void SetupHeaders()
        {
            _httpctx.Response.ContentType = DefaultContentType;                         // default content type if not set anything by the application
            _httpctx.Response.Headers["X-Powered-By"] = new StringValues(s_XPoweredBy); //
        }

        static void AddVariables(PhpArray target, IEnumerable<KeyValuePair<string, StringValues>> values)
        {
            foreach (var pair in values)
            {
                var strs = pair.Value;
                for (int i = 0; i < strs.Count; i++)
                {
                    Superglobals.AddVariable(target, pair.Key, strs[i]);
                }
            }
        }

        IHttpRequestFeature/*!*/HttpRequestFeature => _httpctx.Features.Get<IHttpRequestFeature>();
        IHttpConnectionFeature/*!*/HttpConnectionFeature => _httpctx.Features.Get<IHttpConnectionFeature>();

        /// <summary>
        /// Loads $_SERVER from <see cref="_httpctx"/>.
        /// </summary>
        protected override PhpArray InitServerVariable()
        {
            var array = new PhpArray(32);

            var request = HttpRequestFeature;
            var connection = HttpConnectionFeature;
            var host = HostString.FromUriComponent(request.Headers["Host"]);

            //// adds variables defined by ASP.NET and IIS:
            //var serverVariables = _httpctx.Features.Get<IServerVariablesFeature>()?.ServerVariables;
            //if (serverVariables != null)
            //{
            //    foreach (string name in serverVariables)
            //    {
            //        // gets all values associated with the name:
            //        string[] values = serverVariables.GetValues(name);

            //        if (values == null)
            //            continue;   // http://phalanger.codeplex.com/workitem/30132

            //        // adds all items:
            //        if (name != null)
            //        {
            //            foreach (string value in values)
            //                Superglobals.AddVariable(array, name, value, null);
            //        }
            //        else
            //        {
            //            // if name is null, only name of the variable is stated:
            //            // e.g. for GET variables, URL looks like this: ...&test&...
            //            // we add the name of the variable and an empty string to get what PHP gets:
            //            foreach (string value in values)
            //            {
            //                Superglobals.AddVariable(array, value, string.Empty, null);
            //            }
            //        }
            //    }
            //}

            //// adds argv, argc variables:
            //if (RegisterArgcArgv)
            //{
            //    array["argv"] = PhpValue.Create(new PhpArray(1) { request.QueryString });
            //    array["argc"] = PhpValue.Create(0);
            //}

            // variables defined in PHP manual
            // order as it is by builtin PHP server
            array[CommonPhpArrayKeys.DOCUMENT_ROOT] = (PhpValue)RootPath;    // string, backslashes, no trailing slash
            array[CommonPhpArrayKeys.REMOTE_ADDR] = (PhpValue)((connection.RemoteIpAddress != null) ? connection.RemoteIpAddress.ToString() : request.Headers["X-Real-IP"].ToString());
            array[CommonPhpArrayKeys.REMOTE_PORT] = (PhpValue)connection.RemotePort;
            array[CommonPhpArrayKeys.LOCAL_ADDR] = array[CommonPhpArrayKeys.SERVER_ADDR] = (PhpValue)connection.LocalIpAddress?.ToString();
            array[CommonPhpArrayKeys.LOCAL_PORT] = (PhpValue)connection.LocalPort;
            array[CommonPhpArrayKeys.SERVER_SOFTWARE] = (PhpValue)ServerSoftware;
            array[CommonPhpArrayKeys.SERVER_PROTOCOL] = (PhpValue)request.Protocol;
            array[CommonPhpArrayKeys.SERVER_NAME] = (PhpValue)host.Host;
            array[CommonPhpArrayKeys.SERVER_PORT] = (PhpValue)(host.Port ?? connection.LocalPort);
            array[CommonPhpArrayKeys.REQUEST_URI] = (PhpValue)request.RawTarget;
            array[CommonPhpArrayKeys.REQUEST_METHOD] = (PhpValue)request.Method;
            array[CommonPhpArrayKeys.SCRIPT_NAME] = PhpValue.Null;  // set in ProcessScript // "/path_to_script.php"
            array[CommonPhpArrayKeys.SCRIPT_FILENAME] = PhpValue.Null; // set in ProcessScript
            array[CommonPhpArrayKeys.PHP_SELF] = PhpValue.Null; // set in ProcessScript
            array[CommonPhpArrayKeys.QUERY_STRING] = (PhpValue)(!string.IsNullOrEmpty(request.QueryString) ? request.QueryString.Substring(1) : string.Empty);
            foreach (KeyValuePair<string, StringValues> header in request.Headers)
            {
                // HTTP_{HEADER_NAME} = HEADER_VALUE
                array.Add(string.Concat("HTTP_" + header.Key.Replace('-', '_').ToUpperInvariant()), header.Value.ToString());
            }
            array[CommonPhpArrayKeys.REQUEST_TIME_FLOAT] = (PhpValue)DateTimeUtils.UtcToUnixTimeStampFloat(DateTime.UtcNow);
            array[CommonPhpArrayKeys.REQUEST_TIME] = (PhpValue)DateTimeUtils.UtcToUnixTimeStamp(DateTime.UtcNow);

            if (string.Equals(request.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                array[CommonPhpArrayKeys.HTTPS] = "on";
            }

            //
            return array;
        }

        protected override PhpArray InitGetVariable()
        {
            var query = _httpctx.Request.Query;
            var form = (_httpctx.Request.Method == HttpMethods.Get && _httpctx.Request.HasFormContentType) ? _httpctx.Request.Form : null;

            if (query.Count != 0 || form != null)
            {
                var result = new PhpArray(query.Count);

                if (form != null && form.Count != 0)
                {
                    // variables passed through GET request using multipart/form-data
                    AddVariables(result, form);
                }

                AddVariables(result, query);

                return result;
            }
            else
            {
                return PhpArray.NewEmpty();
            }
        }

        protected override PhpArray InitPostVariable()
        {
            if (_httpctx.Request.HasFormContentType)
            {
                var form = _httpctx.Request.Form;
                if (form.Count != 0)
                {
                    var result = new PhpArray(form.Count);
                    AddVariables(result, form);
                    return result;
                }
            }

            return PhpArray.NewEmpty();
        }

        protected override PhpArray InitFilesVariable()
        {
            PhpArray files;
            int count;

            if (_httpctx.Request.HasFormContentType && (count = _httpctx.Request.Form.Files.Count) != 0)
            {
                files = new PhpArray(count);

                // gets a path where temporary files are stored:
                var temppath = Path.GetTempPath(); // global_config.PostedFiles.GetTempPath(global_config.SafeMode);
                // temporary file name (first part)
                var basetempfilename = string.Concat("php_", DateTime.UtcNow.Ticks.ToString("x"), "-");
                var basetempfileid = this.GetHashCode();

                foreach (var file in _httpctx.Request.Form.Files)
                {
                    string file_path, type, file_name;
                    int error = 0;

                    if (!string.IsNullOrEmpty(file.FileName))
                    {
                        type = file.ContentType;

                        // CONSIDER: keep files in memory, use something like virtual fs (ASP.NET Core has one) or define post:// paths ?

                        var tempfilename = string.Concat(basetempfilename, (basetempfileid++).ToString("X"), ".tmp");
                        file_path = Path.Combine(temppath, tempfilename);
                        file_name = Path.GetFileName(file.FileName);

                        // registers the temporary file for deletion at request end:
                        AddTemporaryFile(file_path);

                        // saves uploaded content to the temporary file:
                        using (var stream = new FileStream(file_path, FileMode.Create))
                        {
                            file.CopyTo(stream);
                        }
                    }
                    else
                    {
                        file_path = type = file_name = string.Empty;
                        error = 4; // PostedFileError.NoFile;
                    }

                    //
                    Superglobals.AddFormFile(
                        files, file.Name,
                        file_name, type, file_path, error, file.Length
                    );
                }
            }
            else
            {
                files = PhpArray.NewEmpty();
            }

            //
            return files;
        }

        protected override PhpArray InitCookieVariable()
        {
            var result = PhpArray.NewEmpty();

            var cookies = _httpctx.Request.Cookies;
            if (cookies.Count != 0)
            {
                foreach (var c in cookies)
                {
                    Superglobals.AddVariable(result, c.Key, System.Net.WebUtility.UrlDecode(c.Value));
                }
            }

            //
            return result;
        }
    }
}
