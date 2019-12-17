using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Pchp.CodeAnalysis.Semantics;
using Pchp.Core.Dynamic;

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
        internal class ClrFieldProperty : PhpPropertyInfo
        {
            protected readonly FieldInfo _field;

            readonly Lazy<Func<object, PhpValue>> _lazyGetter;
            readonly Lazy<Action<Context, object, PhpValue>> _lazySetValue;
            readonly Lazy<Func<object, PhpAlias>> _lazyEnsureAlias;
            readonly Lazy<Func<object, object>> _lazyEnsureObject;
            readonly Lazy<Func<object, IPhpArray>> _lazyEnsureArray;

            /// <summary>
            /// Creates Func&lt;object, T&gt; depending on the access.
            /// </summary>
            Delegate CompileAccess(AccessMask access)
            {
                var pinstance = Expression.Parameter(typeof(object));

                var expr = Bind(null, Expression.Convert(pinstance, _field.DeclaringType));
                if (access == AccessMask.Read)
                {
                    expr = ConvertExpression.BindToValue(expr);
                }
                else
                {
                    expr = BinderHelpers.BindAccess(expr, null, access, null);
                }

                return Expression.Lambda(expr, true, pinstance).Compile();
            }

            public ClrFieldProperty(PhpTypeInfo tinfo, FieldInfo field)
                : base(tinfo)
            {
                _field = field ?? throw new ArgumentNullException(nameof(field));

                //
                _lazyGetter = new Lazy<Func<object, PhpValue>>(() => (Func<object, PhpValue>)CompileAccess(AccessMask.Read));
                _lazyEnsureAlias = new Lazy<Func<object, PhpAlias>>(() => (Func<object, PhpAlias>)CompileAccess(AccessMask.ReadRef));
                _lazyEnsureObject = new Lazy<Func<object, object>>(() => (Func<object, object>)CompileAccess(AccessMask.EnsureObject));
                _lazyEnsureArray = new Lazy<Func<object, IPhpArray>>(() => (Func<object, IPhpArray>)CompileAccess(AccessMask.EnsureArray));

                // SetValue(instance, PhpValue): void
                _lazySetValue = new Lazy<Action<Context, object, PhpValue>>(() =>
                {
                    if (IsReadOnly)
                    {
                        // error
                        return(_, _instance, _value) =>
                        {
                            PhpException.ErrorException(Resources.ErrResources.readonly_property_written, ContainingType.Name, PropertyName);
                        };
                    }

                    var pctx = Expression.Parameter(typeof(Context));
                    var pinstance = Expression.Parameter(typeof(object));
                    var pvalue = Expression.Parameter(typeof(PhpValue));

                    // expr: <instance>.<field>
                    var expr = Bind(pctx, Expression.Convert(pinstance, _field.DeclaringType));

                    // expr: <field> := <value>
                    expr = BinderHelpers.BindAccess(expr, pctx, AccessMask.Write, pvalue);

                    //
                    var lambda = Expression.Lambda(Expression.Block(expr, Expression.Empty()), pctx, pinstance, pvalue);

                    return (Action<Context, object, PhpValue>)lambda.Compile();
                });
            }

            public override FieldAttributes Attributes => _field.Attributes;

            public override bool IsReadOnly => _field.IsInitOnly || _field.IsLiteral;

            public override bool IsConstant => _field.IsLiteral;

            public override bool IsRuntimeProperty => false;

            public override string PropertyName => _field.Name;

            public override PhpValue GetValue(Context _, object instance) => _lazyGetter.Value(instance);

            public override PhpAlias EnsureAlias(Context _, object instance) => _lazyEnsureAlias.Value(instance);

            public override object EnsureObject(Context _, object instance) => _lazyEnsureObject.Value(instance);

            public override IPhpArray EnsureArray(Context _, object instance) => _lazyEnsureArray.Value(instance);

            public override void SetValue(Context ctx, object instance, PhpValue value) => _lazySetValue.Value(ctx, instance, value);

            public override Expression Bind(Expression _, Expression target)
            {
                if (_field.IsLiteral)
                {
                    return Expression.Constant(_field.GetValue(null));
                }

                return Expression.Field(_field.IsStatic ? null : target, _field);
            }
        }

        #endregion

        #region ContainedClrField

        /// <summary>
        /// Instance field contained in <c>__statics</c> container representing context-static PHP property.
        /// </summary>
        internal sealed class ContainedClrField : ClrFieldProperty
        {
            public ContainedClrField(PhpTypeInfo tinfo, FieldInfo field)
                : base(tinfo, field)
            {
                Debug.Assert(field != null);
                Debug.Assert(field.DeclaringType.Name == "_statics");
                Debug.Assert(!field.IsStatic);
            }

            public override FieldAttributes Attributes
            {
                get
                {
                    var attributes = _field.Attributes;

                    var membervisibility = _field.GetCustomAttribute<PhpMemberVisibilityAttribute>(false);
                    if (membervisibility != null)
                    {
                        var access = attributes & FieldAttributes.FieldAccessMask;

                        switch (membervisibility.Accessibility)
                        {
                            case 1:
                                access = FieldAttributes.Private;
                                break;
                            case 3:
                                access = FieldAttributes.Family;
                                break;
                        }

                        attributes = (attributes & ~FieldAttributes.FieldAccessMask) | access;
                    }

                    //
                    return attributes | FieldAttributes.Static;
                }
            }

            public override bool IsConstant => IsReadOnly;

            public override Expression Bind(Expression ctx, Expression target)
            {
                Debug.Assert(ctx != null);

                // Context.GetStatics<_statics>().FIELD
                var getstatics = BinderHelpers.GetStatic_T_Method(_field.DeclaringType);
                return Expression.Field(Expression.Call(ctx, getstatics), _field);
            }
        }

        #endregion

        #region ClrProperty

        internal sealed class ClrProperty : PhpPropertyInfo
        {
            readonly PropertyInfo _property;
            readonly Lazy<Func<object, PhpValue>> _lazyGetter;
            readonly Lazy<Action<Context, object, PhpValue>> _lazySetValue;

            public ClrProperty(PhpTypeInfo tinfo, PropertyInfo property)
                : base(tinfo)
            {
                Debug.Assert(property != null);
                _property = property;

                _lazyGetter = new Lazy<Func<object, PhpValue>>(() =>
                {
                    var pinstance = Expression.Parameter(typeof(object));

                    var expr = Bind(null, Expression.Convert(pinstance, _property.DeclaringType));
                    expr = ConvertExpression.BindToValue(expr);

                    //
                    return (Func<object, PhpValue>)Expression.Lambda(expr, true, pinstance).Compile();
                });

                // SetValue(instance, PhpValue): void
                _lazySetValue = new Lazy<Action<Context, object, PhpValue>>(() =>
                {
                    if (IsReadOnly)
                    {
                        // error
                        return (_, _instance, _value) =>
                        {
                            PhpException.ErrorException(Resources.ErrResources.readonly_property_written, ContainingType.Name, PropertyName);
                        };
                    }

                    var pctx = Expression.Parameter(typeof(Context));
                    var pinstance = Expression.Parameter(typeof(object));
                    var pvalue = Expression.Parameter(typeof(PhpValue));

                    // expr: <instance>.<field>
                    var expr = Bind(pctx, Expression.Convert(pinstance, _property.DeclaringType));

                    // expr: <property> := <value>
                    expr = Expression.Assign(expr, ConvertExpression.Bind(pvalue, expr.Type, pctx));    // TODO: PHP semantic (Operators.SetValue)

                    // {expr}: void
                    var lambda = Expression.Lambda(Expression.Block(expr, Expression.Empty()), pctx, pinstance, pvalue);

                    return (Action<Context, object, PhpValue>)lambda.Compile();
                });
            }

            public override FieldAttributes Attributes
            {
                get
                {
                    var flags = (FieldAttributes)0;
                    var attr = _property.GetMethod.Attributes;

                    switch (attr & MethodAttributes.MemberAccessMask)
                    {
                        case MethodAttributes.Private: flags |= FieldAttributes.Private; break;
                        case MethodAttributes.Assembly: flags |= FieldAttributes.Assembly; break;
                        case MethodAttributes.Family: flags |= FieldAttributes.Family; break;
                        case MethodAttributes.FamORAssem: flags |= FieldAttributes.FamORAssem; break;
                        case MethodAttributes.Public: flags |= FieldAttributes.Public; break;
                    }
                    if ((attr & MethodAttributes.Static) != 0) flags |= FieldAttributes.Static;
                    if ((_property.Attributes & PropertyAttributes.HasDefault) != 0) flags |= FieldAttributes.HasDefault;

                    return flags;
                }
            }

            public override bool IsReadOnly => _property.SetMethod == null;

            public override bool IsConstant => false;

            public override bool IsRuntimeProperty => false;

            public override string PropertyName => _property.Name;

            public override PhpValue GetValue(Context ctx, object instance) => _lazyGetter.Value(instance);

            public override void SetValue(Context ctx, object instance, PhpValue value) => _lazySetValue.Value(ctx, instance, value);

            public override Expression Bind(Expression ctx, Expression target)
            {
                return Expression.Property(
                    _property.GetMethod.IsStatic ? null : target,
                    _property);
            }
        }

        #endregion

        #region RuntimeProperty

        internal sealed class RuntimeProperty : PhpPropertyInfo
        {
            readonly IntStringKey _name;

            public RuntimeProperty(PhpTypeInfo tinfo, IntStringKey name)
                : base(tinfo)
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
                var runtime_fields = ContainingType.GetRuntimeFields(instance);

                // (instance)._runtime_fields[_name]
                if (runtime_fields != null && runtime_fields.TryGetValue(_name, out var value))
                {
                    return value;
                }
                else
                {
                    PhpException.UndefinedProperty(ContainingType.Name, _name.ToString());
                    return PhpValue.Void;
                }
            }

            public override PhpAlias EnsureAlias(Context ctx, object instance)
            {
                var runtime_fields = ContainingType.EnsureRuntimeFields(instance);
                if (runtime_fields == null)
                {
                    throw new NotSupportedException();
                }

                // (instance)._runtime_fields[_name]
                return runtime_fields.EnsureItemAlias(_name);
            }

            public override object EnsureObject(Context ctx, object instance)
            {
                var runtime_fields = ContainingType.EnsureRuntimeFields(instance);
                if (runtime_fields == null)
                {
                    throw new NotSupportedException();
                }

                // (instance)._runtime_fields[_name]
                return runtime_fields.EnsureItemObject(_name);
            }

            public override IPhpArray EnsureArray(Context ctx, object instance)
            {
                var runtime_fields = ContainingType.EnsureRuntimeFields(instance);
                if (runtime_fields == null)
                {
                    throw new NotSupportedException();
                }

                // (instance)._runtime_fields[_name]
                return runtime_fields.EnsureItemArray(_name);
            }

            public override void SetValue(Context ctx, object instance, PhpValue value)
            {
                var runtime_fields = ContainingType.EnsureRuntimeFields(instance);
                if (runtime_fields == null)
                {
                    throw new NotSupportedException();
                }

                if (value.IsAlias)
                {
                    runtime_fields[_name] = value;
                }
                else
                {
                    runtime_fields.SetItemValue(_name, value);
                }
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
        public PhpTypeInfo ContainingType { get; }

        protected PhpPropertyInfo(PhpTypeInfo tinfo)
        {
            ContainingType = tinfo ?? throw new ArgumentNullException(nameof(tinfo));
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
        public abstract PhpValue GetValue(Context ctx, object instance);

        /// <summary>
        /// Ensures the property value to be <see cref="PhpAlias"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">In case the type of the property doesn't allow.</exception>
        public virtual PhpAlias EnsureAlias(Context ctx, object instance) => GetValue(ctx, instance).EnsureAlias();

        /// <summary>
        /// Ensures the property value to be instance of <see cref="object"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">In case the type of the property doesn't allow.</exception>
        public virtual object EnsureObject(Context ctx, object instance) => GetValue(ctx, instance).EnsureObject();

        /// <summary>
        /// Ensures the property value to be an <c>array</c>.
        /// </summary>
        /// <exception cref="NotSupportedException">In case the type of the property doesn't allow.</exception>
        public virtual IPhpArray EnsureArray(Context ctx, object instance) => GetValue(ctx, instance).EnsureArray();

        /// <summary>
        /// Sets new value.
        /// Throws an exception in case of <see cref="IsReadOnly"/>.
        /// </summary>
        /// <exception cref="Exception">Type mismatch.</exception>
        /// <exception cref="NotSupportedException">Property is read-only.</exception>
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
        /// Gets value indicating the property is protected (not public nor private).
        /// This might indicate CLR's <c>internal</c>, <c>internal protected</c> or <c>protected</c>.
        /// </summary>
        public virtual bool IsProtected
        {
            get
            {
                switch (Attributes & FieldAttributes.FieldAccessMask)
                {
                    case FieldAttributes.Public:
                    case FieldAttributes.Private:
                        return false;
                    default:
                        return true;
                }
            }
        }

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
            // quickly check without resolving RuntimeTypeHandle:
            if (IsPublic)
            {
                return true;
            }

            if (caller.Equals(default(RuntimeTypeHandle)))
            {
                return false;
            }

            //
            return IsVisible(Type.GetTypeFromHandle(caller));
        }

        /// <summary>
        /// Gets value indicating the property is visible in given class context.
        /// </summary>
        /// <param name="caller">Class context. By default the method check if the property is publically visible.</param>
        public bool IsVisible(Type caller = null)
        {
            if (IsPublic)
            {
                return true;
            }

            if (caller == null)
            {
                return false;
            }

            // private
            if (IsPrivate)
            {
                return ContainingType.Type == caller;
            }

            // protected|internal
            // language.oop5.visibility: Members declared protected can be accessed only within the class itself and by inheriting and parent classes
            return ContainingType.Type.IsAssignableFrom(caller) || caller.IsAssignableFrom(ContainingType.Type);
        }

        /// <summary>
        /// Gets <see cref="Expression"/> representing the property (field or property).
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="target">Target instance. Can be <c>null</c> for static properties and constants.</param>
        public abstract Expression Bind(Expression ctx, Expression target);
    }
}
