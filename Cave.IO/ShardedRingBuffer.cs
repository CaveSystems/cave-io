#pragma warning disable CS0169

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Cave.IO;

/// <summary>Provides a sharded ring buffer for high-throughput many-writer / few-reader scenarios.</summary>
/// <remarks>
/// Writers are distributed across independent shards via a thread-local shard index,
/// eliminating all write-side contention. Readers round-robin across shards.
/// Best for: many writers (e.g. 64), few readers (e.g. 4).
/// </remarks>
/// <typeparam name="TValue">Item type.</typeparam>
public sealed class ShardedRingBuffer<TValue> : IRingBuffer<TValue>
{
    #region Private Types

    /// <summary>Single shard — writer and reader hot fields separated onto distinct cache lines to avoid false sharing.</summary>
    sealed class Shard
    {
        // --- Cache line: writer-hot fields ---
        long wPad0, wPad1, wPad2, wPad3, wPad4, wPad5, wPad6;
        internal long NextWrite;      // monotonically increasing write sequence (also serves as WriteCount)
        internal long LostCount;
        internal long RejectedCount;
        // pad remainder of writer cache line
        long wPad7, wPad8, wPad9, wPadA, wPadB;

        // --- Cache line: reader-hot fields ---
        long rPad0, rPad1, rPad2, rPad3, rPad4, rPad5, rPad6;
        internal long NextRead;
        internal long ReadCount;
        // pad remainder of reader cache line
        long rPad7, rPad8, rPad9, rPadA, rPadB;

        // --- Non-hot fields ---
        internal readonly TValue[] Items;
        internal readonly int[] Ready;
        internal readonly int Mask;
        internal readonly int Capacity;

        internal Shard(int bits)
        {
            var cap = 1 << bits;
            Items = new TValue[cap];
            Ready = new int[cap];
            Mask = cap - 1;
            Capacity = cap;
        }

        /// <summary>Gets available space derived from write/read sequences — no extra counter needed.</summary>
        internal int FreeSlots
        {
            [MethodImpl((MethodImplOptions)0x0100)]
            get
            {
                var written = Volatile.Read(ref NextWrite);
                var read = Volatile.Read(ref NextRead);
                return Math.Max(0, Capacity - (int)(written - read));
            }
        }
    }

    #endregion Private Types

    #region Private Fields

    readonly Shard[] shards;
    readonly int shardMask;
    // thread-local write shard index — each writer thread gets its own shard
    readonly ThreadLocal<int> writerShard;
    // reader round-robin cursor (one per logical reader thread via ThreadLocal)
    readonly ThreadLocal<int> readerCursor;
    long nextShardAssignment;
    RingBufferOverflowFlags overflowHandling;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="ShardedRingBuffer{TValue}"/> class.</summary>
    /// <param name="shardBits">Number of bits for shard count (e.g. 6 = 64 shards). Defaults to 6.</param>
    /// <param name="bufferBits">Number of bits for per-shard capacity (e.g. 10 = 1024 items). Defaults to 10.</param>
    public ShardedRingBuffer(int shardBits = 6, int bufferBits = 10)
    {
        if (shardBits is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(shardBits));
        if (bufferBits is < 1 or > 24) throw new ArgumentOutOfRangeException(nameof(bufferBits));

        var shardCount = 1 << shardBits;
        shardMask = shardCount - 1;
        shards = new Shard[shardCount];
        for (var i = 0; i < shardCount; i++) shards[i] = new(bufferBits);

        // each writer thread gets a unique shard assigned once
        writerShard = new(() => (int)(Interlocked.Increment(ref nextShardAssignment) - 1) & shardMask);
        readerCursor = new(() => 0);
    }

    #endregion Public Constructors

    #region Public Properties

    /// <inheritdoc/>
    public int Available
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get
        {
            var total = 0L;
            foreach (var s in shards)
                total += Volatile.Read(ref s.NextWrite) - Volatile.Read(ref s.NextRead) - Volatile.Read(ref s.LostCount);
            return (int)Math.Max(0, total);
        }
    }

    /// <inheritdoc/>
    public int Capacity
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => shards.Length * shards[0].Capacity;
    }

    /// <inheritdoc/>
    public long LostCount
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get { var t = 0L; foreach (var s in shards) t += Volatile.Read(ref s.LostCount); return t; }
    }

    /// <inheritdoc/>
    public RingBufferOverflowFlags OverflowHandling
    {
        get => overflowHandling;
        set => overflowHandling = value;
    }

    /// <inheritdoc/>
    public long ReadCount
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get { var t = 0L; foreach (var s in shards) t += Volatile.Read(ref s.ReadCount); return t; }
    }

    /// <inheritdoc/>
    public int ReadPosition => (int)(Volatile.Read(ref shards[0].NextRead) & shards[0].Mask);

    /// <inheritdoc/>
    public long RejectedCount
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get { var t = 0L; foreach (var s in shards) t += Volatile.Read(ref s.RejectedCount); return t; }
    }

    /// <inheritdoc/>
    public int Space
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get { var t = 0; foreach (var s in shards) t += s.FreeSlots; return t; }
    }

    /// <inheritdoc/>
    public long WriteCount
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get { var t = 0L; foreach (var s in shards) t += Volatile.Read(ref s.NextWrite); return t; }
    }

    /// <inheritdoc/>
    public int WritePosition => (int)(Volatile.Read(ref shards[0].NextWrite) & shards[0].Mask);

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc/>
    public void CopyTo(TValue[] array, int index)
    {
        foreach (var s in shards)
        {
            var pos = (int)(Volatile.Read(ref s.NextRead) & s.Mask);
            for (var n = 0; n < s.Capacity; n++)
            {
                var i = (pos + n) & s.Mask;
                if (Volatile.Read(ref s.Ready[i]) != 0) array[index++] = s.Items[i];
            }
        }
    }

    /// <inheritdoc/>
    public IRingBufferCursor<TValue> GetCursor() => throw new NotSupportedException();

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
    [MethodImpl((MethodImplOptions)0x0100)]
    public bool TryRead(out TValue value)
    {
        var start = readerCursor.Value;
        var shardCount = shards.Length;
        for (var n = 0; n < shardCount; n++)
        {
            var idx = (start + n) & shardMask;
            if (TryReadShard(shards[idx], out value))
            {
                readerCursor.Value = (idx + 1) & shardMask;
                return true;
            }
        }
        value = default!;
        return false;
    }

    /// <summary>Writes an item to the caller's thread-local shard — zero contention between writer threads.</summary>
    /// <param name="item">Item to write.</param>
    /// <returns>Returns true on success.</returns>
    [MethodImpl((MethodImplOptions)0x0100)]
    public bool Write(TValue item)
    {
        var s = shards[writerShard.Value];
        return WriteShard(s, item);
    }

    #endregion Public Methods

    #region Private Methods

    [MethodImpl((MethodImplOptions)0x0100)]
    static bool TryReadShard(Shard s, out TValue value)
    {
        while (true)
        {
            // Opt 2: monotone NextWrite needs only Volatile.Read
            var currentWrite = Volatile.Read(ref s.NextWrite);
            var currentRead = Interlocked.Read(ref s.NextRead);

            if (currentRead >= currentWrite) { value = default!; return false; }

            var lag = currentWrite - currentRead;
            if (lag > s.Capacity)
            {
                var advanced = currentWrite - s.Capacity;
                Interlocked.CompareExchange(ref s.NextRead, advanced, currentRead);
                continue;
            }

            var i = (int)(currentRead & s.Mask);
            if (Volatile.Read(ref s.Ready[i]) == 0) { value = default!; return false; }
            if (Interlocked.CompareExchange(ref s.NextRead, currentRead + 1, currentRead) != currentRead) continue;
            if (Interlocked.CompareExchange(ref s.Ready[i], 0, 1) != 1) { value = default!; return false; }

            value = s.Items[i];
            s.Items[i] = default!;
            Interlocked.Increment(ref s.ReadCount);
            return true;
        }
    }

    bool WriteShard(Shard s, TValue item)
    {
        if (s.FreeSlots > 0)
        {
            var seq = Interlocked.Increment(ref s.NextWrite) - 1;
            var i = (int)(seq & s.Mask);
            s.Items[i] = item;
            Volatile.Write(ref s.Ready[i], 1);
            return true;
        }

        bool result;
        if (overflowHandling.HasFlag(RingBufferOverflowFlags.Prevent))
        {
            Interlocked.Increment(ref s.RejectedCount);
            result = false;
        }
        else
        {
            var seq = Interlocked.Increment(ref s.NextWrite) - 1;
            var i = (int)(seq & s.Mask);
            s.Items[i] = item;
            Volatile.Write(ref s.Ready[i], 1);
            Interlocked.Increment(ref s.LostCount);
            result = true;
        }

        if (overflowHandling.HasFlag(RingBufferOverflowFlags.Trace)) Trace.TraceError(new InternalBufferOverflowException().Message);
        if (overflowHandling.HasFlag(RingBufferOverflowFlags.Exception)) throw new InternalBufferOverflowException();
        return result;
    }

    #endregion Private Methods
}

#pragma warning restore CS0169
