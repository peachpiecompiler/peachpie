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
    }
}
