﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using Pchp.Core;
using Pchp.Core.Reflection;
using Pchp.Core.Resources;

[PhpType(PhpTypeAttribute.InheritName), PhpExtension("Core")]
public sealed class Closure : IPhpCallable, IPhpPrintable
{
    /// <summary>Actual anonymous function.</summary>
    internal readonly IPhpCallable _callable;

    /// <summary>Current runtime context.</summary>
    internal readonly Context _ctx;

    /// <summary>Current class scope. (class context)</summary>
    internal readonly RuntimeTypeHandle _scope;

    /// <summary>Late static bound type if it differs from <see cref="_scope"/>. Otherwise <c>null</c> reference.</summary>
    internal readonly PhpTypeInfo _statictype;

    /// <summary>Reference to <c>this</c> instance. Can be <c>null</c>.</summary>
    internal readonly object _this;

    /// <summary>
    /// Fixed (use) parameters to be passed to <see cref="_callable"/>.
    /// </summary>
    internal readonly PhpArray @static;

    /// <summary>
    /// Anonymous function parameters, for dumping only.
    /// </summary>
    internal readonly PhpArray parameter;

    /// <summary>
    /// Explicitly provide properties to be printed in var_dump and print_r.
    /// </summary>
    IEnumerable<KeyValuePair<string, PhpValue>> IPhpPrintable.Properties
    {
        get
        {
            // 1. static
            if (@static.Count != 0)
            {
                yield return new KeyValuePair<string, PhpValue>(nameof(@static), @static);
            }

            // 2. parameter
            if (parameter.Count != 0)
            {
                yield return new KeyValuePair<string, PhpValue>(nameof(parameter), parameter);
            }
        }
    }

    /// <summary>
    /// Constructs the closure.
    /// </summary>
    internal Closure(Context/*!*/ctx, IPhpCallable/*!*/routine, object @this, RuntimeTypeHandle scope, PhpTypeInfo statictype, PhpArray/*!*/parameter, PhpArray/*!*/@static)
    {
        Debug.Assert(ctx != null);
        Debug.Assert(routine != null);
        Debug.Assert(parameter != null);
        Debug.Assert(@static != null);

        _callable = routine;
        _ctx = ctx;
        _this = @this;
        _scope = scope;
        _statictype = statictype;

        this.parameter = parameter;
        this.@static = @static;
    }

    /// <summary>
    /// Duplicates a closure with a specific bound object and class scope.
    /// </summary>
    public static Closure bind(Closure closure, object newthis, PhpValue newscope = default(PhpValue)) => closure.bindTo(newthis, newscope);

    /// <summary>
    /// Create and return a new anonymous function from given callable using the current scope.
    /// This method checks if the callable is callable in the current scope and throws a TypeError if it is not.
    /// </summary>
    public static Closure fromCallable(Context ctx, IPhpCallable callable)
    {
        if (callable is Closure closure)
        {
            return closure;
        }

        if (callable != null && PhpVariable.IsValidBoundCallback(ctx, callable))
        {
            return new Closure(ctx, callable, null, default, null, PhpArray.Empty, PhpArray.Empty);
        }

        // TypeError: Failed to create closure from callable
        throw PhpException.TypeErrorException(ErrResources.cannot_create_closure_from_callable);
    }

    /// <summary>
    /// Duplicates the closure with a new bound object and class scope.
    /// </summary>
    public Closure bindTo(object newthis, PhpValue? newscope = default)
    {
        // create new Closure with updated '$this' and `scope`

        return new Closure(_ctx, _callable, newthis, ResolveNewScope(newthis, newscope), null/*static is replaced with new scope*/, parameter, @static);
    }

    internal RuntimeTypeHandle ResolveNewScope(object newthis, PhpValue? newscope = default)
    {
        if (newscope.HasValue)
        {
            // Operators.TypeNameOrObjectToType(newscope) + handle "static"

            object obj;
            string str;

            if ((obj = (newscope.Value.AsObject())) != null)
            {
                return obj.GetType().TypeHandle;
            }
            else if ((str = PhpVariable.AsString(newscope.Value)) != null)
            {
                return "static".Equals(str, StringComparison.OrdinalIgnoreCase)
                    ? _scope
                    : _ctx.GetDeclaredTypeOrThrow(str, true).TypeHandle;
            }
            else if (newscope.Value.IsNull)
            {
                return default;
            }
            else
            {
                throw new ArgumentException(nameof(newscope));
            }
        }

        return ReferenceEquals(newthis, null) ? _scope : newthis.GetType().TypeHandle;
    }

    /// <summary>
    /// Binds and calls the closure.
    /// </summary>
    public PhpValue call(object newthis, params ReadOnlySpan<PhpValue> parameters)
    {
        return bindTo(newthis).__invoke(parameters);
    }

    /// <summary>
    /// Magic method <c>__invoke</c> invokes the anonymous function with given arguments.
    /// </summary>
    public PhpValue __invoke(params ReadOnlySpan<PhpValue> parameters)
    {
        if (_callable is PhpAnonymousRoutineInfo)
        {
            var pool = ArrayPool<PhpValue>.Shared;

            var newArgsLength = 1 + @static.Count + parameters.Length;
            var newArgsBuffer = pool.Rent(newArgsLength);
            var newArgs = newArgsBuffer.AsSpan(..newArgsLength);

            // { Closure, ... @static, ... parameters }
            newArgs[0] = PhpValue.FromClass(this);

            if (@static.Count != 0)
            {
                @static.CopyValuesTo(newArgs[1..]);
            }

            parameters.CopyTo(newArgs[(1 + @static.Count)..]);

            //
            try
            {
                return _callable.Invoke(_ctx, newArgs);
            }
            finally
            {
                pool.Return(newArgsBuffer, true);
            }
        }
        else
        {
            Debug.Assert(@static.Count == 0);
            return _callable.Invoke(_ctx, parameters);
        }
    }

    #region IPhpCallable

    /// <summary>
    /// Implementation of <see cref="IPhpCallable"/>, invokes the anonymous function.
    /// </summary>
    PhpValue IPhpCallable.Invoke(Context ctx, params ReadOnlySpan<PhpValue> arguments) => __invoke(arguments);

    PhpValue IPhpCallable.ToPhpValue() => PhpValue.FromClass(this);

    #endregion
}
