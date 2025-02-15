using System;
using System.Buffers;
namespace Pchp.Core;
using System.Runtime.CompilerServices;

partial interface IPhpCallable
{
    /// <summary>
    /// Invokes the object with given arguments.
    /// Using ArrayPool to avoid allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed PhpValue Invoke(Context ctx) => Invoke(ctx, ReadOnlySpan<PhpValue>.Empty);

    /// <summary>
    /// Invokes the object with given arguments.
    /// Using ArrayPool to avoid allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0)
    {
        var phpArgs = ArrayPool<PhpValue>.Shared.Rent(1);
        try
        {
            phpArgs[0] = p0;
            return Invoke(ctx, new ReadOnlySpan<PhpValue>(phpArgs)[..1]);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(phpArgs, true);
        }
    }

    /// <summary>
    /// Invokes the object with given arguments.
    /// Using ArrayPool to avoid allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0,
        PhpValue p1)
    {
        var phpArgs = ArrayPool<PhpValue>.Shared.Rent(2);
        try
        {
            phpArgs[0] = p0;
            phpArgs[1] = p1;
            return Invoke(ctx, new ReadOnlySpan<PhpValue>(phpArgs)[..2]);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(phpArgs, true);
        }
    }

    /// <summary>
    /// Invokes the object with given arguments.
    /// Using ArrayPool to avoid allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0,
        PhpValue p1,
        PhpValue p2)
    {
        var phpArgs = ArrayPool<PhpValue>.Shared.Rent(3);
        try
        {
            phpArgs[0] = p0;
            phpArgs[1] = p1;
            phpArgs[2] = p2;
            return Invoke(ctx, new ReadOnlySpan<PhpValue>(phpArgs)[..3]);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(phpArgs, true);
        }
    }

    /// <summary>
    /// Invokes the object with given arguments.
    /// Using ArrayPool to avoid allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0,
        PhpValue p1,
        PhpValue p2,
        PhpValue p3)
    {
        var phpArgs = ArrayPool<PhpValue>.Shared.Rent(4);
        try
        {
            phpArgs[0] = p0;
            phpArgs[1] = p1;
            phpArgs[2] = p2;
            phpArgs[3] = p3;
            return Invoke(ctx, new ReadOnlySpan<PhpValue>(phpArgs)[..4]);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(phpArgs, true);
        }
    }

    /// <summary>
    /// Invokes the object with given arguments.
    /// Using ArrayPool to avoid allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0,
        PhpValue p1,
        PhpValue p2,
        PhpValue p3,
        PhpValue p4)
    {
        var phpArgs = ArrayPool<PhpValue>.Shared.Rent(5);
        try
        {
            phpArgs[0] = p0;
            phpArgs[1] = p1;
            phpArgs[2] = p2;
            phpArgs[3] = p3;
            phpArgs[4] = p4;
            return Invoke(ctx, new ReadOnlySpan<PhpValue>(phpArgs)[..5]);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(phpArgs, true);
        }
    }

    /// <summary>
    /// Invokes the object with given arguments.
    /// Using ArrayPool to avoid allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0,
        PhpValue p1,
        PhpValue p2,
        PhpValue p3,
        PhpValue p4,
        PhpValue p5)
    {
        var phpArgs = ArrayPool<PhpValue>.Shared.Rent(6);
        try
        {
            phpArgs[0] = p0;
            phpArgs[1] = p1;
            phpArgs[2] = p2;
            phpArgs[3] = p3;
            phpArgs[4] = p4;
            phpArgs[5] = p5;
            return Invoke(ctx, new ReadOnlySpan<PhpValue>(phpArgs)[..6]);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(phpArgs, true);
        }
    }

    /// <summary>
    /// Invokes the object with given arguments.
    /// Using ArrayPool to avoid allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0,
        PhpValue p1,
        PhpValue p2,
        PhpValue p3,
        PhpValue p4,
        PhpValue p5,
        PhpValue p6)
    {
        var phpArgs = ArrayPool<PhpValue>.Shared.Rent(7);
        try
        {
            phpArgs[0] = p0;
            phpArgs[1] = p1;
            phpArgs[2] = p2;
            phpArgs[3] = p3;
            phpArgs[4] = p4;
            phpArgs[5] = p5;
            phpArgs[6] = p6;
            return Invoke(ctx, new ReadOnlySpan<PhpValue>(phpArgs)[..7]);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(phpArgs, true);
        }
    }

    /// <summary>
    /// Invokes the object with given arguments.
    /// Using ArrayPool to avoid allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0,
        PhpValue p1,
        PhpValue p2,
        PhpValue p3,
        PhpValue p4,
        PhpValue p5,
        PhpValue p6,
        PhpValue p7)
    {
        var phpArgs = ArrayPool<PhpValue>.Shared.Rent(8);
        try
        {
            phpArgs[0] = p0;
            phpArgs[1] = p1;
            phpArgs[2] = p2;
            phpArgs[3] = p3;
            phpArgs[4] = p4;
            phpArgs[5] = p5;
            phpArgs[6] = p6;
            phpArgs[7] = p7;
            return Invoke(ctx, new ReadOnlySpan<PhpValue>(phpArgs)[..8]);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(phpArgs, true);
        }
    }

    /// <summary>
    /// Invokes the object with given arguments.
    /// Using ArrayPool to avoid allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0,
        PhpValue p1,
        PhpValue p2,
        PhpValue p3,
        PhpValue p4,
        PhpValue p5,
        PhpValue p6,
        PhpValue p7,
        PhpValue p8)
    {
        var phpArgs = ArrayPool<PhpValue>.Shared.Rent(9);
        try
        {
            phpArgs[0] = p0;
            phpArgs[1] = p1;
            phpArgs[2] = p2;
            phpArgs[3] = p3;
            phpArgs[4] = p4;
            phpArgs[5] = p5;
            phpArgs[6] = p6;
            phpArgs[7] = p7;
            phpArgs[8] = p8;
            return Invoke(ctx, new ReadOnlySpan<PhpValue>(phpArgs)[..9]);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(phpArgs, true);
        }
    }

    /// <summary>
    /// Invokes the object with given arguments.
    /// Using ArrayPool to avoid allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0,
        PhpValue p1,
        PhpValue p2,
        PhpValue p3,
        PhpValue p4,
        PhpValue p5,
        PhpValue p6,
        PhpValue p7,
        PhpValue p8,
        PhpValue p9)
    {
        var phpArgs = ArrayPool<PhpValue>.Shared.Rent(10);
        try
        {
            phpArgs[0] = p0;
            phpArgs[1] = p1;
            phpArgs[2] = p2;
            phpArgs[3] = p3;
            phpArgs[4] = p4;
            phpArgs[5] = p5;
            phpArgs[6] = p6;
            phpArgs[7] = p7;
            phpArgs[8] = p8;
            phpArgs[9] = p9;
            return Invoke(ctx, new ReadOnlySpan<PhpValue>(phpArgs)[..10]);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(phpArgs, true);
        }
    }

    /// <summary>
    /// Invokes the object with given arguments.
    /// Using ArrayPool to avoid allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0,
        PhpValue p1,
        PhpValue p2,
        PhpValue p3,
        PhpValue p4,
        PhpValue p5,
        PhpValue p6,
        PhpValue p7,
        PhpValue p8,
        PhpValue p9,
        PhpValue p10)
    {
        var phpArgs = ArrayPool<PhpValue>.Shared.Rent(11);
        try
        {
            phpArgs[0] = p0;
            phpArgs[1] = p1;
            phpArgs[2] = p2;
            phpArgs[3] = p3;
            phpArgs[4] = p4;
            phpArgs[5] = p5;
            phpArgs[6] = p6;
            phpArgs[7] = p7;
            phpArgs[8] = p8;
            phpArgs[9] = p9;
            phpArgs[10] = p10;
            return Invoke(ctx, new ReadOnlySpan<PhpValue>(phpArgs)[..11]);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(phpArgs, true);
        }
    }

    /// <summary>
    /// Invokes the object with given arguments.
    /// Using ArrayPool to avoid allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0,
        PhpValue p1,
        PhpValue p2,
        PhpValue p3,
        PhpValue p4,
        PhpValue p5,
        PhpValue p6,
        PhpValue p7,
        PhpValue p8,
        PhpValue p9,
        PhpValue p10,
        PhpValue p11)
    {
        var phpArgs = ArrayPool<PhpValue>.Shared.Rent(12);
        try
        {
            phpArgs[0] = p0;
            phpArgs[1] = p1;
            phpArgs[2] = p2;
            phpArgs[3] = p3;
            phpArgs[4] = p4;
            phpArgs[5] = p5;
            phpArgs[6] = p6;
            phpArgs[7] = p7;
            phpArgs[8] = p8;
            phpArgs[9] = p9;
            phpArgs[10] = p10;
            phpArgs[11] = p11;
            return Invoke(ctx, new ReadOnlySpan<PhpValue>(phpArgs)[..12]);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(phpArgs, true);
        }
    }

    /// <summary>
    /// Invokes the object with given arguments.
    /// Using ArrayPool to avoid allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0,
        PhpValue p1,
        PhpValue p2,
        PhpValue p3,
        PhpValue p4,
        PhpValue p5,
        PhpValue p6,
        PhpValue p7,
        PhpValue p8,
        PhpValue p9,
        PhpValue p10,
        PhpValue p11,
        PhpValue p12)
    {
        var phpArgs = ArrayPool<PhpValue>.Shared.Rent(13);
        try
        {
            phpArgs[0] = p0;
            phpArgs[1] = p1;
            phpArgs[2] = p2;
            phpArgs[3] = p3;
            phpArgs[4] = p4;
            phpArgs[5] = p5;
            phpArgs[6] = p6;
            phpArgs[7] = p7;
            phpArgs[8] = p8;
            phpArgs[9] = p9;
            phpArgs[10] = p10;
            phpArgs[11] = p11;
            phpArgs[12] = p12;
            return Invoke(ctx, new ReadOnlySpan<PhpValue>(phpArgs)[..13]);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(phpArgs, true);
        }
    }

    /// <summary>
    /// Invokes the object with given arguments.
    /// Using ArrayPool to avoid allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0,
        PhpValue p1,
        PhpValue p2,
        PhpValue p3,
        PhpValue p4,
        PhpValue p5,
        PhpValue p6,
        PhpValue p7,
        PhpValue p8,
        PhpValue p9,
        PhpValue p10,
        PhpValue p11,
        PhpValue p12,
        PhpValue p13)
    {
        var phpArgs = ArrayPool<PhpValue>.Shared.Rent(14);
        try
        {
            phpArgs[0] = p0;
            phpArgs[1] = p1;
            phpArgs[2] = p2;
            phpArgs[3] = p3;
            phpArgs[4] = p4;
            phpArgs[5] = p5;
            phpArgs[6] = p6;
            phpArgs[7] = p7;
            phpArgs[8] = p8;
            phpArgs[9] = p9;
            phpArgs[10] = p10;
            phpArgs[11] = p11;
            phpArgs[12] = p12;
            phpArgs[13] = p13;
            return Invoke(ctx, new ReadOnlySpan<PhpValue>(phpArgs)[..14]);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(phpArgs, true);
        }
    }

    /// <summary>
    /// Invokes the object with given arguments.
    /// Using ArrayPool to avoid allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0,
        PhpValue p1,
        PhpValue p2,
        PhpValue p3,
        PhpValue p4,
        PhpValue p5,
        PhpValue p6,
        PhpValue p7,
        PhpValue p8,
        PhpValue p9,
        PhpValue p10,
        PhpValue p11,
        PhpValue p12,
        PhpValue p13,
        PhpValue p14)
    {
        var phpArgs = ArrayPool<PhpValue>.Shared.Rent(15);
        try
        {
            phpArgs[0] = p0;
            phpArgs[1] = p1;
            phpArgs[2] = p2;
            phpArgs[3] = p3;
            phpArgs[4] = p4;
            phpArgs[5] = p5;
            phpArgs[6] = p6;
            phpArgs[7] = p7;
            phpArgs[8] = p8;
            phpArgs[9] = p9;
            phpArgs[10] = p10;
            phpArgs[11] = p11;
            phpArgs[12] = p12;
            phpArgs[13] = p13;
            phpArgs[14] = p14;
            return Invoke(ctx, new ReadOnlySpan<PhpValue>(phpArgs)[..15]);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(phpArgs, true);
        }
    }

    /// <summary>
    /// Invokes the object with given arguments.
    /// Using ArrayPool to avoid allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0,
        PhpValue p1,
        PhpValue p2,
        PhpValue p3,
        PhpValue p4,
        PhpValue p5,
        PhpValue p6,
        PhpValue p7,
        PhpValue p8,
        PhpValue p9,
        PhpValue p10,
        PhpValue p11,
        PhpValue p12,
        PhpValue p13,
        PhpValue p14,
        PhpValue p15)
    {
        var phpArgs = ArrayPool<PhpValue>.Shared.Rent(16);
        try
        {
            phpArgs[0] = p0;
            phpArgs[1] = p1;
            phpArgs[2] = p2;
            phpArgs[3] = p3;
            phpArgs[4] = p4;
            phpArgs[5] = p5;
            phpArgs[6] = p6;
            phpArgs[7] = p7;
            phpArgs[8] = p8;
            phpArgs[9] = p9;
            phpArgs[10] = p10;
            phpArgs[11] = p11;
            phpArgs[12] = p12;
            phpArgs[13] = p13;
            phpArgs[14] = p14;
            phpArgs[15] = p15;
            return Invoke(ctx, new ReadOnlySpan<PhpValue>(phpArgs)[..16]);
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(phpArgs, true);
        }
    }

}