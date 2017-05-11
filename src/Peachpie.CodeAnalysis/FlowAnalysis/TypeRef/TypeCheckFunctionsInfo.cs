using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.CodeAnalysis;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Stores information about a type checking function such as is_int.
    /// </summary>
    public sealed class TypeCheckFunctionInfo
    {
        public delegate TypeRefMask TypeRefMaskResolver(TypeRefContext ctx);

        /// <summary>
        /// Provides the mask of the types that the variable is restricted to in order for the function to return true.
        /// </summary>
        public TypeRefMaskResolver RestrictIfTrue { get; }

        /// <summary>
        /// Provides the mask of the types that the variable can't be in order for the function to return false.
        /// </summary>
        /// <remarks>
        /// Usually it is the same as <see cref="RestrictIfTrue"/>, but functions such as is_numeric have stronger implications in
        /// the true branch than in the false one.
        /// </remarks>
        public TypeRefMaskResolver RemoveIfFalse { get; }

        public TypeCheckFunctionInfo(TypeRefMaskResolver restrictIfTrue, TypeRefMaskResolver removeIfFalse)
        {
            this.RestrictIfTrue = restrictIfTrue;
            this.RemoveIfFalse = removeIfFalse;
        }

        public TypeCheckFunctionInfo(TypeRefMaskResolver restrictOrRemove) :
            this(restrictOrRemove, restrictOrRemove)
        {
        }
    }

    /// <summary>
    /// Contains information about functions that query the type of the given variable, e.g. is_int, is_bool.
    /// </summary>
    public static class TypeCheckFunctionsInfo
    {
        public static TypeCheckFunctionInfo IsInt { get; } = new TypeCheckFunctionInfo(ctx => ctx.GetLongTypeMask());

        public static TypeCheckFunctionInfo IsBool { get; } = new TypeCheckFunctionInfo(ctx => ctx.GetBooleanTypeMask());

        public static TypeCheckFunctionInfo IsFloat { get; } = new TypeCheckFunctionInfo(ctx => ctx.GetDoubleTypeMask());

        public static TypeCheckFunctionInfo IsString { get; } =
            new TypeCheckFunctionInfo(ctx => ctx.GetStringTypeMask() | ctx.GetWritableStringTypeMask());

        public static TypeCheckFunctionInfo IsArray { get; } = new TypeCheckFunctionInfo(ctx => ctx.GetArrayTypeMask());

        public static TypeCheckFunctionInfo IsResource { get; } = new TypeCheckFunctionInfo(ctx => ctx.GetResourceTypeMask());

        public static TypeCheckFunctionInfo IsNull { get; } = new TypeCheckFunctionInfo(ctx => ctx.GetNullTypeMask());

        // is_numeric - a string can return true or false, a number always returns true
        public static TypeCheckFunctionInfo IsNumeric { get; } =
                new TypeCheckFunctionInfo(
                    ctx => ctx.GetNumberTypeMask() | ctx.GetStringTypeMask() | ctx.GetWritableStringTypeMask(),
                    ctx => ctx.GetNumberTypeMask());

        // is_callable - various types potentially return true or false, only a closure always returns true
        public static TypeCheckFunctionInfo IsCallable { get; } =
                new TypeCheckFunctionInfo(
                    ctx => ctx.GetCallableTypeMask(),
                    ctx => ctx.GetClosureTypeMask());

        private static Dictionary<string, TypeCheckFunctionInfo> _functionNameInfoMap = new Dictionary<string, TypeCheckFunctionInfo>()
        {
            { "is_int", IsInt },
            { "is_integer", IsInt },
            { "is_long", IsInt },

            { "is_bool", IsBool },

            { "is_float", IsFloat },
            { "is_double", IsFloat },
            { "is_real", IsFloat },

            { "is_string", IsString },

            { "is_array", IsArray },

            // TODO: is_object

            { "is_resource", new TypeCheckFunctionInfo(ctx => ctx.GetResourceTypeMask()) },

            { "is_null", IsNull },

            // TODO: is_scalar

            { "is_numeric", IsNumeric },

            { "is_callable", IsCallable },

            // TODO: is_iterable
        };

        public static bool TryGetByName(string functionName, out TypeCheckFunctionInfo typeAdjustmentInfo)
        {
            return _functionNameInfoMap.TryGetValue(functionName, out typeAdjustmentInfo);
        }
    }
}
