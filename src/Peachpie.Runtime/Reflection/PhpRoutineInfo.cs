#nullable enable

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
    /// Runtime information about a function.
    /// </summary>
    [DebuggerDisplay("{Name,nq}")]
    [DebuggerNonUserCode]
    public abstract class RoutineInfo : IPhpCallable, ICloneable
    {
        /// <summary>
        /// Index to the routine slot.
        /// <c>0</c> is an uninitialized index.
        /// </summary>
        public int Index { get { return _index; } internal set { _index = value; } }
        protected int _index;

        /// <summary>
        /// Gets value indicating the routine was declared in a users code.
        /// Otherwise the function is a library function.
        /// </summary>
        public bool IsUserFunction => _index > 0;

        /// <summary>
        /// Gets the routine name, cannot be <c>null</c> or empty.
        /// </summary>
        public string Name => _name;
        protected readonly string _name;

        /// <summary>
        /// Gets routine callable delegate.
        /// </summary>
        public abstract PhpCallable PhpCallable { get; }

        /// <summary>
        /// Invokes the routine.
        /// </summary>
        public virtual PhpValue Invoke(Context ctx, object? target, params PhpValue[] arguments) => PhpCallable(ctx, arguments);

        //ulong _aliasedParams; // bit field corresponding to parameters that are passed by reference
        //_routineFlags;    // routine requirements, accessibility

        // TODO: PHPDoc

        /// <summary>
        /// Gets methods representing the routine.
        /// </summary>
        public abstract MethodInfo[] Methods { get; }

        /// <summary>
        /// Gets the method declaring type.
        /// Might get <c>null</c> if the routine represents a global function or a delegate.
        /// </summary>
        public abstract PhpTypeInfo? DeclaringType { get; }

        /// <summary>Target instance when binding the MethodInfo call.</summary>
        internal virtual object? Target => null;

        protected RoutineInfo(int index, string name)
        {
            _index = index;
            _name = name;
        }

        /// <summary>
        /// Used by compiler generated code.
        /// Creates instance of <see cref="RoutineInfo"/> representing a user PHP function.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="handle">CLR method handle.</param>
        /// <param name="overloads">Additional method handles of the method overloads.</param>
        /// <returns>Instance of routine info with uninitialized slot index and unbound delegate.</returns>
        public static RoutineInfo CreateUserRoutine(string name, RuntimeMethodHandle handle, params RuntimeMethodHandle[] overloads) => PhpRoutineInfo.Create(name, handle, overloads);

        /// <summary>
        /// Creates instance of <see cref="RoutineInfo"/> representing a CLR methods (handling ovberloads).
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="handles">CLR methods.</param>
        /// <returns>Instance of routine info with uninitialized slot index and unbound delegate.</returns>
        public static RoutineInfo CreateUserRoutine(string name, MethodInfo[] handles) => PhpMethodInfo.Create(0, name, handles);

        /// <summary>
        /// Creates user routine from a CLR delegate.
        /// </summary>
        /// <param name="name">PHP routine name.</param>
        /// <param name="delegate">.NET delegate.</param>
        /// <returns>Instance of routine info with uninitialized slot index and unbound delegate.</returns>
        public static RoutineInfo CreateUserRoutine(string name, Delegate @delegate) => new DelegateRoutineInfo(name, @delegate);

        #region IPhpCallable

        PhpValue IPhpCallable.Invoke(Context ctx, params PhpValue[] arguments) => Invoke(ctx, Target, arguments);

        PhpValue IPhpCallable.ToPhpValue() => PhpValue.Null;

        #endregion

        object ICloneable.Clone() => this;
    }

    [DebuggerNonUserCode]
    internal class PhpRoutineInfo : RoutineInfo
    {
        readonly RuntimeMethodHandle _handle;
        PhpCallable? _lazyDelegate;

        /// <summary>
        /// CLR method handle.
        /// </summary>
        protected RuntimeMethodHandle Handle => _handle;

        public override int GetHashCode() => _handle.GetHashCode();

        public override MethodInfo[] Methods => new[] { (MethodInfo)MethodBase.GetMethodFromHandle(_handle) };

        public override PhpTypeInfo? DeclaringType => null;

        public override PhpCallable PhpCallable => _lazyDelegate ?? BindDelegate();

        PhpCallable BindDelegate() => _lazyDelegate = Dynamic.BinderHelpers.BindToPhpCallable(Methods);

        internal static PhpRoutineInfo Create(string name, RuntimeMethodHandle handle, params RuntimeMethodHandle[] overloads)
        {
            return (overloads.Length == 0)
                ? new PhpRoutineInfo(name, handle)
                : new PhpRoutineInfoWithOverloads(name, handle, overloads);
        }

        protected PhpRoutineInfo(string name, RuntimeMethodHandle handle)
            : base(0, name)
        {
            _handle = handle;
        }

        #region PhpRoutineInfoWithOverloads

        sealed class PhpRoutineInfoWithOverloads : PhpRoutineInfo
        {
            readonly RuntimeMethodHandle[] _overloads;

            public PhpRoutineInfoWithOverloads(string name, RuntimeMethodHandle handle, RuntimeMethodHandle[] overloads)
                : base(name, handle)
            {
                _overloads = overloads ?? throw new ArgumentNullException(nameof(overloads));
            }

            public override MethodInfo[] Methods
            {
                get
                {
                    var overloads = _overloads;

                    // [Method] + Overloads

                    var methods = new MethodInfo[1 + overloads.Length];
                    methods[0] = (MethodInfo)MethodBase.GetMethodFromHandle(Handle);
                    for (int i = 0; i < overloads.Length; i++)
                    {
                        methods[1 + i] = (MethodInfo)MethodBase.GetMethodFromHandle(overloads[i]);
                    }

                    //
                    return methods;
                }
            }
        }

        #endregion
    }

    /// <summary>Represents anonymous function with special <see cref="Closure"/> parameter.</summary>
    [DebuggerNonUserCode]
    internal sealed class PhpAnonymousRoutineInfo : PhpRoutineInfo
    {
        public PhpAnonymousRoutineInfo(string name, RuntimeMethodHandle handle)
            : base(name, handle)
        {
        }
    }

    [DebuggerNonUserCode]
    internal class DelegateRoutineInfo : RoutineInfo
    {
        readonly Delegate _delegate;
        PhpInvokable? _lazyInvokable;

        /// <summary>
        /// Cache of already bound methods,
        /// avoids unnecessary allocations and dynamic code emit.
        /// </summary>
        static readonly Dictionary<MethodInfo, PhpInvokable> s_bound = new Dictionary<MethodInfo, PhpInvokable>();

        public override MethodInfo[] Methods => new[] { _delegate.GetMethodInfo() };

        public override PhpTypeInfo? DeclaringType => null;

        internal override object Target => _delegate.Target;

        public override int GetHashCode() => _delegate.GetHashCode();

        internal PhpInvokable PhpInvokable => _lazyInvokable ?? BindDelegate();

        PhpInvokable BindDelegate()
        {
            var method = _delegate.GetMethodInfo();

            if (!s_bound.TryGetValue(method, out _lazyInvokable))   // TODO: RW lock
            {
                lock (s_bound)
                {
                    s_bound[method] = _lazyInvokable = Dynamic.BinderHelpers.BindToPhpInvokable(new[] { method });
                }
            }

            //
            return _lazyInvokable;
        }

        public override PhpCallable PhpCallable => (ctx, args) => Invoke(ctx, Target, args);

        public override PhpValue Invoke(Context ctx, object? target, params PhpValue[] arguments) => PhpInvokable(ctx, target, arguments);

        public DelegateRoutineInfo(string name, Delegate @delegate)
            : base(0, name)
        {
            _delegate = @delegate ?? throw new ArgumentNullException(nameof(@delegate));
        }
    }

    [DebuggerNonUserCode]
    internal class ClrRoutineInfo : RoutineInfo
    {
        PhpCallable? _lazyDelegate;

        MethodInfo[] _methods;

        public override MethodInfo[] Methods => _methods;

        public override PhpTypeInfo? DeclaringType => null;

        public override PhpCallable PhpCallable => _lazyDelegate ?? BindDelegate();

        PhpCallable BindDelegate()
        {
            return _lazyDelegate = Dynamic.BinderHelpers.BindToPhpCallable(Methods);
        }

        public ClrRoutineInfo(int index, string name, MethodInfo method)
            : base(index, name)
        {
            _methods = new MethodInfo[] { method };
        }

        internal void AddOverload(MethodInfo method)
        {
            if (Array.IndexOf(_methods, method) < 0)
            {
                Array.Resize(ref _methods, _methods.Length + 1);
                _methods[_methods.Length - 1] = method;

                //
                _lazyDelegate = null;
            }
        }
    }

    /// <summary>
    /// PHP routine representing class methods.
    /// </summary>
    [DebuggerNonUserCode]
    internal class PhpMethodInfo : RoutineInfo
    {
        #region PhpMethodInfoWithBoundType

        sealed class PhpMethodInfoWithBoundType : PhpMethodInfo
        {
            public override PhpTypeInfo LateStaticType { get; }

            public PhpMethodInfoWithBoundType(int index, string name, MethodInfo[] methods, PhpTypeInfo lateStaticType)
                : base(index, name, methods)
            {
                LateStaticType = lateStaticType ?? throw new ArgumentNullException(nameof(lateStaticType));
            }
        }

        #endregion

        /// <summary>
        /// Creates instance of <see cref="PhpMethodInfo"/>.
        /// </summary>
        public static PhpMethodInfo Create(int index, string name, MethodInfo[] methods, PhpTypeInfo? callertype = null)
        {
            if (callertype != null)
            {
                if (callertype.Type.IsClass && methods.Any(Dynamic.BinderHelpers.HasLateStaticParameter))
                {
                    // if method requires late static bound type, remember it:
                    return new PhpMethodInfoWithBoundType(index, name, methods, lateStaticType: callertype);
                }

                // reuse PhpMethodInfo from base type if possible
                if (AllDeclaredInBase(methods, callertype.Type) &&
                    callertype.BaseType != null &&
                    callertype.BaseType.RuntimeMethods[name] is PhpMethodInfo frombase)
                {
                    return frombase;
                }
            }

            return new PhpMethodInfo(index, name, methods);
        }

        static bool AllDeclaredInBase(MethodInfo[] methods, Type callertype)
        {
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].DeclaringType == callertype) return false;
            }

            return true;
        }

        PhpInvokable? _lazyDelegate;

        /// <summary>
        /// Array of CLR methods. Cannot be <c>null</c> or empty.
        /// </summary>
        public override MethodInfo[] Methods => _methods;
        readonly MethodInfo[] _methods;

        public override PhpTypeInfo DeclaringType => _methods[0].DeclaringType.GetPhpTypeInfo();

        /// <summary>Optional. Bound static type.</summary>
        public virtual PhpTypeInfo? LateStaticType => null;

        protected PhpMethodInfo(int index, string name, MethodInfo[] methods)
            : base(index, name)
        {
            _methods = methods;
        }

        internal PhpInvokable PhpInvokable => _lazyDelegate ?? BindDelegate();

        PhpInvokable BindDelegate()
        {
            return _lazyDelegate = Dynamic.BinderHelpers.BindToPhpInvokable(_methods, LateStaticType);
        }

        public override PhpCallable PhpCallable
        {
            get
            {
                Debug.Assert(_methods.All(TypeMembersUtils.s_isMethodStatic));
                return (ctx, args) => Invoke(ctx, null, args);
            }
        }

        public override PhpValue Invoke(Context ctx, object? target, params PhpValue[] arguments) => PhpInvokable(ctx, target, arguments);
    }
}
