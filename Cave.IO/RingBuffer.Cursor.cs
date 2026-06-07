using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Cave.IO;

partial class RingBuffer<TValue>
{
    #region Private Classes

    sealed class Cursor(RingBuffer<TValue> buf) : IRingBufferCursor<TValue>
    {
        #region Private Fields

        int threadEnterCheck;

        #endregion Private Fields

        #region Public Properties

        /// <inheritdoc/>
        public int Available
        {
            [MethodImpl((MethodImplOptions)0x0100)]
            get => (int)(Interlocked.Read(ref buf.nextWrite) - ReadCount);
        }

        /// <inheritdoc/>
        public long LostCount { get; private set; }

        /// <inheritdoc/>
        public long ReadCount { get; private set; }

        /// <inheritdoc/>
        public int ReadPosition { get; private set; } = buf.WritePosition;

        #endregion Public Properties

        #region Public Methods

        /// <inheritdoc/>
        public TValue Read()
        {
            var spin = new SpinWait();
            for (; ; )
            {
                if (TryRead(out var result)) return result;
                spin.SpinOnce();
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
            var capacity = buf.Capacity;
            var result = new List<TValue>(capacity);
            var write = buf.WritePosition;
            var read = ReadPosition;
            var count = ((write - read) + capacity) & buf.mask;
            for (var n = 0; n < count; n++)
            {
                var i = (read + n) & buf.mask;
                if (Volatile.Read(ref buf.seq[i]) >= 0)
                {
                    result.Add(buf.items[i]);
                }
            }
            return result.ToArray();
        }

        /// <inheritdoc/>
        public bool TryRead(out TValue value)
        {
            try
            {
                if (Interlocked.Increment(ref threadEnterCheck) > 1)
                    throw new NotSupportedException("Multithread enter detected. Use GetCursor() per thread.");

                // skip lapped slots
                var nw = Interlocked.Read(ref buf.nextWrite);
                var basePos = (long)ReadPosition;
                if (nw - basePos > buf.Capacity)
                {
                    var jump = (int)((nw - buf.Capacity) & buf.mask);
                    LostCount += (jump - ReadPosition + buf.Capacity) & buf.mask;
                    ReadPosition = jump;
                }

                if (ReadCount + LostCount >= nw) { value = default!; return false; }

                var i = ReadPosition;
                var s = Volatile.Read(ref buf.seq[i]);
                if (s < 0) { value = default!; return false; }

                ReadPosition = (ReadPosition + 1) & buf.mask;
                ReadCount++;
                value = buf.items[i];
                return true;
            }
            finally
            {
                Interlocked.Decrement(ref threadEnterCheck);
            }
        }

        #endregion Public Methods
    }

    #endregion Private Classes
}
