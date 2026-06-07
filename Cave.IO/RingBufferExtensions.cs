using System;
using System.Threading;

namespace Cave.IO;

/// <summary>Provides extension methods for <see cref="IRingBuffer{TValue}"/>.</summary>
public static class RingBufferExtensions
{
    /// <summary>Blocking dequeue: spins until an item is available and returns it.</summary>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <param name="buffer">Ring buffer to dequeue from.</param>
    /// <returns>Returns the dequeued item.</returns>
    public static TValue Dequeue<TValue>(this IRingBuffer<TValue> buffer)
    {
        //hot path
        if (buffer.TryRead(out var value)) return value;
        //slow path: spin until item is available
        var spin = new SpinWait();
        while (!buffer.TryRead(out value))
        {
            spin.SpinOnce();
        }
        return value;
    }

    /// <summary>Blocking dequeue with timeout: spins until an item is available or the timeout expires.</summary>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <param name="buffer">Ring buffer to dequeue from.</param>
    /// <param name="timeout">Maximum time to wait for an item.</param>
    /// <param name="value">Returns the dequeued item or default if timed out.</param>
    /// <returns>Returns true if an item was dequeued, false if the timeout expired.</returns>
    public static bool TryDequeue<TValue>(this IRingBuffer<TValue> buffer, TimeSpan timeout, out TValue value)
    {
        if (buffer.TryRead(out value)) return true;
        var spin = new SpinWait();
        var deadline = DateTime.UtcNow + timeout;
        do
        {
            spin.SpinOnce();
            if (buffer.TryRead(out value))
            {
                return true;
            }
        }
        while (DateTime.UtcNow < deadline);
        return false;
    }
}
