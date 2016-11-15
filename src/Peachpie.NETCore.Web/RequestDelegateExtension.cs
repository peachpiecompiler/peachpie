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
        /// Installs PHP request handler within the application.
        /// </summary>
        public static IApplicationBuilder UsePhp(this IApplicationBuilder builder /* , TODO: options, allowed extensions, directory */)
        {
            return builder.Use(new Func<RequestDelegate, RequestDelegate>(next => async context =>
            {
                var script = RequestContextCore.ResolveScript(context.Request);
                if (script.IsValid)
                {
                    await Task.Run(() =>
                    {
                        using (var phpctx = new RequestContextCore(context))
                        {
                            script.MainMethod(phpctx, phpctx.Globals, null);
                        }
                    });
                }
                else
                {
                    await next(context);
                }
            }));
        }
    }
}
