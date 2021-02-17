using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pchp.Core;
using Pchp.Core.Utilities;
using Pchp.Library;

namespace Peachpie.AspNetCore.Web
{
    /// <summary>
    /// ASP.NET Core application middleware handling requests to compiled PHP scripts.
    /// </summary>
    internal sealed class PhpHandlerMiddleware
    {
        sealed class PhpOptions : IPhpOptions
        {
            readonly IPhpConfigurationService _globalconfiguration;

            public PhpOptions(IPhpConfigurationService globalconfiguration = null)
            {
                _globalconfiguration = globalconfiguration ?? Context.DefaultPhpConfigurationService.Instance;
            }

            public Encoding StringEncoding { get; set; } = Encoding.UTF8;

            internal ICollection<Assembly> ScriptAssemblyCollection { get; } = new List<Assembly>();

            public string RootPath { get; set; }

            public event Action<Context> RequestStart;

            public const string DefaultLoggerCategory = "PHP";

            /// <inheritdoc/>
            public string LoggerCategory { get; set; } = DefaultLoggerCategory;

            public void InvokeRequestStart(Context ctx) => RequestStart?.Invoke(ctx);

            IPhpConfigurationService IPhpConfigurationService.Parent => _globalconfiguration;

            public PhpCoreConfiguration Core => _globalconfiguration.Core;

            public IPhpSessionConfiguration Session => _globalconfiguration.Get<IPhpSessionConfiguration>();

            IEnumerator<IPhpConfiguration> IEnumerable<IPhpConfiguration>.GetEnumerator() => _globalconfiguration.GetEnumerator();

            TOptions IPhpConfigurationService.Get<TOptions>() => _globalconfiguration.Get<TOptions>();

            IEnumerator IEnumerable.GetEnumerator() => _globalconfiguration.GetEnumerator();
        }

        readonly RequestDelegate _next;
        readonly string _rootPath;
        readonly PhpOptions _options;
        readonly PathString _prefix;

        public PhpHandlerMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv, IServiceProvider services, PhpHandlerConfiguration configuration)
        {
            if (hostingEnv == null)
            {
                throw new ArgumentNullException(nameof(hostingEnv));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _next = next ?? throw new ArgumentNullException(nameof(next));
            _rootPath = hostingEnv.GetDefaultRootPath();
            _options = new PhpOptions(Context.DefaultPhpConfigurationService.Instance)
            {
                RootPath = _rootPath,
            };
            _prefix = configuration.PathPrefix;

            if (_prefix.Value == "/")
            {
                _prefix = PathString.Empty;
            }

            // legacy options
            ConfigureOptions(_options, configuration.LegacyOptions);

            // sideload script assemblies
            LoadScriptAssemblies(_options);

            // global options 
            ConfigureOptions(_options, services);

            // local options
            if (configuration.ConfigureContext != null)
            {
                _options.RequestStart += configuration.ConfigureContext;
            }

            // determine application's root path:
            if (_options.RootPath != default && _options.RootPath != _rootPath)
            {
                // use the root path option, relative to the ASP.NET Core Web Root
                _rootPath = Path.GetFullPath(Path.Combine(_rootPath, _options.RootPath));
            }

            if (configuration.RootPath != null && configuration.RootPath != _rootPath)
            {
                _rootPath = Path.GetFullPath(Path.Combine(_rootPath, configuration.RootPath));
            }

            // normalize slashes
            _rootPath = NormalizeRootPath(_rootPath);

            // setup logger
            ConfigureLogger(_options, services.GetService<ILoggerFactory>());

            // TODO: pass hostingEnv.ContentRootFileProvider to the Context for file system functions
        }

        static void ConfigureOptions(PhpOptions options, PhpRequestOptions oldoptions)
        {
            if (oldoptions == null)
            {
                return;
            }

            if (oldoptions.StringEncoding != null)
            {
                options.StringEncoding = oldoptions.StringEncoding;
            }

            if (oldoptions.RootPath != null)
            {
                options.RootPath = oldoptions.RootPath;
            }

            if (oldoptions.ScriptAssembliesName != null)
            {
                foreach (var ass in oldoptions.ScriptAssembliesName)
                {
                    options.ScriptAssemblyCollection.Add(Assembly.Load(new AssemblyName(ass)));
                }
            }

            if (oldoptions.BeforeRequest != null)
            {
                options.RequestStart += oldoptions.BeforeRequest;
            }
        }

        static void ConfigureOptions(PhpOptions options, IServiceProvider services)
        {
            foreach (var configservice in services.GetServices<IConfigureOptions<IPhpOptions>>())
            {
                configservice.Configure(options);
            }

            foreach (var configservice in services.GetServices<IPostConfigureOptions<IPhpOptions>>())
            {
                configservice.PostConfigure(Microsoft.Extensions.Options.Options.DefaultName, options);
            }

            //
            if (options.Session?.AutoStart == true)
            {
                options.RequestStart += ctx =>
                {
                    var httpctx = ctx.HttpPhpContext;
                    if (httpctx != null)
                    {
                        httpctx.SessionHandler?.StartSession(ctx, httpctx);
                    }
                };
            }
        }

        static void ConfigureLogger(IPhpOptions options, ILoggerFactory factory)
        {
            if (factory != null)
            {
                var logger = factory.CreateLogger(options.LoggerCategory ?? PhpOptions.DefaultLoggerCategory);
                if (logger != null && !s_loggerregistered)
                {
                    s_loggerregistered = true;

                    PhpException.OnError += (error, message) =>
                    {
                        switch (error)
                        {
                            case PhpError.Error:
                                logger.LogError(message);
                                break;

                            case PhpError.Warning:
                                logger.LogWarning(message);
                                break;

                            case PhpError.Notice:
                            default:
                                logger.LogInformation(message);
                                break;
                        }
                    };
                }
            }
        }

        /// <summary>flag we have already registered ILogger into PhpException.OnError</summary>
        static bool s_loggerregistered;

        /// <summary>
        /// Loads and reflects assemblies containing compiled PHP scripts.
        /// </summary>
        static void LoadScriptAssemblies(PhpOptions options)
        {
            if (options.ScriptAssemblyCollection.Count != 0)
            {
                foreach (var assembly in options.ScriptAssemblyCollection)
                {
                    Context.AddScriptReference(assembly);
                }
            }
            else
            {
                // import libraries that has "Peachpie.App" as a dependency
                var runtimelibs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Peachpie.App",
                    typeof(Context).Assembly.GetName().Name, // "Peachpie.Runtime"
                };

                // reads dependencies from DependencyContext
                foreach (var lib in DependencyContext.Default.RuntimeLibraries)
                {
                    if (lib.Type != "package" && lib.Type != "project")
                    {
                        continue;
                    }

                    if (lib.Name == "Peachpie.App")
                    {
                        continue;
                    }

                    // process assembly if it has a dependency to runtime
                    var dependencies = lib.Dependencies;
                    for (int i = 0; i < dependencies.Count; i++)
                    {
                        if (runtimelibs.Contains(dependencies[i].Name))
                        {
                            try
                            {
                                // assuming DLL is deployed with the executable,
                                // and contained lib is the same name as package:
                                Context.AddScriptReference(Assembly.Load(new AssemblyName(lib.Name)));
                            }
                            catch
                            {
                                // 
                            }
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Normalize slashes and ensures the path ends with slash.
        /// </summary>
        static string NormalizeRootPath(string path)
        {
            return string.IsNullOrEmpty(path)
                ? string.Empty
                : CurrentPlatform.NormalizeSlashes(path.TrimEndSeparator());
        }

        /// <summary>
        /// Handles new context.
        /// </summary>
        void OnContextCreated(RequestContextCore ctx)
        {
            _options.InvokeRequestStart(ctx);
            ctx.TrySetTimeLimit(GetRequestTimeout(ctx));
        }

        static Exception RequestTimeoutException()
        {
            // Note: FatalError in PHP
            return new TimeoutException();
        }

        /// <summary>
        /// Gets the global request time limit.
        /// </summary>
        static TimeSpan GetRequestTimeout(Context phpctx)
        {
            var seconds = phpctx.Configuration.Core.ExecutionTimeout;

            return seconds <= 0 || Debugger.IsAttached
                ? Timeout.InfiniteTimeSpan
                : TimeSpan.FromSeconds(seconds);
        }

        public Task InvokeAsync(HttpContext context)
        {
            if (string.IsNullOrEmpty(_prefix.Value) || context.Request.Path.StartsWithSegments(_prefix))
            {
                var script = RequestContextCore.ResolveScript(context.Request, out var path_info);
                if (script.IsValid)
                {
                    return InvokeScriptAsync(context, script, path_info);
                }
            }

            //
            return _next(context);
        }

        async Task InvokeScriptAsync(HttpContext context, Context.ScriptInfo script, string path_info)
        {
            Debug.Assert(script.IsValid);

            using var phpctx = new RequestContextCore(context, _rootPath, _options.StringEncoding);

            OnContextCreated(phpctx);

            // run the script, dispose phpctx when finished
            // using threadpool since we have to be able to end the request and keep script running
            var task = Task.Run(() =>
            {
                try
                {
                    phpctx.ProcessScript(script, path_info);
                }
                finally
                {
                    phpctx.RequestCompletionSource.TrySetResult(RequestCompletionReason.Finished);
                }
            });

            // wait for the request to finish,
            // do not block current thread
            var reason = await phpctx.RequestCompletionSource.Task;

            if (task.Exception != null)
            {
                // rethrow script exception
                throw task.Exception;
            }
            else if (reason == RequestCompletionReason.Timeout)
            {
                throw RequestTimeoutException();
            }
        }
    }

    /// <summary>
    /// Middleware configuration.
    /// </summary>
    internal class PhpHandlerConfiguration
    {
        /// <summary>
        /// Prefix of request paths to be processed by the middleware.
        /// Can be <c>default</c> (empty) which means the middleware handled all requested PHP scripts.
        /// </summary>
        public PathString PathPrefix { get; set; }

        /// <summary>
        /// Configure context and options callback.
        /// Can be <c>null</c>.
        /// </summary>
        public Action<Context> ConfigureContext { get; set; }

        /// <summary>
        /// Old options object to be applied to the middleware options.
        /// Can be <c>null</c>.
        /// </summary>
        public PhpRequestOptions LegacyOptions { get; set; }

        /// <summary>
        /// Gets or sets physical path used as root of the PHP scripts.
        /// </summary>
        public string RootPath { get; set; }
    }
}
