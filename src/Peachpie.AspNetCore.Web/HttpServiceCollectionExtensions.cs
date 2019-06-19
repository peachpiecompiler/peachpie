using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pchp.Core;
using Pchp.Core.Utilities;
using Peachpie.AspNetCore.Web;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring HttpContext services.
    /// </summary>
    public static class HttpServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a default implementation for the <see cref="IHttpContextAccessor"/> service
        /// and configures PHP services.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddPeachpie(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            ContextExtensions.CurrentContextProvider = () => HttpContextExtension.GetOrCreateContext();

            return services;
        }
    }
}
