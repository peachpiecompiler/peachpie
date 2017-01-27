using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Pchp.Core.Reflection
{
    public static class TypeMembersUtils
    {
        /// <summary>
        /// Gets runtime instance fields of <see cref="stdClass"/>.
        /// </summary>
        public static PhpArray GetRuntimeFields(this stdClass obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            return obj.__peach__runtimeFields;
        }

        /// <summary>
        /// Gets runtime instance fields of given object.
        /// </summary>
        /// <param name="tinfo">Type of <paramref name="instance"/>.</param>
        /// <param name="instance">Instance of type <paramref name="tinfo"/>.</param>
        /// <returns>Array representing internal runtime fields or <c>null</c> if no values are available.</returns>
        public static PhpArray GetRuntimeFields(this PhpTypeInfo tinfo, object instance)
        {
            Debug.Assert(instance != null);
            Debug.Assert(instance.GetType() == tinfo.Type);

            // PhpArray __runtime_fields
            if (tinfo.RuntimeFieldsHolder != null)
            {
                // all runtime fields are considered public,
                // no visibility check needed

                return (PhpArray)tinfo.RuntimeFieldsHolder.GetValue(instance);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Enumerates visible instance fields of given object.
        /// </summary>
        /// <param name="instance">Object which fields will be enumerated.</param>
        /// <param name="caller">Current class context for field visibility check.</param>
        /// <returns>Enumeration of fields and their values, including runtime fields.</returns>
        public static IEnumerable<KeyValuePair<IntStringKey, PhpValue>> EnumerateInstanceFields(object instance, RuntimeTypeHandle caller = default(RuntimeTypeHandle))
        {
            Debug.Assert(instance != null);

            // PhpTypeInfo
            var tinfo = PhpTypeInfoExtension.GetPhpTypeInfo(instance.GetType());

            // iterate through type and its base types
            for (var t = tinfo; t != null; t = t.BaseType)
            {
                // iterate through instance fields
                foreach (var f in t.DeclaredFields.InstanceFields)
                {
                    // perform visibility check
                    if (IsVisible(f, caller))
                    {
                        yield return new KeyValuePair<IntStringKey, PhpValue>(
                            new IntStringKey(f.Name),
                            PhpValue.FromClr(f.GetValue(instance)));
                    }
                }

                // TODO: CLR properties
            }

            // PhpArray __runtime_fields
            var runtime_fields = tinfo.GetRuntimeFields(instance);
            if (runtime_fields != null && runtime_fields.Count != 0)
            {
                // all runtime fields are considered public,
                // no visibility check needed

                var enumerator = runtime_fields.GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
            }
        }

        /// <summary>
        /// Casts object to given PHP array.
        /// </summary>
        /// <param name="instance">Object instance, cannot be <c>null</c>.</param>
        /// <param name="arr">Array to be filled with object instance properties.</param>
        public static void InstanceFieldsToPhpArray(object instance, PhpArray arr)
        {
            Debug.Assert(instance != null);
            Debug.Assert(arr != null);

            // PhpTypeInfo
            var tinfo = PhpTypeInfoExtension.GetPhpTypeInfo(instance.GetType());

            // iterate through type and its base types
            for (var t = tinfo; t != null; t = t.BaseType)
            {
                // iterate through instance fields
                foreach (var f in t.DeclaredFields.InstanceFields)
                {
                    arr[FieldAsArrayKey(f, t)] = PhpValue.FromClr(f.GetValue(instance)).DeepCopy();
                }

                // TODO: CLR properties
            }

            // PhpArray __runtime_fields
            var runtime_fields = tinfo.GetRuntimeFields(instance);
            if (runtime_fields != null && runtime_fields.Count != 0)
            {
                // all runtime fields are considered public
                var enumerator = runtime_fields.GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    arr[enumerator.CurrentKey] = enumerator.CurrentValue.DeepCopy();
                }
            }
        }

        /// <summary>
        /// Gets field name to be used as array key when casting object to array.
        /// </summary>
        static string FieldAsArrayKey(FieldInfo f, PhpTypeInfo declaringType)
        {
            Debug.Assert(f != null);
            Debug.Assert(declaringType != null);

            if (f.IsPublic) return f.Name;
            if (f.IsFamily) return " * " + f.Name;
            if (f.IsPrivate) return " " + declaringType.Name + " " + f.Name;

            Debug.Fail($"Unexpected field attributes {f.Attributes}");

            return f.Name;
        }

        static bool IsVisible(this FieldInfo f, RuntimeTypeHandle caller)
        {
            return
                (f.IsPublic) ||
                (f.IsPrivate && f.DeclaringType.TypeHandle.Equals(caller)) ||
                (f.IsFamily && IsVisible(f.DeclaringType, caller));
        }

        public static bool IsVisible(this FieldInfo f, Type caller)
        {
            return
                (f.IsPublic) ||
                (f.IsPrivate && f.DeclaringType.Equals(caller)) ||
                (f.IsFamily && IsVisible(f.DeclaringType, caller));
        }

        static bool IsVisible(Type memberctx, RuntimeTypeHandle caller)
        {
            Debug.Assert(memberctx != null);

            if (caller.Equals(default(RuntimeTypeHandle)))
            {
                return false;   // global context
            }
            else
            {
                return IsVisible(memberctx, Type.GetTypeFromHandle(caller));
            }
        }

        static bool IsVisible(Type memberctx, Type caller)
        {
            Debug.Assert(memberctx != null);

            return caller != null && memberctx.GetTypeInfo().IsAssignableFrom(caller);
        }
    }
}
