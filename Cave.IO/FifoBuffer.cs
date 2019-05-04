using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Cave.IO
{
    /// <summary>
    /// Provides a simple byte[] buffer queue able to work with buffers of any size.
    /// </summary>
    public sealed class FifoBuffer
    {
        /// <summary>
        /// Reads a byte array of specified length from the source pointer starting at the specified byte offset.
        /// </summary>
        /// <param name="source">The source pointer.</param>
        /// <param name="offset">The byte offset for the read position.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        public static byte[] Read(IntPtr source, int offset, int count)
        {
            IntPtr ptr = (offset == 0) ? source : new IntPtr(source.ToInt64() + offset);
            byte[] buffer = new byte[count];
            Marshal.Copy(ptr, buffer, 0, count);
            return buffer;
        }

        LinkedList<byte[]> buffersList = new LinkedList<byte[]>();

        /// <summary>
        /// Directly prepends a copy of the specified byte buffer.
        /// </summary>
        /// <param name="buffer">The buffer to add (will not be copied).</param>
        [Obsolete("Use Prepend(byte[] buffer, bool doNotCopy) instead.")]
        public void Prepend(byte[] buffer) => Prepend(buffer, false);

        /// <summary>
        /// Directly prepends the specified byte buffer.
        /// </summary>
        /// <param name="buffer">The buffer to add.</param>
        /// <param name="doNotCopy">Prevents copying of the <paramref name="buffer"/> data. Use only when you know what you are doing.</param>
        public void Prepend(byte[] buffer, bool doNotCopy)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (!doNotCopy)
            {
                buffer = (byte[])buffer.Clone();
            }
            buffersList.AddFirst(buffer);
            Length += buffer.Length;
        }

        /// <summary>
        /// Enqueues a number of bytes from the specified stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="count">The number of bytes to enqueue.</param>
        public void Enqueue(Stream stream, int count)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            byte[] buffer = new byte[count];
            int len = stream.Read(buffer, 0, count);
            if (len == count)
            {
                Enqueue(buffer, true);
            }
            else
            {
                Enqueue(buffer, 0, len);
            }
        }

        /// <summary>
        /// Directly enqueues the specified byte buffer.
        /// </summary>
        /// <param name="buffer">The buffer to add.</param>
        [Obsolete("Use Enqueue(byte[] buffer, bool doNotCopy) instead.")]
        public void Enqueue(byte[] buffer) => Enqueue(buffer, false);

        /// <summary>
        /// Directly enqueues the specified byte buffer.
        /// </summary>
        /// <param name="buffer">The buffer to add.</param>
        /// <param name="doNotCopy">Prevents copying of the <paramref name="buffer"/> data. Use only when you know what you are doing.</param>
        public void Enqueue(byte[] buffer, bool doNotCopy)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (!doNotCopy)
            {
                buffer = (byte[])buffer.Clone();
            }
            buffersList.AddLast(buffer);
            Length += buffer.Length;
        }

        /// <summary>
        /// Enqueues datas from the specified buffer (will be copied).
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The byte offset to start reading from.</param>
        /// <param name="count">The number of bytes to copy.</param>
        public void Enqueue(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            byte[] newBuffer = new byte[count];
            Buffer.BlockCopy(buffer, offset, newBuffer, 0, count);
            Enqueue(newBuffer, true);
        }

        /// <summary>
        /// Enqueues data from the specified pointer.
        /// </summary>
        /// <param name="ptr">The location to start reading at.</param>
        /// <param name="count">The number of bytes to copy.</param>
        public void Enqueue(IntPtr ptr, int count)
        {
            Enqueue(Read(ptr, 0, count), true);
        }

        /// <summary>
        /// Enqueues data from the specified pointer.
        /// </summary>
        /// <param name="ptr">The location to start reading at.</param>
        /// <param name="offset">The byte offset to start reading from.</param>
        /// <param name="count">The number of bytes to copy.</param>
        public void Enqueue(IntPtr ptr, int offset, int count)
        {
            Enqueue(Read(ptr, offset, count));
        }

        /// <summary>
        /// Peeks at the first buffer (may be of any size &gt; 0).
        /// </summary>
        /// <returns>Returns the first buffer (may be of any size &gt; 0).</returns>
        public byte[] Peek()
        {
            return buffersList.First.Value;
        }

        /// <summary>
        /// Peeks at the buffer returning the specified number of bytes as new byte[] buffer.
        /// </summary>
        /// <param name="size">The number of bytes to peek at.</param>
        /// <returns>Returns a new buffer of the specified size.</returns>
        public byte[] Peek(int size)
        {
            if (Length < size)
            {
                throw new EndOfStreamException();
            }

            byte[] result = new byte[size];
            int pos = 0;
            LinkedListNode<byte[]> node = buffersList.First;
            while (pos < size)
            {
                byte[] current = node.Value;
                node = node.Next;
                int len = Math.Min(current.Length, size - pos);
                Buffer.BlockCopy(current, 0, result, pos, len);
                pos += len;
            }
            return result;
        }

        /// <summary>
        /// Peeks at the buffer and writes the data to the specified location.
        /// </summary>
        /// <param name="size">The number of bytes to peek at.</param>
        /// <param name="ptr">The location to start writing at.</param>
        public void Peek(int size, IntPtr ptr)
        {
            if (Length < size)
            {
                throw new EndOfStreamException();
            }

            int pos = 0;
            LinkedListNode<byte[]> node = buffersList.First;
            while (pos < size)
            {
                byte[] current = node.Value;
                node = node.Next;
                int len = Math.Min(current.Length, size - pos);
                Marshal.Copy(current, 0, ptr, len);
                ptr = new IntPtr(len + ptr.ToInt64());
                pos += len;
            }
        }

        /// <summary>
        /// Dequeues the first buffer (may be of any size &gt; 0).
        /// </summary>
        /// <returns>Returns a dequeued buffer (may be of any size &gt; 0).</returns>
        public byte[] Dequeue()
        {
            byte[] buffer = buffersList.First.Value;
            buffersList.RemoveFirst();
            Length -= buffer.Length;
            return buffer;
        }

        /// <summary>
        /// Dequeues the specified number of bytes as new byte[] buffer.
        /// </summary>
        /// <param name="size">The number of bytes to dequeue.</param>
        /// <returns>Returns a dequeued buffer of the specified size.</returns>
        public byte[] Dequeue(int size)
        {
            if (Length < size)
            {
                throw new EndOfStreamException();
            }

            byte[] result;
            if (Length == size)
            {
                result = ToArray();
                Clear();
            }
            else
            {
                result = new byte[size];
                int pos = 0;
                while (pos < size)
                {
                    byte[] current = Dequeue();
                    int len = Math.Min(current.Length, size - pos);
                    Buffer.BlockCopy(current, 0, result, pos, len);
                    pos += len;
                    if (len < current.Length)
                    {
                        byte[] remainder = new byte[current.Length - len];
                        Buffer.BlockCopy(current, len, remainder, 0, remainder.Length);
                        buffersList.AddFirst(remainder);
                        Length += remainder.Length;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Dequeues the specified number of bytes and writes them to the specified location.
        /// </summary>
        /// <param name="size">The number of bytes to dequeue.</param>
        /// <param name="ptr">The location to start writing at.</param>
        public void Dequeue(int size, IntPtr ptr)
        {
            if (Length < size)
            {
                throw new EndOfStreamException();
            }

            int pos = 0;
            while (pos < size)
            {
                byte[] current = Dequeue();
                int len = Math.Min(current.Length, size - pos);
                Marshal.Copy(current, 0, ptr, len);
                ptr = new IntPtr(len + ptr.ToInt64());
                pos += len;
                if (len < current.Length)
                {
                    byte[] remainder = new byte[current.Length - len];
                    Buffer.BlockCopy(current, len, remainder, 0, remainder.Length);
                    buffersList.AddFirst(remainder);
                    Length += remainder.Length;
                }
            }
        }

        /// <summary>
        /// Gets a byte[] array containing all currently buffered bytes.
        /// </summary>
        /// <returns>Returns a byte[] array size = <see cref="Length"/>.</returns>
        public byte[] ToArray()
        {
            byte[] result = new byte[Length];
            int pos = 0;
            foreach (byte[] buffer in buffersList)
            {
                Buffer.BlockCopy(buffer, 0, result, pos, buffer.Length);
                pos += buffer.Length;
            }
            return result;
        }

        /// <summary>
        /// Clears the buffer.
        /// </summary>
        public void Clear()
        {
            buffersList.Clear();
            Length = 0;
        }

        /// <summary>
        /// Gets number of bytes currently buffered.
        /// </summary>
        public int Length { get; private set; }
    }
}
