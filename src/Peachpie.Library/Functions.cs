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

        /// <summary>
        /// Checks the list of defined functions, both built-in and user-defined.
        /// </summary>
        /// <returns>Whether the function is declared.</returns>
        public static bool function_exists(Context ctx, string name)
        {
            return ctx.GetDeclaredFunction(name) != null;
        }
    }
}
