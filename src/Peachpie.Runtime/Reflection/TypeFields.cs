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

        internal TypeFields(TypeInfo tinfo)
        {
            var t = tinfo.GetPhpTypeInfo();
            var properties = new Dictionary<string, PhpPropertyInfo>();

            foreach (var field in tinfo.DeclaredFields.Where(s_isAllowedField))
            {
                properties[field.Name] = new PhpPropertyInfo.ClrFieldProperty(t, field);
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

                var staticsGetter = CreateStaticsGetter(staticscontainer);

                foreach (var field in staticscontainer.DeclaredFields.Where(s_isAllowedField))
                {
                    properties[field.Name] = new PhpPropertyInfo.ContainedClrField(t, staticsGetter, field);
                }
            }

            foreach (var prop in tinfo.DeclaredProperties.Where(s_isAllowedProperty))
            {
                properties[prop.Name] = new PhpPropertyInfo.ClrProperty(t, prop);
            }

            //
            _properties = properties.Count != 0 ? properties : EmptyDictionary<string, PhpPropertyInfo>.Singleton;
        }

        static readonly Func<PhpPropertyInfo, bool> s_isInstanceProperty = p => !p.IsStatic;

        //static readonly Func<FieldInfo, bool> s_isInstanceField = f => !f.IsStatic;

        static readonly Func<FieldInfo, bool> s_isAllowedField = f =>
            ReflectionUtils.IsAllowedPhpName(f.Name) &&
            !ReflectionUtils.IsRuntimeFields(f) &&
            !ReflectionUtils.IsContextField(f) &&
            !f.IsPhpHidden() &&
            !f.FieldType.IsPointer;

        //static readonly Func<FieldInfo, string> s_fieldName = f => f.Name;

        static readonly Func<PropertyInfo, bool> s_isAllowedProperty = p => ReflectionUtils.IsAllowedPhpName(p.Name) && p.GetIndexParameters().Length == 0 && !p.IsPhpHidden();

        //static readonly Func<PropertyInfo, string> s_propertyName = p => p.Name;

        static Func<Context, object> CreateStaticsGetter(Type _statics)
        {
            Debug.Assert(_statics.Name == "_statics");
            Debug.Assert(_statics.IsNested);

            var getter = BinderHelpers.GetStatic_T_Method(_statics);    // ~ Context.GetStatics<_statics>, in closure
            // TODO: getter.CreateDelegate
            return ctx => getter.Invoke(ctx, Array.Empty<object>());
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
