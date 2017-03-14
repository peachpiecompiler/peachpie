using Pchp.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    partial class Context
    {
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
        public T Create<T>([ImportCallerClass]RuntimeTypeHandle caller, params PhpValue[] arguments)
            => (T)TypeInfoHolder<T>.TypeInfo.ResolveCreator(Type.GetTypeFromHandle(caller))(this, arguments);

        /// <summary>
        /// Creates an instance of a type dynamically.
        /// </summary>
        public object Create(string classname) => Create(classname, Array.Empty<PhpValue>());

        /// <summary>
        /// Creates an instance of a type dynamically with constructor overload resolution.
        /// </summary>
        /// <param name="classname">Full name of the class to instantiate. The name uses PHP syntax of name separators (<c>\</c>) and is case insensitive.</param>
        /// <param name="arguments">Arguments to be passed to the constructor.</param>
        /// <returns>Object instance or <c>null</c> if class is not declared.</returns>
        public object Create(string classname, params object[] arguments) => Create(classname, PhpValue.FromClr(arguments));

        /// <summary>
        /// Creates an instance of a type dynamically with constructor overload resolution.
        /// </summary>
        /// <param name="classname">Full name of the class to instantiate. The name uses PHP syntax of name separators (<c>\</c>) and is case insensitive.</param>
        /// <param name="arguments">Arguments to be passed to the constructor.</param>
        /// <returns>Object instance or <c>null</c> if class is not declared.</returns>
        public object Create(string classname, params PhpValue[] arguments) => Create(default(RuntimeTypeHandle), classname, arguments);

        /// <summary>
        /// Creates an instance of a type dynamically with constructor overload resolution.
        /// </summary>
        /// <param name="caller">
        /// Class context for resolving constructors visibility.
        /// Can be <c>default(<see cref="RuntimeTypeHandle"/>)</c> to resolve public constructors only.</param>
        /// <param name="classname">Full name of the class to instantiate. The name uses PHP syntax of name separators (<c>\</c>) and is case insensitive.</param>
        /// <param name="arguments">Arguments to be passed to the constructor.</param>
        /// <returns>Object instance or <c>null</c> if class is not declared.</returns>
        public object Create([ImportCallerClass]RuntimeTypeHandle caller, string classname, params PhpValue[] arguments) => Create(caller, this.GetDeclaredType(classname, true), arguments);

        /// <summary>
        /// Creates an instance of a type dynamically with constructor overload resolution.
        /// </summary>
        /// <param name="caller">
        /// Class context for resolving constructors visibility.
        /// Can be <c>default(<see cref="RuntimeTypeHandle"/>)</c> to resolve public constructors only.</param>
        /// <param name="tinfo">Type to be instantiated.</param>
        /// <param name="arguments">Arguments to be passed to the constructor.</param>
        /// <returns>Object instance or <c>null</c> if class is not declared.</returns>
        public object Create([ImportCallerClass]RuntimeTypeHandle caller, PhpTypeInfo tinfo, params PhpValue[] arguments)
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
        public static IEnumerable<string> GetRoutinesByExtension(string extension)
        {
            return ExtensionsAppContext.ExtensionsTable.GetRoutinesByExtension(extension).Select(r => r.Name);
        }

        /// <summary>
        /// Gets enumeration of scripts that were included.
        /// </summary>
        public IEnumerable<ScriptInfo> GetIncludedScripts() => _scripts.GetIncludedScripts();
    }
}
