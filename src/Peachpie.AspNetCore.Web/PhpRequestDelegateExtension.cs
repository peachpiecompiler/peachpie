using Microsoft.AspNetCore.Http;
using Pchp.Core;
using Peachpie.AspNetCore.Web;
using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Builder
{
    public static class PhpRequestDelegateExtension
    {
        static IApplicationBuilder UsePhp(IApplicationBuilder builder, PhpHandlerConfiguration configuration)
        {
            return builder.UseMiddleware<PhpHandlerMiddleware>(configuration ?? new PhpHandlerConfiguration { });
        }

        /// <summary>
        /// Installs request handler to compiled PHP scripts.
        /// </summary>
        //[Obsolete("Use 'UsePhp(PathString, Action<Context>)' instead.")]
        public static IApplicationBuilder UsePhp(this IApplicationBuilder builder, PhpRequestOptions options)
        {
            return UsePhp(builder, new PhpHandlerConfiguration
            {
                LegacyOptions = options,
            });
        }

        /// <summary>
        /// Installs middleware to handle all compiled PHP scripts.
        /// </summary>
        public static IApplicationBuilder UsePhp(this IApplicationBuilder builder)
        {
            return UsePhp(builder, new PhpHandlerConfiguration
            {
            });
        }

        /// <summary>
        /// Installs middleware to handle compiled PHP scripts.
        /// </summary>
        /// <param name="builder">Application builder on which the middleware ins installed.</param>
        /// <param name="prefix">
        /// Optional path prefix.
        /// Only requests prefixed with this path segment(s) will be processed by this middleware.
        /// The prefix can be empty of <c>default</c> in which case all requested PHP scripts will be handled.
        /// </param>
        /// <param name="configureContext">Optional callback allowing to setup PHP request context of handled scripts.</param>
        /// <param name="rootPath">
        /// Physical path on local system to be treated as the root of the application.
        /// All the compiled script files will be translated to be relative to this root path.
        /// If not specified, the web host's default root path is used (wwwroot by default).
        /// </param>
        public static IApplicationBuilder UsePhp(
            this IApplicationBuilder builder,
            PathString prefix,
            Action<Context> configureContext = null,
            string rootPath = null)
        {
            return UsePhp(builder, new PhpHandlerConfiguration
            {
                PathPrefix = prefix,
                ConfigureContext = configureContext,
                RootPath = string.IsNullOrEmpty(rootPath) ? null : rootPath,
            });
        }
    }
}
