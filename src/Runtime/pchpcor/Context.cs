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

namespace Pchp.Core
{
    using TFunctionsMap = Context.HandleMap<RuntimeMethodHandle, Providers.RuntimeMethodHandleComparer, Providers.OrdinalIgnoreCaseStringComparer>;
    using TTypesMap = Context.HandleMap<Type, Providers.TypeComparer, Providers.OrdinalIgnoreCaseStringComparer>;
    
    /// <summary>
    /// Runtime context for a PHP application.
    /// </summary>
    /// <remarks>
    /// The object represents a current Web request or the application run.
    /// Its instance is passed to all PHP function.
    /// The context is not thread safe.
    /// </remarks>
    public partial class Context : IDisposable
    {
        #region Create

        protected Context()
        {
            _functions = new TFunctionsMap(FunctionRedeclared);
            _types = new TTypesMap(TypeRedeclared);
            _statics = new object[_staticsCount];

            _globals = new PhpArray();
            // TODO: InitGlobalVariables(); //_globals.SetItemAlias(new IntStringKey("GLOBALS"), new PhpAlias(PhpValue.Create(_globals)));
        }

        /// <summary>
        /// Creates context to be used within a console application.
        /// </summary>
        public static Context CreateConsole()
        {
            return new Context();
            // TODO: Add console output filter
        }

        public static Context CreateEmpty()
        {
            return new Context();
        }

        #endregion

        #region Symbols

        /// <summary>
        /// Map of global functions.
        /// </summary>
        readonly TFunctionsMap _functions;

        /// <summary>
        /// Map of global types.
        /// </summary>
        readonly TTypesMap _types;

        /// <summary>
        /// Map of global constants.
        /// </summary>
        readonly ConstsMap _constants = new ConstsMap();

        readonly ScriptsMap _scripts = new ScriptsMap();

        /// <summary>
        /// Internal method to be used by loader to load referenced symbols.
        /// </summary>
        /// <typeparam name="TScript"><c>&lt;Script&gt;</c> type in compiled assembly. The type contains static methods for enumerating referenced symbols.</typeparam>
        public static void AddScriptReference<TScript>() => AddScriptReference(typeof(TScript));

        private static void AddScriptReference(Type tscript)
        {
            Debug.Assert(tscript != null);
            Debug.Assert(tscript.Name == "<Script>");

            var tscriptinfo = tscript.GetTypeInfo();

            TFunctionsMap.LazyAddReferencedSymbols(() =>
            {
                tscriptinfo.GetDeclaredMethod("EnumerateReferencedFunctions")
                    .Invoke(null, new object[] { new Action<string, RuntimeMethodHandle>(TFunctionsMap.AddReferencedSymbol) });
            });

            tscriptinfo.GetDeclaredMethod("EnumerateScripts")
                .Invoke(null, new object[] { new Action<string, RuntimeMethodHandle>(ScriptsMap.DeclareScript) });

            tscriptinfo.GetDeclaredMethod("EnumerateConstants")
                .Invoke(null, new object[] { new Action<string, PhpValue, bool>(ConstsMap.DefineAppConstant) });
        }

        /// <summary>
        /// Declare a runtime function.
        /// </summary>
        /// <param name="index">Index variable.</param>
        /// <param name="name">Fuction name.</param>
        /// <param name="handle">Function runtime handle.</param>
        public void DeclareFunction(ref int index, string name, RuntimeMethodHandle handle)
        {
            _functions.Declare(ref index, name, handle);
        }

        public void AssertFunctionDeclared(ref int index, string name, RuntimeMethodHandle handle)
        {
            if (!_functions.IsDeclared(ref index, name, handle))
            {
                // TODO: ErrCode function is not declared
            }
        }

        /// <summary>
        /// Gets declared function with given name. In case of more items they are considered as overloads.
        /// </summary>
        internal RuntimeMethodHandle[] GetDeclaredFunction(string name) => _functions.TryGetHandle(name);

        /// <summary>
        /// Declare a runtime type.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="name">Type name.</param>
        public void DeclareType<T>(string name)
        {
            _types.Declare(ref IndexHolder<T>.Index, name, typeof(T));
        }

        public void AssertTypeDeclared<T>(string name)
        {
            if (!_types.IsDeclared(ref IndexHolder<T>.Index, name, typeof(T)))
            {
                // TODO: ErrCode type is not declared
            }
        }

        /// <summary>
        /// Gets declared function with given name. In case of more items they are considered as overloads.
        /// </summary>
        internal Type[] GetDeclaredType(string name) => _types.TryGetHandle(name);

        void FunctionRedeclared(RuntimeMethodHandle handle)
        {
            // TODO: ErrCode & throw
            throw new InvalidOperationException($"Function {System.Reflection.MethodBase.GetMethodFromHandle(handle).Name} redeclared!");
        }

        void TypeRedeclared(Type handle)
        {
            Debug.Assert(handle != null);

            // TODO: ErrCode & throw
            throw new InvalidOperationException($"Type {handle.FullName} redeclared!");
        }

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
        public void OnInclude<TScript>() => _scripts.SetIncluded<TScript>();

        /// <summary>
        /// Resolves path according to PHP semantics, lookups the file in runtime tables and calls its Main method within the global scope.
        /// </summary>
        /// <param name="dir">Current script directory. Used for relative path resolution. Can be <c>null</c> to not resolve against current directory.</param>
        /// <param name="path">The relative or absolute path to resolve and include.</param>
        /// <param name="once">Whether to include according to include once semantics.</param>
        /// <param name="throwOnError">Whether to include according to require semantics.</param>
        /// <returns>Inclusion result value.</returns>
        public PhpValue Include(string dir, string path, bool once = false, bool throwOnError = false)
            => Include(dir, path, _globals, null, once, throwOnError);

        /// <summary>
        /// Resolves path according to PHP semantics, lookups the file in runtime tables and calls its Main method.
        /// </summary>
        /// <param name="cd">Current script directory. Used for relative path resolution. Can be <c>null</c> to not resolve against current directory.</param>
        /// <param name="path">The relative or absolute path to resolve and include.</param>
        /// <param name="locals">Variables scope for the included script.</param>
        /// <param name="this">Reference to <c>this</c> variable.</param>
        /// <param name="once">Whether to include according to include once semantics.</param>
        /// <param name="throwOnError">Whether to include according to require semantics.</param>
        /// <returns>Inclusion result value.</returns>
        public PhpValue Include(string cd, string path, PhpArray locals, object @this = null, bool once = false, bool throwOnError = false)
        {
            var script = ScriptsMap.SearchForIncludedFile(path, null, cd, _scripts.GetScript);  // TODO: _scripts.GetScript => make relative path from absolute
            if (script.IsValid)
            {
                if (once && _scripts.IsIncluded(script.Index))
                {
                    return PhpValue.Create(true);
                }
                else
                {
                    return script.MainMethod(this, locals, @this);
                }
            }
            else
            {
                if (throwOnError)
                {
                    throw new ArgumentException();   // TODO: ErrCode
                }
                else
                {
                    return PhpValue.Create(false);   // TODO: Warning
                }
            }
        }

        #endregion

        #region Path Resolving

        /// <summary>
        /// Root directory (web root or console app root) where loaded scripts are relative to.
        /// </summary>
        /// <remarks>
        /// - <c>__FILE__</c> and <c>__DIR__</c> magic constants are resolved as concatenation with this value.
        /// </remarks>
        public virtual string RootPath { get; } = "";

        /// <summary>
        /// Gets full script path in current context.
        /// </summary>
        /// <typeparam name="TScript">Script type.</typeparam>
        /// <returns>Full script path.</returns>
        public string ScriptPath<TScript>() => RootPath + ScriptsMap.GetScript<TScript>().Path;

        #endregion

        #region GetStatic

        /// <summary>
        /// Helper generic class holding an app static index to array of static objects.
        /// </summary>
        /// <typeparam name="T">Type of object kept as context static.</typeparam>
        static class IndexHolder<T>
        {
            /// <summary>
            /// Index of the object of type <typeparamref name="T"/>.
            /// </summary>
            public static int Index;
        }

        /// <summary>
        /// Gets static object instance within the context with given index.
        /// Initializes the index with new unique value if necessary.
        /// </summary>
        T GetStatic<T>(ref int idx) where T : new()
        {
            if (idx <= 0)
                idx = NewIdx();

            return GetStatic<T>(idx);
        }

        /// <summary>
        /// Gets static object instance within the context with given index.
        /// </summary>
        T GetStatic<T>(int idx) where T : new()
        {
            EnsureStaticsSize(idx);
            return GetStatic<T>(ref _statics[idx]);
        }

        /// <summary>
        /// Ensures the <see cref="_statics"/> array has sufficient size to hold <paramref name="idx"/>;
        /// </summary>
        /// <param name="idx">Index of an object to be stored within statics.</param>
        void EnsureStaticsSize(int idx)
        {
            if (_statics.Length <= idx)
            {
                Array.Resize(ref _statics, (idx + 1) * 2);
            }
        }

        /// <summary>
        /// Ensures the context static object is initialized.
        /// </summary>
        T GetStatic<T>(ref object obj) where T : new()
        {
            if (obj == null)
            {
                obj = new T();

                if (obj is IStaticInit)
                {
                    ((IStaticInit)obj).Init(this);
                }
            }

            Debug.Assert(obj is T);
            return (T)obj;
        }

        /// <summary>
        /// Gets context static object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type of the object to be stored within context.</typeparam>
        public T GetStatic<T>() where T : new() => GetStatic<T>(ref IndexHolder<T>.Index);

        /// <summary>
        /// Gets new index to be used within <see cref="_statics"/> array.
        /// </summary>
        int NewIdx()
        {
            int idx;

            lock (_statics)
            {
                idx = Interlocked.Increment(ref _staticsCount);
            }

            return idx;
        }

        /// <summary>
        /// Static objects within the context.
        /// Cannot be <c>null</c>.
        /// </summary>
        object[] _statics;

        /// <summary>
        /// Number of static objects so far registered within context.
        /// </summary>
        static volatile int/*!*/_staticsCount;

        #endregion

        #region Superglobals

        /// <summary>
        /// Array of global variables. Cannot be <c>null</c>.
        /// </summary>
        public PhpArray Globals
        {
            get { return _globals; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();  // TODO: ErrCode
                }

                _globals = value;
            }
        }
        PhpArray _globals;

        #endregion

        #region Constants

        /// <summary>
        /// Gets a constant value.
        /// </summary>
        public PhpValue GetConstant(string name)
        {
            int idx = 0;
            return GetConstant(name, ref idx);
        }

        /// <summary>
        /// Gets a constant value.
        /// </summary>
        public PhpValue GetConstant(string name, ref int idx)
        {
            return _constants.GetConstant(name, ref idx);

            // TODO: check the constant is valid (PhpValue.IsSet) otherwise Warning: undefined constant
        }

        /// <summary>
        /// Defines a runtime constant.
        /// </summary>
        public bool DefineConstant(string name, PhpValue value, bool ignorecase = false) => _constants.DefineConstant(name, value, ignorecase);

        /// <summary>
        /// Determines whether a constant with given name is defined.
        /// </summary>
        public bool IsConstantDefined(string name) => _constants.IsDefined(name);

        /// <summary>
        /// Gets enumeration of all available constants and their values.
        /// </summary>
        public IEnumerable<KeyValuePair<string, PhpValue>> GetConstants() => _constants;

        #endregion

        #region Error Reporting

        /// <summary>
        /// Whether to throw an exception on soft error (Notice, Warning, Strict).
        /// </summary>
        public bool ThrowExceptionOnError { get; set; } = true;

        /// <summary>
        /// Gets whether error reporting is disabled or enabled.
        /// </summary>
        public bool ErrorReportingDisabled => _errorReportingDisabled != 0; // && !config.ErrorControl.IgnoreAtOperator;
        int _errorReportingDisabled = 0;

        /// <summary>
        /// Disables error reporting. Can be called for multiple times. To enable reporting again 
        /// <see cref="EnableErrorReporting"/> should be called as many times as <see cref="DisableErrorReporting"/> was.
        /// </summary>
        public void DisableErrorReporting()
        {
            _errorReportingDisabled++;
        }

        /// <summary>
        /// Enables error reporting disabled by a single call to <see cref="DisableErrorReporting"/>.
        /// </summary>
        public void EnableErrorReporting()
        {
            if (_errorReportingDisabled > 0)
                _errorReportingDisabled--;
        }

        /// <summary>
        /// Terminates execution of the current script by throwing an exception.
        /// </summary>
        /// <param name="status">Exit status.</param>
        public virtual void Exit(PhpValue status)
        {
            if (IsExitStatusPrintable(ref status))
            {
                Echo(status);
            }
            else
            {
                //this.ExitCode = status.ToLong();
                // TODO: in Main() return ctx.ExitCode;
            }

            throw new ScriptDiedException(status);
        }

        static bool IsExitStatusPrintable(ref PhpValue status)
        {
            switch (status.TypeCode)
            {
                case PhpTypeCode.Int32:
                case PhpTypeCode.Long:
                    return false;

                case PhpTypeCode.Alias:
                    return IsExitStatusPrintable(ref status.Alias.Value);

                default:
                    return true;
            }
        }

        public void Exit(long status) => Exit(PhpValue.Create(status));

        public void Exit() => Exit(255);

        #endregion

        #region IDisposable

        public void Dispose()
        {

        }

        #endregion
    }
}
