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
        public PhpValue Call(string function, params object[] arguments) => PhpCallback.Create(function).Invoke(this, arguments.Select(obj => PhpValue.FromClr(obj)).ToArray());

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
    }
}
