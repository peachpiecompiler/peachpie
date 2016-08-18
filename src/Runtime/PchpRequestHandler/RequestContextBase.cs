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
    /// <summary>
    /// Script context for web requests.
    /// </summary>
    class RequestContextBase : Context
    {
        #region .cctor

        static RequestContextBase()
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

        #region Construction

        public RequestContextBase(Stream output)
            :base()
        {
            this.InitOutput(output);

            // TODO: set superglobal variables as expected within a web server
            // TODO: start session if AutoStart is On
        }

        #endregion

        /// <summary>
        /// Web application.
        /// </summary>
        public override bool IsWebApplication => true;
    }
}
