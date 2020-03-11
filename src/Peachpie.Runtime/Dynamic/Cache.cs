using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core.Reflection;

namespace Pchp.Core.Dynamic
{
    internal static class Cache
    {
        public static class Types
        {
            public static Type[] Empty => Array.Empty<Type>();
            public static Type[] Int = new Type[] { typeof(int) };
            public static Type[] Long = new Type[] { typeof(long) };
            public static Type[] UInt64 = new Type[] { typeof(ulong) };
            public static Type[] Double = new Type[] { typeof(double) };
            public static Type[] String = new Type[] { typeof(string) };
            public static Type[] Bool = new Type[] { typeof(bool) };
            public static Type[] Object = new Type[] { typeof(object) };
            public static Type[] PhpString = new Type[] { typeof(Core.PhpString) };
            public static Type PhpValue = typeof(Core.PhpValue);
            public static Type[] PhpAlias = new Type[] { typeof(Core.PhpAlias) };
            public static Type[] PhpNumber = new Type[] { typeof(Core.PhpNumber) };
            public static Type[] PhpArray = new Type[] { typeof(Core.PhpArray) };

            public static Type IndirectLocal = typeof(Core.IndirectLocal);
        }

        public static class Operators
        {
            /// <summary><see cref="Core.Operators.SetValue(ref PhpValue, PhpValue)"/>.</summary>
            public static MethodInfo SetValue_PhpValueRef_PhpValue = typeof(Core.Operators).GetMethod("SetValue", Types.PhpValue.MakeByRefType(), Types.PhpValue);
            public static MethodInfo IsSet_PhpValue = new Func<PhpValue, bool>(Core.Operators.IsSet).Method;

            public static MethodInfo ToString_Double_Context = new Func<double, Context, string>(Core.Convert.ToString).Method;
            public static MethodInfo ToLongOrThrow_String = new Func<string, long>(Core.StrictConvert.ToLong).Method;
            public static MethodInfo ToDouble_String = new Func<string, double>(Core.Convert.StringToDouble).Method;
            public static MethodInfo ToPhpString_PhpValue_Context = new Func<PhpValue, Context, Core.PhpString>(Core.Convert.ToPhpString).Method;
            public static MethodInfo ToPhpNumber_String = new Func<string, PhpNumber>(Core.Convert.ToNumber).Method;
            public static MethodInfo ToBoolean_Object = new Func<object, bool>(Core.Convert.ToBoolean).Method;
            public static MethodInfo ToDateTime_PhpValue = new Func<PhpValue, DateTime>(Core.Convert.ToDateTime).Method;

            public static MethodInfo Object_EnsureArray = typeof(Core.Operators).GetMethod("EnsureArray", Types.Object);

            public static MethodInfo PhpAlias_EnsureObject = Types.PhpAlias[0].GetMethod("EnsureObject", Types.Empty);
            public static MethodInfo PhpAlias_EnsureArray = Types.PhpAlias[0].GetMethod("EnsureArray", Types.Empty);

            public static MethodInfo EnsureObject_PhpValueRef = Types.PhpValue.GetMethod("EnsureObject", Types.PhpValue.MakeByRefType());
            public static MethodInfo EnsureArray_PhpValueRef = Types.PhpValue.GetMethod("EnsureArray", Types.PhpValue.MakeByRefType());
            public static MethodInfo EnsureAlias_PhpValueRef = Types.PhpValue.GetMethod("EnsureAlias", Types.PhpValue.MakeByRefType());
            public static MethodInfo PhpValue_ToLongOrThrow = new Func<PhpValue, long>(Core.StrictConvert.ToLong).Method;
            public static MethodInfo PhpValue_ToClass = Types.PhpValue.GetMethod("ToClass", Types.Empty);
            public static MethodInfo PhpValue_ToArray = Types.PhpValue.GetMethod("ToArray", Types.Empty);
            /// <summary>Get the underlaying PhpArray, or <c>null</c>. Throws in case of a scalar or object.</summary>
            public static MethodInfo PhpValue_ToArrayOrThrow = new Func<PhpValue, PhpArray>(StrictConvert.ToArray).Method;
            public static MethodInfo PhpValue_AsCallable_RuntimeTypeHandle_Object = Types.PhpValue.GetMethod("AsCallable", typeof(RuntimeTypeHandle), typeof(object));
            public static MethodInfo PhpValue_AsObject = Types.PhpValue.GetMethod("AsObject", Types.Empty);
            public static MethodInfo PhpValue_AsString_Context = Types.PhpValue.GetMethod("AsString", typeof(Context));
            public static MethodInfo PhpValue_ToIntStringKey = Types.PhpValue.GetMethod("ToIntStringKey");
            public static MethodInfo PhpValue_GetValue = Types.PhpValue.GetMethod("GetValue");
            public static MethodInfo PhpValue_DeepCopy = Types.PhpValue.GetMethod("DeepCopy");

            public static MethodInfo PhpNumber_ToString_Context = Types.PhpNumber[0].GetMethod("ToString", typeof(Context));
            public static MethodInfo PhpNumber_ToClass = Types.PhpNumber[0].GetMethod("ToObject", Types.Empty);

            public static MethodInfo PhpArray_ToClass = typeof(PhpArray).GetMethod("ToObject", Types.Empty);
            public static MethodInfo PhpArray_SetItemAlias = typeof(PhpArray).GetMethod("SetItemAlias", typeof(Core.IntStringKey), Types.PhpAlias[0]);
            public static MethodInfo PhpArray_SetItemValue = typeof(PhpArray).GetMethod("SetItemValue", typeof(Core.IntStringKey), Types.PhpValue);
            public static MethodInfo PhpArray_EnsureItemObject = typeof(PhpArray).GetMethod("EnsureItemObject", typeof(Core.IntStringKey));
            public static MethodInfo PhpArray_EnsureItemArray = typeof(PhpArray).GetMethod("EnsureItemArray", typeof(Core.IntStringKey));
            public static MethodInfo PhpArray_EnsureItemAlias = typeof(PhpArray).GetMethod("EnsureItemAlias", typeof(Core.IntStringKey));
            public static MethodInfo PhpArray_GetItemValue = typeof(PhpArray).GetMethod("GetItemValue", typeof(Core.IntStringKey));
            public static MethodInfo PhpArray_Remove = typeof(PhpHashtable).GetMethod("Remove", typeof(Core.IntStringKey)); // PhpHashtable.Remove(IntStringKey) returns bool
            public static MethodInfo PhpArray_TryGetValue = typeof(PhpArray).GetMethod("TryGetValue", typeof(Core.IntStringKey), Types.PhpValue.MakeByRefType());
            public static MethodInfo PhpArray_ContainsKey = typeof(PhpArray).GetMethod("ContainsKey", typeof(Core.IntStringKey));

            public static MethodInfo ToBoolean_PhpArray = typeof(PhpArray).GetOpExplicit(typeof(bool));
            public static MethodInfo ToBoolean_PhpValue = typeof(PhpValue).GetOpImplicit(typeof(bool));
            public static MethodInfo ToBoolean_PhpNumber = typeof(PhpNumber).GetOpImplicit(typeof(bool));
            public static MethodInfo ToBoolean_String = typeof(Convert).GetMethod("ToBoolean", Types.String);

            public static MethodInfo ToDouble_PhpArray = typeof(PhpArray).GetOpExplicit(typeof(double));
            public static MethodInfo ToDouble_PhpNumber = typeof(PhpNumber).GetOpImplicit(typeof(double));
            public static MethodInfo ToDouble_PhpValue = typeof(PhpValue).GetOpExplicit(typeof(double));

            public static MethodInfo ToLong_PhpNumber = typeof(PhpNumber).GetOpImplicit(typeof(long));
            public static MethodInfo ToLong_Boolean = typeof(System.Convert).GetMethod("ToInt64", Types.Bool);

            public static MethodInfo RuntimeTypeHandle_Equals_RuntimeTypeHandle = typeof(RuntimeTypeHandle).GetMethod("Equals", typeof(RuntimeTypeHandle));

            public static readonly Func<Context, PhpTypeInfo, object, string, PhpValue> RuntimePropertyGetValue = new Func<Context, PhpTypeInfo, object, string, PhpValue>(Core.Operators.RuntimePropertyGetValue);

            public static MethodInfo Or_ConversionCost_ConversionCost = typeof(CostOf).GetMethod("Or", typeof(ConversionCost), typeof(ConversionCost));
        }

        public static class Properties
        {
            public static readonly PropertyInfo PhpValue_Object = Types.PhpValue.GetProperty("Object");
            public static readonly PropertyInfo PhpValue_IsAlias = Types.PhpValue.GetProperty("IsAlias");
            public static readonly FieldInfo PhpValue_Null = Types.PhpValue.GetField("Null");
            public static readonly FieldInfo PhpValue_False = Types.PhpValue.GetField("False");
            public static readonly FieldInfo PhpValue_True = Types.PhpValue.GetField("True");
            public static readonly FieldInfo PhpNumber_Default = Types.PhpNumber[0].GetField("Default");
            public static readonly PropertyInfo PhpValue_IsNull = Types.PhpValue.GetProperty("IsNull");
            public static readonly PropertyInfo PhpValue_IsFalse = Types.PhpValue.GetProperty("IsFalse");
        }

        public static class PhpString
        {
            public static ConstructorInfo ctor_String = Types.PhpString[0].GetCtor(Types.String);
            public static ConstructorInfo ctor_ByteArray = Types.PhpString[0].GetCtor(typeof(byte[]));
            public static readonly MethodInfo ToString_Context = Types.PhpString[0].GetMethod("ToString", typeof(Context));
            public static readonly MethodInfo ToBytes_Context = Types.PhpString[0].GetMethod("ToBytes", typeof(Context));
            public static readonly PropertyInfo IsDefault = Types.PhpString[0].GetProperty("IsDefault");
            public static MethodInfo ToBoolean = Types.PhpString[0].GetMethod("ToBoolean");
            public static MethodInfo ToDouble = Types.PhpString[0].GetMethod("ToDouble");
            public static MethodInfo ToLongOrThrow = new Func<Core.PhpString, long>(Core.StrictConvert.ToLong).Method;
        }

        public static class IntStringKey
        {
            public static ConstructorInfo ctor_String = typeof(Core.IntStringKey).GetCtor(Types.String);
            public static ConstructorInfo ctor_Long = typeof(Core.IntStringKey).GetCtor(Types.Long);
        }

        public static class PhpAlias
        {
            public static readonly FieldInfo Value = Types.PhpAlias[0].GetField("Value");
            public static ConstructorInfo ctor_PhpValue_int => Types.PhpAlias[0].GetCtor(Types.PhpValue, Types.Int[0]);
        }

        public static class IndirectLocal
        {
            public static readonly MethodInfo GetValue = Types.IndirectLocal.GetMethod("GetValue");
            //public static readonly PropertyInfo ValueRef = Types.IndirectLocal.GetProperty("ValueRef", BindingFlags.NonPublic | BindingFlags.Instance);
            public static readonly MethodInfo EnsureAlias = Types.IndirectLocal.GetMethod("EnsureAlias");
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
            var result = type.GetRuntimeMethod(name, ptypes);
            if (result != null)
            {
                return result;
            }
            
            foreach (var m in type.GetTypeInfo().GetDeclaredMethods(name))  // non public methods
                {
                    if (ParamsMatch(m.GetParameters(), ptypes))
                        return m;
                }

            throw new InvalidOperationException($"{type.Name}.{name}({string.Join<Type>(", ", ptypes)}) was not resolved.");
        }

        static MethodInfo GetOpImplicit(this Type type, Type resultType) =>
            GetOpMethod(type, "op_Implicit", resultType);

        static MethodInfo GetOpExplicit(this Type type, Type resultType) =>
            GetOpMethod(type, "op_Explicit", resultType);

        static MethodInfo GetOpMethod(Type type, string opname, Type resultType)
        {
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == opname && methods[i].ReturnType == resultType && methods[i].GetParameters()[0].ParameterType == type)
                {
                    return methods[i];
                }
            }

            throw new InvalidOperationException($"{resultType.Name} {type.Name}.{opname} was not resolved.");
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
            var ctors = type.GetConstructors();//.GetTypeInfo().DeclaredConstructors;
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

            throw new InvalidOperationException($"{type.Name}..ctor({string.Join<Type>(", ", ptypes)}) was not resolved.");
        }
    }
}
