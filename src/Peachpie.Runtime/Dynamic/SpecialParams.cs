using System;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using Pchp.Core.Reflection;

namespace Pchp.Core.Dynamic
{
    #region ISpecialParamHolder

    /// <summary>
    /// Denotates a parameter that is special, provided by compiler.
    /// </summary>
    internal interface ISpecialParamHolder
    {
        /// <summary>
        /// Gets value indicating the parameter is loaded by compiler providing context.
        /// </summary>
        bool IsImplicit { get; }

        /// <summary>
        /// Sets information to the callsite context.
        /// </summary>
        void Process(CallSiteContext info, Expression valueExpr);
    }

    #endregion

    /// <summary>
    /// Wraps an argument passed to callsite denotating a special meaning of the value.
    /// </summary>
    [DebuggerNonUserCode]
    public readonly struct ContextParam : ISpecialParamHolder
    {
        /// <summary>
        /// Runtime context.
        /// </summary>
        public readonly Context Value;

        bool ISpecialParamHolder.IsImplicit => true;

        void ISpecialParamHolder.Process(CallSiteContext info, Expression valueExpr)
        {
            info.Context = valueExpr;
        }

        /// <summary>Initializes the structure.</summary>
        public ContextParam(Context value) => Value = value;
    }

    /// <summary>
    /// Wraps an argument passed to callsite denotating a function or property name.
    /// </summary>
    [DebuggerNonUserCode]
    public readonly struct NameParam<T> : ISpecialParamHolder
    {
        /// <summary>
        /// The invoked member, <c>callable</c>.
        /// </summary>
        public readonly T Value;

        bool ISpecialParamHolder.IsImplicit => true;

        void ISpecialParamHolder.Process(CallSiteContext info, Expression valueExpr)
        {
            if (valueExpr.Type != typeof(string))
            {
                throw new InvalidOperationException();
            }

            var name = (string)(object)Value;

            info.IndirectName = valueExpr;
            info.AddRestriction(Expression.Equal(valueExpr, Expression.Constant(name)));

            if (info.Name == null)
            {
                info.Name = name;
            }
        }

        /// <summary>Initializes the structure.</summary>
        public NameParam(T value) => Value = value;
    }

    /// <summary>
    /// Wraps an argument passed to callsite denotating target type of a static invocation operation (call, static field, class const).
    /// </summary>
    [DebuggerNonUserCode]
    public readonly struct TargetTypeParam : ISpecialParamHolder
    {
        /// <summary>
        /// Target type.
        /// </summary>
        public readonly PhpTypeInfo Value;

        bool ISpecialParamHolder.IsImplicit => true;

        void ISpecialParamHolder.Process(CallSiteContext info, Expression valueExpr)
        {
            Debug.Assert(Value != null);

            info.AddRestriction(BindingRestrictions.GetInstanceRestriction(valueExpr, Value));  // {arg} != null && {arg} == Value
            info.TargetType = Value;
        }

        /// <summary>Initializes the structure.</summary>
        public TargetTypeParam(PhpTypeInfo value) => Value = value;
    }

    /// <summary>
    /// Wraps an argument passed to callsite denotating late static type of a method call through self:: or parent::.
    /// </summary>
    [DebuggerNonUserCode]
    public readonly struct LateStaticTypeParam : ISpecialParamHolder
    {
        /// <summary>
        /// Target type.
        /// </summary>
        public readonly PhpTypeInfo Value;

        bool ISpecialParamHolder.IsImplicit => true;

        void ISpecialParamHolder.Process(CallSiteContext info, Expression valueExpr)
        {
            info.LateStaticType = valueExpr;
        }

        /// <summary>Initializes the structure.</summary>
        public LateStaticTypeParam(PhpTypeInfo value) => Value = value;
    }

    /// <summary>
    /// Wraps an argument passed to callsite denotating a generic argument.
    /// </summary>
    [DebuggerNonUserCode]
    public readonly struct GenericParam : ISpecialParamHolder
    {
        /// <summary>
        /// A generic argument.
        /// </summary>
        public readonly PhpTypeInfo Value;

        bool ISpecialParamHolder.IsImplicit => true;

        void ISpecialParamHolder.Process(CallSiteContext info, Expression valueExpr)
        {
            throw new NotImplementedException();
        }

        /// <summary>Initializes the structure.</summary>
        public GenericParam(PhpTypeInfo value) => Value = value;
    }

    /// <summary>
    /// Wraps an argument passed to callsite denotating a caller type.
    /// </summary>
    [DebuggerNonUserCode]
    public readonly struct CallerTypeParam : ISpecialParamHolder
    {
        /// <summary>
        /// Caller type context.
        /// </summary>
        public readonly RuntimeTypeHandle Value;

        bool ISpecialParamHolder.IsImplicit => true;

        void ISpecialParamHolder.Process(CallSiteContext info, Expression valueExpr)
        {
            info.AddRestriction(Expression.Call(valueExpr, Cache.Operators.RuntimeTypeHandle_Equals_RuntimeTypeHandle, Expression.Constant(Value)));
            info.ClassContext = Type.GetTypeFromHandle(Value);
        }

        /// <summary>Initializes the structure.</summary>
        public CallerTypeParam(RuntimeTypeHandle value) => Value = value;
    }

    /// <summary>
    /// Wraps the argument unpacking.
    /// </summary>
    [DebuggerNonUserCode]
    public readonly struct UnpackingParam<T> : ISpecialParamHolder
    {
        public readonly T Value;

        bool ISpecialParamHolder.IsImplicit => false;

        void ISpecialParamHolder.Process(CallSiteContext info, Expression valueExpr)
        {
            throw new InvalidOperationException();
        }

        /// <summary>Initializes the structure.</summary>
        public UnpackingParam(T value) => Value = value;
    }
}
