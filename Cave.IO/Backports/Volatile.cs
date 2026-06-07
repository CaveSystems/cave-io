using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Cave.IO;

#if NET20 || NET35 || NET40 || NET45
static class Volatile
{
    [MethodImpl((MethodImplOptions)0x0100)]
    internal static int Read(ref int location)
    {
        Thread.MemoryBarrier();
        return location;
    }

    [MethodImpl((MethodImplOptions)0x0100)]
    internal static long Read(ref long location)
    {
        if (IntPtr.Size == 4)
        {
            return Interlocked.CompareExchange(ref location, 0, 0);
        }
        Thread.MemoryBarrier();
        return location;
    }

    [MethodImpl((MethodImplOptions)0x0100)]
    internal static void Write(ref int location, int value)
    {
        Thread.MemoryBarrier();
        location = value;
    }

    [MethodImpl((MethodImplOptions)0x0100)]
    internal static void Write(ref long location, long value)
    {
        if (IntPtr.Size == 4)
        {
            Interlocked.Exchange(ref location, value);
            return;
        }

        Thread.MemoryBarrier();
        location = value;
    }

    [MethodImpl((MethodImplOptions)0x0100)]
    internal static TClass? Read<TClass>(ref TClass? location) where TClass : class
    {
        Thread.MemoryBarrier();
        return location;
    }

    [MethodImpl((MethodImplOptions)0x0100)]
    internal static void Write<TClass>(ref TClass? location, TClass? value) where TClass : class
    {
        Thread.MemoryBarrier();
        location = value;
    }

}

#endif
