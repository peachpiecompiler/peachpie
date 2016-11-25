using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Pchp.Library
{
    public static class Objects
    {
        /// <summary>
		/// Tests whether a given class is defined.
		/// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="className">The name of the class.</param>
		/// <param name="autoload">Whether to attempt to call <c>__autoload</c>.</param>
		/// <returns><B>true</B> if the class given by <paramref name="className"/> has been defined,
		/// <B>false</B> otherwise.</returns>
		public static bool class_exists(Context ctx, string className, bool autoload = true)
        {
            var info = ctx.GetDeclaredType(className);
            if (info == null && autoload)
            {
                throw new NotImplementedException("autoload");
            }

            //
            return info != null;
        }

        /// <summary>
		/// Tests whether a given interface is defined.
		/// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="ifaceName">The name of the interface.</param>
		/// <param name="autoload">Whether to attempt to call <c>__autoload</c>.</param>
		/// <returns><B>true</B> if the interface given by <paramref name="ifaceName"/> has been defined,
		/// <B>false</B> otherwise.</returns>
		public static bool interface_exists(Context ctx, string ifaceName, bool autoload = true)
        {
            var info = ctx.GetDeclaredType(ifaceName);
            if (info == null && autoload)
            {
                throw new NotImplementedException("autoload");
            }

            //
            return info != null && info.Type.GetTypeInfo().IsInterface;
        }

        /// <summary>
		/// Tests whether <paramref name="obj"/>'s class is derived from a class given by <paramref name="class_name"/>.
		/// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="obj">The object to test.</param>
		/// <param name="class_name">The name of the class.</param>
        /// <returns><B>true</B> if the object <paramref name="obj"/> belongs to <paramref name="class_name"/> class or
		/// a class which is a subclass of <paramref name="class_name"/>, <B>false</B> otherwise.</returns>
        public static bool is_a(Context ctx, object obj, string class_name)
        {
            if (obj != null)
            {
                var tinfo = ctx.GetDeclaredType(class_name);
                return tinfo != null && tinfo.Type.GetTypeInfo().IsInstanceOfType(obj);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
		/// Tests whether <paramref name="value"/>'s class is derived from a class given by <paramref name="class_name"/>.
		/// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="value">The object to test.</param>
		/// <param name="class_name">The name of the class.</param>
        /// <param name="allow_string">If this parameter set to FALSE, string class name as object is not allowed. This also prevents from calling autoloader if the class doesn't exist.</param>
        /// <returns><B>true</B> if the object <paramref name="value"/> belongs to <paramref name="class_name"/> class or
		/// a class which is a subclass of <paramref name="class_name"/>, <B>false</B> otherwise.</returns>
        public static bool is_a(Context ctx, PhpValue value, string class_name, bool allow_string)
        {
            var obj = value.AsObject();

            if (allow_string)
            {
                // value can be a string specifying a class name
                // autoload is allowed

                throw new NotImplementedException();
            }
            else
            {
                return is_a(ctx, obj, class_name);
            }
        }
    }
}
