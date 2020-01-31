using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Pchp.Core.Reflection
{
    public static class Serialization
    {
        /// <summary>
        /// Returns names and properties of all instance properties or only PHP fields (including runtime fields).
        /// </summary>
        /// <param name="instance">The instance being serialized.</param>
        /// <param name="tinfo"><paramref name="instance"/> type info.</param>
        /// <returns>Name-value pairs. Names are properly formatted for serialization.</returns>
        public static IEnumerable<KeyValuePair<string, PhpValue>> EnumerateSerializableProperties(object/*!*/ instance, PhpTypeInfo tinfo)
        {
            return TypeMembersUtils.EnumerateInstanceFields(instance,
                FormatSerializedPropertyName,
                TypeMembersUtils.s_keyToString,
                (m) => !m.IsReadOnly);
        }

        /// <summary>
        /// Copies fields from <paramref name="source"/> object to <paramref name="target"/> object.
        /// Types of both objects must be the same.
        /// </summary>
        /// <param name="tinfo">Type of both objects.</param>
        /// <param name="source">Source instance.</param>
        /// <param name="target">Target instance.</param>
        public static void MemberwiseClone(PhpTypeInfo tinfo, object source, object target)
        {
            Debug.Assert(tinfo != null);
            Debug.Assert(source != null);
            Debug.Assert(target != null);
            Debug.Assert(source.GetType() == target.GetType());
            Debug.Assert(source.GetType() == tinfo.Type.AsType());

            // copy CLR fields, skipping runtime fields
            foreach (var prop in TypeMembersUtils.EnumerateInstanceFields(source, Utilities.FuncExtensions.Identity<PhpPropertyInfo>(), null, null, ignoreRuntimeFields: true))
            {
                var value = prop.Value.DeepCopy();
                prop.Key.SetValue(null, target, value);
            }

            // fast copy of runtime fields
            var runtime_fields = tinfo.GetRuntimeFields(source);
            if (runtime_fields != null && runtime_fields.Count != 0)
            {
                tinfo.RuntimeFieldsHolder.SetValue(target, runtime_fields.Clone());
            }

            // TODO: traits ?
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
		/// <returns>The property name formatted according to the <paramref name="property"/> as used by PHP serialization.
		/// </returns>
		public static string/*!*/ FormatSerializedPropertyName(PhpPropertyInfo/*!*/ property)
        {
            if (property.IsPrivate) return "\0" + property.ContainingType.Name + "\0" + property.PropertyName;
            if (property.IsProtected) return "\0*\0" + property.PropertyName;
            return property.PropertyName;
        }

        #endregion
    }
}
