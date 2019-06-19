using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Pchp.Core;

namespace Peachpie.AspNetCore.Web
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
        /// Gets <see cref="HttpContext"/> associated with given Web <see cref="Context"/>.
        /// </summary>
        /// <exception cref="ArgumentException">Given context is not a web context.</exception>
        public static HttpContext/*!*/GetHttpContext(this Context context)
        {
            if (context is RequestContextCore reqcontext)
            {
                return reqcontext.HttpContext;
            }
            else
            {
                throw new ArgumentException(nameof(context));
            }
        }

        /// <summary>
        /// Gets context associated with current <see cref="HttpContext"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">Thrown in case <see cref="IHttpContextAccessor"/> is not registered
        /// or a current <see cref="HttpContext"/> cannot be obtained.</exception>
        public static Context GetOrCreateContext()
        {
            if (s_HttpContextAccessor == null)
            {
                s_HttpContextAccessor = new HttpContextAccessor();
            }

            var httpcontext = s_HttpContextAccessor.HttpContext; // uses AsyncLocal to maintain value within ExecutionContext, set by ASP.NET Core framework
            if (httpcontext != null)
            {
                return httpcontext.GetOrCreateContext();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        static HttpContextAccessor s_HttpContextAccessor;

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
            var rootpath = GetDefaultRootPath(hostingEnv);

            return new RequestContextCore(httpctx,
                rootPath: rootpath,
                encoding: Encoding.UTF8)
            {
                WorkingDirectory = rootpath,
                EnableImplicitAutoload = true,
            };
        }
    }
}
