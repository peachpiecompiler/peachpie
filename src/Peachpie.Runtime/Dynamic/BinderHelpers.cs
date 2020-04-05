using Pchp.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Pchp.CodeAnalysis.Semantics;
using System.Runtime.CompilerServices;

namespace Pchp.Core.Dynamic
{
    [DebuggerNonUserCode]
    internal static class BinderHelpers
    {
        public static bool IsParamsParameter(this ParameterInfo p)
        {
            return p.ParameterType.IsArray && p.CustomAttributes.Any(attr => attr.AttributeType == typeof(ParamArrayAttribute));
        }

        /// <summary>
        /// Determines the parameter is considered as implicitly passed by runtime.
        /// </summary>
        public static bool IsImplicitParameter(this ParameterInfo p)
        {
            return
                p.IsContextParameter() ||
                p.IsImportValueParameter(out _) ||
                p.IsDummyFieldsOnlyCtor() ||
                p.IsLateStaticParameter() ||
                p.IsClosureParameter();
        }

        public static bool IsContextParameter(this ParameterInfo p)
        {
            return p.Position == 0
                && p.ParameterType == typeof(Context)
                && (p.Name == "ctx" || p.Name == "<ctx>" || p.Name == "context" || p.Name == ".ctx");
        }

        public static bool IsLateStaticParameter(this ParameterInfo p)
        {
            return p.ParameterType == typeof(PhpTypeInfo) && p.Name == "<static>";
        }

        public static bool IsDummyFieldsOnlyCtor(this ParameterInfo p)
        {
            return p.ParameterType == typeof(DummyFieldsOnlyCtor);
        }

        public static bool IsImportValueParameter(this ParameterInfo p, out ImportValueAttribute.ValueSpec value)
        {
            var attr = p.GetCustomAttribute<ImportValueAttribute>();
            if (attr != null)
            {
                value = attr.Value;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public static bool IsClosureParameter(this ParameterInfo p)
        {
            return p.ParameterType == typeof(Closure) && p.Name == "<closure>";
        }

        /// <summary>
        /// Gets value indicating the given type is of type <c>Nullable&lt;T&gt;</c>.
        /// </summary>
        /// <param name="type">Tested type.</param>
        /// <param name="T">In case <paramref name="type"/> is nullable, this will be set to the generic argument of given nullable.</param>
        /// <returns>Whether the type is nullable.</returns>
        public static bool IsNullable_T(this Type type, out Type T)
        {
            Debug.Assert(type != null);
            if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                T = type.GenericTypeArguments[0];
                return true;
            }
            else
            {
                T = null;
                return false;
            }
        }

        /// <summary>
        /// Checks the given type refers to <see cref="IRuntimeChain"/> value.
        /// </summary>
        public static bool IsRuntimeChain(Type t)
        {
            // a value type implementing `IRuntimeChain`,
            // in a namespace `Dynamic.RuntimeChain`
            if (t.IsValueType && t.Namespace == typeof(RuntimeChain.ChainEnd).Namespace)
            {
                Debug.Assert(t.GetInterfaces().Contains(typeof(IRuntimeChain)));

                return true;
            }

            //
            return false;
        }

        /// <summary>
        /// Checks the given type refers to <see cref="IRuntimeChain"/> value.
        /// </summary>
        public static bool IsRuntimeChain(Type t, out MethodInfo getValue, out MethodInfo getAlias)
        {
            if (IsRuntimeChain(t))
            {
                getValue = t.GetMethod("GetValue");
                getAlias = t.GetMethod("GetAlias");
                return true;
            }

            //
            getValue = getAlias = default;
            return false;
        }

        public static bool TryAppendRuntimeChain(ref Expression expr, Expression possibleChainExpr, Expression ctx, Type classContext, bool asalias)
        {
            if (possibleChainExpr != null && IsRuntimeChain(possibleChainExpr.Type, out var getValue, out var getAlias))
            {
                var method = asalias ? getAlias : getValue;
                var valueExpr = ConvertExpression.BindToValue(expr);

                // Template: chain.GetValue( expr, ctx, classContext )
                // Template: chain.GetAlias( ref expr, ctx, classContext )
                expr = Expression.Call(possibleChainExpr, method, valueExpr, ctx, Expression.Constant(classContext, typeof(Type)));

                return true;
            }

            return false;
        }

        public static bool HasLateStaticParameter(MethodInfo m)
        {
            if (m != null)
            {
                var ps = m.GetParameters();
                for (int i = 0; i < ps.Length; i++)
                {
                    if (IsLateStaticParameter(ps[i]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines the parameter does not have a default value explicitly specified.
        /// </summary>
        public static bool IsMandatoryParameter(this ParameterInfo p)
        {
            return
                !p.HasDefaultValue && // CLR default value
                !p.IsOptional && // has [Optional} attribute
                p.GetCustomAttribute<DefaultValueAttribute>() == null && // has [DefaultValue] attribute
                !p.IsParamsParameter(); // is params
        }

        /// <summary>
        /// Gets <see cref="Context.GetStatic{T}()"/> method bound to a type.
        /// </summary>
        public static MethodInfo GetStatic_T_Method(Type t)
        {
            return typeof(Context).GetMethod("GetStatic", Cache.Types.Empty).MakeGenericMethod(t);
        }

        /// <summary>
        /// Access <paramref name="target"/> as object instance.
        /// </summary>
        /// <param name="target">Given target.</param>
        /// <param name="instance">Resolved instance with restrictions.</param>
        /// <returns>Whether <paramref name="target"/> contains an object instance.</returns>
        /// <remarks>Necessary restriction are already resolved within returned <paramref name="instance"/>.</remarks>
        public static bool TryTargetAsObject(DynamicMetaObject target, out DynamicMetaObject instance)
        {
            var expr = target.Expression;
            var value = target.Value;
            var restrictions = target.Restrictions;

            if (value == null)
            {
                instance = new DynamicMetaObject(
                    expr,
                    restrictions.Merge(BindingRestrictions.GetExpressionRestriction(Expression.ReferenceEqual(expr, Expression.Constant(null)))),
                    null);

                return false;
            }

            // dereference PhpAlias first:
            if (expr.Type == typeof(PhpAlias))
            {
                // PhpAlias.Value
                expr = Expression.Field(expr, Cache.PhpAlias.Value);
                value = ((PhpAlias)value).Value;

                //
                return TryTargetAsObject(
                    new DynamicMetaObject(expr, restrictions, value),
                    out instance);
            }

            if (value is PhpAlias alias)
            {
                // PhpAlias is provided but typed as System.Object
                // create restriction and retry with properly typed {expr:PhpAlias}
                restrictions = restrictions.Merge(BindingRestrictions.GetTypeRestriction(expr, typeof(PhpAlias)));
                expr = Expression.Convert(expr, typeof(PhpAlias));

                return TryTargetAsObject(
                    new DynamicMetaObject(expr, restrictions, alias),
                    out instance);
            }

            // unwrap PhpValue
            if (expr.Type == typeof(PhpValue))
            {
                // PhpValue.Object
                expr = Expression.Property(expr, Cache.Properties.PhpValue_Object);
                value = ((PhpValue)value).Object;

                return TryTargetAsObject(
                    new DynamicMetaObject(expr, restrictions, value),
                    out instance);
            }

            // well-known non-objects:
            Type nonObjectType = null;
            if (value is PhpResource) nonObjectType = typeof(PhpResource);
            if (value is PhpArray) nonObjectType = typeof(PhpArray);
            if (value is PhpString.Blob) nonObjectType = typeof(PhpString.Blob);

            if (nonObjectType != null)
            {
                instance = new DynamicMetaObject(
                    expr,
                    restrictions.Merge(BindingRestrictions.GetExpressionRestriction(Expression.TypeIs(expr, nonObjectType))),   // PhpValue.Object is T (including any derived class)
                    value);

                return false;
            }

            //

            var lt = target.Expression.Type;
            if (!lt.IsValueType && !lt.IsSealed && !typeof(PhpArray).IsAssignableFrom(lt) && !typeof(PhpResource).IsAssignableFrom(lt))
            {
                // we need to set the type restriction
                restrictions = restrictions.Merge(BindingRestrictions.GetTypeRestriction(expr, value.GetType()));

                if (!value.GetType().IsAssignableFrom(expr.Type))
                {
                    expr = Expression.Convert(expr, value.GetType());
                }
            }

            //
            instance = new DynamicMetaObject(expr, restrictions, value);
            return !(
                value is string ||
                value is bool ||
                value is int ||
                value is long ||
                value is double ||
                value is char ||
                value is uint ||
                value is ulong ||
                value is PhpString);
        }

        /// <summary>
        /// Template: PhpException.VariableMisusedAsObject( var, bool ) : void
        /// </summary>
        public static Expression VariableMisusedAsObject(Expression var, bool reference)
        {
            return Expression.Call(
                typeof(PhpException), "VariableMisusedAsObject", Array.Empty<Type>(),
                   ConvertExpression.BindToValue(var),
                   Expression.Constant(reference));
        }

        public static Expression EnsureNotNullPhpArray(Expression variable)
        {
            // variable ?? (variable = [])
            return Expression.Coalesce(
                variable,
                Expression.Assign(variable, Expression.New(typeof(PhpArray))));
        }

        /// <summary>
        /// Gets expression representing <code>Array&lt;paramref name="element_type"&gt;.Empty()</code>.
        /// </summary>
        public static Expression EmptyArray(Type element_type)
        {
            return Expression.Call(typeof(Array), "Empty", new[] { element_type });
        }

        public static Expression NewPhpArray(Expression[] values, Expression ctx, Type classContext = null)
        {
            Expression arr;

            if (values.Length == 0)
            {
                // PhpArray.NewEmpty()
                return Expression.Call(typeof(PhpArray), "NewEmpty", Cache.Types.Empty); // CONSIDER: just PhpArray.Empty
            }
            else if (values.Any(IsArgumentUnpacking))
            {
                // TODO: values.Length == 1 && values[0] is PhpArray => return values[0], AddRestriction

                // unpacking
                arr = UnpackArgumentsToArray(null, values, ctx, classContext);
            }
            else
            {
                var items = new List<Expression>(values.Length);
                for (int i = 0; i < values.Length; i++)
                {
                    var expr = values[i];
                    if (i + 1 < values.Length && TryAppendRuntimeChain(ref expr, values[i + 1], ctx, classContext, false))
                    {
                        i++;
                    }

                    items.Add(ConvertExpression.BindToValue(expr));
                }

                arr = Expression.NewArrayInit(typeof(PhpValue), items);
            }

            // PhpArray.New( values[] )
            return Expression.Call(typeof(PhpArray), "New", Cache.Types.Empty, arr);
        }

        /// <summary>
        /// Determines whether given expression represents argument unpacking.
        /// </summary>
        internal static bool IsArgumentUnpacking(Expression arg)
        {
            var tinfo = arg.Type.GetTypeInfo();
            return tinfo.IsGenericType && tinfo.GetGenericTypeDefinition() == typeof(UnpackingParam<>);
        }

        public static Expression UnpackArgumentsToArray(MethodBase[] methods, Expression[] arguments, Expression ctx, Type classContext)
        {
            //if (arguments.Length == 1 && IsArgumentUnpacking(arguments[0]))
            //{
            //    // Template: (...$arg0)
            //    // TODO: if (arg0 is PhpArray) return arg0.ToArray();
            //}

            // create byrefs mask: (but mask of parameters passed by ref)
            ulong byrefs = 0uL;

            if (methods != null)
            {
                foreach (var m in methods)
                {
                    var ps = m.GetParameters();
                    var skip = ps.TakeWhile(IsImplicitParameter).Count();

                    for (int i = skip; i < ps.Length; i++)
                    {
                        if (ps[i].ParameterType == typeof(PhpAlias)) { byrefs |= (1uL << (i - skip)); }
                    }
                }
            }

            // List<PhpValue> list;
            var list_var = Expression.Variable(typeof(List<PhpValue>), "list");
            var exprs = new List<Expression>();
            var byrefs_expr = Expression.Constant(byrefs);

            // list = new List<PhpValue>( LENGTH )
            exprs.Add(Expression.Assign(list_var, Expression.New(list_var.Type.GetConstructor(Cache.Types.Int), Expression.Constant(arguments.Length))));

            // arguments.foreach(  Unpack(list, arg_i)  );
            for (int i = 0; i < arguments.Length; i++)
            {
                var expr = arguments[i];
                if (IsArgumentUnpacking(expr))
                {
                    Expression unpackexpr;

                    // Template: Operators.Unpack(list, arg, byrefs)

                    // Unpack(List<PhpValue> stack, PhpValue|PhpArray|Traversable argument, ulong byrefs)

                    expr = Expression.Field(expr, "Value"); // UnpackingParam<>.Value

                    if (i + 1 < arguments.Length && TryAppendRuntimeChain(ref expr, arguments[i + 1], ctx, classContext, false))
                    {
                        i++;
                    }

                    //if (typeof(PhpArray).IsAssignableFrom(arg_value.Type)) // TODO
                    //{

                    //}
                    //else if (typeof(Traversable).IsAssignableFrom(arg_value.Type)) // TODO
                    //{

                    //}
                    //else // PhpValue
                    {
                        unpackexpr = Expression.Call(
                            typeof(Operators), "Unpack", Cache.Types.Empty,
                            list_var, ConvertExpression.BindToValue(expr), byrefs_expr);
                    }

                    exprs.Add(unpackexpr);
                }
                else
                {
                    // list.Add((PhpValue)arg)
                    if (i + 1 < arguments.Length && TryAppendRuntimeChain(ref expr, arguments[i + 1], ctx, classContext, false))
                    {
                        i++;
                    }

                    exprs.Add(Expression.Call(list_var, "Add", Cache.Types.Empty, ConvertExpression.BindToValue(expr)));
                }
            }

            // return list.ToArray()
            exprs.Add(Expression.Call(list_var, "ToArray", Cache.Types.Empty));

            //
            return Expression.Block(new[] { list_var }, exprs);
        }

        /// <summary>
        /// Find field corresponding to object's runtime fields.
        /// </summary>
        public static FieldInfo LookupRuntimeFields(Type target)
        {
            return target.GetRuntimeFields().FirstOrDefault(ReflectionUtils.IsRuntimeFields);
        }

        public static Expression BindAccess(Expression expr, Expression ctx, AccessMask access, Expression rvalue)
        {
            if (access.EnsureObject())
            {
                if (expr.Type == typeof(PhpAlias))
                {
                    // ((PhpAlias)fld).EnsureObject()
                    expr = Expression.Call(expr, Cache.Operators.PhpAlias_EnsureObject);
                }
                else if (expr.Type == typeof(PhpValue))
                {
                    // ((PhpValue)fld).EnsureObject()
                    expr = Expression.Call(Cache.Operators.EnsureObject_PhpValueRef, expr);
                }
                else
                {
                    // getter // TODO: ensure it is not null
                    Debug.Assert(!expr.Type.GetTypeInfo().IsValueType);
                }
            }
            else if (access.EnsureArray())
            {
                if (expr.Type == typeof(PhpAlias))
                {
                    // ((PhpAlias)fld).EnsureArray()
                    expr = Expression.Call(expr, Cache.Operators.PhpAlias_EnsureArray);
                }
                else if (expr.Type == typeof(PhpValue))
                {
                    // ((PhpValue)fld).EnsureArray()
                    expr = Expression.Call(Cache.Operators.EnsureArray_PhpValueRef, expr);
                }
                else if (expr.Type == typeof(PhpArray))
                {
                    // (PhpArray)fld // TODO: ensure it is not null
                }
                else
                {
                    // Operators.EnsureArray( fld )
                    // TODO: string
                    expr = Expression.Call(Cache.Operators.Object_EnsureArray, expr);
                }
            }
            else if (access.EnsureAlias())
            {
                if (expr.Type == typeof(PhpAlias))
                {
                    // (PhpAlias)getter
                }
                else if (expr.Type == typeof(PhpValue))
                {
                    // ((PhpValue)fld).EnsureAlias()
                    expr = Expression.Call(Cache.Operators.EnsureAlias_PhpValueRef, expr);
                }
                else
                {
                    // getter // cannot read as reference
                }
            }
            else if (access.WriteAlias())
            {
                // write alias

                Debug.Assert(rvalue.Type == typeof(PhpAlias));
                rvalue = ConvertExpression.Bind(rvalue, typeof(PhpAlias), ctx);
                // TODO: PhpAlias.AddRef

                if (expr.Type == typeof(PhpAlias))
                {
                    // ok
                }
                else if (expr.Type == typeof(PhpValue))
                {
                    // fld = PhpValue.Create(alias)
                    rvalue = Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.PhpAlias), rvalue);
                }
                else
                {
                    // fld is not aliasable
                    Debug.Assert(false, "Cannot assign aliased value to field of type " + expr.Type.ToString());
                    rvalue = ConvertExpression.Bind(rvalue, expr.Type, ctx);
                }

                expr = Expression.Assign(expr, rvalue);
            }
            else if (access.Unset())
            {
                Debug.Assert(rvalue == null);

                expr = Expression.Assign(expr, ConvertExpression.BindDefault(expr.Type));
            }
            else if (access.Write())
            {
                // write by value

                if (expr.Type == typeof(PhpAlias))
                {
                    // Template: fld.Value = (PhpValue)value
                    expr = Expression.Assign(Expression.Field(expr, Cache.PhpAlias.Value), ConvertExpression.Bind(rvalue, typeof(PhpValue), ctx));
                }
                else if (expr.Type == typeof(PhpValue))
                {
                    // Template: Operators.SetValue(ref fld, (PhpValue)value)
                    expr = Expression.Call(Cache.Operators.SetValue_PhpValueRef_PhpValue, expr, ConvertExpression.Bind(rvalue, typeof(PhpValue), ctx));
                }
                else
                {
                    // Template: fld = value
                    // default behaviour by value to value
                    expr = Expression.Assign(expr, ConvertExpression.Bind(rvalue, expr.Type, ctx));
                }
            }
            else if (access.Isset())
            {
                if (expr.Type == typeof(PhpAlias))
                {
                    expr = Expression.Field(expr, Cache.PhpAlias.Value);
                }

                //
                if (expr.Type == typeof(PhpValue))
                {
                    // Template: Operators.IsSet( value )
                    expr = Expression.Call(Cache.Operators.IsSet_PhpValue, expr);
                }
                else if (IsNullable_T(expr.Type, out var T))
                {
                    // Template: Nullable.HasValue
                    expr = Expression.Property(expr, "HasValue");
                }
                else if (!expr.Type.GetTypeInfo().IsValueType)
                {
                    // Template: value != null
                    expr = Expression.ReferenceNotEqual(expr, Expression.Constant(null, typeof(object)));
                }
                else
                {
                    // if there is bound typed symbol, it is always set:
                    expr = Expression.Constant(true, typeof(bool));
                }
            }

            // Read, IsSet
            return expr;
        }

        static Expression BindArrayAccess(Expression arr, Expression key, Expression ctx, AccessMask access, Expression rvalue)
        {
            Debug.Assert(key.Type == typeof(IntStringKey));

            if (access.EnsureObject())
            {
                // (arr ?? arr = []).EnsureItemObject(key)
                return Expression.Call(
                    EnsureNotNullPhpArray(arr),
                    Cache.Operators.PhpArray_EnsureItemObject, key);
            }
            else if (access.EnsureArray())
            {
                // (arr ?? arr = []).EnsureItemArray(key)
                return Expression.Call(
                    EnsureNotNullPhpArray(arr),
                    Cache.Operators.PhpArray_EnsureItemArray, key);
            }
            else if (access.EnsureAlias())
            {
                // (arr ?? arr = []).EnsureItemAlias(key)
                return Expression.Call(
                    EnsureNotNullPhpArray(arr),
                    Cache.Operators.PhpArray_EnsureItemAlias, key);
            }
            else if (access.WriteAlias())
            {
                Debug.Assert(rvalue.Type == typeof(PhpAlias));
                rvalue = ConvertExpression.Bind(rvalue, typeof(PhpAlias), ctx);

                // (arr ?? arr = []).SetItemAlias(key, value)
                return Expression.Call(
                    EnsureNotNullPhpArray(arr),
                    Cache.Operators.PhpArray_SetItemAlias, key, rvalue);
            }
            else if (access.Unset())
            {
                Debug.Assert(rvalue == null);

                // remove key

                // arr.RemoveKey(name)
                // TODO: if (arr != null)
                return Expression.Call(arr, Cache.Operators.PhpArray_Remove, key);
            }
            else if (access.Write())
            {
                rvalue = ConvertExpression.Bind(rvalue, typeof(PhpValue), ctx);

                return Expression.Call(
                    EnsureNotNullPhpArray(arr),
                    Cache.Operators.PhpArray_SetItemValue, key, rvalue);
            }
            else if (access.Isset())
            {
                // Template: arr != null && IsSet(arr[key])
                return Expression.AndAlso(
                    Expression.ReferenceNotEqual(arr, Expression.Constant(null, typeof(object))), // arr != null
                    Expression.Call(Cache.Operators.IsSet_PhpValue, Expression.Call(arr, Cache.Operators.PhpArray_GetItemValue, key))); // isset( arr[key] )
            }
            else
            {
                // read
                // TODO: (arr != null) ? arr[key] : (quiet ? void : ERROR)
                return Expression.Call(arr, Cache.Operators.PhpArray_GetItemValue, key);
            }
        }

        public static PhpMethodInfo FindMagicMethod(PhpTypeInfo type, TypeMethods.MagicMethods magic)
        {
            return (PhpMethodInfo)type.RuntimeMethods[magic];
        }

        static Expression BindMagicMethod(PhpTypeInfo type, Type classCtx, Expression target, Expression ctx, TypeMethods.MagicMethods magic, string field, Expression rvalue = null)
        {
            var m = FindMagicMethod(type, magic);
            if (m != null)
            {
                var methods = m.Methods.Length == 1
                    ? (m.Methods[0].IsVisible(classCtx) ? m.Methods : Array.Empty<MethodInfo>())    // optimization for array[1]
                    : m.Methods.Where(x => x.IsVisible(classCtx)).ToArray();

                if (methods.Length != 0)
                {
                    switch (magic)
                    {
                        case TypeMethods.MagicMethods.__set:
                            // __set(name, value)
                            return OverloadBinder.BindOverloadCall(typeof(void), target, methods, ctx, new Expression[] { Expression.Constant(field), rvalue }, false);

                        default:
                            // __get(name), __unset(name), __isset(name)
                            return OverloadBinder.BindOverloadCall(methods[0].ReturnType, target, methods, ctx, new Expression[] { Expression.Constant(field) }, false);
                    }
                }
                else
                {
                    // TODO: ERR inaccessible
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves property with respect to staticness and visibility.
        /// </summary>
        /// <param name="type">Property receiver.</param>
        /// <param name="classCtx">Current class context (visibility).</param>
        /// <param name="static">Whether to lookup static properties.</param>
        /// <param name="name">Property name.</param>
        /// <param name="property">Set to the resolved property. Can be set even the methods returns <c>false</c> which means the property is there but inaccessible.</param>
        public static bool TryResolveDeclaredProperty(PhpTypeInfo type, Type classCtx, bool @static, string name, out PhpPropertyInfo property)
        {
            property = null;

            for (var t = type; t != null; t = t.BaseType)
            {
                var p = t.DeclaredFields.TryGetPhpProperty(name);
                if (p != null && p.IsStatic == @static)
                {
                    property = p;

                    if (p.IsVisible(classCtx))
                    {
                        return true;
                    }
                }
            }

            //
            return false;
        }

        static Expression/*!*/BindInvalidPropertyAccess(string className, string propName, Type classCtx, PhpPropertyInfo candidate)
        {
            // This is an exception since we are accessing a CLR object for sure.
            // A PHP object would have runtime fields.

            var exceptionClass = candidate != null
                ? typeof(FieldAccessException)
                : typeof(MissingFieldException);

            var classCtxName = classCtx != null
                ? classCtx.GetPhpTypeInfo().Name    // PHP class name
                : PhpStackFrame.GlobalCodeName;

            var message = candidate == null
                ? string.Format(Resources.ErrResources.undefined_property_accessed, className, propName)
                : (candidate.IsReadOnly && candidate.IsVisible(classCtx))
                    ? string.Format(Resources.ErrResources.readonly_property_written, className, propName)
                    : candidate.IsPrivate
                        ? string.Format(Resources.ErrResources.private_property_accessed, className, propName, classCtxName)
                        : string.Format(Resources.ErrResources.protected_property_accessed, className, propName, classCtxName);

            // Template: throw new exception( message)
            return Expression.Throw(Expression.New(exceptionClass.GetConstructor(Cache.Types.String), Expression.Constant(message)));
        }

        // NOTE: will be replaced with "IRuntimeChain"
        public static Expression BindField(PhpTypeInfo type, Type classCtx, Expression target, string field, Expression ctx, AccessMask access, Expression rvalue)
        {
            if (access.Write() != (rvalue != null))
            {
                throw new ArgumentException();
            }

            // lookup a declared field
            if (TryResolveDeclaredProperty(type, classCtx, target == null, field, out var prop))
            {
                if (access.Write() && prop.IsReadOnly)
                {
                    return BindInvalidPropertyAccess(type.Name, field, classCtx, prop);
                }

                return BindAccess(prop.Bind(ctx, target), ctx, access, rvalue);
            }

            //
            // runtime fields & magic methods
            // only applies to instance fields
            //

            if (target != null && type.RuntimeFieldsHolder != null)   // we don't handle magic methods without the runtime fields
            {
                var runtimeflds = Expression.Field(target, type.RuntimeFieldsHolder);   // Template: target->__runtime_fields
                var fieldkey = Expression.Constant(new IntStringKey(field));            // Template: IntStringKey(field)
                var resultvar = Expression.Variable(Cache.Types.PhpValue, "result");    // Template: PhpValue result;

                // Template: runtimeflds != null && runtimeflds.TryGetValue(field, out result)
                var trygetfield = Expression.AndAlso(Expression.ReferenceNotEqual(runtimeflds, Expression.Constant(null)), Expression.Call(runtimeflds, Cache.Operators.PhpArray_TryGetValue, fieldkey, resultvar));

                // Template: runtimeflds != null && runtimeflds.ContainsKey(field)
                var containsfield = Expression.AndAlso(Expression.ReferenceNotEqual(runtimeflds, Expression.Constant(null)), Expression.Call(runtimeflds, Cache.Operators.PhpArray_ContainsKey, fieldkey));

                Expression result;

                //
                if (access.EnsureObject())
                {
                    // (object)target->field->

                    // Template: runtimeflds.EnsureObject(key)
                    result = Expression.Call(EnsureNotNullPhpArray(runtimeflds), Cache.Operators.PhpArray_EnsureItemObject, fieldkey);

                    var __get = BindMagicMethod(type, classCtx, target, ctx, TypeMethods.MagicMethods.__get, field, null);
                    if (__get != null)
                    {
                        // Template: runtimeflds.Contains(key) ? runtimeflds.EnsureObject(key) : ( __get(key) ?? runtimeflds.EnsureObject(key))
                        return Expression.Condition(containsfield,
                                Expression.Call(runtimeflds, Cache.Operators.PhpArray_EnsureItemObject, fieldkey),
                                InvokeHandler(ctx, target, field, __get, access, result, Cache.Types.Object[0]));
                    }
                    else
                    {
                        return result;
                    }
                }
                else if (access.EnsureArray())
                {
                    // (IPhpArray)target->field[] =
                    result = Expression.Call(EnsureNotNullPhpArray(runtimeflds), Cache.Operators.PhpArray_EnsureItemArray, fieldkey);

                    var __get = BindMagicMethod(type, classCtx, target, ctx, TypeMethods.MagicMethods.__get, field, null);
                    if (__get != null)
                    {
                        // Template: runtimeflds.Contains(key) ? runtimeflds.EnsureArray(key) : ( __get(key) ?? runtimeflds.EnsureArray(key))
                        return Expression.Condition(containsfield,
                                Expression.Call(runtimeflds, Cache.Operators.PhpArray_EnsureItemArray, fieldkey),
                                InvokeHandler(ctx, target, field, __get, access, result, typeof(IPhpArray)));
                    }
                    else
                    {
                        // runtimeflds.EnsureItemArray(key)
                        return result;
                    }
                }
                else if (access.EnsureAlias())
                {
                    // (PhpAlias)&target->field

                    result = Expression.Call(EnsureNotNullPhpArray(runtimeflds), Cache.Operators.PhpArray_EnsureItemAlias, fieldkey);

                    var __get = BindMagicMethod(type, classCtx, target, ctx, TypeMethods.MagicMethods.__get, field, null);
                    if (__get != null)
                    {
                        // Template: runtimeflds.Contains(key) ? runtimeflds.EnsureItemAlias(key) : ( __get(key) ?? runtimeflds.EnsureItemAlias(key))
                        return Expression.Condition(containsfield,
                                Expression.Call(runtimeflds, Cache.Operators.PhpArray_EnsureItemAlias, fieldkey),
                                InvokeHandler(ctx, target, field, __get, access, result, Cache.Types.PhpAlias[0]));
                    }
                    else
                    {
                        // runtimeflds.EnsureItemAlias(key)
                        return result;
                    }
                }
                else if (access.Unset())
                {
                    // unset(target->field)
                    // Template: if (runtimeflds == null || !runtimeflds.RemoveKey(key)) __unset(key)

                    var removekey = Expression.Call(runtimeflds, Cache.Operators.PhpArray_Remove, fieldkey);
                    Debug.Assert(removekey.Type == typeof(bool));

                    var __unset = BindMagicMethod(type, classCtx, target, ctx, TypeMethods.MagicMethods.__unset, field, null);
                    if (__unset != null)
                    {
                        return Expression.IfThen(
                            Expression.OrElse(Expression.ReferenceEqual(runtimeflds, Expression.Constant(null)), Expression.IsFalse(removekey)),
                            InvokeHandler(ctx, target, field, __unset, access, Expression.Block(), typeof(void)));
                    }
                    else
                    {
                        // if (runtimeflds != null) runtimeflds.RemoveKey(key)
                        return Expression.IfThen(
                            Expression.ReferenceNotEqual(runtimeflds, Expression.Constant(null)),
                            removekey);
                    }
                }
                else if (access.Write())
                {
                    var __set = BindMagicMethod(type, classCtx, target, ctx, TypeMethods.MagicMethods.__set, field, rvalue);

                    if (access.WriteAlias())
                    {
                        // target->field = (PhpAlias)&rvalue
                        Debug.Assert(rvalue.Type == typeof(PhpAlias));
                        rvalue = ConvertExpression.Bind(rvalue, typeof(PhpAlias), ctx);

                        // EnsureNotNull(runtimeflds).SetItemAlias(key, rvalue)
                        result = Expression.Call(EnsureNotNullPhpArray(runtimeflds), Cache.Operators.PhpArray_SetItemAlias, fieldkey, rvalue);

                        if (__set != null)
                        {
                            // if (ContainsKey(key)) ? runtimeflds.SetItemAlias(rvalue) : (__set(key, rvalue) ?? runtimeflds.SetItemAlias(key, rvalue)
                            return Expression.Condition(containsfield,
                                    Expression.Call(runtimeflds, Cache.Operators.PhpArray_SetItemAlias, fieldkey, rvalue),
                                    InvokeHandler(ctx, target, field, __set, access, result, typeof(void)));
                        }
                        else
                        {
                            return result;
                        }
                    }
                    else
                    {
                        // target->field = rvalue
                        rvalue = ConvertExpression.Bind(rvalue, typeof(PhpValue), ctx);

                        /* Template:
                         * return runtimeflds != null && runtimeflds.ContainsKey(field)
                         *   ? runtimeflds.SetItemValue(key, rvalue)
                         *   : (__set(field, value) ?? runtimeflds.SetItemValue(key, value))
                         */

                        result = Expression.Call(EnsureNotNullPhpArray(runtimeflds), Cache.Operators.PhpArray_SetItemValue, fieldkey, rvalue);

                        if (__set != null)
                        {
                            return Expression.Condition(containsfield,
                                Expression.Call(runtimeflds, Cache.Operators.PhpArray_SetItemValue, fieldkey, rvalue),
                                InvokeHandler(ctx, target, field, __set, access, result, typeof(void)));
                        }
                        else
                        {
                            return result;
                        }
                    }
                }
                else if (access.Isset())
                {
                    // isset(target->field)

                    var __isset =
                        BindMagicMethod(type, classCtx, target, ctx, TypeMethods.MagicMethods.__isset, field, null) ??
                        BindMagicMethod(type, classCtx, target, ctx, TypeMethods.MagicMethods.__get, field, null);

                    // Template: TryGetField(result) ? isset(result) : (bool)(__isset(key)??NULL)
                    result = Expression.Condition(
                            trygetfield,
                            Expression.Call(Cache.Operators.IsSet_PhpValue, resultvar),
                            ConvertExpression.BindToBool(InvokeHandler(ctx, target, field, __isset, access)));
                }
                else
                {
                    // = target->field

                    // Template: Operators.GetRuntimeProperty(ctx, PhpTypeInfo, instance, propertyName)
                    return Expression.Call(Cache.Operators.RuntimePropertyGetValue.Method, ctx, Expression.Constant(type), target, Expression.Constant(field));
                }

                //
                return Expression.Block(result.Type, new[] { resultvar }, result);
            }

            // TODO: IDynamicMetaObject

            // field cannot be found:
            if (access.Isset())
            {
                // FALSE
                return Expression.Constant(false);
            }

            //
            return BindInvalidPropertyAccess(type.Name, field, classCtx, prop);
        }

        public static Expression BindClassConstant(PhpTypeInfo type, Type classCtx, string constName, Expression ctx)
        {
            var p = type.GetDeclaredConstant(constName);
            if (p != null && p.IsVisible(classCtx))
            {
                return p.Bind(ctx, null);
            }

            //
            return null;
        }

        /// <summary>
        /// Binds recursion check for property magic method.
        /// </summary>
        static Expression InvokeHandler(Expression ctx, Expression target, string field, Expression getter, AccessMask access, Expression @default = null, Type resultType = null)
        {
            // default
            resultType = resultType ?? Cache.Types.PhpValue;
            @default = @default ?? Expression.Field(null, Cache.Properties.PhpValue_Null);   // TODO: ERR field not found
            @default = ConvertExpression.Bind(@default, resultType, ctx);

            if (getter == null)
            {
                return @default;
            }
            else
            {
                /* Template:
                 * var token;
                 * try {
                 *   return (token = new Context.RecursionCheckToken(_ctx, target, access))).IsInRecursion)
                 *     ? default
                 *     : getter;
                 * } finally {
                 *   token.Dispose();
                 * }
                 */

                // recursion prevention key ~ do not invoke getter twice for the same field
                int subkey1 = access.Write() ? 1 : access.Unset() ? 2 : access.Isset() ? 3 : 4;
                int subkey = field.GetHashCode() ^ (1 << subkey1);

                // Template: RecursionCheckToken token;
                var tokenvar = Expression.Variable(typeof(Context.RecursionCheckToken), "token");

                // Template: token = new RecursionCheckToken(_ctx, (object)target, (int)subkey))
                var tokenassign = Expression.Assign(tokenvar, Expression.New(Cache.RecursionCheckToken.ctor_ctx_object_int,
                    ctx, Expression.Convert(target, Cache.Types.Object[0]), Expression.Constant(subkey)));

                // bind getter access
                if (access.EnsureAlias() || access.EnsureArray() || access.EnsureObject())
                {
                    getter = BindAccess(getter, ctx, access, rvalue: null);
                }

                getter = ConvertExpression.Bind(getter, resultType, ctx);

                //
                return Expression.Block(resultType,
                    new[] { tokenvar },
                    Expression.TryFinally(
                        Expression.Condition(Expression.Property(tokenassign, Cache.RecursionCheckToken.IsInRecursion),
                            @default,
                            getter),
                        Expression.Call(tokenvar, Cache.RecursionCheckToken.Dispose)
                    ));
            }
        }

        public static Expression BindToCall(Expression instance, MethodBase method, Expression ctx, OverloadBinder.ArgumentsBinder args, bool isStaticCallSyntax, object lateStaticType)
        {
            Debug.Assert(method is MethodInfo || method is ConstructorInfo);

            var ps = method.GetParameters();
            var boundargs = new Expression[ps.Length];

            int argi = 0;

            for (int i = 0; i < ps.Length; i++)
            {
                var p = ps[i];
                if (argi == 0)
                {
                    if (p.IsContextParameter())
                    {
                        boundargs[i] = ctx;
                        continue;
                    }
                    else if (p.IsImportValueParameter(out var value))
                    {
                        switch (value)
                        {
                            case ImportValueAttribute.ValueSpec.CallerScript:
                                // we don't have this info
                                throw new NotSupportedException("Pass <CallerScript> dynamically.");

                            case ImportValueAttribute.ValueSpec.CallerArgs:
                                // we don't have this info
                                throw new NotSupportedException("Pass <CallerArgs> dynamically.");    // TODO: empty array & report warning

                            case ImportValueAttribute.ValueSpec.Locals:
                                // pass NULL value if we don't have the locals
                                boundargs[i] = Expression.Default(p.ParameterType);

                                // TODO: report it might get wrong
                                // TODO: if we create IPhpCallback in compile-time, we know the routine needs the locals, ...
                                break;

                            case ImportValueAttribute.ValueSpec.This:
                                // $this is unknown, get NULL
                                boundargs[i] = Expression.Default(p.ParameterType);
                                break;

                            case ImportValueAttribute.ValueSpec.CallerClass:
                                // TODO: pass classctx from the callsite
                                Debug.WriteLine("TODO: pass classctx from the callsite");
                                boundargs[i] = Expression.Default(p.ParameterType);
                                break;

                            case ImportValueAttribute.ValueSpec.CallerStaticClass:
                                throw new NotSupportedException("ImportCallerStaticClassAttribute dynamically."); // we don't know current late static bound type
                        }

                        continue;
                    }
                    else if (p.IsDummyFieldsOnlyCtor())
                    {
                        boundargs[i] = Expression.Default(p.ParameterType);
                    }
                    else if (p.IsLateStaticParameter())
                    {
                        if (lateStaticType != null)
                        {
                            boundargs[i] = lateStaticType switch
                            {
                                // Template: PhpTypeInfoExtension.GetPhpTypeInfo<lateStaticType>()
                                PhpTypeInfo phpt => Expression.Call(null,
                                    typeof(PhpTypeInfoExtension).GetMethod("GetPhpTypeInfo", Cache.Types.Empty).MakeGenericMethod(phpt.Type)),

                                // Template: (PhpTypeInfo)$arg2.Value
                                Expression expr => expr,

                                // unk
                                _ => throw new ArgumentException(),
                            }; 
                        }
                        else
                        {
                            throw new InvalidOperationException("static context not available.");
                        }

                        continue;
                    }
                    else if (p.IsClosureParameter())
                    {
                        boundargs[i] = args.BindArgument(argi, p);
                        argi++;

                        continue;
                    }
                }

                // regular parameter:

                if (i == ps.Length - 1 && p.IsParamsParameter())
                {
                    var element_type = p.ParameterType.GetElementType();
                    boundargs[i] = args.BindParams(argi, element_type);
                    break;
                }
                else
                {
                    boundargs[i] = args.BindArgument(argi, p);
                }

                //
                argi++;
            }

            //
            Debug.Assert(boundargs.All(x => x != null));

            //
            if (method.IsStatic)
            {
                instance = null;
            }

            //
            if (method.IsConstructor)
            {
                return Expression.New((ConstructorInfo)method, boundargs);
            }

            if (HasToBeCalledNonVirtually(instance, method, isStaticCallSyntax))
            {
                // Ugly hack here,
                // we NEED to call the method nonvirtually, but LambdaCompiler emits .callvirt always and there is no way how to change it (except we can emit all the stuff by ourselfs).
                // We use DynamicMethod to emit .call inside, and use its MethodInfo which is static.
                // LambdaCompiler generates .call to static DynamicMethod which calls our method via .call as well,
                // after all the inlining, there should be no overhead.

                instance = Expression.Convert(instance, method.DeclaringType);
                method = WrapInstanceMethodToStatic((MethodInfo)method);

                //
                var newargs = new Expression[boundargs.Length + 1];
                newargs[0] = instance;
                Array.Copy(boundargs, 0, newargs, 1, boundargs.Length);
                boundargs = newargs;
                instance = null;
            }

            if (instance != null && !method.DeclaringType.IsAssignableFrom(instance.Type))
            {
                instance = Expression.Convert(instance, method.DeclaringType);
            }

            // NOTE: instead of "HasToBeCalledNonVirtually" magic above, it would be great to just use ".call" opcode always (as of now Linq cannot do that)

            //
            return Expression.Call(instance, (MethodInfo)method, boundargs);
        }

        /// <summary>
        /// Determines whether we has to use ".call" opcode explicitly.
        /// </summary>
        static bool HasToBeCalledNonVirtually(Expression instance, MethodBase method, bool isStaticCallSyntax)
        {
            if (instance == null || !method.IsVirtual)
            {
                // method is static or non-virtual
                // .call is emitted by Linq implicitly:
                return false;
            }

            if (method.IsAbstract)
            {
                // method is abstract,
                // .callvirt is fine:
                return false;
            }

            if (method.DeclaringType.IsSealed || method.IsFinal)
            {
                return false;
            }

            if (isStaticCallSyntax)
            {
                //if (instance.Type == method.DeclaringType)
                //{
                //    // corresponding DynamicMetaObject is restricted to {instance.Type} (which is the callsite's runtime type),
                //    // .callvirt method within this DynamicMetaObject refer to {method} and nothing else,
                //    // .callvirt is safe:
                //    return false;
                //}

                //for (var t = instance.Type; t != method.DeclaringType && t != null; t = t.BaseType)
                //{
                //    var routine = t.GetPhpTypeInfo().RuntimeMethods[method.Name];
                //    if (routine != null) // always true
                //    {
                //        var possible_overrides = routine.Methods;
                //        for (int i = 0; i < possible_overrides.Length; i++)
                //        {
                //            var m = possible_overrides[i];
                //            if (m != method && m.IsVirtual && m.DeclaringType.IsSubclassOf(method.DeclaringType))
                //            {
                //                return true;
                //            }
                //        }
                //    }
                //}
                return true;
            }

            // .callvirt is safe,
            // we did not find anything that overrides {method} within current DynamicMetaObject
            return false;
        }

        /// <summary>
        /// Builds MethodInfo as a static method calling an instance method nonvirtually inside.
        /// </summary>
        static MethodInfo WrapInstanceMethodToStatic(MethodInfo method)
        {
            if (method.IsStatic)
            {
                return method;
            }

            var ps = method.GetParameters();

            // dynamic method parameters
            var dtypes = new Type[ps.Length + 1];
            dtypes[0] = method.DeclaringType;   // target
            for (int i = 0; i < ps.Length; i++)
            {
                dtypes[i + 1] = ps[i].ParameterType;    // parameter_i
            }

            // dynamic method
            var d = new DynamicMethod("<>." + method.Name, method.ReturnType, dtypes, method.DeclaringType, true);

            // return ARG0.{method}(ARG1, ..., ARGN)
            var il = d.GetILGenerator();

            for (int i = 0; i < dtypes.Length; i++)
            {
                il.Emit(OpCodes.Ldarg, i);
            }
            il.EmitCall(OpCodes.Call, method, null);    // .call instead of .callvirt
            il.Emit(OpCodes.Ret);

            //
            return d;
        }

        public static PhpCallable BindToPhpCallable(MethodBase[] targets)
        {
            Debug.Assert(targets.All(t => t.IsStatic), "Only static methods can be bound to PhpCallable delegate.");

            // (Context ctx, PhpValue[] arguments)
            var ps = new ParameterExpression[] { Expression.Parameter(typeof(Context), "ctx"), Expression.Parameter(typeof(PhpValue[]), "argv") };

            // invoke targets
            var invocation = OverloadBinder.BindOverloadCall(typeof(PhpValue), null, targets, ps[0], ps[1], true);
            Debug.Assert(invocation.Type == typeof(PhpValue));

            // compile & create delegate
            var lambda = Expression.Lambda<PhpCallable>(invocation, targets[0].Name + "#" + targets.Length, true, ps);
            return lambda.Compile();
        }

        public static PhpInvokable BindToPhpInvokable(MethodInfo[] methods, PhpTypeInfo lateStaticType = null)
        {
            // (Context ctx, object target, PhpValue[] arguments)
            var ps = new ParameterExpression[] {
                Expression.Parameter(typeof(Context), "ctx"),
                Expression.Parameter(typeof(object), "target"),
                Expression.Parameter(typeof(PhpValue[]), "argv") };

            // invoke targets
            var invocation = OverloadBinder.BindOverloadCall(typeof(PhpValue), ps[1], methods, ps[0], ps[2], true, lateStaticType);
            Debug.Assert(invocation.Type == typeof(PhpValue));

            // compile & create delegate
            var lambda = Expression.Lambda<PhpInvokable>(invocation, methods[0].Name + "#" + methods.Length, true, ps);
            return lambda.Compile();
        }

        public static TObjectCreator BindToCreator(Type type, ConstructorInfo[] ctors)
        {
            Debug.Assert(ctors.All(ctor => ctor?.DeclaringType == type));

            // (Context ctx, PhpValue[] arguments)
            var ps = new ParameterExpression[] { Expression.Parameter(typeof(Context), "ctx"), Expression.Parameter(typeof(PhpValue[]), "argv") };

            if (ctors.Length != 0)
            {
                // invoke targets
                var invocation = OverloadBinder.BindOverloadCall(type, null, ctors, ps[0], ps[1], isStaticCallSyntax: false);
                Debug.Assert(invocation.Type == type);

                // compile & create delegate
                var lambda = Expression.Lambda<TObjectCreator>(invocation, ctors[0].Name + "#" + ctors.Length, true, ps);
                return lambda.Compile();
            }
            else
            {
                // TODO: lambda {error; NULL;}
                throw new ArgumentException("No constructor accessible for " + type.FullName);
            }
        }
    }
}
