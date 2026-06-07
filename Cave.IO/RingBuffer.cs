using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Cave.IO;

/// <summary>Provides a lock free ring buffer optimized for maximum write throughput.</summary>
/// <remarks>
/// Write cost: 1x Interlocked.Increment + 1x Volatile.Write.
/// Read cost: higher — lapping detection and lost-item accounting happen on read.
/// Overflow always overwrites unless <see cref="RingBufferOverflowFlags.Prevent"/> is set.
/// </remarks>
/// <typeparam name="TValue">Item type.</typeparam>
public partial class RingBuffer<TValue> : IRingBuffer<TValue>
{
    #region Private Fields

    readonly TValue[] items;
    readonly long[] seq;
    readonly int mask;
    long nextWrite;
    long nextRead;
    long lostCount;
    long readCount;
    long rejectedCount;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="RingBuffer{TValue}"/> class.</summary>
    /// <param name="bits">Number of bits for item capacity (default 12 = 4096 items).</param>
    public RingBuffer(int bits = 12)
    {
        if (bits is < 1 or > 31) throw new ArgumentOutOfRangeException(nameof(bits));
        var capacity = 1 << bits;
        items = new TValue[capacity];
        seq = new long[capacity];
        mask = capacity - 1;
        // sentinel: seq[i] = i - capacity → all slots unwritten (seq < 0 for first round)
        for (var i = 0; i < capacity; i++) seq[i] = i - capacity;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <inheritdoc/>
    public int Available
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get
        {
            var nw = Interlocked.Read(ref nextWrite);
            var nr = Interlocked.Read(ref nextRead);
            var avail = nw - nr;
            return avail > Capacity ? Capacity : (int)avail;
        }
    }

    /// <inheritdoc/>
    public int Capacity
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => mask + 1;
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
        get => (int)(Interlocked.Read(ref nextRead) & mask);
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
        get => Capacity - Available;
    }

    /// <inheritdoc/>
    public long WriteCount
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => Interlocked.Read(ref nextWrite);
    }

    /// <inheritdoc/>
    public int WritePosition
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => (int)(Interlocked.Read(ref nextWrite) & mask);
    }

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc/>
    public void CopyTo(TValue[] array, int index)
    {
        var nw = Interlocked.Read(ref nextWrite);
        var nr = Interlocked.Read(ref nextRead);
        var start = nw - nr > Capacity ? nw - Capacity : nr;
        var count = (int)(nw - start);
        for (var n = 0; n < count; n++)
        {
            var i = (int)((start + n) & mask);
            if (Volatile.Read(ref seq[i]) == start + n)
                array[index++] = items[i];
        }
    }

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
        var list = new List<TValue>(count);
        for (var n = 0; n < count; n++)
        {
            if (!TryRead(out var value)) break;
            list.Add(value);
        }
        return list;
    }

    /// <inheritdoc/>
    public TValue[] ToArray()
    {
        var result = new TValue[Capacity];
        CopyTo(result, 0);
        return result;
    }

    /// <inheritdoc/>
    public bool TryRead(out TValue value)
    {
        while (true)
        {
            var pos = Interlocked.Read(ref nextRead);
            var nw = Interlocked.Read(ref nextWrite);

            // nothing written yet
            if (pos >= nw) { value = default!; return false; }

            var i = (int)(pos & mask);
            var s = Volatile.Read(ref seq[i]);

            if (s < pos)
            {
                // slot not yet published by writer → nothing readable
                value = default!;
                return false;
            }

            if (s > pos)
            {
                // lapping: slot already overwritten, advance reader to oldest valid position
                var jump = nw - Capacity;
                if (jump <= pos) jump = pos + 1;
                var lost = jump - pos;
                if (Interlocked.CompareExchange(ref nextRead, jump, pos) == pos)
                    Interlocked.Add(ref lostCount, lost);
                continue;
            }

            // s == pos: slot is ready, claim it
            if (Interlocked.CompareExchange(ref nextRead, pos + 1, pos) != pos) continue;

            value = items[i];
            items[i] = default!;
            Interlocked.Increment(ref readCount);
            return true;
        }
    }

    /// <summary>
    /// Writes an item. Only 1x <see cref="Interlocked.Increment(ref long)"/> + 1x <see cref="Volatile.Write(ref long, long)"/> in the hot path.
    /// If <see cref="RingBufferOverflowFlags.Prevent"/> is set a space check is added.
    /// </summary>
    /// <param name="item">Item to write.</param>
    /// <returns>Returns true on success.</returns>
    [MethodImpl((MethodImplOptions)0x0100)]
    public bool Write(TValue item)
    {
        if (OverflowHandling.HasFlag(RingBufferOverflowFlags.Prevent))
        {
            // atomic check-and-claim
            while (true)
            {
                var nw = Interlocked.Read(ref nextWrite);
                var nr = Interlocked.Read(ref nextRead);
                if (nw - nr >= Capacity)
                {
                    Interlocked.Increment(ref rejectedCount);
                    if (OverflowHandling.HasFlag(RingBufferOverflowFlags.Trace))
                        Trace.TraceError(new InternalBufferOverflowException().Message);
                    if (OverflowHandling.HasFlag(RingBufferOverflowFlags.Exception))
                        throw new InternalBufferOverflowException();
                    return false;
                }
                // atomically claim the slot - retry if another thread was faster
                if (Interlocked.CompareExchange(ref nextWrite, nw + 1, nw) == nw)
                {
                    var i = (int)(nw & mask);
                    items[i] = item;
                    Volatile.Write(ref seq[i], nw); // publish
                    return true;
                }
            }
        }

        // hot path (no overflow prevention): 1x Interlocked.Increment + 1x Volatile.Write
        var pos = Interlocked.Increment(ref nextWrite) - 1;
        var overwrittenPos = pos - Capacity;
        if (overwrittenPos >= 0)
        {
            if (Interlocked.CompareExchange(ref nextRead, overwrittenPos + 1, overwrittenPos) == overwrittenPos)
                Interlocked.Increment(ref lostCount);
        }
        var idx = (int)(pos & mask);
        items[idx] = item;
        Volatile.Write(ref seq[idx], pos); // publish
        return true;
    }

    #endregion Public Methods
}
