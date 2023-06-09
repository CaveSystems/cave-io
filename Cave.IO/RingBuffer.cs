using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

#nullable enable

namespace Cave.IO;

/// <summary>Provides a lock free ring buffer without overflow checking.</summary>
/// <typeparam name="TValue">Item type.</typeparam>
public class RingBuffer<TValue> : IRingBuffer<TValue>
{
    class Container
    {
        public Container(TValue value) => Value = value;

        public readonly TValue Value;
    }

    #region Private Fields

    int space;
    long readCount;
    int nextReadPosition;
    long writeCount;
    int nextWritePosition;
    long rejectedCount;
    long lostCount;
    readonly Container?[] Buffer;

    #endregion Private Fields

    #region Protected Fields

    /// <summary>Gets the mask for indices.</summary>
    protected readonly int Mask;

    #endregion Protected Fields

    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="UncheckedRingBuffer{TValue}"/> class.</summary>
    /// <param name="bits">Number of bits to use for item capacity (defaults to 12 = 4096 items).</param>
    public RingBuffer(int bits = 12)
    {
        if (bits is < 1 or > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(bits));
        }

        Buffer = new Container?[1 << bits];
        Mask = Capacity - 1;
        space = Capacity;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <inheritdoc/>
    public RingBufferOverflowFlags OverflowHandling { get; set; }

    /// <inheritdoc/>
    public int Available => (int)(WriteCount - ReadCount);

    /// <inheritdoc/>
    public int Capacity => Buffer.Length;

    /// <inheritdoc/>
    public long LostCount => Interlocked.Read(ref lostCount);

    /// <inheritdoc/>
    public long ReadCount => Interlocked.Read(ref readCount);

    /// <inheritdoc/>
    public int ReadPosition => nextReadPosition & Mask;

    /// <inheritdoc/>
    public long RejectedCount => Interlocked.Read(ref rejectedCount);

    /// <inheritdoc/>
    public long WriteCount => Interlocked.Read(ref writeCount);

    /// <inheritdoc/>
    public int WritePosition => nextWritePosition & Mask;

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc/>
    public void CopyTo(TValue[] array, int index) => Buffer.CopyTo(array, index);

    /// <inheritdoc/>
    public bool TryRead(out TValue value)
    {
        //first check, handles entry into reader
        if (Interlocked.Read(ref readCount) >= Interlocked.Read(ref writeCount))
        {
            value = default!;
            return false;
        }
        //read
        var i = (Interlocked.Increment(ref nextReadPosition) - 1) & Mask;
        var result = Interlocked.Exchange(ref Buffer[i], null);
        //second check, handles overshooting of multiple (same time) read operations passing first check
        if (result is null)
        {
            //overshot, return to prev read position
            Interlocked.Decrement(ref nextReadPosition);
            value = default!;
            return false;
        }
        //all clear, count
        Interlocked.Increment(ref readCount);
        Interlocked.Increment(ref space);
        value = result.Value;
        return true;
    }

    /// <inheritdoc/>
    public TValue Read()
    {
        while (true)
        {
            if (TryRead(out var result)) return result;
            Thread.Sleep(1);
        }
    }

    /// <inheritdoc/>
    public IList<TValue> ReadList(int count = 0)
    {
        if (count <= 0) count = Available;
        List<TValue> list = new(count);
        for (var i = 0; i < count; i++)
        {
            if (!TryRead(out var value)) break;
            list.Add(value);
        }
        return list;
    }

    /// <inheritdoc/>
    public TValue[] ToArray()
    {
        var clone = new TValue[Capacity];
        CopyTo(clone, 0);
        return clone;
    }

    /// <inheritdoc/>
    public bool Write(TValue item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        var container = new Container(item);
        if (Interlocked.Decrement(ref space) >= 0)
        {
            var i = (Interlocked.Increment(ref nextWritePosition) - 1) & Mask;
            var prev = Interlocked.Exchange(ref Buffer[i], container);
            if (prev != null) throw new Exception("Fatal buffer corruption detected!");
            Interlocked.Increment(ref writeCount);
            return true;
        }
        //overflow handling
        {
            bool result;
            if (OverflowHandling.HasFlag(RingBufferOverflowFlags.Prevent))
            {
                Interlocked.Increment(ref space);
                Interlocked.Increment(ref rejectedCount);
                result = false;
            }
            else
            {
                var i = (Interlocked.Increment(ref nextWritePosition) - 1) & Mask;
                Buffer[i] = new(item);
                Interlocked.Increment(ref writeCount);
                Interlocked.Increment(ref lostCount);
                result = true;
            }
            if (OverflowHandling.HasFlag(RingBufferOverflowFlags.Trace))
            {
                Trace.TraceError(new InternalBufferOverflowException().Message);
            }
            if (OverflowHandling.HasFlag(RingBufferOverflowFlags.Exception))
            {
                throw new InternalBufferOverflowException();
            }
            return result;
        }
    }

    #endregion Public Methods
}
