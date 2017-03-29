using Microsoft.AspNetCore.Http;
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

namespace Peachpie.Web
{
    /// <summary>
    /// Runtime context for ASP.NET Core request.
    /// </summary>
    [DebuggerDisplay("RequestContextCore({DebugRequestDisplay,nq})")]
    sealed class RequestContextCore : Context, IHttpPhpContext
    {
        /// <summary>
        /// Debug display string.
        /// </summary>
        string DebugRequestDisplay => $"{_httpctx.Request.Path.Value}{_httpctx.Request.QueryString.Value}";

        #region IHttpPhpContext

        /// <summary>Gets value indicating HTTP headers were already sent.</summary>
        public bool HeadersSent
        {
            get { return _httpctx.Response.HasStarted; }
        }

        public void SetHeader(string name, string value)
        {
            StringValues newitem = new StringValues(value);
            //StringValues olditem;
            //if (_httpctx.Response.Headers.TryGetValue(name, out olditem))
            //{
            //    newitem = StringValues.Concat(olditem, newitem);
            //}

            //
            _httpctx.Response.Headers[name] = newitem;
        }

        public void RemoveHeader(string name) { _httpctx.Response.Headers.Remove(name); }

        public void RemoveHeaders() { _httpctx.Response.Headers.Clear(); }

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

        void IHttpPhpContext.AddCookie(string name, string value, DateTimeOffset? expires, string path, string domain, bool secure, bool httpOnly)
        {
            _httpctx.Response.Cookies.Append(name, value, new CookieOptions()
            {
                Expires = expires,
                Path = path,
                Domain = string.IsNullOrEmpty(domain) ? null : domain,  // IE, Edge: cookie with the empty domain was not passed to request
                Secure = secure,
                HttpOnly = httpOnly
            });
        }

        void IHttpPhpContext.Flush()
        {
            _httpctx.Response.Body.Flush();
        }

        #endregion

        #region Request Lifecycle

        /// <summary>
        /// The default document.
        /// </summary>
        const string DefaultDocument = "index.php";

        public static ScriptInfo ResolveScript(HttpRequest req)
        {
            var script = default(ScriptInfo);
            var path = req.Path.Value;
            var isfile = path.Last() != '/';

            // trim slashes
            path = ScriptsMap.NormalizeSlashes(ArrayUtils.Trim(path, '/'));

            if (isfile)
            {
                script = ScriptsMap.GetDeclaredScript(path);
            }

            if (!script.IsValid)
            {
                // path/defaultdocument
                path = (path.Length != 0) ? (path + ('/' + DefaultDocument)) : DefaultDocument;
                script = ScriptsMap.GetDeclaredScript(path);
            }

            //
            return script;
        }

        /// <summary>
        /// Performs the request lifecycle, invokes given entry script and cleanups the context.
        /// </summary>
        /// <param name="script">Entry script.</param>
        public void ProcessScript(ScriptInfo script)
        {
            Debug.Assert(script.IsValid);

            // set additional $_SERVER items
            AddServerScriptItems(script);

            // remember the initial script file
            this.MainScriptFile = script;

            //

            try
            {
                if (Debugger.IsAttached)
                {
                    script.Evaluate(this, this.Globals, null);
                }
                else
                {
                    using (_requestTimer = new Timer(RequestTimeout, null, this.Configuration.Core.ExecutionTimeout, Timeout.Infinite))
                    {
                        script.Evaluate(this, this.Globals, null);
                    }
                }
            }
            catch (ScriptDiedException died)
            {
                died.ProcessStatus(this);
            }
        }

        void RequestTimeout(object state)
        {

        }

        void AddServerScriptItems(ScriptInfo script)
        {
            var array = this.Server;

            array["SCRIPT_FILENAME"] = (PhpValue)(this.RootPath + "/" + script.Path);
            array["PHP_SELF"] = (PhpValue)("/" + script.Path);
        }

        /// <summary>
        /// Disposes request resources.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
        }

        #endregion

        public override IHttpPhpContext HttpPhpContext => this;

        public override Encoding StringEncoding => _encoding;
        readonly Encoding _encoding;

        /// <summary>
        /// Application physical root directory including trailing slash.
        /// </summary>
        public override string RootPath => _rootPath;
        readonly string _rootPath;

        /// <summary>
        /// Reference to current <see cref="HttpContext"/>.
        /// Cannot be <c>null</c>.
        /// </summary>
        readonly HttpContext _httpctx;

        /// <summary>
        /// Internal timer used to cancel execution upon timeout.
        /// </summary>
        Timer _requestTimer;

        public RequestContextCore(HttpContext httpcontext, string rootPath, Encoding encoding)
        {
            Debug.Assert(httpcontext != null);
            Debug.Assert(rootPath != null);
            Debug.Assert(rootPath == ScriptsMap.NormalizeSlashes(rootPath));
            Debug.Assert(rootPath.Length != 0 && rootPath[rootPath.Length - 1] != '/');
            Debug.Assert(encoding != null);

            _httpctx = httpcontext;
            _rootPath = rootPath;
            _encoding = encoding;

            this.InitOutput(httpcontext.Response.Body, new ResponseTextWriter(httpcontext.Response, encoding));
            this.InitSuperglobals();

            // TODO: start session if AutoStart is On
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

        /// <summary>
        /// Loads $_SERVER from <see cref="_httpctx"/>.
        /// </summary>
        protected override PhpArray InitServerVariable()
        {
            var array = new PhpArray(32);

            var request = _httpctx.Request;

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
            //            // we add the name of the variable and an emtpy string to get what PHP gets:
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
            array["DOCUMENT_ROOT"] = (PhpValue)RootPath;    // string, backslashes, no trailing slash

            //var f_connection = _httpctx.Features.Get<IHttpConnectionFeature>();
            array["REMOTE_ADDR"] = (PhpValue)_httpctx.Connection.RemoteIpAddress.ToString();
            array["REMOTE_PORT"] = (PhpValue)_httpctx.Connection.RemotePort;
            array["LOCAL_ADDR"] = array["SERVER_ADDR"] = (PhpValue)_httpctx.Connection.LocalIpAddress.ToString();
            array["LOCAL_PORT"] = (PhpValue)_httpctx.Connection.LocalPort;
            array["SERVER_SOFTWARE"] = (PhpValue)"ASP.NET Core Server";
            array["SERVER_PROTOCOL"] = (PhpValue)request.Protocol;
            array["SERVER_NAME"] = (PhpValue)request.Host.Host;
            array["SERVER_PORT"] = (PhpValue)request.Host.Port;
            array["REQUEST_URI"] = (PhpValue)(request.Path.Value + request.QueryString.Value);
            array["REQUEST_METHOD"] = (PhpValue)request.Method;
            array["SCRIPT_NAME"] = (PhpValue)request.Path.ToString();
            array["SCRIPT_FILENAME"] = PhpValue.Null; // set in ProcessScript
            array["PHP_SELF"] = PhpValue.Null; // set in ProcessScript
            array["QUERY_STRING"] = (PhpValue)(request.QueryString.HasValue ? request.QueryString.Value.Substring(1) : string.Empty);
            array["HTTP_HOST"] = (PhpValue)request.Headers["Host"].ToString();
            array["HTTP_CONNECTION"] = (PhpValue)request.Headers["Connection"].ToString();
            array["HTTP_USER_AGENT"] = (PhpValue)request.Headers["User-Agent"].ToString();
            array["HTTP_ACCEPT"] = (PhpValue)request.Headers["Accept"].ToString();
            array["HTTP_ACCEPT_ENCODING"] = (PhpValue)request.Headers["Accept-Encoding"].ToString();
            array["HTTP_ACCEPT_LANGUAGE"] = (PhpValue)request.Headers["Accept-Language"].ToString();
            array["HTTP_REFERER"] = (PhpValue)request.Headers["Referer"].ToString();
            //array["REQUEST_URI"] = (PhpValue)request.RawUrl;
            array["REQUEST_TIME_FLOAT"] = (PhpValue)DateTimeUtils.UtcToUnixTimeStampFloat(DateTime.UtcNow);
            array["REQUEST_TIME"] = (PhpValue)DateTimeUtils.UtcToUnixTimeStamp(DateTime.UtcNow);
            array["HTTPS"] = PhpValue.Create(request.IsHttps);

            //
            return array;
        }

        protected override PhpArray InitGetVariable()
        {
            var result = PhpArray.NewEmpty();

            if (_httpctx.Request.Method == "GET" && _httpctx.Request.HasFormContentType)
            {
                AddVariables(result, _httpctx.Request.Form);
            }

            AddVariables(result, _httpctx.Request.Query);

            //
            return result;
        }

        protected override PhpArray InitPostVariable()
        {
            var result = PhpArray.NewEmpty();

            if (_httpctx.Request.Method == "POST" && _httpctx.Request.HasFormContentType)
            {
                AddVariables(result, _httpctx.Request.Form);
            }

            return result;
        }

        protected override PhpArray InitFilesVariable()
        {
            return base.InitFilesVariable();
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
