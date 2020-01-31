#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Pchp.Core.Reflection;
using Pchp.Core.Utilities;

namespace Pchp.Core
{
    partial class Context
    {
        #region Functions

        /// <summary>
        /// Declare user function into the PHP runtime context.
        /// </summary>
        /// <param name="name">Global PHP function name.</param>
        /// <param name="delegate">Delegate to represent the PHP function.</param>
        public void DeclareFunction(string name, Delegate @delegate) => _functions.DeclarePhpRoutine(RoutineInfo.CreateUserRoutine(name, @delegate));

        /// <summary>
        /// Call a function by its name dynamically.
        /// </summary>
        /// <param name="function">Function name valid within current runtime context.</param>
        /// <returns>Returns value given from the function call.</returns>
        public PhpValue Call(string function) => PhpCallback.Create(function).Invoke(this, Array.Empty<PhpValue>());

        /// <summary>
        /// Call a function by its name dynamically.
        /// </summary>
        /// <param name="function">Function name valid within current runtime context.</param>
        /// <param name="arguments">Arguments to be passed to the function call.</param>
        /// <returns>Returns value given from the function call.</returns>
        public PhpValue Call(string function, params PhpValue[] arguments) => PhpCallback.Create(function).Invoke(this, arguments);

        /// <summary>
        /// Call a function by its name dynamically.
        /// </summary>
        /// <param name="function">Function name valid within current runtime context.</param>
        /// <param name="arguments">Arguments to be passed to the function call.</param>
        /// <returns>Returns value given from the function call.</returns>
        public PhpValue Call(string function, params object[] arguments) => PhpCallback.Create(function).Invoke(this, PhpValue.FromClr(arguments));

        #endregion

        #region Instantiation

        /// <summary>
        /// Creates an instance of a type dynamically with constructor overload resolution.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="arguments">Arguments to be passed to the constructor.</param>
        /// <returns>New instance of <typeparamref name="T"/>.</returns>
        public T Create<T>(params PhpValue[] arguments) => (T)TypeInfoHolder<T>.TypeInfo.Creator(this, arguments);

        /// <summary>
        /// Creates an instance of a type dynamically with constructor overload resolution.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="caller">
        /// Class context for resolving constructors visibility.
        /// Can be <c>default(<see cref="RuntimeTypeHandle"/>)</c> to resolve public constructors only.</param>
        /// <param name="arguments">Arguments to be passed to the constructor.</param>
        /// <returns>New instance of <typeparamref name="T"/>.</returns>
        public T Create<T>([ImportValue(ImportValueAttribute.ValueSpec.CallerClass)]RuntimeTypeHandle caller, params PhpValue[] arguments)
            => (T)TypeInfoHolder<T>.TypeInfo.ResolveCreator(Type.GetTypeFromHandle(caller))(this, arguments);

        /// <summary>
        /// Creates an instance of a type dynamically.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the class is not declared.</exception>
        public object Create(string classname) => Create(classname, Array.Empty<PhpValue>());

        /// <summary>
        /// Creates an instance of a type dynamically with constructor overload resolution.
        /// </summary>
        /// <param name="classname">Full name of the class to instantiate. The name uses PHP syntax of name separators (<c>\</c>) and is case insensitive.</param>
        /// <param name="arguments">Arguments to be passed to the constructor.</param>
        /// <returns>The object instance.</returns>
        /// <exception cref="InvalidOperationException">If the class is not declared.</exception>
        public object Create(string classname, params object[] arguments) => Create(classname, PhpValue.FromClr(arguments));

        /// <summary>
        /// Creates an instance of a type dynamically with constructor overload resolution.
        /// </summary>
        /// <param name="classname">Full name of the class to instantiate. The name uses PHP syntax of name separators (<c>\</c>) and is case insensitive.</param>
        /// <param name="arguments">Arguments to be passed to the constructor.</param>
        /// <returns>The object instance.</returns>
        /// <exception cref="InvalidOperationException">If the class is not declared.</exception>
        public object Create(string classname, params PhpValue[] arguments) => Create(default(RuntimeTypeHandle), classname, arguments);

        /// <summary>
        /// Creates an instance of a type dynamically with constructor overload resolution.
        /// </summary>
        /// <param name="caller">
        /// Class context for resolving constructors visibility.
        /// Can be <c>default(<see cref="RuntimeTypeHandle"/>)</c> to resolve public constructors only.</param>
        /// <param name="classname">Full name of the class to instantiate. The name uses PHP syntax of name separators (<c>\</c>) and is case insensitive.</param>
        /// <param name="arguments">Arguments to be passed to the constructor.</param>
        /// <returns>The object instance.</returns>
        /// <exception cref="InvalidOperationException">If the class is not declared.</exception>
        public object Create([ImportValue(ImportValueAttribute.ValueSpec.CallerClass)]RuntimeTypeHandle caller, string classname, params PhpValue[] arguments)
        {
            var tinfo = this.GetDeclaredTypeOrThrow(classname, true);
            return Create(caller, tinfo, arguments);
        }

        /// <summary>
        /// Creates an instance of a type dynamically with constructor overload resolution.
        /// </summary>
        /// <param name="caller">
        /// Class context for resolving constructors visibility.
        /// Can be <c>default(<see cref="RuntimeTypeHandle"/>)</c> to resolve public constructors only.</param>
        /// <param name="tinfo">Type to be instantiated.</param>
        /// <param name="arguments">Arguments to be passed to the constructor.</param>
        /// <returns>The object instance.</returns>
        /// <exception cref="ArgumentNullException">If provided <paramref name="tinfo"/> is <c>null</c>.</exception>
        public object Create([ImportValue(ImportValueAttribute.ValueSpec.CallerClass)]RuntimeTypeHandle caller, PhpTypeInfo tinfo, params PhpValue[] arguments)
        {
            if (tinfo != null)
            {
                return tinfo.ResolveCreator(Type.GetTypeFromHandle(caller))(this, arguments);
            }
            else
            {
                throw new ArgumentNullException(nameof(tinfo));
            }
        }

        #endregion

        #region Extensions

        /// <summary>
        /// Gets collection of extension names loaded into the application context.
        /// </summary>
        public static ICollection<string> GetLoadedExtensions() => ExtensionsAppContext.ExtensionsTable.GetExtensions();

        /// <summary>
        /// Gets value indicating that given extension was loaded.
        /// </summary>
        public static bool IsExtensionLoaded(string extension) => ExtensionsAppContext.ExtensionsTable.ContainsExtension(extension);

        /// <summary>
        /// Gets routines associated with specified extension.
        /// </summary>
        /// <param name="extension">Extension name.</param>
        /// <returns>Enumeration of routine names associated with given extension.</returns>
        public static IEnumerable<RoutineInfo> GetRoutinesByExtension(string extension)
        {
            return ExtensionsAppContext.ExtensionsTable.GetRoutinesByExtension(extension);
        }

        /// <summary>
        /// Gets types (classes, interfaces and traits) associated with specified extension.
        /// </summary>
        /// <param name="extension">Extension name.</param>
        /// <returns>Enumeration of types associated with given extension.</returns>
        public static IEnumerable<PhpTypeInfo> GetTypesByExtension(string extension)
        {
            return ExtensionsAppContext.ExtensionsTable.GetTypesByExtension(extension);
        }

        #endregion

        #region Scripts

        /// <summary>
        /// Gets enumeration of scripts that were included.
        /// </summary>
        public IEnumerable<ScriptInfo> GetIncludedScripts() => _scripts.GetIncludedScripts();

        /// <summary>
        /// Declares or redeclares script within runtime using delegate.
        /// The script will be available for inclusions.
        /// </summary>
        /// <param name="relpath">Relative path of the script without leading slash.</param>
        /// <param name="main">Script entry point.</param>
        public static void DeclareScript(string relpath, MainDelegate main) => ScriptsMap.DeclareScript(relpath, main);

        /// <summary>
        /// Tries to resolve compiled script according to given path.
        /// </summary>
        public static ScriptInfo TryResolveScript(string root, string path) => ScriptsMap.ResolveInclude(path, root, null, null, null);

        /// <summary>
        /// Gets script according to its relative path as it was declared in <see cref="Context"/>.
        /// </summary>
        /// <param name="relpath">Relative script path.</param>
        /// <returns>Script descriptor, can be invalid if script was not declared.</returns>
        public static ScriptInfo TryGetDeclaredScript(string relpath) => ScriptsMap.GetDeclaredScript(relpath);

        /// <summary>
        /// Gets scripts in given directory.
        /// </summary>
        public static bool TryGetScriptsInDirectory(string root, string path, out IEnumerable<ScriptInfo> scripts)
        {
            // assert: root is not suffixed with directory separator
            
            // trim leading {root} path:
            if (!string.IsNullOrEmpty(root) && path.StartsWith(root, CurrentPlatform.PathStringComparison))
            {
                if (path.Length == root.Length)
                {
                    path = string.Empty;
                }
                else if (path[root.Length] == CurrentPlatform.DirectorySeparator)
                {
                    path = (path.Length > root.Length + 1) ? path.Substring(root.Length + 1) : string.Empty;
                }
                else
                {
                    scripts = Enumerable.Empty<ScriptInfo>();
                    return false;
                }
            }

            // try to get compiled scripts within path:
            return ScriptsMap.TryGetDirectory(path, out scripts);
        }

        #endregion

        #region Constants

        /// <summary>
        /// Tries to get a global constant from current context.
        /// </summary>
        public bool TryGetConstant(string name, out PhpValue value)
        {
            int idx = 0;
            return TryGetConstant(name, out value, ref idx);
        }

        /// <summary>
        /// Tries to get a global constant from current context.
        /// </summary>
        internal bool TryGetConstant(string name, out PhpValue value, ref int idx) => _constants.TryGetConstant(name, ref idx, out value);

        /// <summary>
        /// Defines a user constant.
        /// </summary>
        public bool DefineConstant(string name, PhpValue value, bool ignorecase = false)
        {
            int idx = 0;
            return DefineConstant(name, value, ref idx, ignorecase);
        }

        /// <summary>
        /// Defines a user constant.
        /// </summary>
        internal bool DefineConstant(string name, PhpValue value, ref int idx, bool ignorecase = false) => ConstsMap.DefineConstant(ref _constants, name, value, ref idx, ignorecase);

        /// <summary>
        /// Determines whether a constant with given name is defined.
        /// </summary>
        public bool IsConstantDefined(string name) => _constants.IsDefined(name);

        /// <summary>
        /// Gets enumeration of all available constants and their values.
        /// </summary>
        public IEnumerable<ConstantInfo> GetConstants() => _constants;

        #endregion
    }
}
