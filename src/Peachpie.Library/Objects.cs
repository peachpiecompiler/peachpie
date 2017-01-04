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
            => ctx.GetDeclaredType(className, autoload) != null;

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
            var info = ctx.GetDeclaredType(ifaceName, autoload);
            return info != null && info.IsInterface;
        }

        /// <summary>
        /// Returns the name of the callers class context.
        /// </summary>
        /// <param name="tctx">Current class context.</param>
        /// <returns>Current class name.</returns>
        [return: CastToFalse]
        public static string get_class([ImportCallerClass]string tctx)
        {
            return tctx;
        }

        /// <summary>
        /// Returns the name of the class of which the object <paramref name="obj"/> is an instance.
        /// </summary>
        /// <param name="tctx">Current class context.</param>
        /// <param name="obj">The object whose class is requested.</param>
        /// <returns><paramref name="obj"/>'s class name or current class name if <paramref name="obj"/> is <B>null</B>.</returns>
        [return: CastToFalse]
        public static string get_class([ImportCallerClass]string tctx, PhpValue obj)
        {
            if (obj.IsSet)
            {
                if (obj.IsObject)
                {
                    return obj.Object.GetType().FullName.Replace('.', '\\');
                }
                else if (obj.IsAlias)
                {
                    return get_class(tctx, obj.Alias.Value);
                }
                else
                {
                    // TODO: E_WARNING
                    throw new ArgumentException(nameof(obj));
                }
            }

            return tctx;
        }

        /// <summary>
        /// Helper getting declared classes or interfaces.
        /// </summary>
        /// <param name="ctx">Runtime context with declared types.</param>
        /// <param name="interfaces">Whether to list interfaces or classes.</param>
        static PhpArray get_declared_types(Context ctx, bool interfaces)
        {
            var result = new PhpArray();

            foreach (var t in ctx.GetDeclaredTypes())
            {
                if (t.IsInterface == interfaces)
                {
                    result.Add(t.Name);
                }
            }

            return result;
        }

        /// <summary>
		/// Returns a <see cref="PhpArray"/> with names of all defined classes (system and user).
		/// </summary>
		/// <returns><see cref="PhpArray"/> of class names.</returns>
		public static PhpArray get_declared_classes(Context ctx) => get_declared_types(ctx, false);

        /// <summary>
        /// Returns a <see cref="PhpArray"/> with names of all defined interfaces (system and user).
        /// </summary>
        /// <returns><see cref="PhpArray"/> of interface names.</returns>
        public static PhpArray get_declared_interfaces(Context ctx) => get_declared_types(ctx, true);

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
            return obj != null && Core.Convert.IsInstanceOf(obj, ctx.GetDeclaredType(class_name));  // double check (obj!=null) for performance reasons
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
