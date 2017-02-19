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

        public static MethodInfo[] SelectVisible(this MethodInfo[] methods, Type classCtx)
        {
            if (methods.Length == 1)
            {
                return methods[0].IsVisible(classCtx) ? methods : Array.Empty<MethodInfo>();
            }

            var result = new List<MethodInfo>(methods.Length);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].IsVisible(classCtx))
                {
                    result.Add(methods[i]);
                }
            }

            return (result.Count == methods.Length) ? methods : result.ToArray();
        }

        public static IEnumerable<MethodBase> SelectVisible(this IEnumerable<MethodBase> candidates, Type classCtx)
        {
            if (classCtx == null)
            {
                return candidates.Where(m => m.IsPublic);
            }

            return candidates.Where(m => m.IsVisible(classCtx));
        }

        /// <summary>
        /// Selects only static methods.
        /// </summary>
        public static IEnumerable<MethodBase> SelectStatic(this IEnumerable<MethodBase> candidates)
        {
            return candidates.Where(m => m.IsStatic);
        }

        /// <summary>
        /// Gets value indicating the parameter is a special late static bound parameter.
        /// </summary>
        static bool IsStaticBoundParameter(ParameterInfo p)
        {
            return p.ParameterType == typeof(Type) && p.Name == "<static>";
        }

        /// <summary>
        /// Gets value indicating the parameter is a special local parameters parameter.
        /// </summary>
        static bool IsLocalsParameter(ParameterInfo p)
        {
            return p.ParameterType == typeof(PhpArray) && p.Name == "<locals>";
        }
    }
}
