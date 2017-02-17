using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Reflection;

[PhpType("Closure")]
public sealed class Closure : IPhpCallable
{
    /// <summary>
    /// Actual anonymous function.
    /// </summary>
    readonly internal RoutineInfo routine;

    /// <summary>
    /// Fixed (use) parameters to be passed to <see cref="routine"/>.
    /// </summary>
    readonly PhpArray @static;
    
    /// <summary>
    /// Anonymous function parameters, for dumping only.
    /// </summary>
    readonly PhpArray parameter;

    /// <summary>
    /// Constructs the closure.
    /// </summary>
    internal Closure(RoutineInfo routine, PhpArray parameter, PhpArray @static)
    {
        Debug.Assert(routine != null);
        Debug.Assert(parameter != null);
        Debug.Assert(@static != null);

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
        // create new Closure with updated "this" fixed argument

        PhpValue oldthis;
        if (@static.TryGetValue("this", out oldthis))
        {
            if (!object.ReferenceEquals(oldthis.Object, newthis))
            {
                return new Closure(this.routine, this.parameter, this.@static);
            }
        }
        else
        {
            // TODO: Err
        }

        // "this" was not changed
        return this;
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
        // { ... @static, ... arguments }

        var newargs = new PhpValue[@static.Count + arguments.Length];

        if (@static.Count != 0)
        {
            @static.CopyValuesTo(newargs, 0);
        }

        //
        Array.Copy(arguments, 0, newargs, @static.Count, arguments.Length);

        //
        return this.routine.PhpCallable(ctx, newargs);
    }

    /// <summary>
    /// Implementation of <see cref="IPhpCallable"/>, invokes the anonymous function.
    /// </summary>
    PhpValue IPhpCallable.Invoke(Context ctx, params PhpValue[] arguments) => __invoke(ctx, arguments);

    PhpValue IPhpCallable.ToPhpValue() => PhpValue.FromClass(this);
}
