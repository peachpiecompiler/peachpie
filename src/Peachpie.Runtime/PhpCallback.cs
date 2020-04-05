using Pchp.Core.Reflection;
using Pchp.Core.Resources;
using Pchp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// An object that can be invoked dynamically.
    /// </summary>
    /// <remarks>The interface is implemented by compiler for classes with <c>__invoke</c> method.</remarks>
    public interface IPhpCallable
    {
        /// <summary>
        /// Invokes the object with given arguments.
        /// </summary>
        PhpValue Invoke(Context ctx, params PhpValue[] arguments);

        /// <summary>
        /// Gets PHP value representing the callback.
        /// </summary>
        PhpValue ToPhpValue();
    }

    /// <summary>
    /// Delegate for dynamic routine invocation.
    /// </summary>
    /// <param name="ctx">Current runtime context. Cannot be <c>null</c>.</param>
    /// <param name="arguments">List of arguments to be passed to called routine.</param>
    /// <returns>Result of the invocation.</returns>
    public delegate PhpValue PhpCallable(Context ctx, params PhpValue[] arguments);

    /// <summary>
    /// Delegate for dynamic method invocation.
    /// </summary>
    /// <param name="ctx">Current runtime context. Cannot be <c>null</c>.</param>
    /// <param name="target">For instance methods, the target object.</param>
    /// <param name="arguments">List of arguments to be passed to called routine.</param>
    /// <returns>Result of the invocation.</returns>
    internal delegate PhpValue PhpInvokable(Context ctx, object target, params PhpValue[] arguments);

    /// <summary>
    /// Callable object representing callback to a routine.
    /// Performs dynamic binding to actual method and provides <see cref="IPhpCallable"/> interface.
    /// </summary>
    public abstract class PhpCallback : IPhpCallable
    {
        [Flags]
        enum CallbackFlags
        {
            Default = 0,

            /// <summary>
            /// The callback has been marked as invalid.
            /// </summary>
            IsInvalid = 1,

            /// <summary>
            /// When invalid is invoked, exception is thrown.
            /// </summary>
            InvalidThrowsException = 2,
        }

        CallbackFlags _flags = CallbackFlags.Default;

        /// <summary>
        /// Resolved routine to be invoked.
        /// </summary>
        protected PhpCallable _lazyResolved;

        /// <summary>
        /// Gets value indicating the callback is valid.
        /// </summary>
        public virtual bool IsValid => true;

        /// <summary>
        /// Gets value indicating the callback has been already resolved.
        /// </summary>
        public bool IsResolved => _lazyResolved != null;

        /// <summary>
        /// Tries to bind the callback and checks if the callback is valid.
        /// </summary>
        internal bool IsValidBound(Context ctx)
        {
            if (IsValid)
            {
                // ensure the callback is bound
                Bind(ctx);

                // check flags
                return (_flags & CallbackFlags.IsInvalid) == 0;
            }

            return false;
        }

        /// <summary>
        /// Gets value indicating this instance represents the same callback as <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The other instance to compare with.</param>
        /// <returns>Both callbacks represents the same routine.</returns>
        public virtual bool Equals(PhpCallback other) => other != null && ReferenceEquals(_lazyResolved, other._lazyResolved) && _lazyResolved != null;
        public override bool Equals(object obj) => Equals(obj as PhpCallback);
        public override int GetHashCode() => this.GetType().GetHashCode();

        /// <summary>
        /// An empty PHP callback doing no action.
        /// </summary>
        public static readonly PhpCallback Empty = new EmptyCallback();

        #region PhpCallbacks

        [DebuggerDisplay("{_lazyResolved,nq}()")]
        sealed class CallableCallback : PhpCallback
        {
            public CallableCallback(PhpCallable callable)
            {
                _lazyResolved = callable;
            }

            protected override PhpCallable BindCore(Context ctx)
            {
                // cannot be reached
                throw new InvalidOperationException();
            }

            public override PhpValue ToPhpValue() => PhpValue.FromClass(_lazyResolved);
        }

        [DebuggerDisplay("{_function,nq}()")]
        sealed class FunctionCallback : PhpCallback
        {
            /// <summary>
            /// Name of the function to be called.
            /// </summary>
            readonly string _function;

            public FunctionCallback(string function)
            {
                _function = function;
            }

            public override PhpValue ToPhpValue() => PhpValue.Create(_function);

            protected override PhpCallable BindCore(Context ctx) => ctx.GetDeclaredFunction(_function)?.PhpCallable;

            protected override PhpValue InvokeError(Context ctx, PhpValue[] arguments)
            {
                PhpException.UndefinedFunctionCalled(_function);
                return PhpValue.Null;
            }

            public override bool Equals(PhpCallback other) => base.Equals(other) || Equals(other as FunctionCallback);
            bool Equals(FunctionCallback other) => other != null && other._function == _function;
        }

        [DebuggerDisplay("{_class,nq}::{_method,nq}()")]
        sealed class MethodCallback : PhpCallback
        {
            readonly string _class, _method;
            readonly RuntimeTypeHandle _callerCtx;

            /// <summary>
            /// Target object instance.
            /// </summary>
            public object Target { get; set; }

            public MethodCallback(string @class, string method, RuntimeTypeHandle callerCtx)
            {
                _class = @class;
                _callerCtx = callerCtx;
                _method = method;
            }

            public override PhpValue ToPhpValue() => $"{_class}::{_method}";

            PhpCallable BindCore(PhpTypeInfo tinfo)
            {
                if (tinfo != null)
                {
                    var target = Target != null && tinfo.Type.IsAssignableFrom(Target.GetType()) ? Target : null;

                    var routine = (PhpMethodInfo)tinfo.GetVisibleMethod(_method, _callerCtx);
                    if (routine != null)
                    {
                        return routine.PhpInvokable.Bind(target);
                    }
                    else
                    {
                        routine = (PhpMethodInfo)tinfo.RuntimeMethods[target != null ? TypeMethods.MagicMethods.__call : TypeMethods.MagicMethods.__callstatic];
                        if (routine == null && target != null)
                        {
                            routine = (PhpMethodInfo)tinfo.RuntimeMethods[TypeMethods.MagicMethods.__callstatic];
                        }

                        if (routine != null)
                        {
                            return routine.PhpInvokable.BindMagicCall(target, _method);
                        }
                    }
                }

                return null;
            }

            protected override PhpValue InvokeError(Context ctx, PhpValue[] arguments)
            {
                PhpException.UndefinedMethodCalled(_class, _method);
                return PhpValue.Null;
            }

            PhpTypeInfo ResolveType(Context ctx) => ctx.ResolveType(_class, _callerCtx, true);

            protected override PhpCallable BindCore(Context ctx)
            {
                return BindCore(ResolveType(ctx));
            }

            public override PhpCallable BindToStatic(Context ctx, PhpTypeInfo @static)
            {
                var tinfo = ResolveType(ctx);

                if (@static != null && tinfo != null && @static.Type.IsSubclassOf(tinfo.Type.AsType()))
                {
                    tinfo = @static;
                }

                return BindCore(tinfo);
            }

            public override bool Equals(PhpCallback other) => base.Equals(other) || Equals(other as MethodCallback);
            bool Equals(MethodCallback other) => other != null && other._class == _class && other._method == _method;
        }

        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        sealed class ArrayCallback : PhpCallback
        {
            string DebuggerDisplay => $"[{_obj.DisplayString}, {_method}]()";

            readonly PhpValue _obj;
            readonly string _method;
            readonly RuntimeTypeHandle _callerCtx;

            /// <summary>
            /// Target object instance as provided.
            /// </summary>
            public object Target { get; set; }

            public ArrayCallback(PhpValue item1, string method, RuntimeTypeHandle callerCtx)
            {
                _obj = item1;
                _method = method ?? throw new ArgumentNullException(nameof(method));
                _callerCtx = callerCtx;
            }

            public override PhpValue ToPhpValue() => PhpValue.Create(new PhpArray(2) { _obj, _method });

            PhpCallable BindCore(Context ctx, PhpTypeInfo tinfo, object target)
            {
                if (tinfo != null)
                {
                    if (target == null && Target != null && tinfo.Type.IsAssignableFrom(Target.GetType()))
                    {
                        target = this.Target;
                    }

                    var routine = (PhpMethodInfo)tinfo.GetVisibleMethod(_method, _callerCtx);

                    // [$b, "A::foo"] or [$this, "parent::foo"]
                    int colIndex;
                    if (routine == null && (colIndex = _method.IndexOf("::", StringComparison.Ordinal)) > 0)
                    {
                        var methodTypeInfo = ctx.ResolveType(_method.Substring(0, colIndex), _callerCtx, true);
                        if (methodTypeInfo != null && methodTypeInfo.Type.IsAssignableFrom(tinfo.Type))
                        {
                            tinfo = methodTypeInfo;
                            routine = (PhpMethodInfo)methodTypeInfo.GetVisibleMethod(_method.Substring(colIndex + 2), _callerCtx);
                        }
                    }

                    if (routine != null)
                    {
                        if (target != null)
                        {
                            return routine.PhpInvokable.Bind(target);
                        }
                        else
                        {
                            // calling the method statically
                            if (routine.Methods.All(TypeMembersUtils.s_isMethodStatic))
                            {
                                return routine.PhpCallable;
                            }
                            else
                            {
                                // CONSIDER: compiler (and this binder) creates dummy instance of self;
                                // can we create a special singleton instance marked as "null" so use of $this inside the method will fail ?
                                // TODO: use caller instance or warning (calling instance method statically)
                                return routine.PhpInvokable.Bind(tinfo.CreateUninitializedInstance(ctx));
                            }
                        }
                    }
                    else
                    {
                        // __call
                        // __callStatic
                        routine = (PhpMethodInfo)tinfo.RuntimeMethods[(target != null)
                            ? TypeMethods.MagicMethods.__call
                            : TypeMethods.MagicMethods.__callstatic];

                        if (routine != null)
                        {
                            return routine.PhpInvokable.BindMagicCall(target, _method);
                        }
                    }
                }

                return null;
            }

            protected override PhpValue InvokeError(Context ctx, PhpValue[] arguments)
            {
                ResolveType(ctx, out var tinfo, out _);
                if (tinfo != null)
                {
                    PhpException.UndefinedMethodCalled(tinfo.Name, _method);
                }
                else
                {
                    throw PhpException.ClassNotFoundException(_obj.ToString(ctx));
                }

                return PhpValue.Null;
            }

            void ResolveType(Context ctx, out PhpTypeInfo tinfo, out object target)
            {
                if ((target = _obj.AsObject()) != null)
                {
                    tinfo = target.GetPhpTypeInfo();
                }
                else
                {
                    tinfo = ctx.ResolveType(_obj.ToString(ctx), _callerCtx, true);
                }
            }

            protected override PhpCallable BindCore(Context ctx)
            {
                ResolveType(ctx, out var tinfo, out object target);
                return BindCore(ctx, tinfo, target);
            }

            public override PhpCallable BindToStatic(Context ctx, PhpTypeInfo @static)
            {
                ResolveType(ctx, out PhpTypeInfo tinfo, out object target);

                //

                if (@static != null && tinfo != null && target == null && @static.Type.IsSubclassOf(tinfo.Type.AsType()))
                {
                    tinfo = @static;
                }

                //

                return BindCore(ctx, tinfo, target);
            }

            public override bool Equals(PhpCallback other) => base.Equals(other) || Equals(other as ArrayCallback);
            bool Equals(ArrayCallback other) => other != null && EqualsObj(other._obj, _obj) && other._method == _method;

            static bool EqualsObj(PhpValue a, PhpValue b)
            {
                // avoid incomparable object comparison
                var targetSelf = a.AsObject();
                var targetOther = b.AsObject();

                if (targetSelf != null) return ReferenceEquals(targetSelf, targetOther);
                if (targetOther != null) return false;

                //
                return a == b;
            }
        }

        [DebuggerDisplay("empty callback")]
        sealed class EmptyCallback : PhpCallback
        {
            public EmptyCallback() { }

            public override PhpValue ToPhpValue() => PhpValue.Null;

            protected override PhpCallable BindCore(Context ctx) => (_1, _2) => PhpValue.Null;

            public override bool IsValid => true;

            public override int GetHashCode() => 1;

            public override bool Equals(PhpCallback other) => other is EmptyCallback;
        }

        [DebuggerDisplay("invalid callback")]
        sealed class InvalidCallback : PhpCallback
        {
            public InvalidCallback()
            {

            }

            public override PhpValue ToPhpValue() => PhpValue.Null;

            protected override PhpCallable BindCore(Context ctx) => null;

            public override bool IsValid => false;

            public override int GetHashCode() => 0;

            public override bool Equals(PhpCallback other) => other is InvalidCallback;
        }

        #endregion

        #region Create

        public static PhpCallback Create(IPhpCallable callable) => Create(callable.Invoke);

        public static PhpCallback Create(PhpCallable callable) => new CallableCallback(callable);

        public static PhpCallback Create(string function, RuntimeTypeHandle callerCtx = default(RuntimeTypeHandle), object callerObj = null)
        {
            if (function != null)
            {
                int idx;

                return
                    (function.Length <= 3 ||
                    (idx = function.IndexOf(':', 1, function.Length - 2)) < 0 ||
                    (function[idx + 1] != ':'))
                        ? (PhpCallback)new FunctionCallback(function)   // "::" not found in a valid position
                        : new MethodCallback(function.Remove(idx), function.Substring(idx + 2), callerCtx) { Target = callerObj };
            }

            return CreateInvalid();
        }

        public static PhpCallback Create(PhpValue item1, PhpValue item2, RuntimeTypeHandle callerCtx = default, object callerObj = null)
        {
            if (item2.IsString(out var method))
            {
                if (item1.AsObject() != null || item1.IsString())
                {
                    // creates callback from an array,
                    // array entries must be dereferenced so they cannot be changed gainst
                    return new ArrayCallback(item1.GetValue(), method, callerCtx) { Target = callerObj };
                }
            }

            //
            return CreateInvalid();
        }

        public static PhpCallback Create(object targetInstance, string methodName, RuntimeTypeHandle callerCtx = default) => new ArrayCallback(PhpValue.FromClass(targetInstance), methodName, callerCtx);

        public static PhpCallback CreateInvalid() => new InvalidCallback();

        // TODO: Create(Delegate)
        // TODO: Create(object) // look for IPhpCallable, __invoke, PhpCallableRoutine, Delegate

        #endregion

        #region Bind

        /// <summary>
        /// Ensures the routine delegate is bound.
        /// </summary>
        private PhpCallable Bind(Context ctx) => _lazyResolved ?? BindNew(ctx);

        /// <summary>
        /// Binds the routine delegate.
        /// </summary>
        /// <returns>Instance to the delegate. Cannot be <c>null</c>.</returns>
        private PhpCallable BindNew(Context ctx)
        {
            var resolved = BindCore(ctx);
            if (resolved == null)
            {
                _flags |= CallbackFlags.IsInvalid;
                resolved = InvokeError;
            }

            //

            _lazyResolved = resolved;

            return resolved;
        }

        /// <summary>
        /// Missing function call callback.
        /// </summary>
        protected virtual PhpValue InvokeError(Context ctx, PhpValue[] arguments)
        {
            throw PhpException.ErrorException(ErrResources.invalid_callback);
        }

        /// <summary>
        /// Performs binding to the routine delegate.
        /// </summary>
        /// <returns>Actual delegate or <c>null</c> if routine cannot be bound.</returns>
        protected abstract PhpCallable BindCore(Context ctx);

        /// <summary>
        /// Binds callback to given late static bound type.
        /// </summary>
        public virtual PhpCallable BindToStatic(Context ctx, PhpTypeInfo @static) => Bind(ctx);

        #endregion

        #region IPhpCallable

        /// <summary>
        /// Invokes the callback with given arguments.
        /// </summary>
        public PhpValue Invoke(Context ctx, params PhpValue[] arguments) => Bind(ctx)(ctx, arguments);

        /// <summary>
        /// Gets value representing the callback.
        /// Used for human readable representation of the callback.
        /// </summary>
        public abstract PhpValue ToPhpValue();

        #endregion
    }

    internal static class PhpCallableExtension
    {
        /// <summary>
        /// Binds <see cref="PhpInvokable"/> to <see cref="PhpCallable"/> by fixing the target argument.
        /// </summary>
        public static PhpCallable Bind(this PhpInvokable invokable, object target) => (ctx, arguments) => invokable(ctx, target, arguments);

        /// <summary>
        /// Binds <see cref="PhpInvokable"/> to <see cref="PhpCallable"/> while wrapping arguments to a single argument of type <see cref="PhpArray"/>.
        /// </summary>
        public static PhpCallable BindMagicCall(this PhpInvokable invokable, object target, string name)
            => (ctx, arguments) => invokable(ctx, target, new[] { (PhpValue)name, (PhpValue)PhpArray.New(arguments) });
    }
}
