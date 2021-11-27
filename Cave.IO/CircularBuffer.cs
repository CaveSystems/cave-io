using System;
using System.Diagnostics;
using System.Threading;

namespace Cave.IO
{
    /// <summary>Provides a lock free circular buffer with overflow checking.</summary>
    /// <typeparam name="TValue">Item type.</typeparam>
    public class CircularBuffer<TValue> : IRingBuffer<TValue> where TValue : class
    {
        #region Private Fields

        int queued;
        long readCount;
        int readPosition;
        long rejected;
        long writeCount;
        int writePosition = -1;

        #endregion Private Fields

        #region Protected Fields

        /// <summary>Gets the underlying buffer instance.</summary>
        protected readonly TValue[] Buffer;

        /// <summary>Gets the mask for indices.</summary>
        protected readonly int Mask;

        #endregion Protected Fields

        #region Public Constructors

        /// <summary>Initializes a new instance of the <see cref="CircularBuffer{TValue}"/> class.</summary>
        /// <param name="bits">Number of bits to use for item capacity (defaults to 12 = 4096 items).</param>
        public CircularBuffer(int bits = 12)
        {
            if (bits is < 1 or > 31)
            {
                throw new ArgumentOutOfRangeException(nameof(bits));
            }

            Buffer = new TValue[1 << bits];
            Mask = Capacity - 1;
        }

        #endregion Public Constructors

        #region Public Properties

        /// <inheritdoc/>
        public int Available
        {
            get
            {
                var diff = writePosition - readPosition;
                if (diff < 0) diff = Capacity - diff;
                return diff;
            }
        }

        /// <inheritdoc/>
        public int Capacity => Buffer.Length;

        /// <inheritdoc/>
        public long LostCount => 0;

        /// <summary>Gets the maximum number of items queued.</summary>
        public int MaxQueuedCount { get; set; }

        /// <summary>Throw exceptions at <see cref="Write"/> on overflow (buffer under run)</summary>
        public bool OverflowExceptions { get; set; }

        /// <summary>Write overflow (buffer under run) to <see cref="Trace"/>.</summary>
        public bool OverflowTrace { get; set; }

        /// <inheritdoc/>
        public long ReadCount => Interlocked.Read(ref readCount);

        /// <inheritdoc/>
        public int ReadPosition => readPosition;

        /// <inheritdoc/>
        public long RejectedCount => Interlocked.Read(ref rejected);

        /// <inheritdoc/>
        public long WriteCount => Interlocked.Read(ref writeCount);

        /// <inheritdoc/>
        public int WritePosition => writePosition;

        #endregion Public Properties

        #region Public Methods

        /// <inheritdoc/>
        public void CopyTo(TValue[] array, int index) => Buffer.CopyTo(array, index);

        /// <inheritdoc/>
        public TValue Read() => TryRead(out var result) ? result : default;

        /// <inheritdoc/>
        public TValue[] ToArray() => (TValue[])Buffer.Clone();

        /// <inheritdoc/>
        public bool TryRead(out TValue item)
        {
            var n = Interlocked.Decrement(ref queued);
            if (n < 0)
            {
                Interlocked.Increment(ref queued);
                item = default;
                return false;
            }

            var i = Interlocked.Increment(ref readPosition) & Mask;
            item = Buffer[i];
            Interlocked.Increment(ref readCount);
            return true;
        }

        /// <inheritdoc/>
        public bool Write(TValue item)
        {
            var n = Interlocked.Increment(ref queued);
            if (n > Mask)
            {
                Interlocked.Decrement(ref queued);
                Interlocked.Increment(ref rejected);
                const string message = "Buffer overflow!";
                if (OverflowExceptions)
                {
                    throw new Exception(message);
                }

                if (OverflowTrace)
                {
                    Trace.TraceError(message);
                }

                return false;
            }

            if (n > MaxQueuedCount)
            {
                MaxQueuedCount = n;
            }

            var i = Interlocked.Increment(ref writePosition) & Mask;
            Buffer[i] = item;
            Interlocked.Increment(ref writeCount);
            return true;
        }

        #endregion Public Methods
    }
}
