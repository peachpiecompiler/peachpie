using Peachpie.AspNetCore.Web;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Builder
{
    public static class PhpRequestDelegateExtension
    {
        /// <summary>
        /// Installs request handler to compiled PHP scripts.
        /// </summary>
        public static IApplicationBuilder UsePhp(this IApplicationBuilder builder, PhpRequestOptions options = null)
        {
            return builder.UseMiddleware<PhpHandlerMiddleware>(options ?? new PhpRequestOptions());
        }
    }
}
