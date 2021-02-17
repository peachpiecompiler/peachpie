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
        /// Configures PHP services and configuration options.<br/>
        /// Adds <see cref="IHttpContextAccessor"/>.<br/>
        /// Allows current <see cref="Context"/> to be obtained from current ExecutionContext through <see cref="ContextExtensions.CurrentContext"/>.<br/>
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configureOptions">Delegate configuring the options. Can be <c>null</c>.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddPhp(this IServiceCollection services, Action<IPhpOptions> configureOptions = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            //
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            ContextExtensions.CurrentContextProvider = () => HttpContextExtension.GetOrCreateContext();

            ConfigurePhp(services, configureOptions);

            //
            return services;
        }

        /// <summary>
        /// Configures PHP configuration options.<br/>
        /// </summary>
        public static IServiceCollection ConfigurePhp(this IServiceCollection services, Action<IPhpOptions> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureOptions != null)
            {
                // add service:
                // IConfigureOptions<IPhpOptions>
                services.Configure(configureOptions);
            }

            return services;
        }

        /// <summary>
        /// Adds a default implementation for the <see cref="IHttpContextAccessor"/> service
        /// and configures PHP services.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The service collection.</returns>
        [Obsolete("Use AddPhp() instead.")]
        public static IServiceCollection AddPeachpie(this IServiceCollection services)
        {
            return AddPhp(services);
        }
    }
}
