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
    public abstract class RoutineInfo : IPhpCallable
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
        public virtual PhpValue Invoke(Context ctx, object target, params PhpValue[] arguments) => PhpCallable(ctx, arguments);

        //ulong _aliasedParams; // bit field corresponding to parameters that are passed by reference
        //_routineFlags;    // routine requirements, accessibility

        // TODO: PHPDoc

        /// <summary>
        /// Gets methods representing the routine.
        /// </summary>
        public abstract MethodInfo[] Methods { get; }

        /// <summary>Target instance when binding the MethodInfo call.</summary>
        internal virtual object Target => null;

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

        PhpValue IPhpCallable.ToPhpValue() => PhpValue.Void;

        #endregion
    }

    internal class PhpRoutineInfo : RoutineInfo
    {
        readonly RuntimeMethodHandle _handle;
        PhpCallable _lazyDelegate;

        /// <summary>
        /// CLR method handle.
        /// </summary>
        protected RuntimeMethodHandle Handle => _handle;

        public override int GetHashCode() => _handle.GetHashCode();

        public override MethodInfo[] Methods => new[] { (MethodInfo)MethodBase.GetMethodFromHandle(_handle) };

        public override PhpCallable PhpCallable => _lazyDelegate ?? (_lazyDelegate = BindDelegate());

        PhpCallable BindDelegate() => Dynamic.BinderHelpers.BindToPhpCallable(Methods);

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
    internal sealed class PhpAnonymousRoutineInfo : PhpRoutineInfo
    {
        public PhpAnonymousRoutineInfo(string name, RuntimeMethodHandle handle)
            : base(name, handle)
        {
        }
    }

    internal class DelegateRoutineInfo : RoutineInfo
    {
        Delegate _delegate;
        PhpInvokable _lazyInvokable;

        /// <summary>
        /// Cache of already bound methods,
        /// avoids unnecessary allocations and dynamic code emittion.
        /// </summary>
        static readonly Dictionary<MethodInfo, PhpInvokable> s_bound = new Dictionary<MethodInfo, PhpInvokable>();

        public override MethodInfo[] Methods => new[] { _delegate.GetMethodInfo() };

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

        public override PhpValue Invoke(Context ctx, object target, params PhpValue[] arguments) => PhpInvokable(ctx, target, arguments);

        public DelegateRoutineInfo(string name, Delegate @delegate)
            : base(0, name)
        {
            _delegate = @delegate ?? throw new ArgumentNullException(nameof(@delegate));
        }
    }

    internal class ClrRoutineInfo : RoutineInfo
    {
        PhpCallable _lazyDelegate;

        RuntimeMethodHandle[] _handles;

        public override MethodInfo[] Methods
        {
            get
            {
                var handles = _handles;
                var methods = new MethodInfo[handles.Length];
                for (int i = 0; i < handles.Length; i++)
                {
                    methods[i] = (MethodInfo)MethodBase.GetMethodFromHandle(handles[i]);
                }
                //
                return methods;
            }
        }

        public override PhpCallable PhpCallable => _lazyDelegate ?? BindDelegate();

        PhpCallable BindDelegate()
        {
            return _lazyDelegate = Dynamic.BinderHelpers.BindToPhpCallable(Methods);
        }

        public ClrRoutineInfo(int index, string name, RuntimeMethodHandle handle)
            : base(index, name)
        {
            _handles = new RuntimeMethodHandle[] { handle };
        }

        internal void AddOverload(RuntimeMethodHandle handle)
        {
            if (!_handles.Contains(handle))
            {
                var length = _handles.Length;
                Array.Resize(ref _handles, length + 1);
                _handles[length] = handle;

                //
                _lazyDelegate = null;
            }
        }
    }

    /// <summary>
    /// PHP routine representing class methods.
    /// </summary>
    internal class PhpMethodInfo : RoutineInfo
    {
        #region PhpMethodInfoWithBoundType

        sealed class PhpMethodInfoWithBoundType : PhpMethodInfo
        {
            public override PhpTypeInfo LateStaticType => _lateStaticType;
            readonly PhpTypeInfo _lateStaticType;

            public PhpMethodInfoWithBoundType(int index, string name, MethodInfo[] methods, PhpTypeInfo lateStaticType)
                : base(index, name, methods)
            {
                Debug.Assert(lateStaticType != null);
                _lateStaticType = lateStaticType;
            }
        }

        #endregion

        /// <summary>
        /// Creates instance of <see cref="PhpMethodInfo"/>.
        /// </summary>
        public static PhpMethodInfo Create(int index, string name, MethodInfo[] methods, PhpTypeInfo callertype = null)
        {
            if (callertype != null)
            {
                if (callertype.Type.IsClass && methods.Any(Dynamic.BinderHelpers.HasLateStaticParameter))
                {
                    // if method requires late static bound type, remember it:
                    return new PhpMethodInfoWithBoundType(index, name, methods, lateStaticType: callertype);
                }

                // reuse PhpMethodInfo from base type if possible
                if (AllDeclaredInBase(methods, callertype.Type.AsType()) &&
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

        PhpInvokable _lazyDelegate;

        /// <summary>
        /// Array of CLR methods. Cannot be <c>null</c> or empty.
        /// </summary>
        public override MethodInfo[] Methods => _methods;
        readonly MethodInfo[] _methods;

        /// <summary>Optional. Bound static type.</summary>
        public virtual PhpTypeInfo LateStaticType => null;

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
                Debug.Assert(_methods.All(TypeMembersUtils.IsStatic));
                return (ctx, args) => Invoke(ctx, null, args);
            }
        }

        public override PhpValue Invoke(Context ctx, object target, params PhpValue[] arguments) => PhpInvokable(ctx, target, arguments);
    }
}
