using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Web
{
    public static class RequestDelegateExtension
    {
        /// <summary>
        /// Installs request handler to compiled PHP scripts.
        /// </summary>
        public static IApplicationBuilder UsePhp(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PhpHandlerMiddleware>();
        }
    }
}
