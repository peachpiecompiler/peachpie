using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Pchp.Core;
using Pchp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peachpie.Web
{
    /// <summary>
    /// Runtime context for ASP.NET Core request.
    /// </summary>
    sealed class RequestContextCore : Context // , IHttpPhpContext
    {
        #region .cctor

        static RequestContextCore()
        {
            LoadScriptReferences();
        }

        /// <summary>
        /// Loads assemblies representing referenced scripts and reflects their symbols to be used by the runtime.
        /// </summary>
        static void LoadScriptReferences()
        {
            LoadScript(new System.Reflection.AssemblyName("website"));
        }

        static void LoadScript(System.Reflection.AssemblyName assname)
        {
            try
            {
                var ass = System.Reflection.Assembly.Load(assname);
                if (ass != null)
                {
                    AddScriptReference(ass.GetType(ScriptInfo.ScriptTypeName));
                }
            }
            catch
            {
            }

        }

        #endregion

        #region IHttpPhpContext

        // TODO

        #endregion

        public static ScriptInfo ResolveScript(HttpRequest req)
        {
            var path = req.Path.Value.Replace('/', '\\').Trim('\\');    // TODO: normalized form
            return ScriptsMap.GetDeclaredScript(path);
        }

        public override IHttpPhpContext HttpPhpContext => null;    // TODO

        public override Encoding StringEncoding => Encoding.UTF8;

        /// <summary>
        /// Application physical root directory including trailing slash.
        /// </summary>
        public override string RootPath => System.IO.Directory.GetCurrentDirectory() + "\\";

        /// <summary>
        /// Reference to current <see cref="HttpContext"/>.
        /// Cannot be <c>null</c>.
        /// </summary>
        readonly HttpContext _httpctx;

        public RequestContextCore(HttpContext httpcontext)
            : base()
        {
            Debug.Assert(httpcontext != null);

            _httpctx = httpcontext;

            this.InitOutput(httpcontext.Response.Body);
            this.InitializeSuperglobals();

            // TODO: start session if AutoStart is On
        }

        /// <summary>
        /// Loads $_SERVER from <see cref="_httpctx"/>.
        /// </summary>
        protected override PhpArray InitializeServerVariable()
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
            
            // additional variables defined in PHP manual:
            array["PHP_SELF"] = request.Path.HasValue ? (PhpValue)request.Path.Value : PhpValue.Null;
            array["DOCUMENT_ROOT"] = (PhpValue)RootPath;
            //array["REQUEST_URI"] = (PhpValue)request.RawUrl;
            array["REQUEST_TIME"] = (PhpValue)DateTimeUtils.UtcToUnixTimeStamp(DateTime.UtcNow);
            //array["SCRIPT_FILENAME"] = (PhpValue)request.PhysicalPath;

            var f_connection = _httpctx.Features.Get<IHttpConnectionFeature>();
            if (f_connection != null)
            {
                array["SERVER_ADDR"]
                    = array["LOCAL_ADDR"]
                    = (PhpValue)f_connection.LocalIpAddress.ToString();
            }

            ////IPv6 is the default in IIS7, convert to an IPv4 address (store the IPv6 as well)
            //if (request.UserHostAddress.Contains(":"))
            //{
            //    array["REMOTE_ADDR_IPV6"] = (PhpValue)request.UserHostAddress;

            //    if (request.UserHostAddress == "::1")
            //    {
            //        array["REMOTE_ADDR"] = array["SERVER_ADDR"] = (PhpValue)"127.0.0.1";
            //    }
            //    else foreach (IPAddress IPA in Dns.GetHostAddresses(request.UserHostAddress))
            //        {
            //            if (IPA.AddressFamily.ToString() == "InterNetwork")
            //            {
            //                array["REMOTE_ADDR"] = (PhpValue)IPA.ToString();
            //                break;
            //            }
            //        }
            //}

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
    }
}
