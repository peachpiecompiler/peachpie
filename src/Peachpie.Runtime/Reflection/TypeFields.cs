using Pchp.Core.Dynamic;
using Pchp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Reflection
{
    /// <summary>
    /// Collection of class fields declared in type.
    /// </summary>
    public class TypeFields
    {
        #region Fields

        /// <summary>
        /// Declared properties.
        /// Cannot be <c>null</c>.
        /// </summary>
        readonly IReadOnlyDictionary<string, PhpPropertyInfo> _properties;

        #endregion

        #region Initialization

        internal TypeFields(PhpTypeInfo t)
        {
            var tinfo = t.Type;
            var properties = new Dictionary<string, PhpPropertyInfo>();

            foreach (var field in tinfo.DeclaredFields)
            {
                if (IsAllowedField(field))
                {
                    properties[field.Name] = new PhpPropertyInfo.ClrFieldProperty(t, field);
                }
            }

            var staticscontainer = tinfo.GetDeclaredNestedType("_statics");
            if (staticscontainer != null)
            {
                if (staticscontainer.IsGenericTypeDefinition)
                {
                    // _statics is always generic type definition (not constructed) in case enclosing type is generic.
                    // Construct the type using enclosing class (trait) generic arguments (TSelf):
                    Debug.Assert(tinfo.GenericTypeArguments.Length == staticscontainer.GenericTypeParameters.Length);   // <!TSelf>
                    staticscontainer = staticscontainer.MakeGenericType(tinfo.GenericTypeArguments).GetTypeInfo();
                }

                foreach (var field in staticscontainer.DeclaredFields)
                {
                    if (IsAllowedField(field))
                    {
                        properties[field.Name] = new PhpPropertyInfo.ContainedClrField(t, field);
                    }
                }
            }

            foreach (var prop in tinfo.DeclaredProperties)
            {
                if (IsAllowedProperty(prop))
                {
                    properties[prop.Name] = new PhpPropertyInfo.ClrProperty(t, prop);
                }
                else if (IsExplicitPropertyDef(prop, out var explicitName)) // explicit interface declaration
                {
                    properties[explicitName] = new PhpPropertyInfo.ClrExplicitProperty(t, prop, explicitName);
                }
            }

            //
            _properties = properties.Count != 0 ? properties : EmptyDictionary<string, PhpPropertyInfo>.Singleton;
        }

        static readonly Func<PhpPropertyInfo, bool> s_isInstanceProperty = p => !p.IsStatic;

        static bool IsAllowedField(FieldInfo f)
        {
            var access = f.Attributes & FieldAttributes.FieldAccessMask;
            return
                access != FieldAttributes.Assembly &&
                access != FieldAttributes.FamANDAssem &&
                ReflectionUtils.IsAllowedPhpName(f.Name) &&
                !ReflectionUtils.IsRuntimeFields(f) &&
                !ReflectionUtils.IsContextField(f) &&
                !f.IsPhpHidden() &&
                !f.FieldType.IsPointer;
        }

        static bool IsAllowedProperty(PropertyInfo p)
        {
            var getter = p.GetMethod;
            if (getter != null)
            {
                var access = getter.Attributes & MethodAttributes.MemberAccessMask;
                if (access == MethodAttributes.Assembly || access == MethodAttributes.FamANDAssem)
                {
                    return false;
                }

                if (ReflectionUtils.IsAllowedPhpName(p.Name) &&
                    p.GetIndexParameters().Length == 0 &&
                    !p.IsPhpHidden())
                {
                    return true;
                }
            }

            return false;
        }

        static bool IsExplicitPropertyDef(PropertyInfo p, out string name)
        {
            const MethodAttributes attrmask =
                MethodAttributes.Private |
                MethodAttributes.SpecialName |
                MethodAttributes.HideBySig |
                MethodAttributes.Virtual |
                MethodAttributes.Final;

            var getter = p.GetMethod;

            var ex =
                getter != null &&
                (getter.Attributes & attrmask) == attrmask &&
                getter.IsGenericMethod == false &&
                getter.IsStatic == false;

            if (ex)
            {
                var dot = p.Name.LastIndexOf('.');
                if (dot >= 0)
                {
                    name = p.Name.Substring(dot + 1);
                    return true;
                }
            }

            //
            name = default;
            return false;
        }

        #endregion

        /// <summary>
        /// Gets enumeration of class instance fields excluding eventual <c>__runtime_fields</c>.
        /// </summary>
        public IEnumerable<PhpPropertyInfo> InstanceProperties => _properties.Values.Where(s_isInstanceProperty);

        /// <summary>
        /// Enumerates all the properties in the class excluding runtime fields.
        /// </summary>
        public IEnumerable<PhpPropertyInfo> Properties => _properties.Values;

        /// <summary>
        /// Obtains the PHP property descriptor matching given name.
        /// The result may be an instance field, static field, CLR property, class constant or <c>null</c>.
        /// </summary>
        /// <returns>
        /// Instance of <see cref="PhpPropertyInfo"/> describing the property/field/constant.
        /// Can be <c>null</c> if specified property is not declared on current type.
        /// </returns>
        /// <remarks>The method return <c>null</c> in case the property is a runtime property. This case has to be handled separately.</remarks>
        public PhpPropertyInfo TryGetPhpProperty(string name) => _properties.TryGetValue(name, out var p) ? p : null;
    }
}
