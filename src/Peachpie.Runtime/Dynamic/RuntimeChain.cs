using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Text;
using Pchp.CodeAnalysis.Semantics;
using Pchp.Core.Reflection;

namespace Pchp.Core.Dynamic
{
    /// <summary>
    /// Describes what operation is being performed within the <see cref="IRuntimeChain"/>.
    /// </summary>
    public enum RuntimeChainOperation : sbyte
    {
        End = 0,

        Value,   // local variable or evaluated value

        Property,   // resolving property

        ArrayItem,  // resolving array item
    }

    /// <summary>
    /// Describes an operation of accessing a variable/property/item in runtime.
    /// This operation will be defined by compiler and value resolved by runtime dynamically.
    /// </summary>
    public interface IRuntimeChain
    {
        /// <summary>
        /// Gets the operation type.
        /// </summary>
        RuntimeChainOperation Operation { get; }

        PhpValue GetValue(PhpValue value, Context ctx, Type classContext);

        PhpAlias GetAlias(ref PhpValue value, Context ctx, Type classContext);

        // TODO: SetValue()

        ///// <summary>
        ///// Gets expression providing <c>read</c> operation.
        ///// The result type of the expression may be anything and it has to be converted to the target type.
        ///// </summary>
        //DynamicMetaObject BindRead(DynamicMetaObject receiver, Expression self, Type classContext, DynamicMetaObject context);

        ///// <summary>
        ///// Gets expression providing <c>readref</c> operation.
        ///// The result type of the expression is <see cref="PhpAlias"/>.
        ///// </summary>
        //DynamicMetaObject BindReadRef(DynamicMetaObject receiver, Expression self, Type classContext, DynamicMetaObject context);
    }
}

namespace Pchp.Core.Dynamic.RuntimeChain
{
    [DebuggerDisplay("$${Next}")]
    public struct Value<TNext> : IRuntimeChain where TNext : IRuntimeChain
    {
        public TNext Next; // can be "ChainEnd"

        public RuntimeChainOperation Operation => RuntimeChainOperation.Value;

        public PhpValue GetValue(PhpValue value, Context ctx, Type classContext)
        {
            return Next.GetValue(value, ctx, classContext);
        }

        public PhpAlias GetAlias(ref PhpValue value, Context ctx, Type classContext)
        {
            return Next.GetAlias(ref Ensure(ref value), ctx, classContext);
        }

        ref PhpValue Ensure(ref PhpValue value)
        {
            Debug.Assert(Next.Operation != RuntimeChainOperation.End);

            switch (Next.Operation)
            {
                case RuntimeChainOperation.Value:
                    break;

                case RuntimeChainOperation.Property:
                    value = PhpValue.FromClass(PhpValue.EnsureObject(ref value));
                    break;

                case RuntimeChainOperation.ArrayItem:
                    value = PhpValue.Create(PhpValue.EnsureArray(ref value));
                    break;

                default:
                    throw new InvalidOperationException();
            }

            //
            return ref value;
        }

        //public DynamicMetaObject BindRead(DynamicMetaObject receiver, Expression self, Type classContext, DynamicMetaObject context)
        //{
        //    var result = receiver;

        //    // load & unwrap <receiver>
        //    if (receiver.Expression.Type == Cache.Types.IndirectLocal)
        //    {
        //        // Template: <receiver>.Value
        //        result = new DynamicMetaObject(
        //            Expression.Property(receiver.Expression, Cache.IndirectLocal.Value),
        //            BindingRestrictions.Empty,
        //            ((IndirectLocal)receiver.Value).Value);
        //    }
        //    else if (receiver.Expression.Type == typeof(PhpAlias))
        //    {
        //        // Template: <receiver>.Value
        //        result = new DynamicMetaObject(
        //            Expression.Field(receiver.Expression, Cache.PhpAlias.Value),
        //            BindingRestrictions.Empty,
        //            ((PhpAlias)receiver.Value).Value);
        //    }

        //    // chain:
        //    return (Next.Operation != RuntimeChainOperation.End)
        //        ? Next.BindRead(result, Expression.Field(self, "Next"), classContext, context)
        //        : result;
        //}

        //public DynamicMetaObject BindReadRef(DynamicMetaObject receiver, Expression self, Type classContext, DynamicMetaObject context)
        //{
        //    throw new NotImplementedException();
        //}
    }

    [DebuggerDisplay("->{Name,nq}{Next}")]
    public struct Property<TNext> : IRuntimeChain where TNext : IRuntimeChain
    {
        public TNext Next; // can be "ChainEnd"

        public string Name;

        public RuntimeChainOperation Operation => RuntimeChainOperation.Property;

        public PhpValue GetValue(PhpValue value, Context ctx, Type classContext)
        {
            var receiver = value.AsObject();
            if (receiver != null)
            {
                // CONSIDER: value = Operators.PropertyGetValue( .. )

                var t = receiver.GetPhpTypeInfo();
                if (BinderHelpers.TryResolveDeclaredProperty(t, classContext, false, Name, out var p))
                {
                    value = p.GetValue(ctx, receiver);
                }
                else
                {
                    value = Operators.RuntimePropertyGetValue(ctx, t, receiver, propertyName: Name);
                }

                return Next.GetValue(value, ctx, classContext);
            }
            else
            {
                PhpException.VariableMisusedAsObject(value, false);
                return PhpValue.Null;
            }
        }

        public PhpAlias GetAlias(ref PhpValue value, Context ctx, Type classContext)
        {
            var receiver = PhpValue.EnsureObject(ref value);
            var t = receiver.GetPhpTypeInfo();

            PhpValue tmp;

            if (BinderHelpers.TryResolveDeclaredProperty(t, classContext, false, Name, out var prop))
            {
                switch (Next.Operation)
                {
                    case RuntimeChainOperation.Property:
                        tmp = PhpValue.FromClass(prop.EnsureObject(ctx, receiver));
                        break;

                    case RuntimeChainOperation.ArrayItem:
                        tmp = PhpValue.Create(prop.EnsureArray(ctx, receiver));
                        break;

                    case RuntimeChainOperation.End:
                        return prop.EnsureAlias(ctx, receiver);

                    default:
                        throw new InvalidOperationException();
                }
            }
            else
            {
                // Template: runtimeflds.Contains(key) ? runtimeflds.EnsureObject(key) : ( __get(key) ?? runtimeflds.EnsureObject(key))

                var runtimeFields = t.GetRuntimeFields(receiver);
                if (runtimeFields == null || !runtimeFields.Contains(Name))
                {
                    //
                    var __get = t.RuntimeMethods[TypeMethods.MagicMethods.__get];
                    if (__get != null)
                    {
                        // NOTE: magic methods must have public visibility, therefore the visibility check is unnecessary

                        // int subkey1 = access.Write() ? 1 : access.Unset() ? 2 : access.Isset() ? 3 : 4;
                        int subkey = Name.GetHashCode() ^ (1 << 4/*subkey1*/);

                        using (var token = new Context.RecursionCheckToken(ctx, receiver, subkey))
                        {
                            if (!token.IsInRecursion)
                            {
                                tmp = __get.Invoke(ctx, receiver, Name);
                                return Next.GetAlias(ref tmp, ctx, classContext);
                            }
                        }
                    }
                }

                if (runtimeFields == null)
                {
                    runtimeFields = t.EnsureRuntimeFields(receiver);
                }

                //

                switch (Next.Operation)
                {
                    case RuntimeChainOperation.Property:
                        tmp = PhpValue.FromClass(runtimeFields.EnsureItemObject(Name));
                        break;

                    case RuntimeChainOperation.ArrayItem:
                        tmp = PhpValue.Create(runtimeFields.EnsureItemArray(Name));
                        break;

                    case RuntimeChainOperation.End:
                        return runtimeFields.EnsureItemAlias(Name);

                    default:
                        throw new InvalidOperationException();
                }
            }

            // chain:
            return Next.GetAlias(ref tmp, ctx, classContext);
        }

        ///// <summary>
        ///// Already loaded value if any.
        ///// </summary>
        //internal PhpValue Value;

        //public DynamicMetaObject BindRead(DynamicMetaObject receiver, Expression self, Type classContext, DynamicMetaObject context)
        //{
        //    if (BinderHelpers.TryTargetAsObject(receiver, out var instance))
        //    {
        //        Expression expr;
        //        object value;
        //        var restrictions = receiver.Restrictions.Merge(instance.Restrictions);

        //        var receiver_type = instance.RuntimeType.GetPhpTypeInfo();

        //        var p = BinderHelpers.ResolveDeclaredProperty(receiver_type, classContext, @static: false, name: Name);
        //        if (p != null)
        //        {
        //            // field is simple instance field/property:
        //            // Template: <receiver>.<field>
        //            expr = p.Bind(context.Expression, instance.Expression);
        //            value = p.GetValue(null, instance.Value);
        //        }
        //        else
        //        {
        //            // resolve the value and
        //            // remember it so it won't get evaluated twice!
        //            this.Value = Operators.RuntimePropertyGetValue((Context)context.Value, receiver_type, instance.Value, Name);

        //            // property is more complex (runtime field or magic method has to be called)
        //            // Cache the value, since it might be used for further restrictions and evaluation itself.

        //            // Template: self.Value.IsDefault ? (self.Value = <RuntimePropertyGetValue>) : self.Value;
        //            var self_value = Expression.Field(self, "Value");
        //            expr = Expression.Condition(
        //                test: Expression.Property(self_value, Cache.Properties.PhpValue_IsDefault), // self.Value.IsDefault
        //                ifTrue: Expression.Assign(self_value, Expression.Call(Cache.Operators.RuntimePropertyGetValue.Method, context.Expression, Expression.Constant(receiver_type), instance.Expression, Expression.Constant(Name))),
        //                ifFalse: self_value);
        //            value = (object)this.Value;
        //        }

        //        //
        //        //if (NameIsDynamic)
        //        {
        //            // Name == self.<Name>
        //            restrictions = BindingRestrictions.GetExpressionRestriction(
        //                    Expression.Equal(Expression.Constant(Name), Expression.Field(self, "Name")))
        //                .Merge(restrictions);
        //        }

        //        var result = new DynamicMetaObject(expr, restrictions, value);

        //        if (Next.Operation != RuntimeChainOperation.End)
        //        {
        //            return Next.BindRead(result, Expression.Field(self, "Next"), classContext, context);
        //        }
        //        else
        //        {
        //            return result;
        //        }
        //    }
        //    else
        //    {
        //        // invalid receiver type:
        //        // VariableMisusedAsObject( receiver, false )
        //        // return;
        //        return new DynamicMetaObject(
        //            BinderHelpers.VariableMisusedAsObject(receiver.Expression, false),
        //            receiver.Restrictions.Merge(instance.Restrictions)
        //            );
        //    }
        //}

        //public DynamicMetaObject BindReadRef(DynamicMetaObject receiver, Expression self, Type classContext, DynamicMetaObject context)
        //{
        //    throw new NotImplementedException();
        //}
    }

    [DebuggerDisplay("[{Key,nq}]{Next}")]
    public struct ArrayItem<TNext> : IRuntimeChain where TNext : IRuntimeChain
    {
        public TNext Next; // can be "ChainEnd"

        public IntStringKey Key;

        public RuntimeChainOperation Operation => RuntimeChainOperation.ArrayItem;

        public PhpValue GetValue(PhpValue value, Context ctx, Type classContext)
        {
            value = value.GetArrayItem(Key);

            return (Next.Operation != RuntimeChainOperation.End)
                ? Next.GetValue(value, ctx, classContext)
                : value;
        }

        public PhpAlias GetAlias(ref PhpValue value, Context ctx, Type classContext)
        {
            var arr = PhpValue.EnsureArray(ref value);
            PhpValue tmp;

            switch (Next.Operation)
            {
                case RuntimeChainOperation.ArrayItem:
                    tmp = PhpValue.Create(arr.EnsureItemArray(Key));
                    break;

                case RuntimeChainOperation.Property:
                    tmp = PhpValue.FromClass(arr.EnsureItemObject(Key));
                    break;

                case RuntimeChainOperation.End:
                    return arr.EnsureItemAlias(Key);

                default:
                    throw new InvalidOperationException();
            }

            //
            return Next.GetAlias(ref tmp, ctx, classContext);
        }

        //public DynamicMetaObject BindRead(DynamicMetaObject receiver, Expression self, Type classContext, DynamicMetaObject context)
        //{
        //    throw new NotImplementedException();
        //}

        //public DynamicMetaObject BindReadRef(DynamicMetaObject receiver, Expression self, Type classContext, DynamicMetaObject context)
        //{
        //    throw new NotImplementedException();
        //}
    }

    [DebuggerDisplay("[]{Next}")]
    public struct ArrayNewItem<TNext> : IRuntimeChain where TNext : IRuntimeChain
    {
        public TNext Next; // can be "ChainEnd"

        public RuntimeChainOperation Operation => RuntimeChainOperation.ArrayItem;

        public PhpValue GetValue(PhpValue value, Context ctx, Type classContext)
        {
            throw new NotSupportedException();
        }

        public PhpAlias GetAlias(ref PhpValue value, Context ctx, Type classContext)
        {
            var result = PhpValue.Null;
            var alias = Next.GetAlias(ref result, ctx, classContext);

            PhpValue.EnsureArray(ref value).AddValue(result);

            return alias;
        }

        //public DynamicMetaObject BindRead(DynamicMetaObject receiver, Expression self, Type classContext, DynamicMetaObject context)
        //{
        //    throw new NotImplementedException();
        //}

        //public DynamicMetaObject BindReadRef(DynamicMetaObject receiver, Expression self, Type classContext, DynamicMetaObject context)
        //{
        //    throw new NotImplementedException();
        //}
    }

    [DebuggerDisplay(";")]
    public readonly struct ChainEnd : IRuntimeChain // void struct
    {
        RuntimeChainOperation IRuntimeChain.Operation => RuntimeChainOperation.End; // -1

        public PhpAlias GetAlias(ref PhpValue value, Context ctx, Type classContext) => PhpValue.EnsureAlias(ref value);

        public PhpValue GetValue(PhpValue value, Context ctx, Type classContext) => value;

        //DynamicMetaObject IRuntimeChain.BindRead(DynamicMetaObject receiver, Expression self, Type classContext, DynamicMetaObject context) => throw new InvalidOperationException();

        //DynamicMetaObject IRuntimeChain.BindReadRef(DynamicMetaObject receiver, Expression self, Type classContext, DynamicMetaObject context) => throw new InvalidOperationException();
    }
}
