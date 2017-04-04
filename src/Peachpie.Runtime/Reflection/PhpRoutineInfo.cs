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
    public abstract class RoutineInfo
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
        /// <returns>Instance of routine info with uninitialized slot index and unbound delegate.</returns>
        public static RoutineInfo CreateUserRoutine(string name, RuntimeMethodHandle handle) => new PhpRoutineInfo(name, handle);

        /// <summary>
        /// Creates instance of <see cref="RoutineInfo"/> representing a CLR methods (handling ovberloads).
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="handles">CLR methods.</param>
        /// <returns>Instance of routine info with uninitialized slot index and unbound delegate.</returns>
        public static RoutineInfo CreateUserRoutine(string name, MethodInfo[] handles) => new PhpMethodInfo(0, name, handles);

        /// <summary>
        /// Creates user routine from a CLR delegate.
        /// </summary>
        /// <param name="name">PHP routine name.</param>
        /// <param name="delegate">.NET delegate.</param>
        /// <returns>Instance of routine info with uninitialized slot index and unbound delegate.</returns>
        public static RoutineInfo CreateUserRoutine(string name, Delegate @delegate) => new DelegateRoutineInfo(name, @delegate);
    }

    internal class PhpRoutineInfo : RoutineInfo
    {
        RuntimeMethodHandle _handle;
        PhpCallable _lazyDelegate;

        /// <summary>
        /// CLR method handle.
        /// </summary>
        public RuntimeMethodHandle Handle => _handle;

        public override int GetHashCode() => _handle.GetHashCode();

        public override MethodInfo[] Methods => new [] { (MethodInfo)MethodBase.GetMethodFromHandle(_handle) };

        public override PhpCallable PhpCallable => _lazyDelegate ?? (_lazyDelegate = BindDelegate());

        PhpCallable BindDelegate()
        {
            return Dynamic.BinderHelpers.BindToPhpCallable((MethodInfo)MethodBase.GetMethodFromHandle(_handle));
        }

        public PhpRoutineInfo(string name, RuntimeMethodHandle handle)
            : base(0, name)
        {
            _handle = handle;
        }
    }

    internal class DelegateRoutineInfo : RoutineInfo
    {
        Delegate _delegate;
        PhpCallable _lazyDelegate;

        public override MethodInfo[] Methods => new[] { _delegate.GetMethodInfo() };

        internal override object Target => _delegate.Target;

        public override int GetHashCode() => _delegate.GetHashCode();

        public override PhpCallable PhpCallable => _lazyDelegate ?? (_lazyDelegate = BindDelegate());

        PhpCallable BindDelegate()
        {
            return (_delegate as PhpCallable) ?? Dynamic.BinderHelpers.BindToPhpCallable(_delegate.GetMethodInfo());
        }

        public DelegateRoutineInfo(string name, Delegate @delegate)
            : base(0, name)
        {
            if (@delegate == null)
            {
                throw new ArgumentNullException(nameof(@delegate));
            }

            _delegate = @delegate;
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
        PhpInvokable _lazyDelegate;

        /// <summary>
        /// Array of CLR methods. Cannot be <c>null</c> or empty.
        /// </summary>
        public override MethodInfo[] Methods => _methods;
        readonly MethodInfo[] _methods;

        public PhpMethodInfo(int index, string name, MethodInfo[] methods)
            : base(index, name)
        {
            _methods = methods;
        }

        internal PhpInvokable PhpInvokable => _lazyDelegate ?? BindDelegate();

        PhpInvokable BindDelegate()
        {
            return _lazyDelegate = Dynamic.BinderHelpers.BindToPhpInvokable(_methods);
        }

        public override PhpCallable PhpCallable
        {
            get
            {
                Debug.Assert(_methods.All(m => m.IsStatic));
                return (ctx, args) => Invoke(ctx, null, args);
            }
        }

        public override PhpValue Invoke(Context ctx, object target, params PhpValue[] arguments) => PhpInvokable(ctx, target, arguments);
    }
}
