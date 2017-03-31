using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Pchp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;

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
            collection.AddSingleton<CompositionContext>(service => new ContainerConfiguration().WithAssemblies(CollectCompositionAssemblies().Distinct()).CreateContainer());
            collection.AddSingleton<IPhpConfigurationService>(DefaultPhpConfigurationService.Instance);
            collection.AddSingleton<IScriptingProvider>(service => service.GetService<CompositionContext>().TryGetExport<IScriptingProvider>() ?? new UnsupportedScriptingProvider());

            //
            return collection;
        }

        static IEnumerable<Assembly> CollectCompositionAssemblies()
        {
            // wellknown components
            var assemblyNames = new List<string>(8)
            {
                "Peachpie.Library.Scripting",
                "Peachpie.Library.PDO.Firebird",
                "Peachpie.Library.PDO.IBM",
                "Peachpie.Library.PDO.MySQL",
                "Peachpie.Library.PDO.PgSQL",
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
            return assemblyNames.Distinct(StringComparer.OrdinalIgnoreCase).Select(CompositionExtension.TryLoad).WhereNotNull();
        }

        // TODO: local (instance) services

        /// <summary>
        /// Gets service provider of global services.
        /// </summary>
        public static IServiceProvider GlobalServices
        {
            get
            {
                if (_globalServices == null)
                {
                    lock (typeof(Context))
                    {
                        if (_globalServices == null)
                        {
                            _globalServices = ConfigureServices().BuildServiceProvider();
                        }
                    }
                }
                return _globalServices;
            }
        }
        static IServiceProvider _globalServices;
        
        /// <summary>
        /// Gets singleton instance of <see cref="CompositionContext"/> with composed MEF components aka <c>ComponentModel</c>
        /// </summary>
        public static CompositionContext CompositionContext => GlobalServices.GetService<CompositionContext>();
    }
}
