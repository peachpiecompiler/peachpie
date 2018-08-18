using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Pchp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Threading;

namespace Pchp.Core
{
    partial class Context
    {
        /// <summary>
        /// Configures global services.
        /// </summary>
        static IServiceCollection ConfigureServices()
        {
            var collection = new ServiceCollection();

            // default services
            collection.AddSingleton<CompositionContext>(service => new ContainerConfiguration().WithAssemblies(CollectCompositionAssemblies()).CreateContainer());
            collection.AddSingleton<IPhpConfigurationService>(DefaultPhpConfigurationService.Instance);
            collection.AddSingleton<IScriptingProvider>(service => service.GetService<CompositionContext>().TryGetExport<IScriptingProvider>() ?? new UnsupportedScriptingProvider());

            //
            return collection;
        }

        static IEnumerable<Assembly> CollectCompositionAssemblies()
        {
            // TODO: list of catalog assemblies in a property / configuration instead of this:

            // wellknown components
            var assemblyNames = new List<string>(8)
            {
                "Peachpie.Library.Scripting",
                "Peachpie.Library.PDO.Firebird",
                "Peachpie.Library.PDO.IBM",
                "Peachpie.Library.PDO.MySQL",
                "Peachpie.Library.PDO.PgSQL",
                "Peachpie.Library.PDO.Sqlite",
                "Peachpie.Library.PDO.SqlSrv",
            };

            //// runtime deps
            //if (DependencyContext.Default != null)
            //{
            //    foreach (var lib in DependencyContext.Default.RuntimeLibraries)
            //    {
            //        if (lib.Dependencies.Any(d => d.Name == runtimeDepName))    // only assemblies depending on this runtime
            //        {
            //            assemblyNames.Add(lib.Name);
            //        }
            //    }
            //}

            // TODO: extension libraries

            //
            return assemblyNames
                .Distinct(StringComparer.OrdinalIgnoreCase) // remove duplicities just in case
                .Select(CompositionExtension.TryLoad)       // try to load the assembly, otherwise gets null
                .WhereNotNull()     // ignore assemblies that couldn;t be loaded
                // TODO: should be done in compile time, the same behavior as evaluation of `extension_loaded` // .Select(Reflection.ExtensionsAppContext.ExtensionsTable.AddAssembly) // reflect every component in case it contains an extension
                ;
        }

        // TODO: local (instance) services

        /// <summary>
        /// Gets service provider of global services.
        /// </summary>
        public static IServiceProvider GlobalServices
        {
            get
            {
                if (_lazyGlobalServices == null)
                {
                    Interlocked.CompareExchange(ref _lazyGlobalServices, ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(ConfigureServices()), null);
                }

                return _lazyGlobalServices;
            }
        }
        static IServiceProvider _lazyGlobalServices;

        /// <summary>
        /// Gets singleton instance of <see cref="CompositionContext"/> with composed MEF components aka <c>ComponentModel</c>
        /// </summary>
        public static CompositionContext CompositionContext => GlobalServices.GetService<CompositionContext>();
    }
}
