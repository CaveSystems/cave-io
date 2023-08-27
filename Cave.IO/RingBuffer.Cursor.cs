using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

#nullable enable

namespace Cave.IO;

partial class RingBuffer<TValue>
{
    class Cursor : IRingBufferCursor<TValue>
    {
        readonly RingBuffer<TValue> ringBuffer;
        int threadEnterCheck;

        public Cursor(RingBuffer<TValue> ringBuffer)
        {
            this.ringBuffer = ringBuffer;
            ReadPosition = ringBuffer.WritePosition;
        }

        public int Available
        {
            [MethodImpl((MethodImplOptions)0x0100)]
            get => (int)(ringBuffer.WriteCount - ReadCount);
        }

        public long LostCount { get; private set; }

        public long ReadCount { get; private set; }

        public int ReadPosition { get; private set; }

        public bool TryRead(out TValue value)
        {
            try
            {
                if (Interlocked.Increment(ref threadEnterCheck) > 1)
                {
                    throw new NotSupportedException("Multithread enter detected. This is not supported by a IRingBufferCursor. Use the global IRingBuffer.Read functions or create a reader for each thread!");
                }

                while (Available > ringBuffer.Capacity)
                {
                    ReadPosition = (ReadPosition + 1) & ringBuffer.mask;
                    LostCount++;
                }

                while (true)
                {
                    //first check, handles entry into reader
                    if (ReadCount + LostCount >= ringBuffer.WriteCount)
                    {
                        value = default!;
                        return false;
                    }
                    //read

                    var i = ReadPosition;
                    ReadPosition = (ReadPosition + 1) & ringBuffer.mask;
                    var result = ringBuffer.buffer[i];
                    if (result is null) continue;
                    ReadCount++;
                    value = result.Value;
                    return true;
                }
            }
            finally
            {
                Interlocked.Decrement(ref threadEnterCheck);
            }
        }

        public TValue Read()
        {
            while (true)
            {
                if (TryRead(out var result)) return result;
                Thread.Sleep(1);
            }
        }

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

        public TValue[] ToArray()
        {
            var block = new Container[ringBuffer.Capacity];
            ringBuffer.buffer.CopyTo(block, 0);
            var write = ringBuffer.WritePosition;
            var read = ReadPosition;
            var selected = (write > read) ? block[read..write] : block[read..].Concat(block[..write]);
            return selected.Where(c => c is not null).Select(c => c!.Value).ToArray();
        }
    }
}
