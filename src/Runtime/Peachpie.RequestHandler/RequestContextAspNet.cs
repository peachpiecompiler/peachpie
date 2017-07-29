using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web;
using Pchp.Core;
using Pchp.Core.Utilities;

namespace Peachpie.RequestHandler
{
    sealed class RequestContextAspNet : Context, IHttpPhpContext
    {
        #region .cctor

        static RequestContextAspNet()
        {
            // load referenced scripts:

            LoadScriptReferences();
        }

        /// <summary>
        /// It is not necessary to load (and shadow copy) all the assemblies from /Bin.
        /// This method gets <c>false</c> for well known assemblies that won't be loaded.
        /// </summary>
        static bool IsAllowedScriptAssembly(AssemblyName assname)
        {
            var token = assname.GetPublicKeyToken();
            if (token != null)
            {
                var tokenstr = string.Join(null, token.Select(b => b.ToString("x2")));   // readable public key token, lowercased
                if (tokenstr == "5b4bee2bf1f98593" || // Peachpie
                    tokenstr == "a7d26565bac4d604" || // Google.Protobuf
                    tokenstr == "840d8b321fee7061" || // Devsense
                    tokenstr == "adb9793829ddae60" || // Microsoft
                    tokenstr == "b03f5f7f11d50a3a" || // System
                    tokenstr == "b77a5c561934e089")   // .NET
                {
                    return false;
                }
            }

            return true;
        }

        static Assembly TryLoadAssemblyFromFile(string fname)
        {
            try
            {
                // First quickly get the full assembly name without loading the assembly into App Domain.
                // This will be used to check whether the assembly isn't a known dependency that we don't have to load
                var name = AssemblyName.GetAssemblyName(fname);

                // LoadFrom() correctly loads the assembly while respecting shadow copying,
                // by default ASP.NET AppDomain has shadow copying enabled so all the assemblies will be loaded from cache location.
                // This avoids locking the DLLs in Bin folder.
                return IsAllowedScriptAssembly(name) ? Assembly.LoadFrom(fname) : null;
            }
            catch
            {
                return null;
            }
        }

        static Type TryGetScriptType(Assembly ass)
        {
            return ass?.GetType(ScriptInfo.ScriptTypeName, false, false);
        }

        /// <summary>
        /// Loads assemblies representing referenced scripts and reflects their symbols to be used by the runtime.
        /// </summary>
        static void LoadScriptReferences()
        {
            // try to load DLL files from /Bin folder containing PHP scripts
            var scriptTypes = Directory.GetFiles(HttpRuntime.BinDirectory, "*.dll", SearchOption.TopDirectoryOnly)
                .Select(TryLoadAssemblyFromFile)
                .Select(TryGetScriptType)
                .WhereNotNull();

            foreach (var t in scriptTypes)
            {
                AddScriptReference(t);
            }
        }

        #endregion

        #region IHttpPhpContext

        /// <summary>Gets value indicating HTTP headers were already sent.</summary>
        public bool HeadersSent
        {
            get
            {
                var code = StatusCode;
                try
                {
                    // a trick (StatusCodes's setter checks whether or not headers has been sent):
                    StatusCode = code;
                }
                catch (HttpException)
                {
                    return true;
                }
                return false;
            }
        }

        public void SetHeader(string name, string value) { _httpctx.Response.Headers.Add(name, value); }

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
        Stream IHttpPhpContext.InputStream => _httpctx.Request.InputStream;

        void IHttpPhpContext.AddCookie(string name, string value, DateTimeOffset? expires, string path, string domain, bool secure, bool httpOnly)
        {
            var cookie = new HttpCookie(name, value)
            {
                Path = path,
                Domain = domain,
                Secure = secure,
                HttpOnly = httpOnly,
            };

            if (expires.HasValue)
            {
                cookie.Expires = expires.Value.UtcDateTime;
            }

            _httpctx.Response.AppendCookie(cookie);
        }

        void IHttpPhpContext.Flush()
        {
            _httpctx.Response.Flush();
        }

        /// <summary>
        /// Gets or sets session handler for current context.
        /// </summary>
        PhpSessionHandler IHttpPhpContext.SessionHandler
        {
            get => AspNetSessionHandler.Default;
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Gets or sets session state.
        /// </summary>
        PhpSessionState IHttpPhpContext.SessionState { get; set; }

        #endregion

        public override IHttpPhpContext HttpPhpContext => this;

        /// <summary>
        /// Reference to current <see cref="HttpContext"/>.
        /// Cannot be <c>null</c>.
        /// </summary>
        public HttpContext HttpContext => _httpctx;
        readonly HttpContext _httpctx;

        public RequestContextAspNet(HttpContext httpcontext)
        {
            Debug.Assert(httpcontext != null);
            Debug.Assert(HttpRuntime.UsingIntegratedPipeline);

            _httpctx = httpcontext;

            this.RootPath = HttpRuntime.AppDomainAppPath;

            this.InitOutput(httpcontext.Response.OutputStream);
            this.InitSuperglobals();

            // TODO: start session if AutoStart is On
        }

        /// <summary>
        /// Loads $_SERVER from <see cref="HttpRequest.ServerVariables"/>.
        /// </summary>
        protected override PhpArray InitServerVariable()
        {
            var array = new PhpArray(32);

            var request = _httpctx.Request;
            var serverVariables = request.ServerVariables;

            // adds variables defined by ASP.NET and IIS:
            foreach (string name in serverVariables)
            {
                // gets all values associated with the name:
                string[] values = serverVariables.GetValues(name);

                if (values == null)
                    continue;   // http://phalanger.codeplex.com/workitem/30132

                // adds all items:
                if (name != null)
                {
                    foreach (string value in values)
                        Superglobals.AddVariable(array, name, value, null);
                }
                else
                {
                    // if name is null, only name of the variable is stated:
                    // e.g. for GET variables, URL looks like this: ...&test&...
                    // we add the name of the variable and an emtpy string to get what PHP gets:
                    foreach (string value in values)
                    {
                        Superglobals.AddVariable(array, value, string.Empty, null);
                    }
                }
            }

            //// adds argv, argc variables:
            //if (RegisterArgcArgv)
            //{
            //    array["argv"] = PhpValue.Create(new PhpArray(1) { request.QueryString });
            //    array["argc"] = PhpValue.Create(0);
            //}

            // additional variables defined in PHP manual:
            array["PHP_SELF"] = (PhpValue)request.Path;

            try
            {
                array["DOCUMENT_ROOT"] = (PhpValue)request.MapPath("/"); // throws exception under mod_aspdotnet
            }
            catch
            {
                array["DOCUMENT_ROOT"] = PhpValue.Null;
            }

            array["SERVER_ADDR"] = (PhpValue)serverVariables["LOCAL_ADDR"];
            array["REQUEST_URI"] = (PhpValue)request.RawUrl;
            array["REQUEST_TIME"] = (PhpValue)Pchp.Core.Utilities.DateTimeUtils.UtcToUnixTimeStamp(_httpctx.Timestamp.ToUniversalTime());
            array["SCRIPT_FILENAME"] = (PhpValue)request.PhysicalPath;

            //IPv6 is the default in IIS7, convert to an IPv4 address (store the IPv6 as well)
            if (request.UserHostAddress.Contains(":"))
            {
                array["REMOTE_ADDR_IPV6"] = (PhpValue)request.UserHostAddress;

                if (request.UserHostAddress == "::1")
                {
                    array["REMOTE_ADDR"] = array["SERVER_ADDR"] = (PhpValue)"127.0.0.1";
                }
                else foreach (IPAddress IPA in Dns.GetHostAddresses(request.UserHostAddress))
                    {
                        if (IPA.AddressFamily.ToString() == "InterNetwork")
                        {
                            array["REMOTE_ADDR"] = (PhpValue)IPA.ToString();
                            break;
                        }
                    }
            }

            // PATH_INFO
            // should contain partial path information only
            // note: IIS has AllowPathInfoForScriptMappings property that do the thing ... but ISAPI does not work then
            // hence it must be done here manually

            if (array.ContainsKey("PATH_INFO"))
            {
                string path_info = array["PATH_INFO"].AsString();
                string script_name = array["SCRIPT_NAME"].AsString();

                // 'ORIG_PATH_INFO'
                // Original version of 'PATH_INFO' before processed by PHP. 
                array["ORIG_PATH_INFO"] = (PhpValue)path_info;

                // 'PHP_INFO'
                // Contains any client-provided pathname information trailing the actual script filename
                // but preceding the query string, if available. For instance, if the current script was
                // accessed via the URL http://www.example.com/php/path_info.php/some/stuff?foo=bar,
                // then $_SERVER['PATH_INFO'] would contain /some/stuff. 

                // php-5.3.2\sapi\isapi\php5isapi.c:
                // 
                // strncpy(path_info_buf, static_variable_buf + scriptname_len - 1, sizeof(path_info_buf) - 1);    // PATH_INFO = PATH_INFO.SubString(SCRIPT_NAME.Length);

                array["PATH_INFO"] = (PhpValue)((script_name.Length <= path_info.Length) ? path_info.Substring(script_name.Length) : string.Empty);
            }

            //
            return array;
        }

        /// <summary>
        /// Includes requested script file.
        /// </summary>
        public bool Include(HttpRequest req)
        {
            var relative_path = req.PhysicalPath.Substring(req.PhysicalApplicationPath.Length);
            var script = ScriptsMap.GetDeclaredScript(relative_path);
            if (script.IsValid)
            {
                this.MainScriptFile = script;
                script.Evaluate(this, this.Globals, null, default(RuntimeTypeHandle));
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
