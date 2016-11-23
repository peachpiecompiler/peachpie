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
        /// Call the object magic method.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="target">Object instance.</param>
        /// <param name="method">Magic method to be called.</param>
        /// <param name="arguments">Provided arguments.</param>
        /// <returns>__invoke return value.</returns>
        public PhpValue Call<T>(T target, TypeMethods.MagicMethods method, params PhpValue[] arguments) where T : class  // TODO: check magic exists
            => ((PhpMethodInfo)TypeInfoHolder<T>.TypeInfo.DeclaredMethods[method]).PhpInvokable(this, target, arguments);

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
        public object Create(string classname, params PhpValue[] arguments)
        {
            var tinfo = _types.GetDeclaredType(classname);
            if (tinfo != null)
            {
                return tinfo.Creator(this, arguments);
            }
            else
            {
                // TODO: Err class not known
                return null;
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
    }
}
