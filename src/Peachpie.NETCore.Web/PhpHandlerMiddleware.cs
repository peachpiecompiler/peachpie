using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Pchp.Core.Utilities;
using System.Diagnostics;

namespace Peachpie.Web
{
    /// <summary>
    /// ASP.NET Core application middleware handling requests to compiled PHP scripts.
    /// </summary>
    internal class PhpHandlerMiddleware
    {
        readonly RequestDelegate _next;
        readonly string _rootPath;
        readonly PhpRequestOptions _options;

        public PhpHandlerMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv, PhpRequestOptions options)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (hostingEnv == null)
            {
                throw new ArgumentNullException(nameof(hostingEnv));
            }

            _next = next;
            _options = options;
            _rootPath = NormalizeRootPath(hostingEnv.WebRootPath ?? hostingEnv.ContentRootPath ?? System.IO.Directory.GetCurrentDirectory());
            // TODO: pass hostingEnv.ContentRootFileProvider to the Context for file system functions

            // sideload script assemblies
            LoadScriptAssemblies(options);
        }

        /// <summary>
        /// Loads and reflects assemblies containing compiled PHP scripts.
        /// </summary>
        static void LoadScriptAssemblies(PhpRequestOptions options)
        {
            if (options.ScriptAssembliesName != null)
            {
                foreach (var assname in options.ScriptAssembliesName.Select(str => new System.Reflection.AssemblyName(str)))
                {
                    var ass = System.Reflection.Assembly.Load(assname);
                    if (ass != null)
                    {
                        Pchp.Core.Context.AddScriptReference(ass);
                    }
                    else
                    {
                        Debug.Assert(false, $"Assembly '{assname}' couldn't be loaded.");
                    }
                }
            }
        }

        /// <summary>
        /// Normalize slashes and ensures the path ends with slash.
        /// </summary>
        static string NormalizeRootPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "/";
            }
            else
            {
                path = path.Replace('\\', '/');
                return path.Last() != '/' ? path : path.Substring(0, path.Length - 1);
            }
        }

        public Task Invoke(HttpContext context)
        {
            var script = RequestContextCore.ResolveScript(context.Request);
            if (script.IsValid)
            {
                return Task.Run(() =>
                {
                    using (var phpctx = new RequestContextCore(context, _rootPath, _options.StringEncoding))
                    {
                        phpctx.ProcessScript(script);
                    }
                });
            }

            return _next(context);
        }
    }
}
