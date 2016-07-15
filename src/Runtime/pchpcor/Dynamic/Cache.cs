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
            public static Type[] PhpString = new Type[] { typeof(Core.PhpString) };
            public static Type[] PhpValue = new Type[] { typeof(Core.PhpValue) };
            public static Type[] PhpAlias = new Type[] { typeof(Core.PhpAlias) };
            public static Type[] PhpNumber = new Type[] { typeof(Core.PhpNumber) };
            public static Type[] PhpArray = new Type[] { typeof(Core.PhpArray) };
        }

        public static class Operators
        {
            /// <summary><see cref="Core.Operators.SetValue(ref PhpValue, PhpValue)"/>.</summary>
            public static MethodInfo SetValue_PhpValueRef_PhpValue = typeof(Core.Operators).GetMethod("SetValue", typeof(PhpValue).MakeByRefType(), typeof(PhpValue));

            public static MethodInfo ToString_Double_Context = typeof(Core.Convert).GetMethod("ToString", typeof(double), typeof(Context));

            public static MethodInfo PhpAlias_EnsureObject_Context = typeof(PhpAlias).GetMethod("EnsureObject", Types.Empty);
            public static MethodInfo PhpAlias_EnsureArray = typeof(PhpAlias).GetMethod("EnsureArray", Types.Empty);

            public static MethodInfo PhpValue_EnsureObject_Context = typeof(PhpValue).GetMethod("EnsureObject", Types.Empty);
            public static MethodInfo PhpValue_EnsureArray = typeof(PhpValue).GetMethod("EnsureArray", Types.Empty);
            public static MethodInfo PhpValue_EnsureAlias = typeof(PhpValue).GetMethod("EnsureAlias", Types.Empty);
            public static MethodInfo PhpValue_ToClass = typeof(PhpValue).GetMethod("ToClass", Types.Empty);
            public static MethodInfo PhpValue_AsArray = typeof(PhpValue).GetMethod("AsArray", Types.Empty);
            public static MethodInfo PhpValue_AsCallable = typeof(PhpValue).GetMethod("AsCallable", Types.Empty);
            public static MethodInfo PhpValue_ToString_Context = typeof(PhpValue).GetMethod("ToString", typeof(Context));

            public static MethodInfo PhpArray_ToClass = typeof(PhpArray).GetMethod("ToClass", Types.Empty);
            public static MethodInfo PhpArray_SetItemAlias = typeof(PhpArray).GetMethod("SetItemAlias", typeof(IntStringKey), typeof(PhpAlias));
            public static MethodInfo PhpArray_SetItemValue = typeof(PhpArray).GetMethod("SetItemValue", typeof(IntStringKey), typeof(PhpValue));
            public static MethodInfo PhpArray_EnsureItemObject = typeof(PhpArray).GetMethod("EnsureItemObject", typeof(IntStringKey));
            public static MethodInfo PhpArray_EnsureItemArray = typeof(PhpArray).GetMethod("EnsureItemArray", typeof(IntStringKey));
            public static MethodInfo PhpArray_EnsureItemAlias = typeof(PhpArray).GetMethod("EnsureItemAlias", typeof(IntStringKey));
            public static MethodInfo PhpArray_GetItemValue = typeof(PhpArray).GetMethod("GetItemValue", typeof(IntStringKey));
            public static MethodInfo PhpArray_RemoveKey = typeof(PhpArray).GetMethod("RemoveKey", typeof(IntStringKey));
        }

        public static class PhpString
        {
            public static ConstructorInfo ctor_String = typeof(Core.PhpString).GetCtor(Types.String);
            public static ConstructorInfo ctor_ByteArray = typeof(Core.PhpString).GetCtor(typeof(byte[]));
        }

        public static class Object
        {
            /// <summary><see cref="System.Object"/>.</summary>
            public static new MethodInfo ToString = typeof(object).GetMethod("ToString", Types.Empty);
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

        /// <summary>
        /// Gets .ctor in given type.
        /// </summary>
        public static ConstructorInfo GetCtor(this Type type, params Type[] ptypes)
        {
            var ctors = type.GetTypeInfo().DeclaredConstructors;
            foreach (var ctor in ctors)
            {
                var ps = ctor.GetParameters();
                if (ps.Length == ptypes.Length)
                {
                    if (Enumerable.SequenceEqual(ptypes, ps.Select(p => p.ParameterType)))
                    {
                        return ctor;
                    }
                }
            }

            throw new ArgumentException();
        }
    }
}
