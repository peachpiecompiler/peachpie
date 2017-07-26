using Pchp.Core;
using Pchp.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;

namespace Pchp.Library.Reflection
{
    [PhpType("[name]"), PhpExtension(ReflectionUtils.ExtensionName)]
    public abstract class ReflectionFunctionAbstract : Reflector
    {
        #region Fields & Properties

        /// <summary>
        /// Gets name of the function.
        /// </summary>
        public string name
        {
            get
            {
                return _routine.Name;
            }
            //set
            //{
            //    // Read-only, throws ReflectionException in attempt to write.
            //    throw new ReflectionException(); // TODO: message
            //}
        }

        /// <summary>
        /// Underlaying routine information.
        /// Cannot be <c>null</c>.
        /// </summary>
        internal protected RoutineInfo _routine;

        #endregion

        //private void __clone(void ) { throw new NotImplementedException(); }
        public ReflectionClass getClosureScopeClass() { throw new NotImplementedException(); }
        public object getClosureThis() { throw new NotImplementedException(); }
        public string getDocComment() { throw new NotImplementedException(); }
        public long getEndLine() { throw new NotImplementedException(); }
        //public ReflectionExtension getExtension() { throw new NotImplementedException(); }
        public string getExtensionName() { throw new NotImplementedException(); }
        public virtual string getFileName(Context ctx) { throw new NotImplementedException(); }
        public string getName() => name;
        public string getNamespaceName()
        {
            // opposite of getShortName()
            var name = this.name;
            var sep = name.LastIndexOf(ReflectionUtils.NameSeparator);
            return (sep < 0) ? string.Empty : name.Remove(sep);
        }
        public long getNumberOfParameters() { throw new NotImplementedException(); }
        public long getNumberOfRequiredParameters() { throw new NotImplementedException(); }

        /// <summary>
        /// Get the parameters as an array of <see cref="ReflectionParameter"/>.
        /// </summary>
        /// <returns>The parameters, as <see cref="ReflectionParameter"/> objects.</returns>
        public PhpArray getParameters()
        {
            int minParamsCount;         // To track the least number of parameters among all the overloads
            int skippedParamsCount;     // The number of initial implicit parameters, which are not retrieved
            ParameterInfo[] parameters;
            if (_routine.Methods.Length == 1)
            {
                // The most common and simplest case
                parameters = _routine.Methods[0].GetParameters();
                skippedParamsCount = CountImplicitParameters(parameters);
                minParamsCount = parameters.Length - skippedParamsCount;
            }
            else
            {
                ProcessParametersOfOverloads(_routine.Methods, out minParamsCount, out parameters, out skippedParamsCount);
            }

            var result = new PhpArray(parameters.Length - skippedParamsCount);
            for (int i = skippedParamsCount; i < parameters.Length; i++)
            {
                // If the parameter is not present in an overload, it is effectively optional
                var realPosition = i - skippedParamsCount;
                bool forceOptional = realPosition >= minParamsCount;

                result.Add(new ReflectionParameter(parameters[i], forceOptional));
            }

            return result;
        }

        /// <summary>
        /// Analyses all the overloads, selects one as the canonical for the reflection purposes
        /// and retrieves information about its parameters.
        /// </summary>
        private static void ProcessParametersOfOverloads(
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
                skippedParamsCounts[i] = CountImplicitParameters(overloadParameters[i]);

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

        private static int CountImplicitParameters(ParameterInfo[] parameters)
        {
            return parameters.TakeWhile(Pchp.Core.Reflection.ReflectionUtils.IsImplicitParameter).Count();
        }

        //public ReflectionType getReturnType() { throw new NotImplementedException(); }
        public string getShortName()
        {
            var name = this.name;
            var sep = name.LastIndexOf(ReflectionUtils.NameSeparator);
            return (sep < 0) ? name : name.Substring(sep + 1);
        }
        public long getStartLine() { throw new NotImplementedException(); }
        public PhpArray getStaticVariables() { throw new NotImplementedException(); }
        public bool hasReturnType() { throw new NotImplementedException(); }
        public bool inNamespace() => name.IndexOf(ReflectionUtils.NameSeparator) > 0;
        public bool isClosure() { throw new NotImplementedException(); }
        public bool isDeprecated() { throw new NotImplementedException(); }
        public bool isGenerator() { throw new NotImplementedException(); }
        public bool isInternal() => !isUserDefined();
        public bool isUserDefined() => _routine.IsUserFunction;
        public bool isVariadic() { throw new NotImplementedException(); }
        public bool returnsReference() => _routine.Methods.Any(m => m.ReturnType == typeof(PhpAlias));

        public string __toString()
        {
            throw new NotImplementedException();
        }
    }
}
