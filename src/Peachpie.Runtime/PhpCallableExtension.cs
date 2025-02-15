using System;
using System.Buffers;

namespace Pchp.Core;

static partial class PhpCallableExtension
{

    public static PhpValue Invoke(this IPhpCallable callable, Context context, PhpValue @parameter0)
    {
        var buffer = ArrayPool<PhpValue>.Shared.Rent(1);
        try
        {
            var span = new Span<PhpValue>(buffer)[..1];
            span[0] = @parameter0;
            return callable.Invoke(context, span);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(buffer, true);
        }
    }
    
    
    public static PhpValue Invoke(this IPhpCallable callable, Context context, PhpValue @parameter0, PhpValue @parameter1)
    {
        var buffer = ArrayPool<PhpValue>.Shared.Rent(2);
        try
        {
            var span = new Span<PhpValue>(buffer)[..2];
            span[0] = @parameter0;
            span[1] = @parameter1;
            return callable.Invoke(context, span);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(buffer, true);
        }
    }
    
    public static PhpValue Invoke(
        this IPhpCallable callable,
        Context context, 
        PhpValue @parameter0, 
        PhpValue @parameter1, 
        PhpValue @parameter2)
    {
        const int arity = 3;
        var buffer = ArrayPool<PhpValue>.Shared.Rent(arity);
        try
        {
            var span = new Span<PhpValue>(buffer)[..arity];
            span[0] = @parameter0;
            span[1] = @parameter1;
            span[2] = @parameter2;
            return callable.Invoke(context, span);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(buffer, true);
        }
    }
    
    public static PhpValue Invoke(
        this IPhpCallable callable,
        Context context, 
        PhpValue @parameter0, 
        PhpValue @parameter1, 
        PhpValue @parameter2, 
        PhpValue @parameter3)
    {
        const int arity = 4;
        var buffer = ArrayPool<PhpValue>.Shared.Rent(arity);
        try
        {
            var span = new Span<PhpValue>(buffer)[..arity];
            span[0] = @parameter0;
            span[1] = @parameter1;
            span[2] = @parameter2;
            span[3] = @parameter3;
            return callable.Invoke(context, span);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(buffer, true);
        }
    }
    
    public static PhpValue Invoke(
        this IPhpCallable callable,
        Context context, 
        PhpValue @parameter0, 
        PhpValue @parameter1, 
        PhpValue @parameter2, 
        PhpValue @parameter3, 
        PhpValue @parameter4)
    {
        const int arity = 5;
        var buffer = ArrayPool<PhpValue>.Shared.Rent(arity);
        try
        {
            var span = new Span<PhpValue>(buffer)[..arity];
            span[0] = @parameter0;
            span[1] = @parameter1;
            span[2] = @parameter2;
            span[3] = @parameter3;
            span[4] = @parameter4;
            return callable.Invoke(context, span);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(buffer, true);
        }
    }
    
    public static PhpValue Invoke(
        this IPhpCallable callable,
        Context context, 
        PhpValue @parameter0, 
        PhpValue @parameter1, 
        PhpValue @parameter2, 
        PhpValue @parameter3, 
        PhpValue @parameter4, 
        PhpValue @parameter5)
    {
        const int arity = 6;
        var buffer = ArrayPool<PhpValue>.Shared.Rent(arity);
        try
        {
            var span = new Span<PhpValue>(buffer)[..arity];
            span[0] = @parameter0;
            span[1] = @parameter1;
            span[2] = @parameter2;
            span[3] = @parameter3;
            span[4] = @parameter4;
            span[5] = @parameter5;
            return callable.Invoke(context, span);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(buffer, true);
        }
    }
    
    public static PhpValue Invoke(
        this IPhpCallable callable,
        Context context, 
        PhpValue @parameter0, 
        PhpValue @parameter1, 
        PhpValue @parameter2, 
        PhpValue @parameter3, 
        PhpValue @parameter4, 
        PhpValue @parameter5, 
        PhpValue @parameter6)
    {
        const int arity = 7;
        var buffer = ArrayPool<PhpValue>.Shared.Rent(arity);
        try
        {
            var span = new Span<PhpValue>(buffer)[..arity];
            span[0] = @parameter0;
            span[1] = @parameter1;
            span[2] = @parameter2;
            span[3] = @parameter3;
            span[4] = @parameter4;
            span[5] = @parameter5;
            span[6] = @parameter6;
            return callable.Invoke(context, span);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(buffer, true);
        }
    }
    
    public static PhpValue Invoke(
        this IPhpCallable callable,
        Context context, 
        PhpValue @parameter0, 
        PhpValue @parameter1, 
        PhpValue @parameter2, 
        PhpValue @parameter3, 
        PhpValue @parameter4, 
        PhpValue @parameter5, 
        PhpValue @parameter6, 
        PhpValue @parameter7)
    {
        const int arity = 8;
        var buffer = ArrayPool<PhpValue>.Shared.Rent(arity);
        try
        {
            var span = new Span<PhpValue>(buffer)[..arity];
            span[0] = @parameter0;
            span[1] = @parameter1;
            span[2] = @parameter2;
            span[3] = @parameter3;
            span[4] = @parameter4;
            span[5] = @parameter5;
            span[6] = @parameter6;
            span[7] = @parameter7;
            return callable.Invoke(context, span);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(buffer, true);
        }
    }
}