using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pchp.Core.Utilities;
using System.Reflection;
using Pchp.Core.Reflection;

namespace Pchp.Core
{
    /// <summary>
    /// Runtime context for a PHP application.
    /// </summary>
    /// <remarks>
    /// The object represents a current Web request or the application run.
    /// Its instance is passed to all PHP function.
    /// The context is not thread safe.
    /// </remarks>
    public partial class Context : IDisposable, IServiceProvider
    {
        #region Create

        /// <summary>
        /// Initializes instance of context.
        /// </summary>
        /// <param name="services">Service provider. Can be <c>null</c> reference to use implicit services.</param>
        protected Context(IServiceProvider services)
        {
            _services = services;

            // tables
            _functions = new RoutinesTable();
            _types = new TypesTable();
            _statics = Array.Empty<object>();
            _constants = ConstsMap.Create(this);
            _scripts = ScriptsMap.Create();
        }

        /// <summary>
        /// Create default context with no output.
        /// </summary>
        /// <param name="cmdargs">
        /// Optional arguments to be passed to PHP <c>$argv</c> and <c>$argc</c> global variables.
        /// If the array is empty, variables are not created.
        /// </param>
        public static Context CreateEmpty(params string[] cmdargs)
        {
            var ctx = new Context(null)
            {
                RootPath = Directory.GetCurrentDirectory(),
                EnableImplicitAutoload = true,
            };

            ctx.WorkingDirectory = ctx.RootPath;
            ctx.InitOutput(null);
            ctx.InitSuperglobals();

            if (cmdargs != null && cmdargs.Length != 0)
            {
                ctx.InitializeArgvArgc(null, cmdargs);
            }

            //
            return ctx;
        }

        /// <summary>
        /// Base service provider, can be <c>null</c>.
        /// Used to provide services to this instance of context.
        /// </summary>
        readonly IServiceProvider _services;

        /// <summary>
        /// Resolves service.
        /// </summary>
        object IServiceProvider.GetService(Type serviceType)
        {
            return null;
        }

        #endregion

        #region Symbols

        static class DllLoaderImpl
        {
            static readonly HashSet<Type> s_processedConstantsContainers = new HashSet<Type>();

            /// <summary>
            /// Set of reflected script assemblies.
            /// </summary>
            public static IReadOnlyCollection<Assembly> ProcessedAssemblies => s_processedAssembliesImmutable ??= s_processedAssemblies.ToArray();
            static Assembly[] s_processedAssembliesImmutable;

            static readonly HashSet<Assembly> s_processedAssemblies = new HashSet<Assembly>();

            /// <summary>
            /// Processes referenced assembly which is annotated as [PhpExtension]
            /// </summary>
            static void AddPhpPackageReference(Type scriptOrModule)
            {
                if (scriptOrModule == null)
                {
                    return;
                }

                if (scriptOrModule.Name == ScriptInfo.ScriptTypeName)
                {
                    // <Script> // it is a PHP assembly
                    AddScriptReference(scriptOrModule.Assembly);
                }
                else if (TryAddAssembly(scriptOrModule.Assembly))
                {
                    // it is a C# assembly
                    ExtensionsAppContext.ExtensionsTable.VisitAssembly(scriptOrModule.Assembly);
                }
            }

            /// <summary>
            /// Adds assembly to the list of referenced packages.
            /// This is used
            /// - by `eval()` to collect references
            /// - to avoid processing same assembly twice
            /// - as a recursion prevention
            /// </summary>
            static bool TryAddAssembly(Assembly/*!*/assembly)
            {
                if (s_processedAssemblies.Add(assembly))
                {
                    s_processedAssembliesImmutable = null;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            /// <summary>
            /// Reflects given assembly for PeachPie compiler specifics - compiled scripts, references to other assemblies, declared functions and classes.
            /// Scripts and declarations are loaded into application context (static).
            /// </summary>
            /// <param name="assembly">PeachPie compiler generated assembly.</param>
            /// <remarks>Not thread safe.</remarks>
            public static void AddScriptReference(Assembly assembly)
            {
                if (assembly == null)
                {
                    throw new ArgumentNullException(nameof(assembly));
                }

                if (assembly.GetType(ScriptInfo.ScriptTypeName) == null || !TryAddAssembly(assembly))
                {
                    // nothing to reflect
                    return;
                }

                // reflect the module for imported symbols:

                var module = assembly.ManifestModule;

                // PhpPackageReferenceAttribute
                foreach (var r in module.GetCustomAttributes<PhpPackageReferenceAttribute>())
                {
                    AddPhpPackageReference(r.ScriptType);
                }

                // ImportPhpTypeAttribute
                foreach (var t in module.GetCustomAttributes<ImportPhpTypeAttribute>())
                {
                    TypesTable.DeclareAppType(PhpTypeInfoExtension.GetPhpTypeInfo(t.ImportedType));
                }

                // ImportPhpFunctionsAttribute
                foreach (var t in module.GetCustomAttributes<ImportPhpFunctionsAttribute>())
                {
                    if (ExtensionsAppContext.ExtensionsTable.VisitFunctionsContainer(t.ContainerType, out var attr))
                    {
                        foreach (var m in t.ContainerType.GetMethods())
                        {
                            if (m.IsPublic && m.IsStatic && !m.IsPhpHidden())
                            {
                                ExtensionsAppContext.ExtensionsTable.AddRoutine(attr, RoutinesTable.DeclareAppRoutine(m.Name, m));
                            }
                        }
                    }
                }

                // ImportPhpConstantsAttribute
                foreach (var t in module.GetCustomAttributes<ImportPhpConstantsAttribute>())
                {
                    if (!s_processedConstantsContainers.Add(t.ContainerType))
                    {
                        // already visited before
                        continue;
                    }

                    //
                    var extensionName = t.ContainerType.GetCustomAttribute<PhpExtensionAttribute>(false)?.FirstExtensionOrDefault;

                    // reflect constants defined in the container
                    foreach (var m in t.ContainerType.GetMembers(BindingFlags.Static | BindingFlags.Public))
                    {
                        if (m is FieldInfo fi && !fi.IsPhpHidden())
                        {
                            Debug.Assert(fi.IsStatic && fi.IsPublic);

                            if (fi.IsInitOnly || fi.IsLiteral)
                            {
                                // constant
                                ConstsMap.DefineAppConstant(fi.Name, PhpValue.FromClr(fi.GetValue(null)), false, extensionName);
                            }
                            else
                            {
                                // static field
                                ConstsMap.DefineAppConstant(fi.Name, new Func<PhpValue>(() => PhpValue.FromClr(fi.GetValue(null))), false, extensionName);
                            }
                        }
                        else if (m is PropertyInfo pi && !pi.IsPhpHidden())
                        {
                            // property
                            ConstsMap.DefineAppConstant(pi.Name, new Func<PhpValue>(() => PhpValue.FromClr(pi.GetValue(null))), false, extensionName);
                        }
                    }
                }

                // define various Core constants dynamically
                DefineCoreConstants();

                // scripts & PHP types
                foreach (var t in assembly.ExportedTypes)
                {
                    if (t.IsAbstract && t.IsSealed)// => static
                    {
                        var sattr = ReflectionUtils.GetScriptAttribute(t);
                        if (sattr != null && sattr.Path != null && t.GetCustomAttribute<PharAttribute>() == null)
                        {
                            ScriptsMap.DeclareScript(sattr.Path, ScriptInfo.CreateMain(t));
                        }
                    }
                    else
                    {
                        if (s_typeMap.AddTypeNoLock(t, out var tinfo) == PhpTypeAttribute.AutoloadAllowNoSideEffect)
                        {
                            TypesTable.DeclareAppType(tinfo);
                        }
                    }
                }

                //
                s_targetPhpLanguageAttribute ??= assembly.GetCustomAttribute<TargetPhpLanguageAttribute>();
            }

            /// <summary>
            /// Defines Core constants depending on host OS and runtime.
            /// </summary>
            static void DefineCoreConstants()
            {
                ConstsMap.DefineAppConstant("PHP_SAPI", new Func<Context, PhpValue>(ctx => ctx.ServerApi), false, "Core");

                if (CurrentPlatform.IsWindows)
                {
                    DefineCoreConstant("PHP_WINDOWS_VERSION_MAJOR", Environment.OSVersion.Version.Major);
                    DefineCoreConstant("PHP_WINDOWS_VERSION_MINOR", Environment.OSVersion.Version.Minor);
                    DefineCoreConstant("PHP_WINDOWS_VERSION_BUILD", Environment.OSVersion.Version.Build);
                    DefineCoreConstant("PHP_WINDOWS_VERSION_PLATFORM", (int)Environment.OSVersion.Platform);

                    // PHP_WINDOWS_VERSION_SP_**
                    WindowsPlatform.GetVersionInformation(
                        out var sp_major, out var sp_minor,
                        out var producttype,
                        out var suitemask);

                    DefineCoreConstant("PHP_WINDOWS_VERSION_SP_MAJOR", (int)sp_major);
                    DefineCoreConstant("PHP_WINDOWS_VERSION_SP_MINOR", (int)sp_minor);

                    DefineCoreConstant("PHP_WINDOWS_VERSION_SUITEMASK", (int)suitemask);
                    DefineCoreConstant("PHP_WINDOWS_VERSION_PRODUCTTYPE", (int)producttype);
                }
            }

            static void DefineCoreConstant(string name, PhpValue value)
            {
                ConstsMap.DefineAppConstant(name, value, false, "Core");
            }
        }

        /// <summary>
        /// Helper class called one-time from compiled DLL static .cctor.
        /// </summary>
        /// <typeparam name="TScript">Type of module's script class.</typeparam>
        public static class DllLoader<TScript>
        {
            /// <summary>
            /// Module initialization method.
            /// Reflects given assembly (through <typeparamref name="TScript"/>.Assembly) 
            /// </summary>
            public static void AddScriptReference()
            {
                Trace.WriteLine($"DLL '{typeof(TScript).Assembly.FullName}' being loaded ...");

                if (typeof(TScript).Name == ScriptInfo.ScriptTypeName)
                {
                    DllLoaderImpl.AddScriptReference(typeof(TScript).Assembly);
                }
                else
                {
                    Trace.TraceError($"Type '{typeof(TScript).Assembly.FullName}' is not expected! Use '{ScriptInfo.ScriptTypeName}' instead.");
                }
            }
        }

        /// <summary>
        /// Map of global functions.
        /// </summary>
        readonly RoutinesTable _functions;

        /// <summary>
        /// Map of global types.
        /// </summary>
        readonly TypesTable _types;

        /// <summary>
        /// Map of global constants.
        /// </summary>
        ConstsMap _constants;

        /// <summary>
        /// Set of scripts that have been included in current context.
        /// </summary>
        ScriptsMap _scripts;

        /// <summary>
        /// Load PHP scripts and referenced symbols from PHP assembly.
        /// </summary>
        /// <param name="assembly">PHP assembly containing special <see cref="ScriptInfo.ScriptTypeName"/> class.</param>
        /// <exception cref="ArgumentNullException">In case given assembly is a <c>null</c> reference.</exception>
        public static void AddScriptReference(Assembly assembly)
        {
            DllLoaderImpl.AddScriptReference(assembly);
        }

        /// <summary>
        /// Internal.
        /// Gets enumeration of <see cref="Assembly"/> representing script assemblies that were reflected.
        /// </summary>
        public static IReadOnlyCollection<Assembly> GetScriptReferences() => DllLoaderImpl.ProcessedAssemblies;

        /// <summary>
        /// Declare a runtime user function.
        /// </summary>
        public void DeclareFunction(RoutineInfo routine) => _functions.DeclarePhpRoutine(routine);

        public void AssertFunctionDeclared(RoutineInfo routine)
        {
            if (!_functions.IsDeclared(routine))
            {
                // TODO: ErrCode function is not declared
            }
        }

        /// <summary>
        /// Internal. Used by callsites cache to check whether called function is the same as the one declared.
        /// </summary>
        internal bool CheckFunctionDeclared(int index, int expectedHashCode) => AssertFunction(_functions.GetDeclaredRoutine(index - 1), expectedHashCode);

        /// <summary>
        /// Checks the routine has expected hash code. The routine can be null.
        /// </summary>
        static bool AssertFunction(RoutineInfo routine, int expectedHashCode) => routine != null && routine.GetHashCode() == expectedHashCode;

        /// <summary>
        /// Gets declared function with given name. In case of more items they are considered as overloads.
        /// </summary>
        public RoutineInfo GetDeclaredFunction(string name) => _functions.GetDeclaredRoutine(name);

        /// <summary>Gets enumeration of all functions declared within the context, including library and user functions.</summary>
        /// <returns>Enumeration of all routines. Cannot be <c>null</c>.</returns>
        public IEnumerable<RoutineInfo> GetDeclaredFunctions() => _functions.EnumerateRoutines();

        /// <summary>
        /// Declare a runtime user type.
        /// </summary>
        /// <typeparam name="T">Type to be declared in current context.</typeparam>
        public void DeclareType<T>() => _types.DeclareType(TypeInfoHolder<T>.TypeInfo);

        /// <summary>
        /// Declare a runtime user type unser an aliased name.
        /// </summary>
        /// <param name="tinfo">Original type descriptor.</param>
        /// <param name="typename">Type name alias, can differ from <see cref="PhpTypeInfo.Name"/>.</param>
        public void DeclareType(PhpTypeInfo tinfo, string typename) => _types.DeclareTypeAlias(tinfo, typename);

        /// <summary>
        /// Called by runtime when it expects that given type is declared.
        /// If not, autoload is invoked and if the type mismatches or cannot be declared, an exception is thrown.
        /// </summary>
        /// <typeparam name="T">Type which is expected to be declared.</typeparam>
        public void ExpectTypeDeclared<T>()
        {
            void EnsureTypeDeclared()
            {
                var tinfo = TypeInfoHolder<T>.TypeInfo;

                // perform regular load with autoload
                if (tinfo != GetDeclaredTypeOrThrow(tinfo.Name, true))
                {
                    throw PhpException.ClassNotFoundException(tinfo.Name);
                }
            }

            //
            var tinfo = TypeInfoHolder<T>.TypeInfo;
            if (tinfo.IsUserType && !IsUserTypeDeclared(tinfo))
            {
                EnsureTypeDeclared();
            }
        }

        /// <summary>
        /// Gets runtime type information, or <c>null</c> if type with given is name not declared.
        /// </summary>
        public PhpTypeInfo GetDeclaredType(string name, bool autoload = false)
            => _types.GetDeclaredType(name) ?? (autoload ? this.AutoloadService.AutoloadTypeByName(name) : null);

        /// <summary>
        /// Gets runtime type information, or throws if type with given name is not declared.
        /// </summary>
        public PhpTypeInfo GetDeclaredTypeOrThrow(string name, bool autoload = false)
        {
            return GetDeclaredType(name, autoload) ?? throw PhpException.ClassNotFoundException(name);
        }

        /// <summary>
        /// Gets runtime type information of given type by its name.
        /// Resolves reserved type names according to current caller context.
        /// Returns <c>null</c> if type was not resolved.
        /// </summary>
        public PhpTypeInfo ResolveType(string name, RuntimeTypeHandle callerCtx, bool autoload = false)
        {
            Debug.Assert(name != null);

            if (name.Length != 0 && name[0] == '\\')
            {
                name = name.Substring(1);
            }

            // reserved type names: parent, self, static
            if (name.Length == 6)
            {
                if (name.EqualsOrdinalIgnoreCase("parent"))
                {
                    if (!callerCtx.Equals(default(RuntimeTypeHandle)))
                    {
                        return Type.GetTypeFromHandle(callerCtx).GetPhpTypeInfo().BaseType;
                    }
                    return null;
                }
                else if (name.EqualsOrdinalIgnoreCase("static"))
                {
                    throw new NotSupportedException();
                }
            }
            else if (name.Length == 4 && name.EqualsOrdinalIgnoreCase("self"))
            {
                if (!callerCtx.Equals(default(RuntimeTypeHandle)))
                {
                    return Type.GetTypeFromHandle(callerCtx).GetPhpTypeInfo();
                }
                return null;
            }

            //
            return GetDeclaredType(name, autoload);
        }

        /// <summary>
        /// Gets enumeration of all types declared in current context.
        /// </summary>
        public IEnumerable<PhpTypeInfo> GetDeclaredTypes() => _types.GetDeclaredTypes();

        /// <summary>
        /// Checks the user type is declared in the current state of <see cref="Context"/>.
        /// </summary>
        /// <param name="phptype">PHP type runtime information. Must not be <c>null</c>.</param>
        /// <returns>True if the type has been declared on the current <see cref="Context"/>.</returns>
        internal bool IsUserTypeDeclared(PhpTypeInfo phptype) => _types.IsDeclared(phptype);

        #endregion

        #region Inclusions

        /// <summary>
        /// Used by runtime.
        /// Determines whether the <c>include_once</c> or <c>require_once</c> is allowed to proceed.
        /// </summary>
        public bool CheckIncludeOnce<TScript>() => !_scripts.IsIncluded<TScript>();

        /// <summary>
        /// Used by runtime.
        /// Called by scripts Main method at its begining.
        /// </summary>
        /// <typeparam name="TScript">Script type containing the Main method/</typeparam>
        public void OnInclude<TScript>() => ScriptsMap.SetIncluded<TScript>(ref _scripts);

        /// <summary>
        /// Resolves path according to PHP semantics, lookups the file in runtime tables and calls its Main method within the global scope.
        /// </summary>
        /// <param name="dir">Current script directory. Used for relative path resolution. Can be <c>null</c> to not resolve against current directory.</param>
        /// <param name="path">The relative or absolute path to resolve and include.</param>
        /// <param name="once">Whether to include according to include once semantics.</param>
        /// <param name="throwOnError">Whether to include according to require semantics.</param>
        /// <returns>Inclusion result value.</returns>
        public PhpValue Include(string dir, string path, bool once = false, bool throwOnError = false)
            => Include(dir, path, Globals,
                once: once,
                throwOnError: throwOnError);

        /// <summary>
        /// Resolves path according to PHP semantics, lookups the file in runtime tables and calls its Main method.
        /// </summary>
        /// <param name="cd">Current script directory. Used for relative path resolution. Can be <c>null</c> to not resolve against current directory.</param>
        /// <param name="path">The relative or absolute path to resolve and include.</param>
        /// <param name="locals">Variables scope for the included script.</param>
        /// <param name="this">Reference to <c>this</c> variable.</param>
        /// <param name="self">Reference to current class context.</param>
        /// <param name="once">Whether to include according to include once semantics.</param>
        /// <param name="throwOnError">Whether to include according to require semantics.</param>
        /// <returns>Inclusion result value.</returns>
        public PhpValue Include(string cd, string path, PhpArray locals, object @this = null, RuntimeTypeHandle self = default(RuntimeTypeHandle), bool once = false, bool throwOnError = false)
        {
            ScriptInfo script;

            if (FileSystemUtils.TryGetScheme(path, out var schemespan))
            {
                // SCHEME://SOMETHING
                script = HandleIncludeWithScheme(schemespan, cd, path.AsSpan(schemespan.Length + 3));
            }
            else
            {
                // regular inclusion resolution
                script = ScriptsMap.ResolveInclude(path, RootPath, IncludePaths, WorkingDirectory, cd);
            }

            if (script.IsValid)
            {
                return (once && _scripts.IsIncluded(script.Index))
                    ? PhpValue.True
                    : script.Evaluate(this, locals, @this, self);
            }
            else
            {
                return HandleMissingScript(cd, path, throwOnError);
            }
        }

        PhpValue HandleMissingScript(string cd, string path, bool throwOnError)
        {
            // TODO: HandleIncludeWithScheme("file", cd, path); // let the StreamWrapper handle it

            if (TryIncludeFileContent(path))    // include non-compiled file (we do not allow dynamic compilation yet)
            {
                return PhpValue.Null;
            }
            else
            {
                var cause = string.Format(Resources.ErrResources.script_not_found, path);

                PhpException.Throw(
                    throwOnError ? PhpError.Error : PhpError.Notice,
                    Resources.ErrResources.script_inclusion_failed, path, cause, string.Join(";", IncludePaths), cd);

                if (throwOnError)
                {
                    throw new ScriptIncludeException(path);
                }

                return PhpValue.False;
            }
        }

        ScriptInfo HandleIncludeWithScheme(ReadOnlySpan<char> scheme, string cd, ReadOnlySpan<char> path)
        {
            // SCHEME://PATH
            var wrapper = GetGlobalStreamWrapper(scheme.ToString());
            if (wrapper != null && wrapper.ResolveInclude(this, cd, path.ToString(), out var script))
            {
                return script;
            }
            else
            {
                return ScriptInfo.Empty;
            }
        }

        /// <summary>
        /// Tries to read a file and outputs its content.
        /// </summary>
        /// <param name="path">Path to the file. Will be resolved using available stream wrappers.</param>
        /// <returns><c>true</c> if file was read and outputted, otherwise <c>false</c>.</returns>
        bool TryIncludeFileContent(string path)
        {
            var fnc = this.GetDeclaredFunction("readfile");
            if (fnc != null)
            {
                Debug.WriteLine($"Note: file '{path}' has not been compiled.");

                return fnc.PhpCallable(this, (PhpValue)path);
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Shutdown

        List<Action<Context>> _lazyShutdownCallbacks;

        /// <summary>
        /// Enqueues a callback to be invoked at the end of request.
        /// </summary>
        /// <param name="action">Callback. Cannot be <c>null</c>.</param>
        public void RegisterShutdownCallback(Action<Context> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var callbacks = _lazyShutdownCallbacks;
            if (callbacks == null)
            {
                _lazyShutdownCallbacks = callbacks = new List<Action<Context>>(1);
            }

            callbacks.Add(action);
        }

        /// <summary>
        /// Invokes callbacks in <see cref="_lazyShutdownCallbacks"/> and disposes the list.
        /// </summary>
        void ProcessShutdownCallbacks()
        {
            var callbacks = _lazyShutdownCallbacks;
            if (callbacks != null)
            {
                try
                {
                    for (int i = 0; i < callbacks.Count; i++)
                    {
                        callbacks[i](this);
                    }
                }
                catch (ScriptDiedException died)
                {
                    died.ProcessStatus(this);
                }

                //
                _lazyShutdownCallbacks = callbacks = null;
            }
        }

        /// <summary>
        /// Closes current web session if opened.
        /// </summary>
        void ShutdownSessionHandler()
        {
            var webctx = HttpPhpContext;
            if (webctx != null && webctx.SessionState == PhpSessionState.Started)
            {
                webctx.SessionHandler.CloseSession(this, webctx, false);
            }
        }

        /// <summary>
        /// Handles program unhandled exception according to PHP's semantic.
        /// If no user exception handler is defined, the function returns <c>false</c>.
        /// </summary>
        /// <param name="exception">Unhandled exception. Cannot be <c>null</c>.</param>
        /// <returns>Value indicating the exception was handled.</returns>
        public virtual bool OnUnhandledException(Exception exception)
        {
            var handler = Configuration.Core.UserExceptionHandler;
            if (handler != null)
            {
                handler.Invoke(this, PhpValue.FromClass(exception));
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Resources // objects that need dispose

        HashSet<IDisposable> _lazyDisposables;

        public virtual void RegisterDisposable(IDisposable obj)
        {
            if (_lazyDisposables == null)
            {
                _lazyDisposables = new HashSet<IDisposable>();
            }

            _lazyDisposables.Add(obj);
        }

        public virtual void UnregisterDisposable(IDisposable obj)
        {
            if (_lazyDisposables != null)
            {
                _lazyDisposables.Remove(obj);
            }
        }

        void ProcessDisposables()
        {
            var set = _lazyDisposables;
            if (set != null && set.Count != 0)
            {
                _lazyDisposables = null;

                foreach (var x in set)
                {
                    x.Dispose();
                }
            }
        }

        #endregion

        #region Temporary Per-Request Files

        /// <summary>
        /// A list of temporary files which was created during the request and should be deleted at its end.
        /// </summary>
        HashSet<string> _temporaryFiles;

        /// <summary>
        /// Silently deletes all temporary files.
        /// </summary>
        private void DeleteTemporaryFiles()
        {
            if (_temporaryFiles != null)
            {
                foreach (var path in _temporaryFiles)
                {
                    try { File.Delete(path); }
                    catch
                    {
                        // ignored
                    }
                }

                _temporaryFiles = null;
            }
        }

        /// <summary>
        /// Adds temporary file to current handler's temp files list.
        /// </summary>
        /// <param name="path">A path to the file.</param>
        protected void AddTemporaryFile(string path)
        {
            Debug.Assert(path != null);

            if (_temporaryFiles == null)
            {
                _temporaryFiles = new HashSet<string>(CurrentPlatform.PathComparer);
            }

            _temporaryFiles.Add(path);
        }

        /// <summary>
        /// Checks whether the given filename is a path to a temporary file
        /// (for example created using the filet upload mechanism).
        /// </summary>
        /// <remarks>
        /// The stored paths are checked case-insensitively.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Argument is a <B>null</B> reference.</exception>
        public bool IsTemporaryFile(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return _temporaryFiles != null && _temporaryFiles.Contains(path);
        }

        /// <summary>
        /// Removes a file from a list of temporary files.
        /// </summary>
        /// <param name="path">A full path to the file.</param>
        /// <exception cref="ArgumentNullException">Argument is a <B>null</B> reference.</exception>
        public bool RemoveTemporaryFile(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return _temporaryFiles != null && _temporaryFiles.Remove(path);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Gets value indicating the context has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        public virtual void Dispose()
        {
            if (!IsDisposed)
            {
                try
                {
                    ProcessShutdownCallbacks();
                    ProcessDisposables();
                    ShutdownSessionHandler();
                    //this.GuardedCall<object, object>(this.FinalizePhpObjects, null, false);
                    FinalizeBufferedOutput();

                    //// additional disposal action
                    //this.TryDispose?.Invoke();
                }
                finally
                {
                    DeleteTemporaryFiles();

                    //// additional disposal action
                    //this.FinallyDispose?.Invoke();

                    //
                    IsDisposed = true;
                }
            }
        }

        #endregion
    }
}
