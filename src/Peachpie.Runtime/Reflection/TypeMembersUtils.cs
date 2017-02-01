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
		/// Returns names and properties of all instance properties or only PHP fields (including runtime fields).
		/// </summary>
		/// <param name="instance">The instance being serialized.</param>
        /// <param name="tinfo"><paramref name="instance"/> type info.</param>
        /// <returns>Name-value pairs. Names are properly formatted for serialization.</returns>
		public static IEnumerable<KeyValuePair<string, PhpValue>> EnumerateSerializableProperties(object/*!*/ instance, PhpTypeInfo tinfo)
        {
            Debug.Assert(instance != null);
            Debug.Assert(instance.GetType() == tinfo.Type.AsType());

            // iterate through type and its base types
            for (var t = tinfo; t != null; t = t.BaseType)
            {
                // iterate through instance fields
                foreach (var f in t.DeclaredFields.InstanceFields)
                {
                    yield return new KeyValuePair<string, PhpValue>(
                        FormatSerializedPropertyName(f, f.Name, t),
                        PhpValue.FromClr(f.GetValue(instance)));
                }
            }

            // PhpArray __runtime_fields
            var runtime_fields = tinfo.GetRuntimeFields(instance);
            if (runtime_fields != null && runtime_fields.Count != 0)
            {
                var enumerator = runtime_fields.GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    var pair = enumerator.Current;
                    yield return new KeyValuePair<string, PhpValue>(pair.Key.ToString(), pair.Value);
                }
            }
        }

        #region ParseSerializedPropertyName, FormatSerializedPropertyName

        /// <summary>
        /// Parses property name used for serialization. 
        /// </summary>
        /// <param name="name">The name found in serialization stream or returned by <B>__sleep</B>.</param>
        /// <param name="typeName">Will receive the name of the declaring type or <B>null</B> if no
        /// type information is embedded in the property <paramref name="name"/>.</param>
        /// <param name="visibility">Will receive the assumed visibility of the property.</param>
        /// <returns>The bare (unmangled) property name.</returns>
        /// <remarks>
        /// Names of protected properties might be prepended with \0*\0, names of private properties might be
        /// prepended with \0declaring_class_name\0
        /// (see <A href="http://bugs.php.net/bug.php?id=26737">http://bugs.php.net/bug.php?id=26737</A>)
        /// </remarks>
        public static string/*!*/ ParseSerializedPropertyName(string/*!*/ name, out string typeName, out FieldAttributes visibility)
        {
            if (name.Length >= 3 && name[0] == '\0')
            {
                if (name[1] == '*' && name[2] == '\0')
                {
                    // probably a protected field
                    visibility = FieldAttributes.Family;
                    typeName = null;
                    return name.Substring(3);
                }
                else
                {
                    // probably a private property
                    int index = name.IndexOf('\0', 2);
                    if (index > 0)
                    {
                        visibility = FieldAttributes.Private;
                        typeName = name.Substring(1, index - 1);  // TODO
                        return name.Substring(index + 1);
                    }
                }
            }

            visibility = FieldAttributes.Public;
            typeName = null;
            return name;
        }

        /// <summary>
		/// Formats a property name for serialization according to its visibility and declaing type.
		/// </summary>
		/// <param name="property">The property info.</param>
		/// <param name="propertyName">The property name.</param>
        /// <param name="declaringtype">Declaring type of the property.</param>
		/// <returns>The property name formatted according to the <paramref name="property"/> as used by PHP serialization.
		/// </returns>
		public static string/*!*/ FormatSerializedPropertyName(FieldInfo/*!*/ property, string/*!*/ propertyName, PhpTypeInfo declaringtype)
        {
            if (property.IsPrivate) return "\0" + declaringtype.Name + "\0" + propertyName;
            if (property.IsFamilyOrAssembly)return "\0*\0" + propertyName;
            return propertyName;
        }

        #endregion

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

        /// <summary>
        /// Resolves an instance property or gets <c>null</c> if the field is not declared.
        /// </summary>
        public static FieldInfo ResolveInstanceField(PhpTypeInfo tinfo, string fieldName)
        {
            for (var t = tinfo; t != null ; t = t.BaseType)
            {
                var fld = t.DeclaredFields.TryGetInstanceField(fieldName);
                if (fld != null)
                {
                    return fld;
                }
            }

            return null;
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
    }
}
