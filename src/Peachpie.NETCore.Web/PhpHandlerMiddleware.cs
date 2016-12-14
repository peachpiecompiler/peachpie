using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Pchp.Core.Utilities;

namespace Peachpie.Web
{
    /// <summary>
    /// ASP.NET Core application middleware handling requests to compiled PHP scripts.
    /// </summary>
    internal class PhpHandlerMiddleware
    {
        readonly RequestDelegate _next;
        readonly string _contentRootPath;

        public PhpHandlerMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv)
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
            _contentRootPath = NormalizeRootPath(hostingEnv.ContentRootPath ?? System.IO.Directory.GetCurrentDirectory());
            // TODO: pass hostingEnv.ContentRootFileProvider to the Context for file system functions
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
                return path.Last() == '/'
                    ? (path)
                    : (path + "/");
            }
        }

        /// <summary>
        /// This examines the request to see if it matches a configured directory, and if there are any files with the
        /// configured default names in that directory.  If so this will append the corresponding file name to the request
        /// path for a later middleware to handle.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Invoke(HttpContext context)
        {
            var script = RequestContextCore.ResolveScript(context.Request);
            if (script.IsValid)
            {
                return Task.Run(() =>
                {
                    using (var phpctx = new RequestContextCore(context, _contentRootPath))
                    {
                        phpctx.ProcessScript(script);
                    }
                });
            }

            return _next(context);
        }
    }
}
