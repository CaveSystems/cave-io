#nullable enable

using System;
using System.Threading;

namespace Cave.IO;

/// <summary>Provides a high speed lock free multi reader multi writer fifo buffer class.</summary>
/// <typeparam name="TValue">Item type</typeparam>
public class Fifo<TValue>
{
    #region Private Classes

    sealed class Container
    {
        #region Private Fields

        readonly TValue? value;
        Container? next;

        #endregion Private Fields

        #region Public Constructors

        public Container() { }

        public Container(TValue value) => this.value = value;

        #endregion Public Constructors

        #region Public Properties

        public Container? Next => next;

        public TValue Value => value ?? throw new NullReferenceException();

        #endregion Public Properties

        #region Public Methods

        public bool SetNext(Container container)
        {
            if (Interlocked.CompareExchange(ref next, container, null) != null)
            {
                return false;
            }
            return true;
        }

        #endregion Public Methods
    }

    #endregion Private Classes

    #region Private Fields

    int available;
    Container first;
    Container last;
    long readCount;
    long writeCount;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>Creates a new instance of the <see cref="Fifo{TValue}"/> class.</summary>
    public Fifo() => first = last = new();

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets the number of items available for reading.</summary>
    public int Available => available;

    /// <summary>Gets the number of items dequeued over the whole lifetime of the object.</summary>
    public long ReadCount => Interlocked.Read(ref readCount);

    /// <summary>Gets the number of items queued over the whole lifetime of the object.</summary>
    public long WriteCount => Interlocked.Read(ref writeCount);

    #endregion Public Properties

    #region Public Methods

    /// <summary>Dequeues an item from the fifo if available or waits until at least one iten is available.</summary>
    /// <returns>Returns the next item at the queue.</returns>
    public TValue Dequeue()
    {
        for (; ; )
        {
            if (TryDequeue(out var result)) return result ?? throw new NullReferenceException();
            Thread.Sleep(0);
        }
    }

    /// <summary>Enqueues an item at the fifo.</summary>
    /// <param name="value">Item to enqueue</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public void Enqueue(TValue value)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        var container = new Container(value);
        while (!last.SetNext(container))
        {
            Thread.MemoryBarrier();
        }
        for (; ; )
        {
            Thread.MemoryBarrier();
            var oldLast = last ?? throw new InvalidOperationException("Buffer corrupt!");
            if (oldLast == Interlocked.CompareExchange(ref last!, last.Next, oldLast))
            {
                break;
            }
        }
        Interlocked.Increment(ref available);
        Interlocked.Increment(ref writeCount);
    }

    /// <summary>Tries to dequeue an item from the fifo. If none is available this returns false.</summary>
    /// <param name="value">The dequeued item.</param>
    /// <returns>Returns true if an item could be dequeued, false otherwise.</returns>
    public bool TryDequeue(out TValue? value)
    {
        if (Interlocked.Decrement(ref available) < 0)
        {
            Interlocked.Increment(ref available);
            value = default;
            return false;
        }
        for (; ; )
        {
            Thread.MemoryBarrier();
            var oldFirst = first ?? throw new InvalidOperationException("Buffer corrupt!");
            if (oldFirst == Interlocked.CompareExchange(ref first!, first.Next, oldFirst))
            {
                Interlocked.Increment(ref readCount);
                value = oldFirst!.Next!.Value;
                return true;
            }
        }
    }

    #endregion Public Methods
}
