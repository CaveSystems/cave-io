using System;
using System.Collections.Generic;
using System.IO;

namespace Cave.IO;

/// <summary>
/// Provides a fifo buffer for byte[] blocks readable as stream. New buffers can be appended to the end of the stream and read like a stream. The performance of
/// this class is best with medium sized buffers (1kiB - 64kiB).
/// </summary>
public class FifoStream : Stream
{
    #region Private Fields

    LinkedListNode<byte[]> currentBuffer;
    int currentBufferPosition;
    int realLength;
    int realPosition;

    #endregion Private Fields

    #region Protected Fields

    /// <summary>Gets the underlying buffer instance.</summary>
    protected readonly LinkedList<byte[]> Buffers = new();

    #endregion Protected Fields

    #region Public Properties

    /// <summary>Gets the number of bytes available from the current read position to the end of the stream.</summary>
    public virtual int Available
    {
        get
        {
            return realLength - realPosition;
        }
    }

    /// <summary>Gets the number of buffers in the stream.</summary>
    public int BufferCount
    {
        get
        {
            return Buffers.Count;
        }
    }

    /// <summary>Gets a value indicating whether this stream can always be read or not.</summary>
    public override bool CanRead => true;

    /// <summary>Gets a value indicating whether this stream can seek or not.</summary>
    public override bool CanSeek => true;

    /// <summary>Gets a value indicating whether this stream can be written or not.</summary>
    public override bool CanWrite => true;

    /// <summary>Gets provides the current length of the stream.</summary>
    public override long Length => realLength;

    /// <summary>Gets or sets the current read position.</summary>
    public override long Position { get => realPosition; set => Seek(value, SeekOrigin.Begin); }

    #endregion Public Properties

    #region Public Indexers

    /// <summary>Gets the byte at the specified index.</summary>
    /// <param name="index">Index in range [0.. <see cref="Available"/>]</param>
    /// <returns>Returns the byte value.</returns>
    public byte this[int index]
    {
        get
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            var node = currentBuffer;
            //first node
            if (node != null)
            {
                var count = node.Value.Length - currentBufferPosition;
                if (index < count) return node.Value[index + currentBufferPosition];
                index -= count;
                node = node.Next;
            }

            while (node != null)
            {
                var count = node.Value.Length;
                if (index < count) return node.Value[index];
                index -= node.Value.Length;
                node = node.Next;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    #endregion Public Indexers

    #region Public Methods

    /// <summary>appends a buffer at the end of the stream (always copies the buffer).</summary>
    /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
    /// <param name="count">The number of bytes to be written to the current stream.</param>
    public virtual void AppendBuffer(byte[] buffer, int offset, int count)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        if (count == 0)
        {
            return;
        }

        var newBuffer = new byte[count];
        Array.Copy(buffer, offset, newBuffer, 0, count);
        PutBuffer(newBuffer);
    }

    /// <summary>Appends a byte buffer of the specified length from the specified Source stream to the end of the stream.</summary>
    /// <param name="source">The source stream.</param>
    /// <param name="count">The number of bytes to append.</param>
    /// <returns>The number of bytes written.</returns>
    public int AppendStream(Stream source, int count)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var buffer = new byte[count];
        var result = source.Read(buffer, 0, count);
        if (result != count)
        {
            Array.Resize(ref buffer, result);
        }

        PutBuffer(buffer);
        return result;
    }

    /// <summary>Appends a whole stream to the end of the stream.</summary>
    /// <param name="source">The source stream.</param>
    /// <returns>The number of bytes written.</returns>
    public long AppendStream(Stream source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        const int bufferSize = 1024 * 1024;
        long result = 0;
        while (true)
        {
            var buffer = new byte[bufferSize];
            var count = source.Read(buffer, 0, bufferSize);
            if (count == 0)
            {
                break;
            }

            result += count;
            if (count != bufferSize)
            {
                Array.Resize(ref buffer, count);
            }

            PutBuffer(buffer);
        }

        return result;
    }

    /// <summary>Clears the buffer.</summary>
    public void Clear()
    {
        Buffers.Clear();
        realLength = 0;
        realPosition = 0;
        currentBuffer = null;
        currentBufferPosition = 0;
    }

    /// <summary>Determines whether the buffer contains the specified byte.</summary>
    /// <param name="b">The byte.</param>
    /// <returns><c>true</c> if the buffer contains the specified byte; otherwise, <c>false</c>.</returns>
    public bool Contains(byte b)
    {
        var node = currentBuffer;
        var pos = currentBufferPosition;
        while (node != null)
        {
            for (; pos < node.Value.Length; pos++)
            {
                if (node.Value[pos] == b)
                {
                    return true;
                }
            }

            node = node.Next;
            pos = 0;
        }

        return false;
    }

    /// <summary>Determines whether the buffer contains the specified data.</summary>
    /// <param name="data">The data.</param>
    /// <returns><c>true</c> if the buffer contains the specified data; otherwise, <c>false</c>.</returns>
    public bool Contains(byte[] data)
    {
        if (data is null) throw new ArgumentNullException(nameof(data));
        var checkIndex = 0;
        var node = currentBuffer;
        var pos = currentBufferPosition;
        while (node != null)
        {
            for (; pos < node.Value.Length; pos++)
            {
                if (node.Value[pos] == data[checkIndex])
                {
                    if (++checkIndex == data.Length)
                    {
                        return true;
                    }
                }
                else
                {
                    checkIndex = 0;
                }
            }

            node = node.Next;
            pos = 0;
        }

        return false;
    }

    /// <summary>Does nothing.</summary>
    public override void Flush() { }

    /// <summary>Removes all buffers in front of the current position.</summary>
    /// <returns>Bytes freed.</returns>
    public virtual int FreeBuffers()
    {
        var bytesFreed = 0;
        while ((Buffers.First != null) && (Buffers.First.Value.Length <= realPosition))
        {
            var len = Buffers.First.Value.Length;
            realPosition -= len;
            realLength -= len;
            Buffers.RemoveFirst();
            bytesFreed += len;
        }

        if (Buffers.Count == 0)
        {
            currentBufferPosition = 0;
            currentBuffer = null;
        }

        return bytesFreed;
    }

    /// <summary>removes all buffers in front of the current position but keeps at least the specified number of bytes.</summary>
    /// <param name="sizeToKeep">The number of bytes to keep at the buffer.</param>
    public virtual void FreeBuffers(int sizeToKeep)
    {
        while ((Buffers.First != null) && (Buffers.First.Value.Length <= realPosition))
        {
            var len = Buffers.First.Value.Length;
            if ((Available - len) >= sizeToKeep)
            {
                realPosition -= len;
                realLength -= len;
                Buffers.RemoveFirst();
            }
            else
            {
                break;
            }
        }

        if (Buffers.Count == 0)
        {
            currentBufferPosition = 0;
            currentBuffer = null;
        }
    }

    /// <summary>Determines whether the buffer contains the specified byte.</summary>
    /// <param name="b">The byte.</param>
    /// <returns>the index (a value &gt;=0) if the buffer contains the specified byte; otherwise, -1.</returns>
    public int IndexOf(byte b)
    {
        var index = 0;
        var node = currentBuffer;
        var pos = currentBufferPosition;
        while (node != null)
        {
            for (; pos < node.Value.Length; pos++, index++)
            {
                if (node.Value[pos] == b)
                {
                    return index;
                }
            }

            node = node.Next;
            pos = 0;
        }

        return -1;
    }

    /// <summary>Determines whether the buffer contains the specified data.</summary>
    /// <param name="data">The data.</param>
    /// <returns>the index (a value &gt;=0) if the buffer contains the specified bytes; otherwise, -1.</returns>
    public int IndexOf(byte[] data)
    {
        if (data is null) throw new ArgumentNullException(nameof(data));
        var index = 0;
        var checkIndex = 0;
        var node = currentBuffer;
        var pos = currentBufferPosition;
        while (node != null)
        {
            for (; pos < node.Value.Length; pos++, index++)
            {
                if (node.Value[pos] == data[checkIndex])
                {
                    if (++checkIndex == data.Length)
                    {
                        return (index - checkIndex) + 1;
                    }
                }
                else
                {
                    checkIndex = 0;
                }
            }

            node = node.Next;
            pos = 0;
        }

        return -1;
    }

    /// <summary>Peeks at the next byte in the buffer. Returns -1 if no more data available.</summary>
    /// <returns>The next byte if available.</returns>
    public virtual int PeekByte()
    {
        if (currentBuffer == null)
        {
            return -1;
        }

        return currentBuffer.Value[currentBufferPosition];
    }

    /// <summary>Puts a buffer to the end of the stream without copying.</summary>
    /// <param name="buffer">The byte buffer to add.</param>
    public virtual void PutBuffer(byte[] buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        Buffers.AddLast(buffer);
        realLength += buffer.Length;
        if (currentBuffer == null)
        {
            Seek(realPosition, SeekOrigin.Begin);
        }
    }

    /// <summary>Reads some bytes at the current position from the stream.</summary>
    /// <param name="buffer">An array of bytes.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
    /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
    /// <returns>The total number of bytes read into the buffer.</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        count = Math.Min(count, Available);
        var resultSize = 0;
        while ((count > 0) && (currentBuffer != null))
        {
            var currentBuffer = this.currentBuffer.Value;
            var blockSize = Math.Min(currentBuffer.Length - currentBufferPosition, count);
            Array.Copy(currentBuffer, currentBufferPosition, buffer, offset, blockSize);
            resultSize += blockSize;
            count -= blockSize;
            offset += blockSize;
            currentBufferPosition += blockSize;
            realPosition += blockSize;
            if (currentBufferPosition == currentBuffer.Length)
            {
                currentBufferPosition = 0;
                this.currentBuffer = this.currentBuffer.Next;
            }
        }

        return resultSize;
    }

    /// <summary>Reads the next byte in the buffer (much faster than <see cref="Read"/>). Returns -1 if no more data available.</summary>
    /// <returns>The next byte if available.</returns>
    public override int ReadByte()
    {
        var result = PeekByte();
        if (result > -1)
        {
            Seek(1, SeekOrigin.Current);
        }

        return result;
    }

    /// <summary>Moves the read / write position in the stream.</summary>
    /// <param name="offset">A byte offset relative to the origin parameter.</param>
    /// <param name="origin">A value indicating the reference point used to obtain the new position.</param>
    /// <returns>The new position within the current stream.</returns>
    public override long Seek(long offset, SeekOrigin origin)
    {
        try
        {
            switch (origin)
            {
                case SeekOrigin.Current:
                {
                    if (((realPosition + offset) > realLength) ||
                        ((realPosition + offset) < 0) ||
                        (currentBuffer == null))
                    {
                        throw new ArgumentOutOfRangeException(nameof(offset));
                    }

                    realPosition += (int)offset;
                    offset += currentBufferPosition;
                    currentBufferPosition = 0;
                    while (offset < 0)
                    {
                        currentBuffer = currentBuffer.Previous;
                        offset += currentBuffer.Value.Length;
                    }

                    if (offset > 0)
                    {
                        while ((currentBuffer != null) && (offset >= currentBuffer.Value.Length))
                        {
                            offset -= currentBuffer.Value.Length;
                            currentBuffer = currentBuffer.Next;
                        }

                        currentBufferPosition = (int)offset;
                        if ((currentBufferPosition > 0) && (currentBuffer == null))
                        {
                            throw new EndOfStreamException();
                        }
                    }

                    return Position;
                }

                case SeekOrigin.Begin:
                {
                    currentBuffer = Buffers.First;
                    currentBufferPosition = 0;
                    realPosition = 0;
                    if (offset != 0)
                    {
                        return Seek(offset, SeekOrigin.Current);
                    }

                    return 0;
                }
                case SeekOrigin.End:
                {
                    realPosition = realLength;
                    currentBuffer = Buffers.Last;
                    currentBufferPosition = currentBuffer.Value.Length;
                    if (offset != 0)
                    {
                        return Seek(offset, SeekOrigin.Current);
                    }

                    return realLength;
                }
                default: throw new NotImplementedException($"SeekOrigin {origin} undefined!");
            }
        }
        catch
        {
            throw new EndOfStreamException();
        }
    }

    /// <summary>Throws new NotSupportedException().</summary>
    /// <param name="value">Not supported.</param>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <summary>Retrieves all data at the buffer as array (peek).</summary>
    /// <returns>An array of bytes.</returns>
    public byte[] ToArray()
    {
        var result = new byte[Available];
        {
            var start = 0;
            var node = currentBuffer;
            if (node != null)
            {
                var count = node.Value.Length - currentBufferPosition;
                Array.Copy(node.Value, currentBufferPosition, result, start, count);
                start += count;
                node = node.Next;
            }

            while (node != null)
            {
                node.Value.CopyTo(result, start);
                start += node.Value.Length;
                node = node.Next;
            }
        }
        return result;
    }

    /// <summary>This always writes at the end of the stream and ignores the current position as position is the read position only!</summary>
    /// <remarks>Uses <see cref="AppendBuffer"/> to add data to the fifo.</remarks>
    /// <param name="buffer">An array of bytes.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
    /// <param name="count">The number of bytes to be written to the current stream.</param>
    public override void Write(byte[] buffer, int offset, int count)
    {
        AppendBuffer(buffer, offset, count);
    }

    #endregion Public Methods
}
