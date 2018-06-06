using System;
using System.Diagnostics;
using System.Reflection;

namespace Pchp.Core.Dynamic
{
    internal static class Cache
    {
        public static class Types
        {
            public static Type[] Empty => Array.Empty<Type>();
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
            public static MethodInfo SetValue_PhpValueRef_PhpValue = typeof(Core.Operators).GetMethod("SetValue", Types.PhpValue[0].MakeByRefType(), Types.PhpValue[0]);
            public static MethodInfo IsSet_PhpValue = typeof(Core.Operators).GetMethod("IsSet", Types.PhpValue[0]);

            public static MethodInfo ToString_Double_Context = typeof(Core.Convert).GetMethod("ToString", typeof(double), typeof(Context));
            public static MethodInfo ToLong_String = typeof(Core.Convert).GetMethod("StringToLongInteger", typeof(string));
            public static MethodInfo ToDouble_String = typeof(Core.Convert).GetMethod("StringToDouble", typeof(string));
            public static MethodInfo ToPhpString_PhpValue_Context = typeof(Core.Convert).GetMethod("ToPhpString", Types.PhpValue[0], typeof(Context));
            public static MethodInfo ToPhpNumber_String = typeof(Core.Convert).GetMethod("ToNumber", Types.String[0]);
            public static MethodInfo ToBoolean_Object = typeof(Core.Convert).GetMethod("ToBoolean", Types.Object[0]);

            public static MethodInfo Object_EnsureArray = typeof(Core.Operators).GetMethod("EnsureArray", Types.Object);

            public static MethodInfo PhpAlias_EnsureObject = Types.PhpAlias[0].GetMethod("EnsureObject", Types.Empty);
            public static MethodInfo PhpAlias_EnsureArray = Types.PhpAlias[0].GetMethod("EnsureArray", Types.Empty);

            public static MethodInfo PhpValue_EnsureObject = Types.PhpValue[0].GetMethod("EnsureObject", Types.Empty);
            public static MethodInfo PhpValue_EnsureArray = Types.PhpValue[0].GetMethod("EnsureArray", Types.Empty);
            public static MethodInfo PhpValue_EnsureAlias = Types.PhpValue[0].GetMethod("EnsureAlias", Types.Empty);
            public static MethodInfo PhpValue_ToClass = Types.PhpValue[0].GetMethod("ToClass", Types.Empty);
            public static MethodInfo PhpValue_ToArray = Types.PhpValue[0].GetMethod("ToArray", Types.Empty);
            public static MethodInfo PhpValue_AsCallable_RuntimeTypeHandle = Types.PhpValue[0].GetMethod("AsCallable", typeof(RuntimeTypeHandle));
            public static MethodInfo PhpValue_AsObject = Types.PhpValue[0].GetMethod("AsObject", Types.Empty);
            public static MethodInfo PhpValue_ToString_Context = Types.PhpValue[0].GetMethod("ToString", typeof(Context));
            public static MethodInfo PhpValue_ToIntStringKey = Types.PhpValue[0].GetMethod("ToIntStringKey");
            public static MethodInfo PhpValue_GetValue = Types.PhpValue[0].GetMethod("GetValue");
            public static MethodInfo PhpValue_DeepCopy = Types.PhpValue[0].GetMethod("DeepCopy");

            public static MethodInfo PhpNumber_ToString_Context = typeof(PhpNumber).GetMethod("ToString", typeof(Context));

            public static MethodInfo PhpArray_ToClass = typeof(PhpArray).GetMethod("ToClass", Types.Empty);
            public static MethodInfo PhpArray_SetItemAlias = typeof(PhpArray).GetMethod("SetItemAlias", typeof(Core.IntStringKey), Types.PhpAlias[0]);
            public static MethodInfo PhpArray_SetItemValue = typeof(PhpArray).GetMethod("SetItemValue", typeof(Core.IntStringKey), Types.PhpValue[0]);
            public static MethodInfo PhpArray_EnsureItemObject = typeof(PhpArray).GetMethod("EnsureItemObject", typeof(Core.IntStringKey));
            public static MethodInfo PhpArray_EnsureItemArray = typeof(PhpArray).GetMethod("EnsureItemArray", typeof(Core.IntStringKey));
            public static MethodInfo PhpArray_EnsureItemAlias = typeof(PhpArray).GetMethod("EnsureItemAlias", typeof(Core.IntStringKey));
            public static MethodInfo PhpArray_GetItemValue = typeof(PhpArray).GetMethod("GetItemValue", typeof(Core.IntStringKey));
            public static MethodInfo PhpArray_RemoveKey = typeof(PhpArray).GetMethod("RemoveKey", typeof(Core.IntStringKey));
            public static MethodInfo PhpArray_TryGetValue = typeof(PhpArray).GetMethod("TryGetValue", typeof(Core.IntStringKey), Types.PhpValue[0].MakeByRefType());
            public static MethodInfo PhpArray_ContainsKey = typeof(PhpArray).GetMethod("ContainsKey", typeof(Core.IntStringKey));

            public static MethodInfo RuntimeTypeHandle_Equals_RuntimeTypeHandle = typeof(RuntimeTypeHandle).GetMethod("Equals", typeof(RuntimeTypeHandle));
        }

        public static class Properties
        {
            public static readonly PropertyInfo PhpValue_Object = Types.PhpValue[0].GetProperty("Object");
            public static readonly FieldInfo PhpValue_Void = Types.PhpValue[0].GetField("Void");
            public static readonly FieldInfo PhpValue_Null = Types.PhpValue[0].GetField("Null");
            public static readonly PropertyInfo PhpValue_False = Types.PhpValue[0].GetProperty("False");
            public static readonly PropertyInfo PhpValue_True = Types.PhpValue[0].GetProperty("True");
            public static readonly FieldInfo PhpNumber_Default = Types.PhpNumber[0].GetField("Default");
        }

        public static class PhpString
        {
            public static ConstructorInfo ctor_String = typeof(Core.PhpString).GetCtor(Types.String);
            public static ConstructorInfo ctor_ByteArray = typeof(Core.PhpString).GetCtor(typeof(byte[]));
            public static readonly MethodInfo ToString_Context = typeof(Core.PhpString).GetMethod("ToString", typeof(Context));
            public static readonly MethodInfo ToBytes_Context = typeof(Core.PhpString).GetMethod("ToBytes", typeof(Context));
            public static readonly PropertyInfo IsDefault = Types.PhpString[0].GetProperty("IsDefault");
        }

        public static class IntStringKey
        {
            public static ConstructorInfo ctor_String = typeof(Core.IntStringKey).GetCtor(Types.String);
            public static ConstructorInfo ctor_Int = typeof(Core.IntStringKey).GetCtor(Types.Int);
        }

        public static class PhpAlias
        {
            public static readonly FieldInfo Value = Types.PhpAlias[0].GetField("Value");
            public static ConstructorInfo ctor_PhpValue_int => Types.PhpAlias[0].GetCtor(Types.PhpValue[0], Types.Int[0]);
        }

        public static class RecursionCheckToken
        {
            public static ConstructorInfo ctor_ctx_object_int = typeof(Context.RecursionCheckToken).GetCtor(typeof(Context), Types.Object[0], Types.Int[0]);
            public static MethodInfo Dispose = typeof(Context.RecursionCheckToken).GetMethod("Dispose");
            public static readonly PropertyInfo IsInRecursion = typeof(Context.RecursionCheckToken).GetProperty("IsInRecursion");
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
            var result = type.GetRuntimeMethod(name, ptypes) ?? type.GetMethod(name, ptypes);
            
            Debug.Assert(result != null);

            return result;
        }

        /// <summary>
        /// Gets .ctor in given type.
        /// </summary>
        public static ConstructorInfo GetCtor(this Type type, params Type[] ptypes)
        {
            return type.GetConstructor(ptypes) ?? throw new ArgumentException();
        }
    }
}