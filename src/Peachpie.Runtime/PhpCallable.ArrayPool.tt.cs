using System;
using System.Runtime.InteropServices;
namespace Pchp.Core;



partial interface IPhpCallable
{
    /// <summary>
    /// Invokes the callback with no arguments.
    /// </summary>
    sealed PhpValue Invoke(Context ctx) => Invoke(ctx, ReadOnlySpan<PhpValue>.Empty);

    /// <summary>
    /// Invokes the callback with given argument.
    /// </summary>
    sealed PhpValue Invoke(Context ctx, PhpValue p0) => Invoke(ctx, MemoryMarshal.CreateReadOnlySpan(ref p0, 1));

    

    /// <summary>
    /// Invokes the callback with given arguments.
    /// Uses ArrayPool to avoid allocation.
    /// </summary>
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0,
        PhpValue p1)
    {
        
        var tuple = new PhpArgTuple2
        {
            Argument0 = p0,
            Argument1 = p1,
        };
        var phpArgs = MemoryMarshal.CreateReadOnlySpan(ref tuple.Argument0, 2);
        return Invoke(ctx, phpArgs);
    }

    

    /// <summary>
    /// Invokes the callback with given arguments.
    /// Uses ArrayPool to avoid allocation.
    /// </summary>
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0,
        PhpValue p1,
        PhpValue p2)
    {
        
        var tuple = new PhpArgTuple3
        {
            Argument0 = p0,
            Argument1 = p1,
            Argument2 = p2,
        };
        var phpArgs = MemoryMarshal.CreateReadOnlySpan(ref tuple.Argument0, 3);
        return Invoke(ctx, phpArgs);
    }

    

    /// <summary>
    /// Invokes the callback with given arguments.
    /// Uses ArrayPool to avoid allocation.
    /// </summary>
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0,
        PhpValue p1,
        PhpValue p2,
        PhpValue p3)
    {
        
        var tuple = new PhpArgTuple4
        {
            Argument0 = p0,
            Argument1 = p1,
            Argument2 = p2,
            Argument3 = p3,
        };
        var phpArgs = MemoryMarshal.CreateReadOnlySpan(ref tuple.Argument0, 4);
        return Invoke(ctx, phpArgs);
    }

    

    /// <summary>
    /// Invokes the callback with given arguments.
    /// Uses ArrayPool to avoid allocation.
    /// </summary>
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0,
        PhpValue p1,
        PhpValue p2,
        PhpValue p3,
        PhpValue p4)
    {
        
        var tuple = new PhpArgTuple5
        {
            Argument0 = p0,
            Argument1 = p1,
            Argument2 = p2,
            Argument3 = p3,
            Argument4 = p4,
        };
        var phpArgs = MemoryMarshal.CreateReadOnlySpan(ref tuple.Argument0, 5);
        return Invoke(ctx, phpArgs);
    }

    

    /// <summary>
    /// Invokes the callback with given arguments.
    /// Uses ArrayPool to avoid allocation.
    /// </summary>
    sealed PhpValue Invoke(
        Context ctx,        
        PhpValue p0,
        PhpValue p1,
        PhpValue p2,
        PhpValue p3,
        PhpValue p4,
        PhpValue p5)
    {
        
        var tuple = new PhpArgTuple6
        {
            Argument0 = p0,
            Argument1 = p1,
            Argument2 = p2,
            Argument3 = p3,
            Argument4 = p4,
            Argument5 = p5,
        };
        var phpArgs = MemoryMarshal.CreateReadOnlySpan(ref tuple.Argument0, 6);
        return Invoke(ctx, phpArgs);
    }

    

    /// <summary>
    /// Invokes the callback with given arguments.
    /// Uses ArrayPool to avoid allocation.
    /// </summary>
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
        
        var tuple = new PhpArgTuple7
        {
            Argument0 = p0,
            Argument1 = p1,
            Argument2 = p2,
            Argument3 = p3,
            Argument4 = p4,
            Argument5 = p5,
            Argument6 = p6,
        };
        var phpArgs = MemoryMarshal.CreateReadOnlySpan(ref tuple.Argument0, 7);
        return Invoke(ctx, phpArgs);
    }

    

    /// <summary>
    /// Invokes the callback with given arguments.
    /// Uses ArrayPool to avoid allocation.
    /// </summary>
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
        
        var tuple = new PhpArgTuple8
        {
            Argument0 = p0,
            Argument1 = p1,
            Argument2 = p2,
            Argument3 = p3,
            Argument4 = p4,
            Argument5 = p5,
            Argument6 = p6,
            Argument7 = p7,
        };
        var phpArgs = MemoryMarshal.CreateReadOnlySpan(ref tuple.Argument0, 8);
        return Invoke(ctx, phpArgs);
    }

    

    /// <summary>
    /// Invokes the callback with given arguments.
    /// Uses ArrayPool to avoid allocation.
    /// </summary>
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
        
        var tuple = new PhpArgTuple9
        {
            Argument0 = p0,
            Argument1 = p1,
            Argument2 = p2,
            Argument3 = p3,
            Argument4 = p4,
            Argument5 = p5,
            Argument6 = p6,
            Argument7 = p7,
            Argument8 = p8,
        };
        var phpArgs = MemoryMarshal.CreateReadOnlySpan(ref tuple.Argument0, 9);
        return Invoke(ctx, phpArgs);
    }

    

    /// <summary>
    /// Invokes the callback with given arguments.
    /// Uses ArrayPool to avoid allocation.
    /// </summary>
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
        
        var tuple = new PhpArgTuple10
        {
            Argument0 = p0,
            Argument1 = p1,
            Argument2 = p2,
            Argument3 = p3,
            Argument4 = p4,
            Argument5 = p5,
            Argument6 = p6,
            Argument7 = p7,
            Argument8 = p8,
            Argument9 = p9,
        };
        var phpArgs = MemoryMarshal.CreateReadOnlySpan(ref tuple.Argument0, 10);
        return Invoke(ctx, phpArgs);
    }

    

    /// <summary>
    /// Invokes the callback with given arguments.
    /// Uses ArrayPool to avoid allocation.
    /// </summary>
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
        
        var tuple = new PhpArgTuple11
        {
            Argument0 = p0,
            Argument1 = p1,
            Argument2 = p2,
            Argument3 = p3,
            Argument4 = p4,
            Argument5 = p5,
            Argument6 = p6,
            Argument7 = p7,
            Argument8 = p8,
            Argument9 = p9,
            Argument10 = p10,
        };
        var phpArgs = MemoryMarshal.CreateReadOnlySpan(ref tuple.Argument0, 11);
        return Invoke(ctx, phpArgs);
    }

    

    /// <summary>
    /// Invokes the callback with given arguments.
    /// Uses ArrayPool to avoid allocation.
    /// </summary>
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
        
        var tuple = new PhpArgTuple12
        {
            Argument0 = p0,
            Argument1 = p1,
            Argument2 = p2,
            Argument3 = p3,
            Argument4 = p4,
            Argument5 = p5,
            Argument6 = p6,
            Argument7 = p7,
            Argument8 = p8,
            Argument9 = p9,
            Argument10 = p10,
            Argument11 = p11,
        };
        var phpArgs = MemoryMarshal.CreateReadOnlySpan(ref tuple.Argument0, 12);
        return Invoke(ctx, phpArgs);
    }

    

    /// <summary>
    /// Invokes the callback with given arguments.
    /// Uses ArrayPool to avoid allocation.
    /// </summary>
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
        
        var tuple = new PhpArgTuple13
        {
            Argument0 = p0,
            Argument1 = p1,
            Argument2 = p2,
            Argument3 = p3,
            Argument4 = p4,
            Argument5 = p5,
            Argument6 = p6,
            Argument7 = p7,
            Argument8 = p8,
            Argument9 = p9,
            Argument10 = p10,
            Argument11 = p11,
            Argument12 = p12,
        };
        var phpArgs = MemoryMarshal.CreateReadOnlySpan(ref tuple.Argument0, 13);
        return Invoke(ctx, phpArgs);
    }

    

    /// <summary>
    /// Invokes the callback with given arguments.
    /// Uses ArrayPool to avoid allocation.
    /// </summary>
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
        
        var tuple = new PhpArgTuple14
        {
            Argument0 = p0,
            Argument1 = p1,
            Argument2 = p2,
            Argument3 = p3,
            Argument4 = p4,
            Argument5 = p5,
            Argument6 = p6,
            Argument7 = p7,
            Argument8 = p8,
            Argument9 = p9,
            Argument10 = p10,
            Argument11 = p11,
            Argument12 = p12,
            Argument13 = p13,
        };
        var phpArgs = MemoryMarshal.CreateReadOnlySpan(ref tuple.Argument0, 14);
        return Invoke(ctx, phpArgs);
    }

    

    /// <summary>
    /// Invokes the callback with given arguments.
    /// Uses ArrayPool to avoid allocation.
    /// </summary>
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
        
        var tuple = new PhpArgTuple15
        {
            Argument0 = p0,
            Argument1 = p1,
            Argument2 = p2,
            Argument3 = p3,
            Argument4 = p4,
            Argument5 = p5,
            Argument6 = p6,
            Argument7 = p7,
            Argument8 = p8,
            Argument9 = p9,
            Argument10 = p10,
            Argument11 = p11,
            Argument12 = p12,
            Argument13 = p13,
            Argument14 = p14,
        };
        var phpArgs = MemoryMarshal.CreateReadOnlySpan(ref tuple.Argument0, 15);
        return Invoke(ctx, phpArgs);
    }

    

    /// <summary>
    /// Invokes the callback with given arguments.
    /// Uses ArrayPool to avoid allocation.
    /// </summary>
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
        
        var tuple = new PhpArgTuple16
        {
            Argument0 = p0,
            Argument1 = p1,
            Argument2 = p2,
            Argument3 = p3,
            Argument4 = p4,
            Argument5 = p5,
            Argument6 = p6,
            Argument7 = p7,
            Argument8 = p8,
            Argument9 = p9,
            Argument10 = p10,
            Argument11 = p11,
            Argument12 = p12,
            Argument13 = p13,
            Argument14 = p14,
            Argument15 = p15,
        };
        var phpArgs = MemoryMarshal.CreateReadOnlySpan(ref tuple.Argument0, 16);
        return Invoke(ctx, phpArgs);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PhpArgTuple2
    {  
        public PhpValue Argument0;
        public PhpValue Argument1;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PhpArgTuple3
    {  
        public PhpValue Argument0;
        public PhpValue Argument1;
        public PhpValue Argument2;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PhpArgTuple4
    {  
        public PhpValue Argument0;
        public PhpValue Argument1;
        public PhpValue Argument2;
        public PhpValue Argument3;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PhpArgTuple5
    {  
        public PhpValue Argument0;
        public PhpValue Argument1;
        public PhpValue Argument2;
        public PhpValue Argument3;
        public PhpValue Argument4;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PhpArgTuple6
    {  
        public PhpValue Argument0;
        public PhpValue Argument1;
        public PhpValue Argument2;
        public PhpValue Argument3;
        public PhpValue Argument4;
        public PhpValue Argument5;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PhpArgTuple7
    {  
        public PhpValue Argument0;
        public PhpValue Argument1;
        public PhpValue Argument2;
        public PhpValue Argument3;
        public PhpValue Argument4;
        public PhpValue Argument5;
        public PhpValue Argument6;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PhpArgTuple8
    {  
        public PhpValue Argument0;
        public PhpValue Argument1;
        public PhpValue Argument2;
        public PhpValue Argument3;
        public PhpValue Argument4;
        public PhpValue Argument5;
        public PhpValue Argument6;
        public PhpValue Argument7;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PhpArgTuple9
    {  
        public PhpValue Argument0;
        public PhpValue Argument1;
        public PhpValue Argument2;
        public PhpValue Argument3;
        public PhpValue Argument4;
        public PhpValue Argument5;
        public PhpValue Argument6;
        public PhpValue Argument7;
        public PhpValue Argument8;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PhpArgTuple10
    {  
        public PhpValue Argument0;
        public PhpValue Argument1;
        public PhpValue Argument2;
        public PhpValue Argument3;
        public PhpValue Argument4;
        public PhpValue Argument5;
        public PhpValue Argument6;
        public PhpValue Argument7;
        public PhpValue Argument8;
        public PhpValue Argument9;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PhpArgTuple11
    {  
        public PhpValue Argument0;
        public PhpValue Argument1;
        public PhpValue Argument2;
        public PhpValue Argument3;
        public PhpValue Argument4;
        public PhpValue Argument5;
        public PhpValue Argument6;
        public PhpValue Argument7;
        public PhpValue Argument8;
        public PhpValue Argument9;
        public PhpValue Argument10;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PhpArgTuple12
    {  
        public PhpValue Argument0;
        public PhpValue Argument1;
        public PhpValue Argument2;
        public PhpValue Argument3;
        public PhpValue Argument4;
        public PhpValue Argument5;
        public PhpValue Argument6;
        public PhpValue Argument7;
        public PhpValue Argument8;
        public PhpValue Argument9;
        public PhpValue Argument10;
        public PhpValue Argument11;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PhpArgTuple13
    {  
        public PhpValue Argument0;
        public PhpValue Argument1;
        public PhpValue Argument2;
        public PhpValue Argument3;
        public PhpValue Argument4;
        public PhpValue Argument5;
        public PhpValue Argument6;
        public PhpValue Argument7;
        public PhpValue Argument8;
        public PhpValue Argument9;
        public PhpValue Argument10;
        public PhpValue Argument11;
        public PhpValue Argument12;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PhpArgTuple14
    {  
        public PhpValue Argument0;
        public PhpValue Argument1;
        public PhpValue Argument2;
        public PhpValue Argument3;
        public PhpValue Argument4;
        public PhpValue Argument5;
        public PhpValue Argument6;
        public PhpValue Argument7;
        public PhpValue Argument8;
        public PhpValue Argument9;
        public PhpValue Argument10;
        public PhpValue Argument11;
        public PhpValue Argument12;
        public PhpValue Argument13;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PhpArgTuple15
    {  
        public PhpValue Argument0;
        public PhpValue Argument1;
        public PhpValue Argument2;
        public PhpValue Argument3;
        public PhpValue Argument4;
        public PhpValue Argument5;
        public PhpValue Argument6;
        public PhpValue Argument7;
        public PhpValue Argument8;
        public PhpValue Argument9;
        public PhpValue Argument10;
        public PhpValue Argument11;
        public PhpValue Argument12;
        public PhpValue Argument13;
        public PhpValue Argument14;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PhpArgTuple16
    {  
        public PhpValue Argument0;
        public PhpValue Argument1;
        public PhpValue Argument2;
        public PhpValue Argument3;
        public PhpValue Argument4;
        public PhpValue Argument5;
        public PhpValue Argument6;
        public PhpValue Argument7;
        public PhpValue Argument8;
        public PhpValue Argument9;
        public PhpValue Argument10;
        public PhpValue Argument11;
        public PhpValue Argument12;
        public PhpValue Argument13;
        public PhpValue Argument14;
        public PhpValue Argument15;
    }
}