#nullable enable

namespace Cave.IO;

/// <summary>Provides a lock free ring buffer without overflow checking.</summary>
/// <typeparam name="TValue">Item type.</typeparam>
public class UncheckedRingBuffer<TValue> : RingBuffer<TValue>
{
    /// <summary>Creates a new instance of the <see cref="UncheckedRingBuffer{TValue}"/> class.</summary>
    /// <param name="bits">Number of bits to use for item capacity (defaults to 12 = 4096 items).</param>
    public UncheckedRingBuffer(int bits = 12) : base(bits) => OverflowHandling = RingBufferOverflowFlags.None;
}
