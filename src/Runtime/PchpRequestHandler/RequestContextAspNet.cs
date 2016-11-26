using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Pchp.Core
{
    sealed class RequestContextAspNet : Context // , IHttpPhpContext
    {
        #region .cctor

        static RequestContextAspNet()
        {
            // load referenced scripts:

            LoadScriptReferences();
        }

        /// <summary>
        /// Loads assemblies representing referenced scripts and reflects their symbols to be used by the runtime.
        /// </summary>
        static void LoadScriptReferences()
        {
            var loaded = Directory.GetFiles(HttpRuntime.BinDirectory, "*.dll", SearchOption.TopDirectoryOnly).Select(fname => Assembly.LoadFile(fname));
            //var loaded = AppDomain.CurrentDomain.GetAssemblies();   // asp.net should load assemblies in /Bin folder by itself or by proper configuration
            foreach (var ass in loaded)
            {
                var t = ass.GetType(ScriptInfo.ScriptTypeName, false, false);
                if (t != null)
                {
                    AddScriptReference(t);
                }
            }
        }

        #endregion

        #region IHttpPhpContext

        // TODO

        #endregion

        public override IHttpPhpContext HttpContext => null; // TODO

        /// <summary>
        /// Application physical root directory including trailing slash.
        /// </summary>
        public override string RootPath => HttpRuntime.AppDomainAppPath;

        public RequestContextAspNet(HttpContext context)
        {
            Debug.Assert(HttpRuntime.UsingIntegratedPipeline);

            this.InitOutput(context.Response.OutputStream);

            // TODO: start session if AutoStart is On
        }

        protected override PhpArray InitializeServerVariable()
        {
            // TODO: init $_SERVER array as expected within a web server
            return base.InitializeServerVariable();
        }

        /// <summary>
        /// Includes requested script file.
        /// </summary>
        public void Include(HttpRequest req)
        {
            this.Include(string.Empty, req.PhysicalPath.Substring(req.PhysicalApplicationPath.Length), false, true);
        }
    }
}
