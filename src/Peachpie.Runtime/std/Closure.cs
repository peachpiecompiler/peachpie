using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;

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
    readonly internal PhpInvokable invokable;

    /// <summary>
    /// Anonymous function parameters, for dumping only.
    /// </summary>
    readonly PhpArray parameter;

    /// <summary>
    /// Fixed (use) parameters to be passed to <see cref="invokable"/>.
    /// </summary>
    readonly internal PhpArray @static;

    /// <summary>
    /// Constructs the closure.
    /// </summary>
    internal Closure(object @this, PhpInvokable invokable, PhpArray parameter, PhpArray @static)
    {
        this.@this = @this;
        this.parameter = parameter;
        this.@static = @static;
        this.invokable = invokable;
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
        return new Closure(newthis, this.invokable, this.parameter, this.@static);
    }

    /// <summary>
    /// Binds and calls the closure.
    /// </summary>
    public PhpValue call(Context ctx, object newthis, params PhpValue[] arguments)
    {
        return ((IPhpCallable)bindTo(newthis)).Invoke(ctx, arguments);
    }

    /// <summary>
    /// Implementation of <see cref="IPhpCallable"/>, invokes the anonymous function.
    /// </summary>
    PhpValue IPhpCallable.Invoke(Context ctx, params PhpValue[] arguments)
    {
        if (@static != null && @static.Count != 0)
        {
            var newargs = new PhpValue[@static.Count + arguments.Length];

            @static.CopyValuesTo(newargs, 0);
            Array.Copy(arguments, 0, newargs, @static.Count, arguments.Length);
        }

        return this.invokable(ctx, @this, arguments);
    }

    PhpValue IPhpCallable.ToPhpValue() => PhpValue.FromClass(this);
}
