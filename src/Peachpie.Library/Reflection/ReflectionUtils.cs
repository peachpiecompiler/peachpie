using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Reflection;
using Pchp.Core.Resources;

namespace Pchp.Library.Reflection
{
    internal static class ReflectionUtils
    {
        public const char NameSeparator = '\\';

        public const string ExtensionName = "Reflection";

        /// <summary>
        /// Resolves type of <paramref name="class"/>.
        /// </summary>
        /// <param name="ctx">Context.</param>
        /// <param name="class">Either string or class instance. Otherwise an exception is thrown.</param>
        /// <returns>Type info. Cannot get <c>null</c>.</returns>
        /// <exception cref="ReflectionException">In case <paramref name="class"/> does not exist or <paramref name="class"/> is not a string or object.</exception>
        public static PhpTypeInfo ResolvePhpTypeInfo(Context ctx, PhpValue @class)
        {
            object instance;

            var classname = @class.ToStringOrNull();
            if (classname != null)
            {
                return ctx.GetDeclaredType(classname, true)
                    ?? throw new ReflectionException(string.Format(Resources.Resources.class_does_not_exist, classname));
            }
            else if ((instance = @class.AsObject()) != null)
            {
                return instance.GetPhpTypeInfo();
            }
            else
            {
                throw new ReflectionException(string.Format(ErrResources.invalid_argument_type, nameof(@class), "string or object"));
            }
        }

        /// <summary>
        /// Analyses all the overloads, selects one as the canonical for the reflection purposes
        /// and retrieves information about its parameters.
        /// </summary>
        public static void ProcessParametersOfOverloads(
            MethodInfo[] overloads,
            out int minParamsCount,
            out ParameterInfo[] canonicalParams,
            out int canonicalSkippedParams)
        {
            var overloadParameters = new ParameterInfo[overloads.Length][];
            var skippedParamsCounts = new int[overloads.Length];
            int canonicalI = 0;
            for (int i = 0; i < overloads.Length; i++)
            {
                // Cache parameters for the particular overload
                overloadParameters[i] = overloads[i].GetParameters();

                // Count skipped (implicit) first parameters
                skippedParamsCounts[i] = Core.Reflection.ReflectionUtils.ImplicitParametersCount(overloadParameters[i]);

                // Consider the last defined overload with the maximum number of parameters as canonical
                int paramsCount = overloadParameters[i].Length - skippedParamsCounts[i];
                if (i != canonicalI && overloadParameters[canonicalI].Length - skippedParamsCounts[canonicalI] <= paramsCount)
                {
                    canonicalI = i;
                }
            }

            // Find the least number of real (not implicit) parameters among all the overloads
            minParamsCount = Enumerable.Range(0, overloads.Length).Min(i => overloadParameters[i].Length - skippedParamsCounts[i]);

            canonicalParams = overloadParameters[canonicalI];
            canonicalSkippedParams = skippedParamsCounts[canonicalI];
        }
    }
}
