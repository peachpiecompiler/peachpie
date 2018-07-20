using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Pchp.Core;

namespace Peachpie.Web
{
    /// <summary>
    /// Provides methods for <see cref="HttpContext"/>.
    /// </summary>
    public static class HttpContextExtension
    {
        /// <summary>
        /// Gets default root path.
        /// </summary>
        internal static string GetDefaultRootPath(this IHostingEnvironment hostingEnv)
        {
            return hostingEnv.WebRootPath ?? hostingEnv.ContentRootPath ?? Directory.GetCurrentDirectory();
        }

        /// <summary>
        /// Gets existing context associated with given <see cref="HttpContext"/> or creates new one with default settings.
        /// </summary>
        public static Context/*!*/GetOrCreateContext(this HttpContext httpctx)
        {
            return RequestContextCore.TryGetFromHttpContext(httpctx) ?? CreateNewContext(httpctx);
        }

        static RequestContextCore CreateNewContext(this HttpContext httpctx)
        {
            var hostingEnv = (IHostingEnvironment)httpctx.RequestServices.GetService(typeof(IHostingEnvironment));

            return new RequestContextCore(httpctx,
                rootPath: GetDefaultRootPath(hostingEnv),
                encoding: Encoding.UTF8);
        }
    }
}
