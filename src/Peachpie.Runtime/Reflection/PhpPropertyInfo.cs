using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Pchp.Core.Reflection
{
    /// <summary>
    /// Provides runtime information about a PHP property or a PHP class constant.
    /// </summary>
    public abstract class PhpPropertyInfo
    {
        #region ClrFieldProperty

        /// <summary>
        /// Instance or app-static property declared directly as a .NET field.
        /// </summary>
        internal sealed class ClrFieldProperty : PhpPropertyInfo
        {
            readonly FieldInfo _field;

            public ClrFieldProperty(PhpTypeInfo tinfo, FieldInfo field)
                : base(tinfo)
            {
                Debug.Assert(field != null);
                _field = field;
            }

            public override FieldAttributes Attributes => _field.Attributes;

            public override bool IsReadOnly => _field.IsInitOnly || IsConstant;

            public override bool IsConstant => _field.IsLiteral;

            public override bool IsRuntimeProperty => false;

            public override string PropertyName => _field.Name;

            public override PhpValue GetValue(Context ctx, object instance = null) => PhpValue.FromClr(_field.GetValue(instance));

            public override void SetValue(Context ctx, object instance, PhpValue value)
            {
                _field.SetValue(instance, value.ToClr(_field.FieldType));
            }

            public override Expression Bind(Expression ctx, Expression target)
            {
                if (_field.IsLiteral)
                {
                    return Expression.Constant(_field.GetValue(null));
                }

                if (_field.IsStatic)
                {
                    return Expression.Field(null, _field);
                }

                return Expression.Field(target, _field);
            }
        }

        #endregion

        #region ContainedClrField

        /// <summary>
        /// Instance field contained in <c>__statics</c> container representing context-static PHP property.
        /// </summary>
        internal sealed class ContainedClrField : PhpPropertyInfo
        {
            readonly FieldInfo _field;
            readonly Func<Context, object> _staticsGetter;

            public ContainedClrField(PhpTypeInfo tinfo, Func<Context, object> staticsGetter, FieldInfo field)
                : base(tinfo)
            {
                Debug.Assert(staticsGetter != null);
                Debug.Assert(field != null);
                Debug.Assert(!field.IsStatic);
                _field = field;
                _staticsGetter = staticsGetter;
            }

            public override FieldAttributes Attributes => _field.Attributes | FieldAttributes.Static;

            public override bool IsReadOnly => _field.IsInitOnly || _field.IsLiteral;

            public override bool IsConstant => IsReadOnly;

            public override bool IsRuntimeProperty => false;

            public override string PropertyName => _field.Name;

            public override PhpValue GetValue(Context ctx, object instance = null)
            {
                return PhpValue.FromClr(_field.GetValue(_staticsGetter(ctx))); // __statics.field
            }

            public override void SetValue(Context ctx, object instance, PhpValue value)
            {
                if (IsReadOnly) throw new NotSupportedException();

                _field.SetValue(_staticsGetter(ctx), value.ToClr(_field.FieldType));
            }

            public override Expression Bind(Expression ctx, Expression target)
            {
                Debug.Assert(target == null);
                Debug.Assert(ctx != null);

                // Context.GetStatics<_statics>().FIELD
                var getstatics = Dynamic.BinderHelpers.GetStatic_T_Method(_field.DeclaringType);
                return Expression.Field(Expression.Call(ctx, getstatics), _field);
            }
        }

        #endregion

        #region ClrProperty

        internal sealed class ClrProperty : PhpPropertyInfo
        {
            readonly PropertyInfo _property;

            public ClrProperty(PhpTypeInfo tinfo, PropertyInfo property)
                : base(tinfo)
            {
                Debug.Assert(property != null);
                _property = property;
            }

            public override FieldAttributes Attributes
            {
                get
                {
                    var flags = (FieldAttributes)0;
                    var attr = _property.GetMethod.Attributes;

                    if ((attr & MethodAttributes.Public) != 0) flags |= FieldAttributes.Public;
                    if ((attr & MethodAttributes.Private) != 0) flags |= FieldAttributes.Private;
                    if ((attr & MethodAttributes.FamORAssem) != 0) flags |= FieldAttributes.FamORAssem;
                    if ((attr & MethodAttributes.Static) != 0) flags |= FieldAttributes.Static;
                    if ((_property.Attributes & PropertyAttributes.HasDefault) != 0) flags |= FieldAttributes.HasDefault;

                    return flags;
                }
            }

            public override bool IsReadOnly => _property.SetMethod == null;

            public override bool IsConstant => false;

            public override bool IsRuntimeProperty => false;

            public override string PropertyName => _property.Name;

            public override PhpValue GetValue(Context ctx, object instance = null) => PhpValue.FromClr(_property.GetValue(instance));

            public override void SetValue(Context ctx, object instance, PhpValue value)
            {
                var setter = _property.SetMethod;
                if (setter == null) throw new NotSupportedException();
                setter.Invoke(instance, new[] { value.ToClr(_property.PropertyType) });
            }

            public override Expression Bind(Expression ctx, Expression target)
            {
                if (_property.GetMethod.IsStatic != (target == null))
                {
                    
                }

                return Expression.Property(target, _property);
            }
        }

        #endregion

        #region RuntimeProperty

        internal sealed class RuntimeProperty : PhpPropertyInfo
        {
            readonly IntStringKey _name;

            public RuntimeProperty(PhpTypeInfo tinfo, IntStringKey name) : base(tinfo)
            {
                _name = name;
            }

            public override FieldAttributes Attributes => FieldAttributes.Public;

            public override bool IsReadOnly => false;

            public override bool IsConstant => false;

            public override bool IsRuntimeProperty => true;

            public override string PropertyName => _name.ToString();

            public override PhpValue GetValue(Context ctx, object instance)
            {
                var runtime_fields = _tinfo.GetRuntimeFields(instance);
                if (runtime_fields != null)
                {
                    // (instance)._runtime_fields[_name]
                    return runtime_fields.GetItemValue(_name);
                }

                return PhpValue.Void;
            }

            public override void SetValue(Context ctx, object instance, PhpValue value)
            {
                _tinfo.EnsureRuntimeFields(instance)[_name] = value;
            }

            public override bool IsPublic => true;

            public override Expression Bind(Expression ctx, Expression target)
            {
                throw new NotSupportedException();
            }
        }

        #endregion

        /// <summary>
        /// Containing type information.
        /// Cannot be <c>null</c>.
        /// </summary>
        public PhpTypeInfo ContainingType => _tinfo;
        readonly PhpTypeInfo _tinfo;

        protected PhpPropertyInfo(PhpTypeInfo tinfo)
        {
            Debug.Assert(tinfo != null);
            _tinfo = tinfo;
        }

        /// <summary>
        /// Gets the PHP property name.
        /// </summary>
        public abstract string PropertyName { get; }

        /// <summary>
        /// Gets value indicating the property cannot change value.
        /// In such case, there is no property setter or the field is constant or readonly.
        /// </summary>
        public abstract bool IsReadOnly { get; }

        /// <summary>
        /// Gets the runtime value of the property.
        /// </summary>
        public abstract PhpValue GetValue(Context ctx, object instance = null);

        /// <summary>
        /// Sets new value.
        /// Throws an exception in case of <see cref="IsReadOnly"/>.
        /// </summary>
        public abstract void SetValue(Context ctx, object instance, PhpValue value);

        /// <summary>
        /// Gets accessibility attributes.
        /// </summary>
        public abstract FieldAttributes Attributes { get; }

        /// <summary>
        /// Gets value indicating whether the property is added in runtime, i.e. as an entry in the runtime fields array.
        /// </summary>
        public abstract bool IsRuntimeProperty { get; }

        /// <summary>
        /// Gets value indicating the property is static hence it does not require an instance object.
        /// </summary>
        public virtual bool IsStatic => (Attributes & FieldAttributes.Static) != 0;

        /// <summary>
        /// Gets value indicating the property is public.
        /// </summary>
        public virtual bool IsPublic => (Attributes & FieldAttributes.Public) == FieldAttributes.Public;

        /// <summary>
        /// Gets value indicating the property is public.
        /// </summary>
        public virtual bool IsPrivate => (Attributes & FieldAttributes.Private) == FieldAttributes.Private;

        /// <summary>
        /// Gets value indicating the property is protected.
        /// </summary>
        public virtual bool IsProtected => !IsPublic && !IsPrivate;

        /// <summary>
        /// Gets value indicating the descriptor corresponds to a class constant.
        /// </summary>
        public abstract bool IsConstant { get; }

        /// <summary>
        /// Gets value indicating the property is visible in given class context.
        /// </summary>
        /// <param name="caller">Class context. By default the method check if the property is publically visible.</param>
        public bool IsVisible(RuntimeTypeHandle caller = default(RuntimeTypeHandle))
        {
            if (IsPublic) return true;

            if (caller.Equals(default(RuntimeTypeHandle))) return false;

            var callerType = Type.GetTypeFromHandle(caller);

            // private
            if (IsPrivate) return _tinfo.Type.Equals(callerType);

            // protected|internal
            return _tinfo.Type.IsAssignableFrom(callerType);
        }

        /// <summary>
        /// Gets value indicating the property is visible in given class context.
        /// </summary>
        /// <param name="caller">Class context. By default the method check if the property is publically visible.</param>
        public bool IsVisible(Type caller = null)
        {
            if (IsPublic) return true;

            if (caller == null) return false;

            // private
            if (IsPrivate) return _tinfo.Type.Equals(caller);

            // protected|internal
            return _tinfo.Type.IsAssignableFrom(caller);
        }

        /// <summary>
        /// Gets <see cref="Expression"/> representing the property (field or property).
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="target">Target instance. Can be <c>null</c> for static properties and constants.</param>
        public abstract Expression Bind(Expression ctx, Expression target);
    }
}
