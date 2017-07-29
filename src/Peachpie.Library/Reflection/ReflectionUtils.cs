using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Pchp.Core.Reflection;

namespace Pchp.Library.Reflection
{
    internal static class ReflectionUtils
    {
        public const char NameSeparator = '\\';

        public const string ExtensionName = "Reflection";

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
