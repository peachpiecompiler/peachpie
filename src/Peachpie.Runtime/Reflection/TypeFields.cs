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
        /// Declared fields.
        /// </summary>
        readonly Dictionary<string, FieldInfo> _fields;

        /// <summary>
        /// Declared properties.
        /// </summary>
        readonly Dictionary<string, PropertyInfo> _properties;

        /// <summary>
        /// Declared fields in <c>__statics</c> nested class.
        /// </summary>
        readonly Dictionary<string, FieldInfo> _staticsFields;

        /// <summary>
        /// Lazily initialized function getting <c>_statics</c> in given runtime <see cref="Context"/>.
        /// </summary>
        Func<Context, object> _staticsGetter;

        #endregion

        #region Initialization

        internal TypeFields(TypeInfo tinfo)
        {
            _fields = tinfo.DeclaredFields.Where(_IsAllowedField).ToDictionary(_FieldName, StringComparer.Ordinal);
            if (_fields.Count == 0)
                _fields = null;

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

                _staticsFields = staticscontainer.DeclaredFields.ToDictionary(_FieldName, StringComparer.Ordinal);
            }

            _properties = tinfo.DeclaredProperties.Where(_IsAllowedProperty).ToDictionary(_PropertyName, StringComparer.Ordinal);
            if (_properties.Count == 0)
                _properties = null;
        }

        Func<Context, object> EnsureStaticsGetter(Type type)
        {
            var getter = _staticsGetter;
            if (getter == null)
            {
                _staticsGetter = getter = CreateStaticsGetter(type);
            }

            //
            return getter;
        }

        static readonly Func<FieldInfo, bool> _isInstanceField = f => !f.IsStatic;
        static readonly Func<FieldInfo, bool> _IsAllowedField = f => ReflectionUtils.IsAllowedPhpName(f.Name) && !ReflectionUtils.IsRuntimeFields(f) && !ReflectionUtils.IsContextField(f) && !f.IsPhpHidden();
        static readonly Func<FieldInfo, string> _FieldName = f => f.Name;
        static readonly Func<PropertyInfo, string> _PropertyName = p => p.Name;
        static readonly Func<PropertyInfo, bool> _IsAllowedProperty = p => ReflectionUtils.IsAllowedPhpName(p.Name) && p.GetIndexParameters().Length == 0 && !p.IsPhpHidden();

        static Func<Context, object> CreateStaticsGetter(Type _statics)
        {
            Debug.Assert(_statics.Name == "_statics");
            Debug.Assert(_statics.IsNested);

            var getter = BinderHelpers.GetStatic_T_Method(_statics);    // ~ Context.GetStatics<_statics>, in closure
            // TODO: getter.CreateDelegate
            return ctx => getter.Invoke(ctx, ArrayUtils.EmptyObjects);
        }

        /// <summary>
        /// Gets declaring type of given <c>_statics</c>.
        /// The method properly constructs generic type containing <c>_statics</c> if it is a constructed generic type.
        /// </summary>
        static PhpTypeInfo GetStaticsDeclaringType(Type _statics)
        {
            var declaring = _statics.DeclaringType;
            if (_statics.IsConstructedGenericType)
            {
                declaring = declaring.MakeGenericType(_statics.GenericTypeArguments);
            }

            return declaring.GetPhpTypeInfo();
        }

        #endregion

        /// <summary>
        /// Gets enumeration of class instance fields excluding eventual <c>__runtime_fields</c>.
        /// </summary>
        public IEnumerable<FieldInfo> InstanceFields => (_fields != null) ? _fields.Values.Where(_isInstanceField) : Array.Empty<FieldInfo>();

        /// <summary>
        /// Gets enumeration of class instance properties.
        /// </summary>
        public IEnumerable<PropertyInfo> InstanceClrProperties => (_properties != null) ? _properties.Values.Where(p => !p.GetMethod.IsStatic) : Array.Empty<PropertyInfo>();

        /// <summary>
        /// Enumerates all the properties in the class excluding runtime fields.
        /// </summary>
        public IEnumerable<PhpPropertyInfo> GetPhpProperties()
        {
            IEnumerable<PhpPropertyInfo> result = Enumerable.Empty<PhpPropertyInfo>();

            //
            if (_fields != null)
            {
                result = _fields.Values.Select(fld => new PhpPropertyInfo.ClrFieldProperty(fld.DeclaringType.GetPhpTypeInfo(), fld));
            }

            //
            if (_staticsFields != null)
            {
                result = result.Concat(_staticsFields.Values.Select(
                    fld =>
                    {
                        var __statics = fld.DeclaringType;
                        return new PhpPropertyInfo.ContainedClrField(GetStaticsDeclaringType(__statics), EnsureStaticsGetter(__statics), fld);
                    }
                ));
            }

            //
            if (_properties != null)
            {
                result = result.Concat(_properties.Values.Select(p => new PhpPropertyInfo.ClrProperty(p.DeclaringType.GetPhpTypeInfo(), p)));
            }

            //
            return result;
        }

        /// <summary>
        /// Obtains the PHP property descriptor matching given name.
        /// The enumeration includes instance fields, static fields, CLR properties and class constants
        /// </summary>
        /// <returns>
        /// Instance of <see cref="PhpPropertyInfo"/> describing the property/field/constant.
        /// Can be <c>null</c> if specified property is not declared on current type.
        /// </returns>
        /// <remarks>The method return <c>null</c> in case the property is a runtime property. This case has to be handled separately.</remarks>
        public IEnumerable<PhpPropertyInfo> GetPhpProperties(string name)
        {
            //
            if (_fields != null && _fields.TryGetValue(name, out var fld))
            {
                yield return new PhpPropertyInfo.ClrFieldProperty(fld.DeclaringType.GetPhpTypeInfo(), fld);
            }

            //
            if (_staticsFields != null && _staticsFields.TryGetValue(name, out fld))
            {
                var __statics = fld.DeclaringType;
                yield return new PhpPropertyInfo.ContainedClrField(GetStaticsDeclaringType(__statics), EnsureStaticsGetter(__statics), fld);
            }

            //
            if (_properties != null && _properties.TryGetValue(name, out var p))
            {
                yield return new PhpPropertyInfo.ClrProperty(p.DeclaringType.GetPhpTypeInfo(), p);
            }

            //
            yield break;
        }
    }
}
