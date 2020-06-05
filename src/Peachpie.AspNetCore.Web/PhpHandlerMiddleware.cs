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

            public ICollection<Assembly> ScriptAssemblyCollection { get; } = new List<Assembly>();

            public string RootPath { get; set; }

            public event Action<Context> RequestStart;

            public void InvokeRequestStart(Context ctx) => RequestStart?.Invoke(ctx);

            IPhpConfigurationService IPhpConfigurationService.Parent => _globalconfiguration.Parent;

            public PhpCoreConfiguration Core => _globalconfiguration.Core;

            public IPhpSessionConfiguration Session => _globalconfiguration.Get<IPhpSessionConfiguration>();

            IEnumerator<IPhpConfiguration> IEnumerable<IPhpConfiguration>.GetEnumerator() => _globalconfiguration.GetEnumerator();

            TOptions IPhpConfigurationService.Get<TOptions>() => _globalconfiguration.Get<TOptions>();

            IEnumerator IEnumerable.GetEnumerator() => _globalconfiguration.GetEnumerator();
        }

        readonly RequestDelegate _next;
        readonly string _rootPath;
        readonly PhpOptions _options;

        public PhpHandlerMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv, IServiceProvider services, PhpRequestOptions oldoptions = null)
        {
            if (hostingEnv == null)
            {
                throw new ArgumentNullException(nameof(hostingEnv));
            }

            _next = next ?? throw new ArgumentNullException(nameof(next));
            _rootPath = hostingEnv.GetDefaultRootPath();
            _options = new PhpOptions(Context.DefaultPhpConfigurationService.Instance)
            {
                RootPath = _rootPath,
            };

            // configure global options:
            ConfigureOptions(_options, oldoptions);
            ConfigureOptions(_options, services);

            // determine resulting root Path:
            if (_options.RootPath != default && _options.RootPath != _rootPath)
            {
                // use the root path option, relative to the ASP.NET Core Web Root
                _rootPath = Path.GetFullPath(Path.Combine(_rootPath, _options.RootPath));
            }

            // normalize slashes
            _rootPath = NormalizeRootPath(_rootPath);

            // TODO: pass hostingEnv.ContentRootFileProvider to the Context for file system functions

            // sideload script assemblies
            LoadScriptAssemblies(_options);
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
                var PeachpieRuntime = typeof(Context).Assembly.GetName().Name; // "Peachpie.Runtime"

                // reads dependencies from DependencyContext
                foreach (var lib in DependencyContext.Default.RuntimeLibraries)
                {
                    if (lib.Type != "package" && lib.Type != "project")
                    {
                        continue;
                    }

                    if (lib.Name.StartsWith("Peachpie.", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    // process assembly if it has a dependency to runtime
                    var dependencies = lib.Dependencies;
                    for (int i = 0; i < dependencies.Count; i++)
                    {
                        if (dependencies[i].Name == PeachpieRuntime)
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
        }

        void InvokeAndDispose(RequestContextCore phpctx, Context.ScriptInfo script)
        {
            try
            {
                OnContextCreated(phpctx);
                phpctx.ProcessScript(script);
            }
            finally
            {
                phpctx.Dispose();
                phpctx.RequestCompletionSource.TrySetResult(RequestCompletionReason.Finished);
            }
        }

        static int GetRequestTimeoutSeconds(Context phpctx) =>
            Debugger.IsAttached
            ? Timeout.Infinite // -1
            : phpctx.Configuration.Core.ExecutionTimeout;

        public async Task Invoke(HttpContext context)
        {
            var script = RequestContextCore.ResolveScript(context.Request);
            if (script.IsValid)
            {
                var completion = new TaskCompletionSource<RequestCompletionReason>();
                var phpctx = new RequestContextCore(context, _rootPath, _options.StringEncoding)
                {
                    RequestCompletionSource = completion,
                };

                //
                // InvokeAndDispose(phpctx, script);
                //

                // run the script, dispose phpctx when finished
                // using threadpool since we have to be able to end the request and keep script running
                var task = Task.Run(() => InvokeAndDispose(phpctx, script));

                // wait for the request to finish,
                // do not block current thread
                var timeout = GetRequestTimeoutSeconds(phpctx);
                if (timeout > 0)
                {
                    await Task.WhenAny(completion.Task, Task.Delay(timeout * 1000));
                }
                else
                {
                    await completion.Task;
                }

                if (task.Exception != null)
                {
                    // rethrow script exception
                    throw task.Exception;
                }
            }
            else
            {
                await _next(context);
            }
        }
    }
}
