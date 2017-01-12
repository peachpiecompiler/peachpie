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
                PhpException.ArgumentNull("function");
                return PhpValue.Null;
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

        #region func_num_args, func_get_arg, func_get_args

        /// <summary>
		/// Retrieves the number of arguments passed to the current user-function.
		/// </summary>
		public static int func_num_args([ImportCallerArgs]PhpValue[] args) => args.Length;

        /// <summary>
        /// Retrieves an argument passed to the current user-function.
        /// </summary>
        /// <remarks><seealso cref="PhpStack.GetArgument"/></remarks>
        public static PhpValue func_get_arg([ImportCallerArgs]PhpValue[] args, int index)
        {
            //// checks correctness of the argument:
            //if (index < 0)
            //{
            //    PhpException.InvalidArgument("index", "arg_negative");
            //    return PhpValue.False;
            //}

            //if (index >= arg_count)
            //{
            //    PhpException.Throw(PhpError.Warning, CoreResources.GetString("argument_not_passed_to_function", index));
            //    return PhpValue.False;
            //}

            //
            return args[index].DeepCopy();
        }

        /// <summary>
        /// Returns an array of arguments of the current user-defined function. 
        /// </summary>
        /// <remarks><seealso cref="PhpStack.GetArguments"/>
        /// Also throws warning if called from global scope.</remarks>
        public static PhpArray func_get_args([ImportCallerArgs]PhpValue[] args)
        {
            // TODO: when called from global code, return FALSE

            var result = new PhpArray(args.Length);

            for (int i = 0; i < args.Length; i++)
            {
                result.AddValue(args[i].DeepCopy());
            }

            //
            return result;
        }

        #endregion

        #region function_exists, get_defined_functions

        /// <summary>
        /// Checks the list of defined functions, both built-in and user-defined.
        /// </summary>
        /// <returns>Whether the function is declared.</returns>
        public static bool function_exists(Context ctx, string name)
        {
            return ctx.GetDeclaredFunction(name) != null;
        }

        /// <summary>
		/// Retrieves defined functions.
		/// </summary>
        /// <param name="ctx">Current runtime context.</param>
		/// <returns>
		/// The <see cref="PhpArray"/> containing two entries with keys "internal" and "user".
		/// The former's value is a <see cref="PhpArray"/> containing PHP library functions as values.
		/// The latter's value is a <see cref="PhpArray"/> containing user defined functions as values.
		/// Keys of both these arrays are integers starting from 0.
		/// </returns>
		/// <remarks>User functions which are declared conditionally and was not declared yet is considered as not existent.</remarks>
		public static PhpArray get_defined_functions(Context ctx)
        {
            var result = new PhpArray(2);
            var library = new PhpArray(500);
            var user = new PhpArray();

            foreach (var routine in ctx.GetDeclaredFunctions())
            {
                (routine.IsUserFunction ? user : library).AddValue((PhpValue)routine.Name);
            }

            //
            result["internal"] = (PhpValue)library;
            result["user"] = (PhpValue)user;

            //
            return result;
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
