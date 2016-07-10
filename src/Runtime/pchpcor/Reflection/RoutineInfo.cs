using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Reflection
{
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

        /// <summary>
        /// Gets routine runtime method handle.
        /// Gets more handles in case of overloads.
        /// </summary>
        public abstract RuntimeMethodHandle[] Handles { get; }

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

        public override RuntimeMethodHandle[] Handles => new RuntimeMethodHandle[] { _handle };

        public override PhpCallable PhpCallable => _lazyDelegate ?? (_lazyDelegate = BindDelegate());

        PhpCallable BindDelegate()
        {
            // TODO: move to Pcph.Core.Dynamic { }

            // (Context ctx, PhpValue[] arguments)
            var ps = new ParameterExpression[] { Expression.Parameter(typeof(Context), "ctx"), Expression.Parameter(typeof(PhpValue[]), "arguments") };
            var target = (MethodInfo)MethodBase.GetMethodFromHandle(_handle);
            var target_ps = target.GetParameters();

            // Convert( Expression.Call( method, Convert(args[0]), Convert(args[1]), ... ), PhpValue)

            // TODO: bind arguments properly, merge with CallFunctionBinder, handle vararg, handle missing mandatory args

            // bind parameters
            var args = new List<Expression>(target_ps.Length);
            int source_index = 0;
            foreach (var p in target_ps)
            {
                if (args.Count == 0)
                {
                    if (p.ParameterType == typeof(Context))
                    {
                        args.Add(ps[0]);
                        continue;
                    }
                }

                //
                args.Add(Dynamic.ConvertExpression.Bind(Expression.ArrayIndex(ps[1], Expression.Constant(source_index++, typeof(int))), p.ParameterType));
            }

            // invoke target
            var invocation = Dynamic.ConvertExpression.Bind(Expression.Call(target, args), typeof(PhpValue));
            
            // compile & create delegate
            var lambda = Expression.Lambda<PhpCallable>(invocation, target.Name, true, ps);
            return lambda.Compile();
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

        public override PhpCallable PhpCallable => _lazyDelegate ?? (_lazyDelegate = BindDelegate());

        PhpCallable BindDelegate()
        {
            if (_handles.Length == 1)
            {

            }
            else
            {
                // TODO: runtime overload resolution
            }

            throw new NotImplementedException();
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
}
