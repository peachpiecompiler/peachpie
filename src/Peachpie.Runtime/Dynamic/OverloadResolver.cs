using Pchp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core.Reflection;

namespace Pchp.Core.Dynamic
{
    /// <summary>
    /// Methods for selecting best method overload from possible candidates.
    /// </summary>
    internal static class OverloadResolver
    {
        /// <summary>
        /// Selects only candidates of given name.
        /// </summary>
        public static MethodInfo[] SelectRuntimeMethods(this PhpTypeInfo tinfo, string name)
        {
            var routine = (PhpMethodInfo)tinfo?.RuntimeMethods[name];
            return (routine != null)
                ? routine.Methods
                : Array.Empty<MethodInfo>();
        }

        public static IEnumerable<MethodInfo> SelectVisible(this MethodInfo[] methods, Type classCtx)
        {
            if (methods.Length == 0)
            {
                return methods;
            }
            else if (methods.Length == 1)
            {
                return methods[0].IsVisible(classCtx) ? methods : Array.Empty<MethodInfo>();
            }
            else
            {
                return methods.Where(m => m.IsVisible(classCtx));
            }
        }

        public static IEnumerable<MethodBase> Construct(this IEnumerable<MethodBase> methods, Type[] typeargs)
        {
            if (typeargs != null && typeargs.Length != 0)
            {
                return methods
                    .Where(m => m.IsGenericMethodDefinition && m.GetGenericArguments().Length == typeargs.Length)
                    .OfType<MethodInfo>()
                    .Select(m =>
                    {
                        try
                        {
                            return m.MakeGenericMethod(typeargs);
                        }
                        catch
                        {
                            return null;
                        }
                    })
                    .WhereNotNull();
            }
            else
            {
                // select non-generic methods
                return methods.Where(m => m.IsGenericMethodDefinition == false);
            }
        }

        /// <summary>
        /// Selects only static methods.
        /// </summary>
        public static IEnumerable<MethodBase> SelectStatic(this IEnumerable<MethodBase> candidates)
        {
            return candidates.Where(m => m.IsStatic);
        }
    }
}
