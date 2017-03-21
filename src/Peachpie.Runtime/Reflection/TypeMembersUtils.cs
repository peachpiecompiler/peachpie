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
            Debug.Assert(instance.GetType() == tinfo.Type.AsType());

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
        /// Gets array of runtime fields in given type.
        /// If the array is not instantiated yet, new instance is created and runtime fields initialized.
        /// If the type does not support runtime fields, <c>null</c> is returned.
        /// </summary>
        public static PhpArray EnsureRuntimeFields(this PhpTypeInfo tinfo, object instance)
        {
            if (tinfo.RuntimeFieldsHolder != null)
            {
                var array = (PhpArray)tinfo.RuntimeFieldsHolder.GetValue(instance);
                if (array == null)
                {
                    tinfo.RuntimeFieldsHolder.SetValue(instance, (array = new PhpArray()));
                }

                return array;
            }

            return null;
        }

        /// <summary>
        /// Enumerates visible instance fields of given object.
        /// </summary>
        /// <param name="instance">Object which fields will be enumerated.</param>
        /// <param name="caller">Current class context for field visibility check.</param>
        /// <returns>Enumeration of fields and their values, including runtime fields.</returns>
        public static IEnumerable<KeyValuePair<IntStringKey, PhpValue>> EnumerateVisibleInstanceFields(object instance, RuntimeTypeHandle caller = default(RuntimeTypeHandle))
        {
            return EnumerateInstanceFields(instance,
                (f, d) => new IntStringKey(f.Name),
                (k) => k,
                (f) => IsVisible(f, caller));
        }

        /// <summary>
        /// Enumerates visible instance fields of given object, transforms field names according to <c>print_r</c> notation.
        /// </summary>
        /// <param name="instance">Object which fields will be enumerated.</param>
        /// <returns>Enumeration of fields and their values, including runtime fields.</returns>
        public static IEnumerable<KeyValuePair<string, PhpValue>> EnumerateInstanceFieldsForPrint(object instance)
        {
            return EnumerateInstanceFields(instance,
                (f, d) => FormatPropertyNameForPrint(f, d),
                (k) => k.ToString(),
                (f) => (f.Attributes & (FieldAttributes.Assembly)) != FieldAttributes.Assembly); // ignore "internal" fields
        }

        static string FormatPropertyNameForPrint(FieldInfo f, PhpTypeInfo declarer)
        {
            if (f.IsPublic) return f.Name;
            if (f.IsPrivate) return $"{f.Name}:{declarer.Name}:private";
            return $"{f.Name}:protected";
        }

        /// <summary>
        /// Enumerates instance fields of given object.
        /// </summary>
        /// <param name="instance">Object which fields will be enumerated.</param>
        /// <param name="keyFormatter">Function converting field to a <typeparamref name="TKey"/>.</param>
        /// <param name="keyFormatter2">Function converting </param>
        /// <param name="predicate">Optional. Predicate filtering instance fields.</param>
        /// <param name="ignoreRuntimeFields">Whether to ignore listing runtime fields.</param>
        /// <returns>Enumeration of fields and their values, including runtime fields.</returns>
        /// <typeparam name="TKey">Enumerated pairs key. Usually <see cref="IntStringKey"/>.</typeparam>
        public static IEnumerable<KeyValuePair<TKey, PhpValue>> EnumerateInstanceFields<TKey>(object instance, Func<FieldInfo, PhpTypeInfo, TKey> keyFormatter, Func<IntStringKey, TKey> keyFormatter2, Func<FieldInfo, bool> predicate = null, bool ignoreRuntimeFields = false)
        {
            Debug.Assert(instance != null);

            // PhpTypeInfo
            var tinfo = instance.GetPhpTypeInfo();

            // iterate through type and its base types
            for (var t = tinfo; t != null; t = t.BaseType)
            {
                // iterate through instance fields
                foreach (var f in t.DeclaredFields.InstanceFields)
                {
                    // perform visibility check
                    if (predicate == null || predicate(f))
                    {
                        yield return new KeyValuePair<TKey, PhpValue>(
                            keyFormatter(f, t),
                            PhpValue.FromClr(f.GetValue(instance)));
                    }
                }

                // TODO: CLR properties
            }

            // PhpArray __runtime_fields
            if (ignoreRuntimeFields == false)
            {
                Debug.Assert(keyFormatter2 != null);

                var runtime_fields = tinfo.GetRuntimeFields(instance);
                if (runtime_fields != null && runtime_fields.Count != 0)
                {
                    // all runtime fields are considered public,
                    // no visibility check needed

                    var enumerator = runtime_fields.GetFastEnumerator();
                    while (enumerator.MoveNext())
                    {
                        yield return new KeyValuePair<TKey, PhpValue>(keyFormatter2(enumerator.CurrentKey), enumerator.CurrentValue);
                    }
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

        /// <summary>
        /// Builds delegate that creates uninitialized class instance for purposes of deserialization.
        /// </summary>
        internal static Func<Context, object> BuildCreateEmptyObjectFunc(PhpTypeInfo tinfo)
        {
            Debug.Assert(tinfo != null);

            var ctors = tinfo.Type.DeclaredConstructors;
            Func<Context, object> candidate = null;

            foreach (var c in ctors)
            {
                if (c.IsStatic) continue;

                if (c.IsPhpFieldsOnlyCtor())
                {
                    return (_ctx) => c.Invoke(new object[] { _ctx });
                }

                var ps = c.GetParameters();
                if (ps.Length == 0) candidate = (_ctx) => c.Invoke(Array.Empty<object>());
                if (ps.Length == 1 && ps[0].ParameterType == typeof(Context)) candidate = (_ctx) => c.Invoke(new object[] { _ctx });
            }

            return candidate
                ?? ((_ctx) =>
                {
                    Debug.Fail(string.Format(Resources.ErrResources.construct_not_supported, tinfo.Name));
                    return null;
                });
        }

        /// <summary>
		/// Gets the number of instance properties contained in this <see cref="object"/>.
		/// </summary>
		public static int FieldsCount(object instance)
        {
            Debug.Assert(instance != null);

            int count = 0;

            // PhpTypeInfo
            var tinfo = instance.GetPhpTypeInfo();

            // iterate through type and its base types
            for (var t = tinfo; t != null; t = t.BaseType)
            {
                // iterate through instance fields
                count += t.DeclaredFields.InstanceFields.Count();

                // TODO: CLR properties
            }

            // PhpArray __runtime_fields
            var runtime_fields = tinfo.GetRuntimeFields(instance);
            if (runtime_fields != null)
            {
                count += runtime_fields.Count;
            }

            //
            return count;
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

        /// <summary>
        /// Selects only candidates visible from the current class context.
        /// </summary>
        public static bool IsVisible(this MethodBase m, Type classCtx)
        {
            if (m.IsPrivate)
            {
                return m.DeclaringType == classCtx;
            }

            if (m.IsFamily)
            {
                if (classCtx == null)
                {
                    return false;
                }

                if (m.DeclaringType == classCtx)
                {
                    return true;
                }
                else
                {
                    var m_type = m.DeclaringType.GetTypeInfo();
                    var classCtx_type = classCtx.GetTypeInfo();

                    return classCtx_type.IsAssignableFrom(m_type) || m_type.IsAssignableFrom(classCtx_type);
                }
            }

            //
            return true;
        }

        #region PhpPropertyInfo

        /// <summary>
        /// Gets descriptor of property defined in given class or its base classes. Does not resolve runtime fields.
        /// </summary>
        /// <returns>Instance of property descriptor or <c>null</c> if such property is not declared.</returns>
        public static PhpPropertyInfo GetDeclaredProperty(this PhpTypeInfo tinfo, string name)
        {
            for (var t = tinfo; t != null; t = t.BaseType)
            {
                foreach (var p in t.DeclaredFields.GetPhpProperties(name))
                {
                    if (!p.IsConstant) return p;
                }
            }

            //
            return null;
        }

        /// <summary>
        /// Gets descriptor of property defined in given class or its base classes. Does not resolve runtime fields.
        /// </summary>
        /// <returns>Instance of property descriptor or <c>null</c> if such property is not declared.</returns>
        public static PhpPropertyInfo GetDeclaredConstant(this PhpTypeInfo tinfo, string name)
        {
            for (var t = tinfo; t != null; t = t.BaseType)
            {
                foreach (var p in t.DeclaredFields.GetPhpProperties(name))
                {
                    if (p.IsConstant) return p;
                }
            }

            // interfaces
            foreach (var itype in tinfo.Type.GetInterfaces())
            {
                foreach (var p in itype.GetPhpTypeInfo().DeclaredFields.GetPhpProperties(name))
                {
                    if (p.IsConstant) return p;
                }
            }

            //
            return null;
        }

        /// <summary>
        /// Gets descriptor representing a runtime field.
        /// Can be <c>null</c> if type does not support runtime fields.
        /// </summary>
        public static PhpPropertyInfo GetRuntimeProperty(this PhpTypeInfo tinfo, string propertyName, object instance)
        {
            if (tinfo.RuntimeFieldsHolder != null)
            {
                var key = new IntStringKey(propertyName);

                if (instance != null)
                {
                    var runtimefields = tinfo.GetRuntimeFields(instance);
                    if (runtimefields == null || runtimefields.Count == 0 || !runtimefields.ContainsKey(key))
                    {
                        return null;
                    }
                }

                return new PhpPropertyInfo.RuntimeProperty(tinfo, key);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets enumeration of declared properties excluding constants.
        /// </summary>
        public static IEnumerable<PhpPropertyInfo> GetDeclaredProperties(this PhpTypeInfo tinfo)
        {
            for (var t = tinfo; t != null; t = t.BaseType)
            {
                foreach (var p in t.DeclaredFields.GetPhpProperties())
                {
                    if (!p.IsConstant) yield return p;
                }
            }
        }

        /// <summary>
        /// Gets enumeration of declared class constants.
        /// </summary>
        public static IEnumerable<PhpPropertyInfo> GetDeclaredConstants(this PhpTypeInfo tinfo)
        {
            for (var t = tinfo; t != null; t = t.BaseType)
            {
                foreach (var p in t.DeclaredFields.GetPhpProperties())
                {
                    if (p.IsConstant) yield return p;
                }
            }

            // interfaces
            foreach (var itype in tinfo.Type.GetInterfaces())
            {
                foreach (var p in itype.GetPhpTypeInfo().DeclaredFields.GetPhpProperties())
                {
                    if (p.IsConstant) yield return p;
                }
            }
        }

        /// <summary>
        /// Gets enumeration of runtime fields.
        /// </summary>
        public static IEnumerable<PhpPropertyInfo> GetRuntimeProperties(this PhpTypeInfo tinfo, object instance)
        {
            var runtimefields = tinfo.GetRuntimeFields(instance);
            if (runtimefields != null && runtimefields.Count != 0)
            {
                using (var enumerator = runtimefields.GetFastEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        yield return new PhpPropertyInfo.RuntimeProperty(tinfo, enumerator.CurrentKey);
                    }
                }
            }
        }

        #endregion
    }
}
