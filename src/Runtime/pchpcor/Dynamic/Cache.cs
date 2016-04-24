using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Dynamic
{
    internal static class Cache
    {
        public static class Types
        {
            public static Type[] Empty = new Type[0];
            public static Type[] Int = new Type[] { typeof(int) };
            public static Type[] Long = new Type[] { typeof(long) };
            public static Type[] Double = new Type[] { typeof(double) };
            public static Type[] String = new Type[] { typeof(string) };
            public static Type[] Bool = new Type[] { typeof(bool) };
            public static Type[] Object = new Type[] { typeof(object) };
            public static Type[] PhpString = new Type[] { typeof(PhpString) };
            public static Type[] PhpValue = new Type[] { typeof(PhpValue) };
            public static Type[] PhpAlias = new Type[] { typeof(PhpAlias) };
        }

        public static class Operators
        {
            /// <summary><see cref="Core.Operators.SetValue(ref PhpValue, PhpValue)"/>.</summary>
            public static MethodInfo SetValue_PhpValueRef_PhpValue = typeof(Core.Operators).GetMethod("SetValue", typeof(PhpValue).MakeByRefType(), typeof(PhpValue));

            public static MethodInfo PhpAlias_EnsureObject_Context = typeof(PhpAlias).GetMethod("EnsureObject", typeof(Context));
            public static MethodInfo PhpAlias_EnsureArray = typeof(PhpAlias).GetMethod("EnsureArray");
            public static MethodInfo PhpValue_EnsureObject_Context = typeof(PhpValue).GetMethod("EnsureObject", typeof(Context));
            public static MethodInfo PhpValue_EnsureArray = typeof(PhpValue).GetMethod("EnsureArray");
            public static MethodInfo PhpValue_EnsureAlias = typeof(PhpValue).GetMethod("EnsureAlias");
            public static MethodInfo PhpValue_ToClass = typeof(PhpValue).GetMethod("ToClass", typeof(Context));
        }

        public static class Object
        {
            /// <summary><see cref="System.Object"/>.</summary>
            public static new MethodInfo ToString = typeof(object).GetMethod("ToString");
        }

        /// <summary>
        /// Gets method info in given type.
        /// </summary>
        public static MethodInfo GetMethod(this Type type, string name, params Type[] ptypes)
        {
            var result = type.GetRuntimeMethod(name, ptypes);
            Debug.Assert(result != null);
            return result;
        }
    }
}
