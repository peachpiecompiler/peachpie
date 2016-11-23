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
        /// Gets the routine name, cannot be <c>null</c> or empty.
        /// </summary>
        public string Name => _name;
        protected readonly string _name;

        /// <summary>
        /// Gets routine callable delegate.
        /// </summary>
        public abstract PhpCallable PhpCallable { get; }

        //ulong _aliasedParams; // bit field corresponding to parameters that are passed by reference
        //_routineFlags;    // routine requirements, accessibility

        // TODO: PHPDoc

        /// <summary>
        /// Gets routine runtime method handle.
        /// Gets more handles in case of overloads.
        /// </summary>
        public abstract RuntimeMethodHandle[] Handles { get; }

        /// <summary>
        /// Gets methods representing the routine.
        /// See <see cref="Handles"/> for more details.
        /// </summary>
        public IEnumerable<MethodBase> Methods => Handles.Select(MethodBase.GetMethodFromHandle);

        protected RoutineInfo(int index, string name)
        {
            _index = index;
            _name = name;
        }

        /// <summary>
        /// Used by compiler generated code.
        /// Creates instance of <see cref="RoutineInfo"/> representing a user PHP function.
        /// </summary>
        /// <param name="name">Functio name.</param>
        /// <param name="handle">CLR method handle.</param>
        /// <returns>Instance of routine info with uninitialized slot index and unbound delegate.</returns>
        public static RoutineInfo CreateUserRoutine(string name, RuntimeMethodHandle handle) => new PhpRoutineInfo(name, handle);
    }

    internal class PhpRoutineInfo : RoutineInfo
    {
        RuntimeMethodHandle _handle;
        PhpCallable _lazyDelegate;

        /// <summary>
        /// CLR method handle.
        /// </summary>
        public RuntimeMethodHandle Handle => _handle;

        public override RuntimeMethodHandle[] Handles => new RuntimeMethodHandle[] { _handle };

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

    internal class ClrRoutineInfo : RoutineInfo
    {
        PhpCallable _lazyDelegate;

        RuntimeMethodHandle[] _handles;

        public override RuntimeMethodHandle[] Handles => _handles;

        public override PhpCallable PhpCallable => _lazyDelegate ?? BindDelegate();

        PhpCallable BindDelegate()
        {
            return _lazyDelegate = Dynamic.BinderHelpers.BindToPhpCallable(Methods.ToArray());
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
        public MethodInfo[] Methods => _methods;
        readonly MethodInfo[] _methods;

        public PhpMethodInfo(int index, string name, MethodInfo[] methods)
            : base(index, name)
        {
            _methods = methods;
        }

        public override RuntimeMethodHandle[] Handles
        {
            get
            {
                throw new NotImplementedException();
            }
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
                // instance methods cannot be bound to PhpCallable
                throw new NotSupportedException();
            }
        }
    }
}
