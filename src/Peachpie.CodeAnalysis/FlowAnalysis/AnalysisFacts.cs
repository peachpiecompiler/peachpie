﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.TypeRef;
using Pchp.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    static class AnalysisFacts
    {
        public static bool IsAutoloadDeprecated(Version langVersion)
        {
            // >= 7.2
            return langVersion != null && (langVersion.Major > 7 || (langVersion.Major == 7 && langVersion.Minor >= 2));
        }

        public static bool IsStringableSupported(PhpCompilation compilation)
        {
            // >= 8.0
            return
                compilation.CoreTypes.Stringable.Symbol is PENamedTypeSymbol pe &&
                pe.TryGetPhpTypeAttribute(out _, out var minLangVersion) &&
                minLangVersion != null &&
                compilation.Options.LanguageVersion >= minLangVersion;

            //var langVersion = compilation.Options.LanguageVersion;
            //return langVersion != null && langVersion.Major >= 8;
        }

        /// <summary>
        /// Determines if given global function symbol is unconditionally declared (always declared).
        /// </summary>
        static bool IsUnconditionalDeclaration(IPhpRoutineSymbol symbol)
        {
            // method declaration in a referenced assembly,
            // not within a compiled source script:
            if (symbol is PEMethodSymbol method)
            {
                var container = method.ContainingType;
                return container != null && !container.IsPhpSourceFile();   // only functions declared in libraries, not in PHP source file
            }

            // Ambiguity,
            // only if all possible symbols are unconditionally declared.
            if (symbol is AmbiguousMethodSymbol ambiguous)
            {
                foreach (var a in ambiguous.Ambiguities)
                {
                    if (!IsUnconditionalDeclaration(a))
                    {
                        return false;
                    }
                }

                return true;
            }

            //
            return false;
        }

        /// <summary>
        /// Resolves value of the function call in compile time if possible and updates the variable type if necessary
        /// </summary>
        public static void HandleSpecialFunctionCall<T>(BoundGlobalFunctionCall call, ExpressionAnalysis<T> analysis, ConditionBranch branch)
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

            var args = call.ArgumentsInSourceOrder;

            // Clear out the constant value result from the previous run of this method (if it was valid, it will be reassigned below)
            call.ConstantValue = default;

            string str;

            switch (name) // TODO: case insensitive
            {
                case "is_callable":     // bool is_callable( string $function_name )
                case "function_exists": // bool function_exists ( string $function_name )
                    if (args.Length == 1 && args[0].Value.ConstantValue.TryConvertToString(out str))
                    {
                        // TRUE <=> function is defined unconditionally in a reference library (PE assembly)
                        if (IsUnconditionalDeclaration(analysis.Model.ResolveFunction(NameUtils.MakeQualifiedName(str, true))))
                        {
                            call.ConstantValue = ConstantValueExtensions.AsOptional(true);
                        }
                    }
                    break;

                // bool class_exists ( string $class_name [, bool $autoload = true ] )
                case "class_exists":
                case "interface_exists":
                    if (args.Length >= 1)
                    {
                        // TRUE <=> class is defined unconditionally in a reference library (PE assembly)
                        var class_name = args[0].Value.ConstantValue.Value as string;
                        if (!string.IsNullOrEmpty(class_name))
                        {
                            var tmp = (TypeSymbol)analysis.Model.ResolveType(NameUtils.MakeQualifiedName(class_name, true));
                            if (tmp is PENamedTypeSymbol && !tmp.IsPhpUserType())   // TODO: + SourceTypeSymbol when reachable unconditional declaration
                            {
                                bool @interface = (name == "interface_exists");
                                if (tmp.TypeKind == (@interface ? TypeKind.Interface : TypeKind.Class))
                                {
                                    call.ConstantValue = ConstantValueExtensions.AsOptional(true);
                                }
                            }
                        }
                    }
                    break;

                // bool method_exists ( string $class_name , string $method_name )
                case "method_exists":
                    if (args.Length == 2)
                    {
                        var class_name = args[0].Value.ConstantValue.Value as string;
                        if (class_name != null && args[1].Value.ConstantValue.TryConvertToString(out str))
                        {
                            var tmp = (NamedTypeSymbol)analysis.Model.ResolveType(NameUtils.MakeQualifiedName(class_name, true));
                            if (tmp is PENamedTypeSymbol && !tmp.IsPhpUserType())
                            {
                                if (tmp.LookupMethods(str).Count != 0) // TODO: why not User Types // TODO: why not resolve FALSE as well below?
                                {
                                    call.ConstantValue = ConstantValueExtensions.AsOptional(true);
                                }
                            }
                        }
                    }
                    break;

                case "defined":     // defined(CONST_NAME)
                case "constant":    // constant(CONST_NAME)
                    if (args.Length == 1 && args[0].Value.ConstantValue.TryConvertToString(out str))
                    {
                        // TODO: const_name in form of "{CLASS}::{NAME}"
                        // TODO: also user constants defined in the same scope?

                        // quick evaluation of PE constants that can't be changed at run time

                        var tmp = analysis.Model.ResolveConstant(str);
                        if (tmp is PEFieldSymbol fld)
                        {
                            if (name == "defined")
                            {
                                call.ConstantValue = ConstantValueExtensions.AsOptional(true);
                            }
                            else // name == "constant"
                            {
                                if (fld.Type.Is_Func_Context_TResult(out var tresult))
                                {
                                    call.TypeRefMask = TypeRefFactory.CreateMask(analysis.TypeCtx, tresult);
                                }
                                else
                                {
                                    var cvalue = fld.GetConstantValue(false);
                                    call.ConstantValue = (cvalue != null) ? new Optional<object>(cvalue.Value) : null;
                                    call.TypeRefMask = TypeRefFactory.CreateMask(analysis.TypeCtx, fld.Type, notNull: fld.IsNotNull());
                                }
                            }
                        }
                        else if (tmp is PEPropertySymbol prop)
                        {
                            if (name == "defined")
                            {
                                call.ConstantValue = ConstantValueExtensions.AsOptional(true);
                            }
                            else // name == "constant"
                            {
                                call.TypeRefMask = TypeRefFactory.CreateMask(analysis.TypeCtx, prop.Type, notNull: prop.IsNotNull());
                            }
                        }
                    }
                    break;

                case "strlen":
                    if (args.Length == 1 && args[0].Value.ConstantValue.TryConvertToString(out string value) && StringUtils.IsAsciiString(value))
                    {
                        call.ConstantValue = new Optional<object>(value.Length);
                    }
                    break;

                case "file_exists":
                    if (args.Length == 1)
                    {
                        if (TryResolveFile(analysis.Model, analysis.Routine, args[0].Value, out var script))
                        {
                            // there is compiled script at this path,
                            // the expression will be always true
                            call.ConstantValue = true.AsOptional();
                        }
                    }
                    break;
            }
        }

        public static bool TryResolveFile(ISymbolProvider model, SourceRoutineSymbol routine, BoundExpression expr, out IPhpScriptTypeSymbol script)
        {
            script = null;

            if (expr.ConstantValue.TryConvertToString(out var path))
            {
                // include (path)
                script = model.ResolveFile(path);
            }
            else if (expr is BoundPseudoConst pc1 && pc1.ConstType == BoundPseudoConst.Types.File) // __FILE__
            {
                script = routine.ContainingFile;
            }
            else if (expr is BoundConcatEx concat) // common case
            {
                // include (dirname( __FILE__ ) . path) // changed to (__DIR__ . path) by graph rewriter
                // include (__DIR__ . path)
                if (concat.ArgumentsInSourceOrder.Length == 2 &&
                    concat.ArgumentsInSourceOrder[0].Value is BoundPseudoConst pc && pc.ConstType == BoundPseudoConst.Types.Dir &&
                    concat.ArgumentsInSourceOrder[1].Value.ConstantValue.TryConvertToString(out path))
                {
                    // create project relative path
                    // not starting with a directory separator!
                    path = routine.ContainingFile.DirectoryRelativePath + path;
                    if (path.Length != 0 && Roslyn.Utilities.PathUtilities.IsAnyDirectorySeparator(path[0])) path = path.Substring(1);   // make nicer when we have a helper method for that
                    script = model.ResolveFile(path);
                }
                else // include (RootPath . path)
                if (concat.ArgumentsInSourceOrder.Length == 2 &&
                    concat.ArgumentsInSourceOrder[0].Value is BoundPseudoConst pc2 && pc2.ConstType == BoundPseudoConst.Types.RootPath &&
                    concat.ArgumentsInSourceOrder[1].Value.ConstantValue.TryConvertToString(out path))
                {
                    // create project relative path
                    // not starting with a directory separator!
                    if (path.Length != 0 && Roslyn.Utilities.PathUtilities.IsAnyDirectorySeparator(path[0])) path = path.Substring(1);   // make nicer when we have a helper method for that
                    script = model.ResolveFile(path);
                }
            }

            return script != null;
        }

        public static bool HasSimpleName(BoundGlobalFunctionCall call, out string name)
        {
            if (call.Name.IsDirect)
            {
                // Take the function name ignoring current namespace resolution, simple names only:
                var qualifiedName = call.NameOpt ?? call.Name.NameValue;
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
        private static bool HandleTypeCheckingFunctions<T>(
            BoundGlobalFunctionCall call,
            string name,
            BoundVariableRef arg,
            ExpressionAnalysis<T> analysis,
            ConditionBranch branch)
        {
            var typeCtx = analysis.TypeCtx;
            var flowState = analysis.State;

            switch (name)
            {
                case "is_int":
                case "is_integer":
                case "is_long":
                    HandleTypeCheckingExpression(arg, typeCtx.GetLongTypeMask(), branch, flowState, checkExpr: call);
                    return true;

                case "is_bool":
                    HandleTypeCheckingExpression(arg, typeCtx.GetBooleanTypeMask(), branch, flowState, checkExpr: call);
                    return true;

                case "is_float":
                case "is_double":
                case "is_real":
                    HandleTypeCheckingExpression(arg, typeCtx.GetDoubleTypeMask(), branch, flowState, checkExpr: call);
                    return true;

                case "is_string":
                    var stringMask = typeCtx.GetStringTypeMask() | typeCtx.GetWritableStringTypeMask();
                    HandleTypeCheckingExpression(arg, stringMask, branch, flowState, checkExpr: call);
                    return true;

                case "is_resource":
                    HandleTypeCheckingExpression(arg, typeCtx.GetResourceTypeMask(), branch, flowState, checkExpr: call);
                    return true;

                case "is_null":
                    HandleTypeCheckingExpression(arg, typeCtx.GetNullTypeMask(), branch, flowState, checkExpr: call);
                    return true;

                case "is_array":
                    HandleTypeCheckingExpression(
                        arg,
                        currentType => typeCtx.GetArraysFromMask(currentType),
                        branch,
                        flowState,
                        skipPositiveIfAnyType: true,
                        checkExpr: call);
                    return true;

                case "is_object":
                    // Keep IncludesSubclasses flag in the true branch and clear it in the false branch
                    HandleTypeCheckingExpression(
                        arg,
                        currentType =>
                        {
                            var objType = currentType.IsAnyType
                            ? typeCtx.GetSystemObjectTypeMask()
                            : typeCtx.GetObjectsFromMask(currentType);
                            
                            return objType.WithSubclasses;
                        },
                        branch,
                        flowState,
                        skipPositiveIfAnyType: false,
                        checkExpr: call);
                    return true;

                // TODO
                //case "is_scalar":
                //    return;

                case "is_numeric":
                    HandleTypeCheckingExpression(
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
                        skipPositiveIfAnyType: true,
                        checkExpr: call);
                    return true;

                case "is_callable":
                    HandleTypeCheckingExpression(
                        arg,
                        currentType =>
                        {
                            // Closure and lambdas are specified in both branches
                            TypeRefMask targetType = typeCtx.GetClosureTypeMask();
                            targetType |= typeCtx.GetLambdasFromMask(currentType);

                            if (branch == ConditionBranch.ToTrue)
                            {
                                // Also string types, arrays and objects can make is_callable return true, but not anything else
                                targetType |= typeCtx.IsReadonlyString(currentType) ? typeCtx.GetStringTypeMask() : 0;
                                targetType |= typeCtx.IsWritableString(currentType) ? typeCtx.GetWritableStringTypeMask() : 0;
                                targetType |= typeCtx.GetArraysFromMask(currentType);
                                targetType |= typeCtx.GetObjectsFromMask(currentType);

                                return targetType;
                            }
                            else
                            {
                                // For closure and lambdas, is_callable always returns true -> remove them from false branch,
                                // don't remove IncludeSubclasses flag
                                return targetType;
                            }
                        },
                        branch,
                        flowState,
                        skipPositiveIfAnyType: true,
                        checkExpr: call);
                    return true;

                // TODO
                //case "is_iterable":
                //    return;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Ensures that the variable is of the given type(s) in the positive branch or not of this type in the negative
        /// branch. If the current branch is unfeasible, assigns an appropriate boolean to the
        /// <see cref="BoundExpression.ConstantValue"/> of <paramref name="checkExpr"/>.
        /// </summary>
        /// <param name="varRef">The reference to the variable whose types to check.</param>
        /// <param name="targetType">The target type of the variable.</param>
        /// <param name="branch">The branch to check - <see cref="ConditionBranch.ToTrue"/> is understood as the positive
        /// branch if <paramref name="isPositiveCheck"/> is true.</param>
        /// <param name="flowState">The flow state of the branch.</param>
        /// <param name="skipPositiveIfAnyType">Whether to skip a mask with <see cref="TypeRefMask.IsAnyType"/> in the
        /// positive branch (in the negative branch, it is always skipped).</param>
        /// <param name="checkExpr">The expression to have its <see cref="BoundExpression.ConstantValue"/> potentially
        /// updated.</param>
        /// <param name="isPositiveCheck">Whether the expression returns true when the type check succeeds. For example,
        /// in the case of != it would be false.</param>
        public static void HandleTypeCheckingExpression(
            BoundVariableRef varRef,
            TypeRefMask targetType,
            ConditionBranch branch,
            FlowState flowState,
            bool skipPositiveIfAnyType = false,
            BoundExpression checkExpr = null,
            bool isPositiveCheck = true)
        {
            HandleTypeCheckingExpression(varRef, (var) => targetType, branch, flowState, skipPositiveIfAnyType, checkExpr, isPositiveCheck);
        }

        /// <summary>
        /// Ensures that the variable is of the given type(s) in the positive branch or not of this type in the negative
        /// branch. If the current branch is unfeasible, assigns an appropriate boolean to the
        /// <see cref="BoundExpression.ConstantValue"/> of <paramref name="checkExpr"/>.
        /// </summary>
        /// <param name="varRef">The reference to the variable whose types to check.</param>
        /// <param name="targetTypeCallback">The callback that receives the current type mask of the variable and returns
        /// the target one.</param>
        /// <param name="branch">The branch to check - <see cref="ConditionBranch.ToTrue"/> is understood as the positive
        /// branch if <paramref name="isPositiveCheck"/> is true.</param>
        /// <param name="flowState">The flow state of the branch.</param>
        /// <param name="skipPositiveIfAnyType">Whether to skip a mask with <see cref="TypeRefMask.IsAnyType"/> in the
        /// positive branch (in the negative branch, it is always skipped).</param>
        /// <param name="checkExpr">The expression to have its <see cref="BoundExpression.ConstantValue"/> potentially
        /// updated.</param>
        /// <param name="isPositiveCheck">Whether the expression returns true when the type check succeeds. For example,
        /// in the case of != it would be false.</param>
        public static void HandleTypeCheckingExpression(
            BoundVariableRef varRef,
            Func<TypeRefMask, TypeRefMask> targetTypeCallback,
            ConditionBranch branch,
            FlowState flowState,
            bool skipPositiveIfAnyType = false,
            BoundExpression checkExpr = null,
            bool isPositiveCheck = true)
        {
            if (!TryGetVariableHandle(varRef.Variable, flowState, out var handle))
            {
                return;
            }

            var currentType = varRef.TypeRefMask; // flowState.GetLocalType(handle);
            var targetType = targetTypeCallback(currentType);

            // Model negative type checks (such as $x != null) by inverting branches for the core checking function
            var branchHlp = isPositiveCheck ? branch : branch.NegativeBranch();

            bool isFeasible = HandleTypeChecking(currentType, targetType, branchHlp, flowState, handle, skipPositiveIfAnyType);

            // If the constant value was not meant to be updated, skip its computation
            if (checkExpr == null)
            {
                return;
            }

            if (!currentType.IsRef)
            {
                // If the true branch proves to be unfeasible, the function always returns false and vice versa
                var resultConstVal = isFeasible ? default(Optional<object>) : new Optional<object>(!branch.TargetValue().Value);

                // Each branch can clean only the constant value it produced during its analysis (in order not to lose result
                // of the other branch): true branch can produce false value and vice versa
                if (!resultConstVal.EqualsOptional(checkExpr.ConstantValue)
                    && (resultConstVal.HasValue
                        || checkExpr.ConstantValue.Value is false && branch == ConditionBranch.ToTrue
                        || checkExpr.ConstantValue.Value is true && branch == ConditionBranch.ToFalse))
                {
                    checkExpr.ConstantValue = resultConstVal;
                }
            }
            else
            {
                // We cannot reason about the result of the check if the variable can be modified by reference
                checkExpr.ConstantValue = default(Optional<object>);
            }
        }

        private static bool TryGetVariableHandle(IVariableReference boundvar, FlowState state, out VariableHandle varHandle)
        {
            if (boundvar is LocalVariableReference local && local.BoundName.IsDirect)  // direct variable name
            {
                if (local.VariableKind == VariableKind.LocalVariable ||
                    local.VariableKind == VariableKind.Parameter ||
                    local.VariableKind == VariableKind.LocalTemporalVariable)
                {
                    varHandle = state.GetLocalHandle(local.BoundName.NameValue);
                    return true;
                }
            }

            //
            varHandle = default(VariableHandle);
            return false;
        }

        private static bool HandleTypeChecking(
            TypeRefMask currentType,
            TypeRefMask targetType,
            ConditionBranch branch,
            FlowState flowState,
            VariableHandle handle,
            bool skipTrueIfAnyType)
        {
            // Information whether this path can ever be taken
            bool isFeasible = true;

            if (branch == ConditionBranch.ToTrue)
            {
                // In the true branch the IsAnyType case can be optionally skipped
                if (skipTrueIfAnyType && currentType.IsAnyType)
                {
                    return isFeasible;
                }

                // Intersect the possible types with those checked by the function, always keeping the IsRef flag.
                // IncludesSubclasses is kept only if it is specified in targetType.
                TypeRefMask resultType = (currentType & (targetType | TypeRefMask.IsRefMask));

                if (resultType.IsVoid)
                {
                    // Clearing the type out in this branch means the variable will never be of that type.
                    // In order to prevent errors in analysis and code generation, set the type to the one specified.
                    resultType = targetType | (currentType & TypeRefMask.IsRefMask);

                    isFeasible = false;
                }

                flowState.SetLocalType(handle, resultType);
            }
            else
            {
                Debug.Assert(branch == ConditionBranch.ToFalse);

                // In the false branch we cannot handle the IsAnyType case
                if (currentType.IsAnyType)
                {
                    return isFeasible;
                }

                // Remove the types and flags excluded by the fact that the function returned false
                TypeRefMask resultType = currentType & (~targetType);

                if (resultType.IsVoid)
                {
                    // Clearing the type out in this branch means the variable will always be of that type
                    // In order to prevent errors in analysis and code generation, do not alter the type in this case.

                    isFeasible = false;
                }
                else
                {
                    flowState.SetLocalType(handle, resultType);
                }
            }

            return isFeasible;
        }

        /// <summary>
        /// Returns whether the given type can be used as an array key.
        /// </summary>
        public static bool IsValidKeyType(IBoundTypeRef type)
        {
            if (type is BoundPrimitiveTypeRef pt)
            {
                switch (pt.TypeCode)
                {
                    case PhpTypeCode.Boolean:
                    case PhpTypeCode.Long:
                    case PhpTypeCode.Double:
                    case PhpTypeCode.String:
                    case PhpTypeCode.WritableString:
                    case PhpTypeCode.Null:
                    case PhpTypeCode.Mixed:
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// If present, transforms the given constant value to a string corresponding to the key under which the item is stored in an array.
        /// </summary>
        /// <param name="keyConst">Constant value of the key.</param>
        /// <param name="key">If <paramref name="keyConst"/> contains a value, the key as a (string, long) tuple.
        /// The second item should be taken into account only if the first one is null.</param>
        /// <returns>Whether the value was constant at all.</returns>
        public static bool TryGetCanonicKeyStringConstant(Optional<object> keyConst, out (string, long) key)
        {
            if (!keyConst.HasValue)
            {
                key = default;
                return false;
            }

            var obj = keyConst.Value;

            if (obj == null)
            {
                key = ("", default);
            }
            else if (keyConst.TryConvertToLong(out long l))
            {
                key = (null, l);
            }
            else if (obj is string s)
            {
                key = (s, default);
            }
            else
            {
                key = default;
                return false;
            }

            return true;
        }
    }
}