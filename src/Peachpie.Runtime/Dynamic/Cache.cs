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
            public static MethodInfo ToPhpString_PhpValue_Context = typeof(Core.Convert).GetMethod("ToPhpString", typeof(PhpValue), typeof(Context));

            public static MethodInfo PhpAlias_EnsureObject = typeof(Core.PhpAlias).GetMethod("EnsureObject", Types.Empty);
            public static MethodInfo PhpAlias_EnsureArray = typeof(Core.PhpAlias).GetMethod("EnsureArray", Types.Empty);

            public static MethodInfo PhpValue_EnsureObject = typeof(PhpValue).GetMethod("EnsureObject", Types.Empty);
            public static MethodInfo PhpValue_EnsureArray = typeof(PhpValue).GetMethod("EnsureArray", Types.Empty);
            public static MethodInfo PhpValue_EnsureAlias = typeof(PhpValue).GetMethod("EnsureAlias", Types.Empty);
            public static MethodInfo PhpValue_ToClass = typeof(PhpValue).GetMethod("ToClass", Types.Empty);
            public static MethodInfo PhpValue_ToArray = typeof(PhpValue).GetMethod("ToArray", Types.Empty);
            public static MethodInfo PhpValue_AsCallable = typeof(PhpValue).GetMethod("AsCallable", Types.Empty);
            public static MethodInfo PhpValue_AsObject = typeof(PhpValue).GetMethod("AsObject", Types.Empty);
            public static MethodInfo PhpValue_ToString_Context = typeof(PhpValue).GetMethod("ToString", typeof(Context));
            public static MethodInfo PhpValue_ToIntStringKey = typeof(PhpValue).GetMethod("ToIntStringKey");

            public static MethodInfo PhpNumber_ToString_Context = typeof(PhpNumber).GetMethod("ToString", typeof(Context));

            public static MethodInfo PhpArray_ToClass = typeof(PhpArray).GetMethod("ToClass", Types.Empty);
            public static MethodInfo PhpArray_SetItemAlias = typeof(PhpArray).GetMethod("SetItemAlias", typeof(Core.IntStringKey), typeof(Core.PhpAlias));
            public static MethodInfo PhpArray_SetItemValue = typeof(PhpArray).GetMethod("SetItemValue", typeof(Core.IntStringKey), typeof(Core.PhpValue));
            public static MethodInfo PhpArray_EnsureItemObject = typeof(PhpArray).GetMethod("EnsureItemObject", typeof(Core.IntStringKey));
            public static MethodInfo PhpArray_EnsureItemArray = typeof(PhpArray).GetMethod("EnsureItemArray", typeof(Core.IntStringKey));
            public static MethodInfo PhpArray_EnsureItemAlias = typeof(PhpArray).GetMethod("EnsureItemAlias", typeof(Core.IntStringKey));
            public static MethodInfo PhpArray_GetItemValue = typeof(PhpArray).GetMethod("GetItemValue", typeof(Core.IntStringKey));
            public static MethodInfo PhpArray_RemoveKey = typeof(PhpArray).GetMethod("RemoveKey", typeof(Core.IntStringKey));
            public static MethodInfo PhpArray_TryGetValue = typeof(PhpArray).GetMethod("TryGetValue", typeof(Core.IntStringKey), typeof(Core.PhpValue).MakeByRefType());
            public static MethodInfo PhpArray_ContainsKey = typeof(PhpArray).GetMethod("ContainsKey", typeof(Core.IntStringKey));
        }

        public static class Properties
        {
            public static readonly FieldInfo PhpValue_Void = Types.PhpValue[0].GetTypeInfo().GetDeclaredField("Void");
            public static readonly FieldInfo PhpValue_Null = Types.PhpValue[0].GetTypeInfo().GetDeclaredField("Null");
            public static readonly PropertyInfo PhpValue_False = Types.PhpValue[0].GetTypeInfo().GetDeclaredProperty("False");
            public static readonly FieldInfo PhpNumber_Default = Types.PhpNumber[0].GetTypeInfo().GetDeclaredField("Default");
        }

        public static class PhpString
        {
            public static ConstructorInfo ctor_String = typeof(Core.PhpString).GetCtor(Types.String);
            public static ConstructorInfo ctor_ByteArray = typeof(Core.PhpString).GetCtor(typeof(byte[]));
            public static readonly MethodInfo ToString_Context = typeof(Core.PhpString).GetMethod("ToString", typeof(Context));
            public static readonly MethodInfo ToBytes_Context = typeof(Core.PhpString).GetMethod("ToBytes", typeof(Context));
        }

        public static class IntStringKey
        {
            public static ConstructorInfo ctor_String = typeof(Core.IntStringKey).GetCtor(Types.String);
            public static ConstructorInfo ctor_Int = typeof(Core.IntStringKey).GetCtor(Types.Int);
        }

        public static class PhpAlias
        {
            public static ConstructorInfo ctor_PhpValue_int => Types.PhpAlias[0].GetCtor(Types.PhpValue[0], Types.Int[0]);
        }

        public static class RecursionCheckToken
        {
            public static ConstructorInfo ctor_ctx_object_int = typeof(Context.RecursionCheckToken).GetCtor(typeof(Context), Types.Object[0], Types.Int[0]);
            public static MethodInfo Dispose = typeof(Context.RecursionCheckToken).GetMethod("Dispose");
            public static readonly PropertyInfo IsInRecursion = typeof(Context.RecursionCheckToken).GetTypeInfo().GetDeclaredProperty("IsInRecursion");
        }

        public static class Object
        {
            /// <summary><see cref="System.Object"/>.</summary>
            public static new MethodInfo ToString = typeof(object).GetMethod("ToString", Types.Empty);
            public static readonly MethodInfo ToString_Bool = typeof(Core.Convert).GetMethod("ToString", Types.Bool);
            public static readonly MethodInfo ToString_Double_Context = typeof(Core.Convert).GetMethod("ToString", Types.Double[0], typeof(Context));
        }

        /// <summary>
        /// Gets method info in given type.
        /// </summary>
        public static MethodInfo GetMethod(this Type type, string name, params Type[] ptypes)
        {
            var result = type.GetRuntimeMethod(name, ptypes);
            if (result == null)
            {
                foreach (var m in type.GetTypeInfo().GetDeclaredMethods(name))  // non public methods
                {
                    if (ParamsMatch(m.GetParameters(), ptypes))
                        return m;
                }
            }

            Debug.Assert(result != null);
            return result;
        }

        static bool ParamsMatch(ParameterInfo[] ps, Type[] ptypes)
        {
            if (ps.Length != ptypes.Length) return false;
            for (int i = 0; i < ps.Length; i++) if (ps[i].ParameterType != ptypes[i]) return false;
            return true;
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
