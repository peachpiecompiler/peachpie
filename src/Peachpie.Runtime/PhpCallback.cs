using Pchp.Core.Reflection;
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
    public delegate PhpValue PhpCallable(Context ctx, PhpValue[] arguments);

    /// <summary>
    /// Delegate for dynamic method invocation.
    /// </summary>
    /// <param name="ctx">Current runtime context. Cannot be <c>null</c>.</param>
    /// <param name="target">For instance methods, the target object.</param>
    /// <param name="arguments">List of arguments to be passed to called routine.</param>
    /// <returns>Result of the invocation.</returns>
    internal delegate PhpValue PhpInvokable(Context ctx, object target, PhpValue[] arguments);

    /// <summary>
    /// Callable object representing callback to a routine.
    /// Performs dynamic binding to actual method and provides <see cref="IPhpCallable"/> interface.
    /// </summary>
    public abstract class PhpCallback : IPhpCallable, IEquatable<PhpCallback>
    {
        /// <summary>
        /// Resolved routine to be invoked.
        /// </summary>
        protected PhpCallable _lazyResolved;

        /// <summary>
        /// Gets value indicating the callback is valid.
        /// </summary>
        public virtual bool IsValid => true;

        /// <summary>
        /// Gets value indicating this instance represents the same callback as <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The other instance to compare with.</param>
        /// <returns>Both callbacks represents the same routine.</returns>
        public virtual bool Equals(PhpCallback other) => object.ReferenceEquals(_lazyResolved, other?._lazyResolved) && _lazyResolved != null;

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
                Debug.WriteLine($"Function '{_function}' is not defined!");
                // TODO: ctx.Error.CallToUndefinedFunction(_function)
                return PhpValue.False;
            }

            public override bool Equals(PhpCallback other) => base.Equals(other) || Equals(other as FunctionCallback);
            bool Equals(FunctionCallback other) => other != null && other._function == _function;
        }

        [DebuggerDisplay("{_class,nq}::{_method,nq}()")]
        sealed class MethodCallback : PhpCallback
        {
            readonly string _class, _method;

            // TODO: caller (to resolve accessibility)

            public MethodCallback(string @class, string method)
            {
                _class = @class;
                _method = method;
            }

            public override PhpValue ToPhpValue() => PhpValue.Create(new PhpArray(2) { (PhpValue)_class, (PhpValue)_method });

            protected override PhpCallable BindCore(Context ctx)
            {
                for (var tinfo = ctx.GetDeclaredType(_class); tinfo != null; tinfo = tinfo.BaseType)
                {
                    var method = (Reflection.PhpMethodInfo)tinfo.DeclaredMethods[_method];
                    if (method != null)
                    {
                        return method.PhpInvokable.Bind(null);
                    }
                }

                return null;
            }

            public override bool Equals(PhpCallback other) => base.Equals(other) || Equals(other as MethodCallback);
            bool Equals(MethodCallback other) => other != null && other._class == _class && other._method == _method;
        }

        [DebuggerDisplay("[{_item1,nq}, {_item2,nq}]()")]
        sealed class ArrayCallback : PhpCallback
        {
            readonly PhpValue _item1, _item2;

            // TODO: caller (to resolve accessibility)

            public ArrayCallback(PhpValue item1, PhpValue item2)
            {
                _item1 = item1;
                _item2 = item2;
            }

            public override PhpValue ToPhpValue() => PhpValue.Create(new PhpArray(2) { _item1, _item2 });

            protected override PhpCallable BindCore(Context ctx)
            {
                PhpTypeInfo tinfo;
                
                if (_item1.IsObject)
                {
                    tinfo = _item1.Object.GetType().GetPhpTypeInfo();
                }
                else
                {
                    tinfo = ctx.GetDeclaredType(_item1.ToString(ctx));
                }

                var method = _item2.ToString(ctx);

                for (; tinfo != null; tinfo = tinfo.BaseType)
                {
                    var routine = (PhpMethodInfo)tinfo.DeclaredMethods[method];
                    if (routine != null)
                    {
                        return routine.PhpInvokable.Bind(_item1.IsObject ? _item1.Object : null);
                    }
                }

                return null;
            }

            public override bool Equals(PhpCallback other) => base.Equals(other) || Equals(other as ArrayCallback);
            bool Equals(ArrayCallback other) => other != null && other._item1 == _item1 && other._item2 == _item2;
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

            public override bool Equals(PhpCallback other) => object.ReferenceEquals(this, other);
        }

        #endregion

        #region Create

        public static PhpCallback Create(IPhpCallable callable) => Create(callable.Invoke);

        public static PhpCallback Create(PhpCallable callable) => new CallableCallback(callable);

        public static PhpCallback Create(string function)
        {
            if (function != null)
            {
                var idx = function.IndexOf("::", StringComparison.Ordinal);
                return (idx < 0)
                    ? (PhpCallback)new FunctionCallback(function)
                    : new MethodCallback(function.Remove(idx), function.Substring(idx + 2));
            }

            return CreateInvalid();
        }

        public static PhpCallback Create(PhpValue item1, PhpValue item2) => new ArrayCallback(item1, item2);

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
            var resolved = BindCore(ctx) ?? InvokeError;

            _lazyResolved = resolved;

            return resolved;
        }

        /// <summary>
        /// Missing function call callback.
        /// </summary>
        protected virtual PhpValue InvokeError(Context ctx, PhpValue[] arguments)
        {
            // TODO: ctx.Errors.InvalidCallback();
            return PhpValue.False;
        }

        /// <summary>
        /// Performs binding to the routine delegate.
        /// </summary>
        /// <returns>Actual delegate or <c>null</c> if routine cannot be bound.</returns>
        protected abstract PhpCallable BindCore(Context ctx);

        #endregion

        #region IPhpCallable

        /// <summary>
        /// Invokes the callback with given arguments.
        /// </summary>
        public PhpValue Invoke(Context ctx, params PhpValue[] arguments) => Bind(ctx)(ctx, arguments);

        /// <summary>
        /// Gets value representing the calleback.
        /// Used for human readable representation of the callback.
        /// </summary>
        public abstract PhpValue ToPhpValue();

        #endregion
    }

    public static class PhpCallableExtension
    {
        /// <summary>
        /// Binds <see cref="PhpInvokable"/> to <see cref="PhpCallable"/> by fixing the target argument.
        /// </summary>
        internal static PhpCallable Bind(this PhpInvokable invokable, object target) => (ctx, arguments) => invokable(ctx, target, arguments);
    }
}
