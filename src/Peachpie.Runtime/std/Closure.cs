using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Reflection;

[PhpType(PhpTypeAttribute.InheritName)]
public sealed class Closure : IPhpCallable
{
    /// <summary>Actual anonymous function.</summary>
    internal readonly IPhpCallable _callable;

    /// <summary>Current runtime context.</summary>
    internal readonly Context _ctx;

    /// <summary>Current class scope. (class context, self, bound scope)</summary>
    internal readonly RuntimeTypeHandle _classCtx;

    /// <summary>Reference to <c>this</c> instance. Can be <c>null</c>.</summary>
    internal readonly object _this;

    /// <summary>
    /// Fixed (use) parameters to be passed to <see cref="_callable"/>.
    /// </summary>
    readonly PhpArray @static;

    /// <summary>
    /// Anonymous function parameters, for dumping only.
    /// </summary>
    readonly PhpArray parameter;

    /// <summary>
    /// Constructs the closure.
    /// </summary>
    internal Closure(Context/*!*/ctx, IPhpCallable/*!*/routine, object @this, RuntimeTypeHandle scope, PhpArray/*!*/parameter, PhpArray/*!*/@static)
    {
        Debug.Assert(ctx != null);
        Debug.Assert(routine != null);
        Debug.Assert(parameter != null);
        Debug.Assert(@static != null);

        _callable = routine;
        _ctx = ctx;
        _this = @this;
        _classCtx = scope;

        this.parameter = parameter;
        this.@static = @static;
    }

    /// <summary>
    /// Duplicates a closure with a specific bound object and class scope.
    /// </summary>
    public static Closure bind(Closure closure, object newthis, string newscope = null) => closure.bindTo(newthis, newscope);

    /// <summary>
    /// Create and return a new anonymous function from given callable using the current scope.
    /// This method checks if the callable is callable in the current scope and throws a TypeError if it is not.
    /// </summary>
    public static Closure fromCallable(Context ctx, [ImportCallerClass]RuntimeTypeHandle scope, IPhpCallable callable)
    {
        if (callable == null)
        {
            throw new ArgumentNullException(nameof(callable));
        }

        if (callable is Closure)
        {
            return (Closure)callable;
        }

        //
        return new Closure(ctx, callable, null, scope, PhpArray.Empty, PhpArray.Empty);
    }

    /// <summary>
    /// Duplicates the closure with a new bound object and class scope.
    /// </summary>
    public Closure bindTo(object newthis, string newscope = null)
    {
        Debug.Assert(newscope == null, "newscope is not supported yet.");

        // create new Closure with updated '$this'

        if (!ReferenceEquals(_this, newthis))   // TODO: newscope
        {
            return new Closure(_ctx, _callable,
                @this: newthis,
                scope: ReferenceEquals(newthis, null) ? default(RuntimeTypeHandle) : newthis.GetType().TypeHandle,
                parameter: parameter,
                @static: @static);
        }

        // '$this' was not changed
        return this;
    }

    /// <summary>
    /// Binds and calls the closure.
    /// </summary>
    public PhpValue call(object newthis, params PhpValue[] arguments)
    {
        return bindTo(newthis).__invoke(arguments);
    }

    /// <summary>
    /// Magic method <c>__invoke</c> invokes the anonymous function with given arguments.
    /// </summary>
    public PhpValue __invoke(params PhpValue[] arguments)
    {
        if (_callable is PhpAnonymousRoutineInfo)
        {
            // { Closure, ... @static, ... arguments }

            var newargs = new PhpValue[1 + @static.Count + arguments.Length];

            newargs[0] = PhpValue.FromClass(this);

            if (@static.Count != 0)
            {
                @static.CopyValuesTo(newargs, 1);
            }

            //
            Array.Copy(arguments, 0, newargs, 1 + @static.Count, arguments.Length);

            return _callable.Invoke(_ctx, newargs);
        }
        else
        {
            Debug.Assert(@static.Count == 0);
            return _callable.Invoke(_ctx, arguments);
        }
    }

    /// <summary>
    /// Implementation of <see cref="IPhpCallable"/>, invokes the anonymous function.
    /// </summary>
    PhpValue IPhpCallable.Invoke(Context ctx, params PhpValue[] arguments) => __invoke(arguments);

    PhpValue IPhpCallable.ToPhpValue() => PhpValue.FromClass(this);
}
