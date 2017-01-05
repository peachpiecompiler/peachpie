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

        internal TypeFields(Type type)
        {
            var tinfo = type.GetTypeInfo();

            _fields = tinfo.DeclaredFields.Where(_IsAllowedField).ToDictionary(_FieldName, StringComparer.Ordinal);
            if (_fields.Count == 0)
                _fields = null;

            var staticscontainer = tinfo.GetDeclaredNestedType("_statics");
            if (staticscontainer != null)
            {
                _staticsFields = staticscontainer.DeclaredFields.ToDictionary(_FieldName, StringComparer.Ordinal);
            }

            _properties = tinfo.DeclaredProperties.ToDictionary(_PropertyName, StringComparer.Ordinal);
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

        static Func<FieldInfo, bool> _IsAllowedField = f => ReflectionUtils.IsAllowedPhpName(f.Name) && !ReflectionUtils.IsRuntimeFields(f);
        static Func<FieldInfo, string> _FieldName = f => f.Name;
        static Func<PropertyInfo, string> _PropertyName = p => p.Name;

        static Func<Context, object> CreateStaticsGetter(Type _statics)
        {
            Debug.Assert(_statics.Name == "_statics");
            Debug.Assert(_statics.IsNested);

            var getter = BinderHelpers.GetStatic_T_Method(_statics);    // ~ Context.GetStatics<_statics>, in closure
            // TODO: getter.CreateDelegate
            return ctx => getter.Invoke(ctx, ArrayUtils.EmptyObjects);
        }

        #endregion

        /// <summary>
        /// Gets value indicating the class contains a field with specified name.
        /// </summary>
        public FieldKind HasField(string name)
        {
            FieldInfo fld;

            //
            if (_fields != null && _fields.TryGetValue(name, out fld))
            {
                if (fld.IsPublic && fld.IsLiteral) return FieldKind.Constant;

                if (fld.IsStatic) return FieldKind.StaticField;

                return FieldKind.InstanceField;
            }

            //
            if (_staticsFields != null && _staticsFields.TryGetValue(name, out fld))
            {
                return fld.IsInitOnly ? FieldKind.Constant : FieldKind.StaticField;
            }

            //
            PropertyInfo p;
            if (_properties != null && _properties.TryGetValue(name, out p))
            {
                return p.GetMethod.IsStatic ? FieldKind.StaticField : FieldKind.InstanceField;
            }

            //
            return FieldKind.Undefined;
        }

        /// <summary>
        /// Resolves a constant value in given context.
        /// </summary>
        public object GetConstantValue(Context ctx, string name)
        {
            if (ctx == null)
            {
                throw new ArgumentNullException("ctx");
            }

            FieldInfo fld;

            // fields
            if (_fields != null && _fields.TryGetValue(name, out fld))
            {
                if (fld.IsPublic && fld.IsLiteral)
                {
                    return fld.GetValue(null);
                }
            }

            // __statics.fields
            if (_staticsFields != null && _staticsFields.TryGetValue(name, out fld))
            {
                if (fld.IsPublic && fld.IsInitOnly)
                {
                    return fld.GetValue(EnsureStaticsGetter(fld.DeclaringType)(ctx));  // Context.GetStatics<_statics>().FIELD
                }
            }

            throw new ArgumentException();
        }

        public enum FieldKind
        {
            Undefined,

            InstanceField,
            StaticField,
            Constant,
        }

        /// <summary>
        /// Gets <see cref="Expression"/> representing field value.
        /// </summary>
        /// <param name="name">Class constant name.</param>
        /// <param name="classCtx">Current class context. Can be <c>null</c>.</param>
        /// <param name="target">Expression representing self instance.</param>
        /// <param name="ctx">Expression representing current <see cref="Context"/>.</param>
        /// <param name="kind">Field kind.</param>
        /// <returns><see cref="Expression"/> instance or <c>null</c> if constant does not exist.</returns>
        internal Expression Bind(string name, Type classCtx, Expression target, Expression ctx, FieldKind kind)
        {
            FieldInfo fld;

            //
            if (_fields != null && _fields.TryGetValue(name, out fld))
            {
                if (fld.IsPublic && fld.IsLiteral)
                {
                    if (kind == FieldKind.Constant)
                    {
                        return Expression.Constant(fld.GetValue(null));
                    }
                }

                if (fld.IsStatic)
                {
                    if (kind == FieldKind.StaticField)
                    {
                        return Expression.Field(null, fld);
                    }
                }

                if (kind == FieldKind.InstanceField)
                {
                    Debug.Assert(target != null);
                    return Expression.Field(target, fld);
                }
            }

            //
            if (kind != FieldKind.InstanceField && _staticsFields != null && _staticsFields.TryGetValue(name, out fld))
            {
                if ((kind == FieldKind.Constant && fld.IsInitOnly) ||
                    (kind == FieldKind.StaticField))
                {
                    Debug.Assert(target == null);
                    Debug.Assert(ctx != null);

                    // Context.GetStatics<_statics>().FIELD
                    var getstatics = BinderHelpers.GetStatic_T_Method(fld.DeclaringType);
                    return Expression.Field(Expression.Call(ctx, getstatics), fld);
                }
            }

            //
            PropertyInfo p;
            if (kind != FieldKind.Constant && _properties != null && _properties.TryGetValue(name, out p))
            {
                var isstatic = p.GetMethod.IsStatic;
                if ((kind == FieldKind.StaticField) == isstatic)
                {
                    Debug.Assert((target == null) == isstatic);
                    return Expression.Property(target, p);
                }
            }

            //
            return null;
        }
    }
}
