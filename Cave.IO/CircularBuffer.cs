#nullable enable

using System;
using System.Diagnostics;

namespace Cave.IO;

/// <summary>Provides a lock free ring buffer with overflow checking.</summary>
/// <typeparam name="TValue">Item type.</typeparam>
public sealed class CircularBuffer<TValue> : RingBuffer<TValue>
{
    int maxQueuedCount;

    /// <summary>Creates a new instance of the <see cref="CircularBuffer{TValue}"/> class.</summary>
    /// <param name="bits">Number of bits to use for item capacity (defaults to 12 = 4096 items).</param>
    public CircularBuffer(int bits = 12) : base(bits)
    {
        OverflowHandling = RingBufferOverflowFlags.Prevent;
    }

    /// <summary>Gets the maximum number of items queued.</summary>
    [Obsolete("This property is only updated on usage and may not work as expected!")]
    public int MaxQueuedCount => maxQueuedCount = Math.Max(maxQueuedCount, Available);

    /// <summary>Throw exceptions on overflow (buffer under run)</summary>
    public bool OverflowExceptions
    {
        get => OverflowHandling.HasFlag(RingBufferOverflowFlags.Exception);
        set => OverflowHandling = value ? OverflowHandling | RingBufferOverflowFlags.Exception : OverflowHandling & ~RingBufferOverflowFlags.Exception;
    }

    /// <summary>Write overflow (buffer under run) to <see cref="Trace"/>.</summary>
    public bool OverflowTrace
    {
        get => OverflowHandling.HasFlag(RingBufferOverflowFlags.Trace);
        set => OverflowHandling = value ? OverflowHandling | RingBufferOverflowFlags.Trace : OverflowHandling & ~RingBufferOverflowFlags.Trace;
    }
}
