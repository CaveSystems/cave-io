using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Cave.IO;

/// <summary>Provides a high speed lock free multi reader multi writer fifo buffer class.</summary>
/// <typeparam name="TValue">Item type.</typeparam>
public class Fifo<TValue>
{
    #region Private Classes

    sealed class Segment
    {
        #region Internal Fields

        internal readonly TValue?[] Items;
        internal readonly int Mask;
        internal Segment? Next;
        internal Segment? Preallocated;
        internal readonly int[] State;

        // EnqueuePos on its own cache line to avoid false sharing with DequeuePos
        internal long EnqueuePos;
#pragma warning disable CS0169
        long ep1, ep2, ep3, ep4, ep5, ep6, ep7;
#pragma warning restore CS0169

        // DequeuePos on its own cache line
        internal long DequeuePos;
#pragma warning disable CS0169
        long dp1, dp2, dp3, dp4, dp5, dp6, dp7;
#pragma warning restore CS0169

        #endregion Internal Fields

        #region Internal Constructors

        internal Segment(int bits)
        {
            var size = 1 << bits;
            Mask = size - 1;
            Items = new TValue?[size];
            State = new int[size];
        }

        #endregion Internal Constructors

        #region Internal Properties

        /// <summary>Gets the capacity of this segment.</summary>
        internal int Capacity
        {
            [MethodImpl((MethodImplOptions)0x0100)]
            get => Items.Length;
        }

        /// <summary>Returns true if all enqueue slots are claimed.</summary>
        internal bool IsFull
        {
            [MethodImpl((MethodImplOptions)0x0100)]
            get => Volatile.Read(ref EnqueuePos) >= Capacity;
        }

        /// <summary>Returns true if all dequeue slots are claimed.</summary>
        internal bool IsExhausted
        {
            [MethodImpl((MethodImplOptions)0x0100)]
            get => Volatile.Read(ref DequeuePos) >= Capacity;
        }

        #endregion Internal Properties

        #region Internal Methods

        /// <summary>Claims an enqueue slot. Returns slot index or -1 if segment is full.</summary>
        [MethodImpl((MethodImplOptions)0x0100)]
        internal int TryClaimEnqueue()
        {
            var pos = Interlocked.Increment(ref EnqueuePos) - 1;
            return pos < Capacity ? (int)(pos & Mask) : -1;
        }

        /// <summary>
        /// Claims a dequeue slot. Caller must have decremented the global available counter as gate.
        /// Returns slot index or -1 if segment is exhausted.
        /// </summary>
        internal int ClaimDequeue()
        {
            var pos = Interlocked.Increment(ref DequeuePos) - 1;
            if (pos >= Capacity) return -1;
            var idx = (int)(pos & Mask);
            // Writer claimed the slot but may not have stored yet — spin until ready.
            var spin = new SpinWait();
            while (Volatile.Read(ref State[idx]) == 0) spin.SpinOnce();
            return idx;
        }

        #endregion Internal Methods
    }

    #endregion Private Classes

    #region Private Fields

    readonly int segmentBits;

    // available and head on separate cache lines to reduce false sharing between readers and writers
    int available;
#pragma warning disable CS0169
    int av1, av2, av3, av4, av5, av6, av7, av8, av9, av10, av11, av12, av13, av14, av15;
#pragma warning restore CS0169

    Segment head;
#pragma warning disable CS0169
    long h1, h2, h3, h4, h5, h6, h7;
#pragma warning restore CS0169

    Segment tail;
#pragma warning disable CS0169
    long t1, t2, t3, t4, t5, t6, t7;
#pragma warning restore CS0169

    long readCount;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>Creates a new instance of the <see cref="Fifo{TValue}"/> class.</summary>
    /// <param name="segmentBits">Bits for segment size (default 10 = 1024 slots per segment).</param>
    public Fifo(int segmentBits = 10)
    {
        this.segmentBits = segmentBits;
        var first = new Segment(segmentBits);
        // Pre-allocate the first overflow segment immediately
        first.Preallocated = new Segment(segmentBits);
        head = tail = first;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets the number of items currently available for reading.</summary>
    public int Available => Volatile.Read(ref available);

    /// <summary>Gets the number of items dequeued over the lifetime of this instance.</summary>
    public long ReadCount => Interlocked.Read(ref readCount);

    /// <summary>Gets the total number of items enqueued over the lifetime of this instance.</summary>
    public long WriteCount => Interlocked.Read(ref readCount) + Volatile.Read(ref available);

    #endregion Public Properties

    #region Public Methods

    /// <summary>Dequeues an item, blocking until one is available.</summary>
    public TValue Dequeue()
    {
        //hot path
        if (TryDequeue(out var result)) return result!;
        //slow path: wait until an item is available, then find it
        var spin = new SpinWait();
        while (true)
        {
            if (TryDequeue(out result)) return result!;
            spin.SpinOnce();
        }
    }

    /// <summary>Dequeues an item, blocking until one is available.</summary>
    public TValue Dequeue(TimeSpan timeout)
    {
        //hot path
        if (TryDequeue(out var result)) return result!;
        //slow path: wait until an item is available, then find it
        Stopwatch stopwatch = new();
        var spin = new SpinWait();
        while (stopwatch.Elapsed < timeout)
        {
            if (TryDequeue(out result)) return result!;
            spin.SpinOnce();
        }
        throw new TimeoutException("Dequeue operation timed out.");
    }

    /// <summary>Dequeues an item, blocking until one is available.</summary>
    public bool TryDequeue(TimeSpan timeout, out TValue result)
    {
        //hot path
        if (TryDequeue(out var value)) { result = value!; return true; }
        //slow path: wait until an item is available, then find it
        Stopwatch stopwatch = new();
        var spin = new SpinWait();
        while (stopwatch.Elapsed < timeout)
        {
            if (TryDequeue(out value)) { result = value!; return true; }
            spin.SpinOnce();
        }
        result = default!;
        return false;
    }

    /// <summary>Enqueues an item.</summary>
    /// <param name="value">Item to enqueue.</param>
    public void Enqueue(TValue value)
    {
        while (true)
        {
            var t = tail;
            var idx = t.TryClaimEnqueue();

            if (idx >= 0)
            {
                t.Items[idx] = value;
                // Release-write: reader sees Items[idx] only after State[idx]==1
                Volatile.Write(ref t.State[idx], 1);
                // Signal reader: item is fully committed
                Interlocked.Increment(ref available);
                return;
            }

            // Segment full — reuse preallocated segment or allocate a new one
            var newSeg = Interlocked.Exchange(ref t.Preallocated!, null) ?? new Segment(segmentBits);
            if (Interlocked.CompareExchange(ref tail!, newSeg, t) == t)
            {
                Volatile.Write(ref t.Next!, newSeg);
                // Eagerly preallocate the next overflow segment in the background
                Interlocked.CompareExchange(ref newSeg.Preallocated!, new Segment(segmentBits), null);
            }
        }
    }

    /// <summary>Tries to dequeue an item. Returns false if none is available.</summary>
    /// <param name="value">The dequeued item.</param>
    public bool TryDequeue(out TValue? value)
    {
        // Cheap non-atomic pre-check: avoid interlocked storm when queue is empty.
        // Under high reader / low writer load, available is almost always 0.
        if (Volatile.Read(ref available) <= 0)
        {
            value = default;
            return false;
        }

        // Gate: atomically reserve one item — if none available, bail immediately.
        if (Interlocked.Decrement(ref available) < 0)
        {
            Interlocked.Increment(ref available);
            value = default;
            return false;
        }

        // We are guaranteed one item exists somewhere — find it
        while (true)
        {
            var h = head;
            var idx = h.ClaimDequeue();

            if (idx >= 0)
            {
                value = h.Items[idx];
#if NET8_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                // Release reference for GC only when needed (avoid cost for value types without refs)
                if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
#endif
                {
                    h.Items[idx] = default;
                }
                // State reset omitted: segment is not reused, no need to reset slot state
                Interlocked.Increment(ref readCount);
                return true;
            }

            // Segment exhausted — advance head to next segment
            var next = Volatile.Read(ref h.Next!);
            if (next is not null)
            {
                Interlocked.CompareExchange(ref head!, next, h);
            }
            // Retry: either new head is set, or another reader already advanced it
        }
    }

    /// <summary>Returns a snapshot of all currently available items without dequeuing them.</summary>
    public TValue[] ToArray()
    {
        var count = Volatile.Read(ref available);
        if (count <= 0) return [];
        var result = new List<TValue>(count);
        var seg = head;
        while (seg is not null)
        {
            var enqPos = (int)Math.Min(Volatile.Read(ref seg.EnqueuePos), seg.Capacity);
            var deqPos = (int)Math.Min(Volatile.Read(ref seg.DequeuePos), enqPos);
            for (var i = deqPos; i < enqPos; i++)
            {
                if (Volatile.Read(ref seg.State[i]) == 1) result.Add(seg.Items[i]!);
            }
            seg = Volatile.Read(ref seg.Next!);
        }
        return result.ToArray();
    }

    #endregion Public Methods
}
