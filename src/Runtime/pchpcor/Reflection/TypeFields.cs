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

            _fields = tinfo.DeclaredFields.ToDictionary(_FieldName);
            if (_fields.Count == 0)
            {
                _fields = null;
            }

            var staticscontainer = tinfo.GetDeclaredNestedType("_statics");
            if (staticscontainer != null)
            {
                _staticsFields = staticscontainer.DeclaredFields.ToDictionary(_FieldName);
            }
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

        static Func<FieldInfo, string> _FieldName = f => f.Name;

        static Func<Context, object> CreateStaticsGetter(Type _statics)
        {
            Debug.Assert(_statics.Name == "_statics");
            Debug.Assert(_statics.IsNested);

            var getter = BinderHelpers.GetStatic_T_Method(_statics);    // ~ Context.GetStatics<_statics>, in closure
            return ctx => getter.Invoke(ctx, ArrayUtils.EmptyObjects);
        }

        #endregion

        /// <summary>
        /// Gets value indicating the class contains a field with specified name.
        /// </summary>
        public FieldKind ResolveField(string name)
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
            return FieldKind.Undefined;
        }

        /// <summary>
        /// Resolves a constant value in given context.
        /// </summary>
        public object GetConstantValue(string name, Context ctx)
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
        /// <param name="target">Expression representing self instance.</param>
        /// <param name="ctx">Expression representing current <see cref="Context"/>.</param>
        /// <returns><see cref="Expression"/> instance or <c>null</c> if constant does not exist.</returns>
        internal Expression Bind(string name, Expression target, Expression ctx, FieldKind kind)
        {
            if (ctx == null)
            {
                throw new ArgumentNullException("ctx");
            }

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
                    return Expression.Field(target, fld);
                }
            }

            //
            if (_staticsFields != null && _staticsFields.TryGetValue(name, out fld))
            {
                if ((kind == FieldKind.Constant && fld.IsInitOnly) ||
                    (kind == FieldKind.StaticField))
                {
                    // Context.GetStatics<_statics>().FIELD
                    var getstatics = BinderHelpers.GetStatic_T_Method(fld.DeclaringType);
                    return Expression.Field(Expression.Call(ctx, getstatics), fld);
                }
            }

            //
            return null;
        }
    }
}
