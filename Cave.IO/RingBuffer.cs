using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

#nullable enable

namespace Cave.IO;

/// <summary>Provides a lock free ring buffer without overflow checking.</summary>
/// <typeparam name="TValue">Item type.</typeparam>
public partial class RingBuffer<TValue> : IRingBuffer<TValue>
{
    #region Private Fields

    readonly Container?[] buffer;
    readonly int mask;
    long lostCount;
    int nextReadPosition;
    int nextWritePosition;
    long readCount;
    long rejectedCount;
    int space;
    long writeCount;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="UncheckedRingBuffer{TValue}"/> class.</summary>
    /// <param name="bits">Number of bits to use for item capacity (defaults to 12 = 4096 items).</param>
    public RingBuffer(int bits = 12)
    {
        if (bits is < 1 or > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(bits));
        }

        buffer = new Container?[1 << bits];
        mask = Capacity - 1;
        space = Capacity;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <inheritdoc/>
    public int Available
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => (int)(WriteCount - ReadCount - LostCount);
    }

    /// <inheritdoc/>
    public int Capacity
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => buffer.Length;
    }

    /// <inheritdoc/>
    public long LostCount
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => Interlocked.Read(ref lostCount);
    }

    /// <inheritdoc/>
    public RingBufferOverflowFlags OverflowHandling { get; set; }

    /// <inheritdoc/>
    public long ReadCount
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => Interlocked.Read(ref readCount);
    }

    /// <inheritdoc/>
    public int ReadPosition
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => nextReadPosition & mask;
    }

    /// <inheritdoc/>
    public long RejectedCount
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => Interlocked.Read(ref rejectedCount);
    }

    /// <inheritdoc/>
    public int Space
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => space;
    }

    /// <inheritdoc/>
    public long WriteCount
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => Interlocked.Read(ref writeCount);
    }

    /// <inheritdoc/>
    public int WritePosition
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => nextWritePosition & mask;
    }

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc/>
    public void CopyTo(TValue[] array, int index) => buffer.CopyTo(array, index);

    /// <inheritdoc/>
    public IRingBufferCursor<TValue> GetCursor() => new Cursor(this);

    /// <inheritdoc/>
    public TValue Read()
    {
        while (true)
        {
            if (TryRead(out var result)) return result;
            Thread.Sleep(0);
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
    public bool TryRead(out TValue value)
    {
        //first check, handles entry into reader
        if (Interlocked.Read(ref readCount) >= Interlocked.Read(ref writeCount))
        {
            value = default!;
            return false;
        }
        //read
        var i = (Interlocked.Increment(ref nextReadPosition) - 1) & mask;
        var result = Interlocked.Exchange(ref buffer[i], null);
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
    public bool Write(TValue item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        var container = new Container(item);
        if (Interlocked.Decrement(ref space) >= 0)
        {
            var i = (Interlocked.Increment(ref nextWritePosition) - 1) & mask;
            var prev = Interlocked.Exchange(ref buffer[i], container);
            if (prev != null) throw new Exception("Fatal buffer corruption detected!");
            Interlocked.Increment(ref writeCount);
            return true;
        }
        //overflow handling

        bool result;
        if (OverflowHandling.HasFlag(RingBufferOverflowFlags.Prevent))
        {
            Interlocked.Increment(ref rejectedCount);
            result = false;
        }
        else
        {
            var i = (Interlocked.Increment(ref nextWritePosition) - 1) & mask;
            buffer[i] = new(item);
            Interlocked.Increment(ref writeCount);
            Interlocked.Increment(ref lostCount);
            result = true;
        }
        //give space back
        Interlocked.Increment(ref space);
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

    #endregion Public Methods
}
