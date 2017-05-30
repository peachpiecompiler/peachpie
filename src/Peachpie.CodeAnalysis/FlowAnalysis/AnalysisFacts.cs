using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    static class AnalysisFacts
    {
        /// <summary>
        /// Resolves value of the function call in compile time if possible and updates the variable type if necessary
        /// </summary>
        public static void HandleFunctionCall(BoundGlobalFunctionCall call, ExpressionAnalysis analysis, ConditionBranch branch)
        {
            // Only direct function names
            if (!HasSimpleName(call, out string name))
            {
                return;
            }

            // Type checking functions
            if (branch != ConditionBranch.AnyResult && CanBeTypeCheckingFunction(call, name, out var arg))
            {
                if (HandleTypeCheckingFunctions(call, name, arg, analysis, branch))
                {
                    return;
                }
            }

            // Functions with all arguments resolved
            if (call.ArgumentsInSourceOrder.All(a => a.Value.ConstantValue.HasValue))
            {
                // Clear out the constant value result from the previous run of this method (if it was valid, it will be reassigned below)
                call.ConstantValue = default(Optional<object>);

                var args = call.ArgumentsInSourceOrder;
                switch (name)
                {
                    case "function_exists":
                        if (args.Length == 1)
                        {
                            // TRUE <=> function name is defined unconditionally in a reference library (PE assembly)
                            var str = args[0].Value.ConstantValue.Value as string;
                            if (str != null)
                            {
                                var tmp = analysis.Model.ResolveFunction(NameUtils.MakeQualifiedName(str, true));
                                if (tmp is PEMethodSymbol || (tmp is AmbiguousMethodSymbol && ((AmbiguousMethodSymbol)tmp).Ambiguities.All(f => f is PEMethodSymbol)))
                                {
                                    call.ConstantValue = new Optional<object>(true);
                                    return;
                                }
                            }
                        }
                        break;
                }
            }
        }

        private static bool HasSimpleName(BoundGlobalFunctionCall call, out string name)
        {
            if (call.Name.IsDirect)
            {
                // Take the function name ignoring current namespace resolution, simple names only:
                var qualifiedName = call.NameOpt.HasValue ? call.NameOpt.Value : call.Name.NameValue;
                if (qualifiedName.IsSimpleName)
                {
                    name = qualifiedName.Name.Value;
                    return true;
                }
            }

            name = null;
            return false;
        }

        private static bool CanBeTypeCheckingFunction(BoundGlobalFunctionCall call, string name, out BoundVariableRef arg)
        {
            if (name.StartsWith("is_") && call.ArgumentsInSourceOrder.Length == 1
                && call.ArgumentsInSourceOrder[0].Value is BoundVariableRef onlyArg)
            {
                arg = onlyArg;
                return true;
            }
            else
            {
                arg = null;
                return false;
            }
        }

        /// <summary>
        /// Processes functions such as is_int, is_bool etc. Returns whether the function was one of these.
        /// </summary>
        private static bool HandleTypeCheckingFunctions(
            BoundGlobalFunctionCall call,
            string name,
            BoundVariableRef arg,
            ExpressionAnalysis analysis,
            ConditionBranch branch)
        {
            var typeCtx = analysis.TypeCtx;
            var flowState = analysis.State;
            Optional<object> resultConstVal;
            bool wasHandled;

            switch (name)
            {
                case "is_int":
                case "is_integer":
                case "is_long":
                    resultConstVal = EnsureType(arg, typeCtx.GetLongTypeMask(), branch, flowState);
                    wasHandled = true;
                    break;

                case "is_bool":
                    resultConstVal = EnsureType(arg, typeCtx.GetBooleanTypeMask(), branch, flowState);
                    wasHandled = true;
                    break;

                case "is_float":
                case "is_double":
                case "is_real":
                    resultConstVal = EnsureType(arg, typeCtx.GetDoubleTypeMask(), branch, flowState);
                    wasHandled = true;
                    break;

                case "is_string":
                    var stringMask = typeCtx.GetStringTypeMask() | typeCtx.GetWritableStringTypeMask();
                    resultConstVal = EnsureType(arg, stringMask, branch, flowState);
                    wasHandled = true;
                    break;

                case "is_resource":
                    resultConstVal = EnsureType(arg, typeCtx.GetResourceTypeMask(), branch, flowState);
                    wasHandled = true;
                    break;

                case "is_null":
                    resultConstVal = EnsureType(arg, typeCtx.GetNullTypeMask(), branch, flowState);
                    wasHandled = true;
                    break;

                case "is_array":
                    resultConstVal = EnsureType(
                        arg,
                        currentType => typeCtx.GetArraysFromMask(currentType),
                        branch,
                        flowState,
                        skipTrueIfAnyType: true);
                    wasHandled = true;
                    break;

                case "is_object":
                    // Keep IncludesSubclasses flag in the true branch and clear it in the false branch
                    resultConstVal = EnsureType(
                        arg,
                        currentType => typeCtx.GetObjectsFromMask(currentType).WithIncludesSubclasses,
                        branch,
                        flowState,
                        skipTrueIfAnyType: true);
                    wasHandled = true;
                    break;

                // TODO
                //case "is_scalar":
                //    return;

                case "is_numeric":
                    resultConstVal = EnsureType(
                        arg,
                        currentType =>
                        {
                            // Specify numeric types if they are present 
                            var targetType = typeCtx.IsLong(currentType) ? typeCtx.GetLongTypeMask() : 0;
                            targetType |= typeCtx.IsDouble(currentType) ? typeCtx.GetDoubleTypeMask() : 0;

                            if (branch == ConditionBranch.ToTrue)
                            {
                                // Also string types can make is_numeric return true, but not anything else
                                targetType |= typeCtx.IsReadonlyString(currentType) ? typeCtx.GetStringTypeMask() : 0;
                                targetType |= typeCtx.IsWritableString(currentType) ? typeCtx.GetWritableStringTypeMask() : 0;

                                return targetType;
                            }
                            else
                            {
                                // For number, is_numeric always returns true -> remove numeric types from false branch
                                return targetType;
                            }
                        },
                        branch,
                        flowState,
                        skipTrueIfAnyType: true);
                    wasHandled = true;
                    break;

                case "is_callable":
                    resultConstVal = EnsureType(
                        arg,
                        currentType =>
                        {
                            // Closure is specified in both branches
                            TypeRefMask targetType = 0;
                            AddTypeIfInContext(typeCtx, NameUtils.SpecialNames.Closure, false, ref targetType);

                            if (branch == ConditionBranch.ToTrue)
                            {
                                // Also string types, arrays and object can make is_callable return true, but not anything else
                                targetType |= typeCtx.IsReadonlyString(currentType) ? typeCtx.GetStringTypeMask() : 0;
                                targetType |= typeCtx.IsWritableString(currentType) ? typeCtx.GetWritableStringTypeMask() : 0;
                                targetType |= typeCtx.GetArraysFromMask(currentType);
                                AddTypeIfInContext(typeCtx, NameUtils.SpecialNames.System_Object, true, ref targetType);

                                return targetType;
                            }
                            else
                            {
                                // For closure, is_callable always returns true -> remove the closure type from false branch,
                                // don't remove IncludeSubclasses flag
                                return targetType;
                            }
                        },
                        branch,
                        flowState,
                        skipTrueIfAnyType: true);
                    wasHandled = true;
                    break;

                // TODO
                //case "is_iterable":
                //    return;

                default:
                    resultConstVal = default(Optional<object>);
                    wasHandled = false;
                    break;
            }

            if (!wasHandled)
            {
                return false;
            }

            // Each branch can clean only the constant value it produced during its analysis (in order not to loose result
            // of the other branch): true branch can produce false value and vice versa
            if (!resultConstVal.EqualsOptional(call.ConstantValue)
                && (resultConstVal.HasValue
                    || call.ConstantValue.Value is false && branch == ConditionBranch.ToTrue
                    || call.ConstantValue.Value is true && branch == ConditionBranch.ToFalse))
            {
                call.ConstantValue = resultConstVal;
            }

            return true;
        }

        private static void AddTypeIfInContext(TypeRefContext typeCtx, QualifiedName name, bool includeSubclasses, ref TypeRefMask mask)
        {
            var closureTypeRef = typeCtx.Types.FirstOrDefault(t => t.QualifiedName == NameUtils.SpecialNames.Closure);
            if (closureTypeRef != null)
            {
                mask |= typeCtx.GetTypeMask(closureTypeRef, includeSubclasses);
            }
        }

        /// <summary>
        /// Ensures that the variable is of the type(s) given by <paramref name="targetType"/> in the true branch and
        /// not of this type in the false branch. If <paramref name="skipTrueIfAnyType"/> is true, the IsAnyType variant
        /// is skipped in the true branch. In the false branch, it is always skipped.
        /// </summary>
        /// <returns>
        /// Returns optional constant value of a comparison of the variable against the type(s).
        /// </returns>
        public static Optional<object> EnsureType(
            BoundVariableRef varRef,
            TypeRefMask targetType,
            ConditionBranch branch,
            FlowState flowState,
            bool skipTrueIfAnyType = false)
        {
            if (!TryGetVariableHandle(varRef, flowState, out VariableHandle handle))
            {
                return default(Optional<object>);
            }

            var currentType = flowState.GetLocalType(handle);

            return EnsureTypeImpl(currentType, targetType, branch, flowState, handle, skipTrueIfAnyType);
        }

        /// <summary>
        /// Ensures that the variable is of the given type(s) (retrieved by <paramref name="targetTypeCallback"/> from its
        /// current type) in the true branch and not of this type in the false branch. If <paramref name="skipTrueIfAnyType"/>
        /// is true, the IsAnyType variant is skipped in the true branch. In the false branch, it is always skipped.
        /// </summary>
        /// <returns>
        /// Returns optional constant value of a comparison of the variable against the type(s).
        /// </returns>
        public static Optional<object> EnsureType(
            BoundVariableRef varRef,
            Func<TypeRefMask, TypeRefMask> targetTypeCallback,
            ConditionBranch branch,
            FlowState flowState,
            bool skipTrueIfAnyType = false)
        {
            if (!TryGetVariableHandle(varRef, flowState, out VariableHandle handle))
            {
                return default(Optional<object>);
            }

            var currentType = flowState.GetLocalType(handle);
            var targetType = targetTypeCallback(currentType);

            return EnsureTypeImpl(currentType, targetType, branch, flowState, handle, skipTrueIfAnyType);
        }

        private static bool TryGetVariableHandle(BoundVariableRef varRef, FlowState state, out VariableHandle varHandle)
        {
            if (varRef.Variable.VariableKind == VariableKind.LocalVariable || varRef.Variable.VariableKind == VariableKind.Parameter)
            {
                varHandle = state.GetLocalHandle(varRef.Variable.Name);
                return true;
            }
            else
            {
                varHandle = default(VariableHandle);
                return false;
            }
        }

        private static Optional<object> EnsureTypeImpl(
            TypeRefMask currentType,
            TypeRefMask targetType,
            ConditionBranch branch,
            FlowState flowState,
            VariableHandle handle,
            bool skipTrueIfAnyType)
        {
            Optional<object> result = default(Optional<object>);

            if (branch == ConditionBranch.ToTrue)
            {
                // In the true branch the IsAnyType case can be optionally skipped
                if (skipTrueIfAnyType && currentType.IsAnyType)
                {
                    return result;
                }

                // Intersect the possible types with those checked by the function, always keeping the IsRef flag.
                // IncludesSubclasses is kept only if it is specified in targetType.
                TypeRefMask resultType = (currentType & (targetType | TypeRefMask.IsRefMask));

                if (resultType.IsVoid)
                {
                    // Clearing the type out in this branch means the function will always return false.
                    // In order to prevent errors in analysis and code generation, set the type to the one specified.
                    resultType = targetType | (currentType & TypeRefMask.IsRefMask);

                    // Make this function always return false
                    result = new Optional<object>(false);
                }

                flowState.SetLocalType(handle, resultType);
            }
            else
            {
                Debug.Assert(branch == ConditionBranch.ToFalse);

                // In the false branch we cannot handle the IsAnyType case
                if (currentType.IsAnyType)
                {
                    return result;
                }

                // Remove the types and flags excluded by the fact that the function returned false
                TypeRefMask resultType = currentType & (~targetType);

                if (resultType.IsVoid)
                {
                    // Clearing the type out in this branch means the function will always return true.
                    // In order to prevent errors in analysis and code generation, do not alter the type in this case.

                    // Make this function always return true
                    result = new Optional<object>(true);
                }
                else
                {
                    flowState.SetLocalType(handle, resultType);
                }
            }

            return result;
        }
    }
}