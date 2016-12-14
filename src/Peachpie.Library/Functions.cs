using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Library
{
    public static class Functions
    {
        #region call_user_func, call_user_func_array

        /// <summary>
		/// Calls a function or a method defined by callback with given arguments.
		/// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="function">Target callback.</param>
		/// <param name="args">The arguments.</param>
		/// <returns>The return value.</returns>
		public static PhpValue call_user_func(Context ctx, IPhpCallable function, params PhpValue[] args)
        {
            if (function == null)
            {
                //PhpException.ArgumentNull("function");
                //return null;
                throw new ArgumentNullException();  // NOTE: should not be reached, runtime converts NULL to InvalidCallback instance
            }

            Debug.Assert(args != null);

            // invoke the callback:
            return function.Invoke(ctx, args);
        }

        /// <summary>
        /// Calls a function or a method defined by callback with arguments stored in an array.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="function">Target callback.</param>
        /// <param name="args">Arguments. Can be null.</param>
        /// <returns>The returned value.</returns>
        public static PhpValue call_user_func_array(Context ctx, IPhpCallable function, PhpArray args)
        {
            PhpValue[] args_array;

            if (args != null && args.Count != 0)
            {
                args_array = new PhpValue[args.Count];
                args.CopyValuesTo(args_array, 0);
            }
            else
            {
                args_array = Core.Utilities.ArrayUtils.EmptyValues;
            }

            return call_user_func(ctx, function, args_array);
        }

        #endregion

        #region function_exists

        /// <summary>
        /// Checks the list of defined functions, both built-in and user-defined.
        /// </summary>
        /// <returns>Whether the function is declared.</returns>
        public static bool function_exists(Context ctx, string name)
        {
            return ctx.GetDeclaredFunction(name) != null;
        }

        #endregion

        #region register_shutdown_function

        /// <summary>
        /// Registers callback which will be called when script processing is complete but before the request
        /// has been complete.
        /// Function has no return value.
        /// </summary>
        /// <param name="ctx">Runtime context. Cannot be <c>null</c>.</param>
        /// <param name="callback">The function which is called after main code of the script is finishes execution.</param>
        /// <param name="arguments">Parameters for the <paramref name="callback"/>.</param>
        /// <remarks>
        /// Although, there is explicitly written in the PHP manual that it is not possible 
        /// to send an output to a browser via echo or another output handling functions you can actually do so.
        /// There is no such limitation with Phalanger.
        /// </remarks>
        public static void register_shutdown_function(Context ctx, IPhpCallable callback, params PhpValue[] arguments)
        {
            if (callback == null)
            {
                //PhpException.ArgumentNull("function");
                //return;
                throw new ArgumentNullException(nameof(callback));
            }

            ctx.RegisterShutdownCallback(() => callback.Invoke(ctx, arguments));
        }

        #endregion
    }
}
