using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Reflection;

[PhpType("Closure")]
public sealed class Closure : IPhpCallable
{
    /// <summary>
    /// Bound <c>$this</c> variable.
    /// Can be <c>null</c> if anonymous function is static or wasn't bound in class instance context.
    /// </summary>
    internal object @this;

    /// <summary>
    /// Actual anonymous function.
    /// </summary>
    readonly internal RoutineInfo routine;

    /// <summary>
    /// Anonymous function parameters, for dumping only.
    /// </summary>
    readonly PhpArray parameter;

    /// <summary>
    /// Fixed (use) parameters to be passed to <see cref="routine"/>.
    /// </summary>
    readonly internal PhpArray @static;

    /// <summary>
    /// Constructs the closure.
    /// </summary>
    internal Closure(object @this, RoutineInfo routine, PhpArray parameter, PhpArray @static)
    {
        this.@this = @this;
        this.routine = routine;
        this.parameter = parameter;
        this.@static = @static;
    }

    /// <summary>
    /// Duplicates a closure with a specific bound object and class scope.
    /// </summary>
    public static Closure bind(Closure closure, object newthis, string newscope = null) => closure.bindTo(newthis, newscope);

    /// <summary>
    /// Duplicates the closure with a new bound object and class scope.
    /// </summary>
    public Closure bindTo(object newthis, string newscope = null)
    {
        return new Closure(newthis, this.routine, this.parameter, this.@static);
    }

    /// <summary>
    /// Binds and calls the closure.
    /// </summary>
    public PhpValue call(Context ctx, object newthis, params PhpValue[] arguments)
    {
        return bindTo(newthis).__invoke(ctx, arguments);
    }

    /// <summary>
    /// Magic method <c>__invoke</c> invokes the anonymous function with given arguments.
    /// </summary>
    public PhpValue __invoke(Context ctx, params PhpValue[] arguments)
    {
        // { @this, ... @static, ... arguments }

        var newargs = new PhpValue[1 + @static.Count + arguments.Length];

        //
        newargs[0] = PhpValue.FromClass(@this);

        //
        if (@static.Count != 0)
        {
            @static.CopyValuesTo(newargs, 1);
        }

        //
        Array.Copy(arguments, 0, newargs, 1 + @static.Count, arguments.Length);

        //
        return this.routine.PhpCallable(ctx, newargs);
    }

    /// <summary>
    /// Implementation of <see cref="IPhpCallable"/>, invokes the anonymous function.
    /// </summary>
    PhpValue IPhpCallable.Invoke(Context ctx, params PhpValue[] arguments) => __invoke(ctx, arguments);

    PhpValue IPhpCallable.ToPhpValue() => PhpValue.FromClass(this);
}
