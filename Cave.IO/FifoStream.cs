using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Cave.IO;

/// <summary>
/// Provides a fifo buffer for byte[] blocks readable as stream. 
/// </summary>
/// <remarks>
/// New buffers can be added to the end of the stream and writing to the stream always appends to
/// the end of the stream. The position is the read position only and does not affect writing. The stream can be cleared with <see cref="Clear"/> and buffers in
/// front of the current position can be freed with <see cref="FreeBuffers()"/>. The stream can be used for example to buffer data from a network stream while
/// processing it at the same time. The stream is not thread safe, so external synchronization is required if used from multiple threads. This class is best
/// with medium sized buffers (1kiB - 64kiB).
/// </remarks>
public sealed class FifoStream : Stream, IFifoStream
{
    #region Public Methods

    /// <summary>Closes the stream.</summary>
    /// <remarks>Only <see cref="ToArray()"/> will be available after the stream is closed.</remarks>
    public override void Close()
    {
        base.Close();
        closed = true;
    }

    #endregion Public Methods

    #region Private Fields

    bool closed;
    LinkedListNode<byte[]>? currentBuffer;
    int currentBufferPosition;
    int realLength;
    int realPosition;

    #endregion Private Fields

    #region Protected Fields

    /// <summary>Gets the underlying buffer instance.</summary>
    readonly LinkedList<byte[]> Buffers = new();

    #endregion Protected Fields

    #region Public Properties

    /// <summary>Gets the number of bytes available from the current read position to the end of the stream.</summary>
    public int Available => realLength - realPosition;

    /// <summary>Gets the number of buffers in the stream.</summary>
    public int BufferCount => Buffers.Count;

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
            if (closed) throw new ObjectDisposedException(nameof(FifoStream));
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

    static int[] PrepareLps(byte[] pattern)
    {
        var m = pattern.Length;
        var lps = new int[m];
        var len = 0;
        var i = 1;
        while (i < m)
        {
            if (pattern[i] == pattern[len])
            {
                lps[i++] = ++len;
            }
            else if (len > 0)
            {
                len = lps[len - 1];
            }
            else
            {
                lps[i++] = 0;
            }
        }
        return lps;
    }

    /// <summary>
    /// Knuth–Morris–Pratt implementation to find the index of a byte pattern in the buffer. This is much faster than <see cref="IndexOf(byte[])"/> for larger patterns.
    /// </summary>
    /// <param name="pattern">The byte pattern to search for.</param>
    /// <returns>The index of the first occurrence of the pattern, or -1 if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the pattern is null.</exception>
    int IndexOfKmp(byte[] pattern)
    {
        if (pattern == null) throw new ArgumentNullException(nameof(pattern));
        if (pattern.Length == 0) return 0;
        var lps = PrepareLps(pattern);
        var m = pattern.Length;
        var globalIndex = 0;
        var j = 0;
        var node = currentBuffer;
        var pos = currentBufferPosition;
        while (node != null)
        {
            var buf = node.Value;

            while (pos < buf.Length)
            {
                var b = buf[pos];

                while (j > 0 && b != pattern[j]) j = lps[j - 1];

                if (b == pattern[j])
                {
                    j++;
                    if (j == m)
                    {
                        return globalIndex - (m - 1);
                    }
                }

                pos++;
                globalIndex++;
            }

            node = node.Next;
            pos = 0;
        }
        return -1;
    }

    int IndexOfNaive(byte[] data)
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

    /// <summary>appends a buffer at the end of the stream (always copies the buffer).</summary>
    /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
    /// <param name="count">The number of bytes to be written to the current stream.</param>
    public void AppendBuffer(byte[] buffer, int offset, int count)
    {
        if (closed) throw new ObjectDisposedException(nameof(FifoStream));
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (count == 0) return;

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
        if (closed) throw new ObjectDisposedException(nameof(FifoStream));
        if (source == null) throw new ArgumentNullException(nameof(source));

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
        if (closed) throw new ObjectDisposedException(nameof(FifoStream));
        if (source == null) throw new ArgumentNullException(nameof(source));

        const int BufferSize = 1024 * 1024;
        long result = 0;
        while (true)
        {
            var buffer = new byte[BufferSize];
            var count = source.Read(buffer, 0, BufferSize);
            if (count == 0)
            {
                break;
            }

            result += count;
            if (count != BufferSize)
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
        if (closed) throw new ObjectDisposedException(nameof(FifoStream));
        Buffers.Clear();
        realLength = 0;
        realPosition = 0;
        currentBuffer = null;
        currentBufferPosition = 0;
    }

    /// <summary>Determines whether the buffer contains the specified byte.</summary>
    /// <remarks>This uses <see cref="IndexOf(byte)"/> internally.</remarks>
    /// <param name="b">The byte.</param>
    /// <returns><c>true</c> if the buffer contains the specified byte; otherwise, <c>false</c>.</returns>
    public bool Contains(byte b) => IndexOf(b) > -1;

    /// <summary>Determines whether the buffer contains the specified data.</summary>
    /// <remarks>This uses <see cref="IndexOf(byte[])"/> internally.</remarks>
    /// <param name="data">The data.</param>
    /// <returns><c>true</c> if the buffer contains the specified data; otherwise, <c>false</c>.</returns>
    public bool Contains(byte[] data) => IndexOf(data) > -1;

    /// <summary>Does nothing.</summary>
    public override void Flush() { if (closed) throw new ObjectDisposedException(nameof(FifoStream)); }

    /// <summary>Removes all buffers in front of the current position.</summary>
    /// <returns>Bytes freed.</returns>
    public int FreeBuffers()
    {
        if (closed) throw new ObjectDisposedException(nameof(FifoStream));
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
    public void FreeBuffers(int sizeToKeep)
    {
        if (closed) throw new ObjectDisposedException(nameof(FifoStream));
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
        if (closed) throw new ObjectDisposedException(nameof(FifoStream));
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
    /// <param name="pattern">The pattern to search for.</param>
    /// <returns>Returns the index (a value &gt;=0) if the buffer contains the specified bytes; otherwise, -1.</returns>
    public int IndexOf(byte[] pattern)
    {
        if (closed) throw new ObjectDisposedException(nameof(FifoStream));
        if (pattern == null) throw new ArgumentNullException(nameof(pattern));
        var m = pattern.Length;
        if (m <= 8 || Available < (m << 2))
        {
            return IndexOfNaive(pattern);
        }
        return IndexOfKmp(pattern);
    }

    /// <summary>
    /// Retrieves all data ( <see cref="Available"/>) after the current <see cref="Position"/> at the buffer as array while not exceeding <paramref
    /// name="maxSize"/> (peek).
    /// </summary>
    /// <param name="maxSize">The maximum number of bytes to retrieve.</param>
    /// <returns>An array of bytes with <paramref name="maxSize"/> elements.</returns>
    public byte[] PeekArray(int maxSize = 0)
    {
        var resultLength = (maxSize > 0 && maxSize < Available) ? maxSize : Available;
        var result = new byte[resultLength];
        var start = 0;
        var node = currentBuffer;

        if (node != null)
        {
            var count = node.Value.Length - currentBufferPosition;
            if (count > resultLength) count = resultLength;
            Buffer.BlockCopy(node.Value, currentBufferPosition, result, start, count);
            start += count;
            resultLength -= count;
            node = node.Next;
        }

        while (node != null && resultLength > 0)
        {
            var count = node.Value.Length;
            if (count > resultLength) count = resultLength;
            Buffer.BlockCopy(node.Value, 0, result, start, count);
            start += count;
            resultLength -= count;
            node = node.Next;
        }

        return result;
    }

    /// <summary>Peeks at the next byte in the buffer. Returns -1 if no more data available.</summary>
    /// <returns>The next byte if available.</returns>
    [MethodImpl(256)]
    public int PeekByte()
    {
        if (closed || currentBuffer == null)
        {
            return -1;
        }
        return currentBuffer.Value[currentBufferPosition];
    }

    /// <summary>Puts a buffer to the end of the stream without copying.</summary>
    /// <param name="buffer">The byte buffer to add.</param>
    public void PutBuffer(byte[] buffer)
    {
        if (closed) throw new ObjectDisposedException(nameof(FifoStream));
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

    /// <summary>Reads some bytes at the current position from the stream. Returns -1 if no more data available.</summary>
    /// <param name="buffer">An array of bytes.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
    /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
    /// <returns>The total number of bytes read into the buffer or -1 at end of stream.</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (closed) return -1;
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
    [MethodImpl(256)]
    public override int ReadByte()
    {
        var result = PeekByte();
        if (result > -1)
        {
            realPosition++;
            currentBufferPosition++;
            if (currentBufferPosition == currentBuffer!.Value.Length)
            {
                currentBuffer = currentBuffer.Next;
                currentBufferPosition = 0;
            }
        }
        else
        {
            //we are doing this late because we only need it after PeekByte() returned EndOfStream.
            if (closed) throw new ObjectDisposedException(nameof(FifoStream));
        }
        return result;
    }

    long SeekCurrent(long offset)
    {
        if (currentBuffer is null)
        {
            if (offset < 0 && realPosition == realLength)
            {
                //we are at the end of the stream and want to seek backwards. We need to move to the last buffer first.
                currentBuffer = Buffers.Last ?? throw new InvalidOperationException("Buffer corrupt!");
                currentBufferPosition = currentBuffer.Value.Length;
            }
            else
            {
                //buffers where emty when seeking (this can happen with FreeBuffers() at EndOfStream and then adding new buffers)
                //we need to seek from beginning to find the correct buffer and position.
                Seek(realPosition + offset, SeekOrigin.Begin);
            }
        }
        var newPos = realPosition + offset;
        if (newPos < 0 || newPos > realLength) throw new ArgumentOutOfRangeException(nameof(offset));

        if (offset == 1)
        {
            realPosition++;
            currentBufferPosition++;
            if (currentBufferPosition == currentBuffer!.Value.Length)
            {
                currentBuffer = currentBuffer.Next ?? throw new InvalidOperationException("Buffer corrupt!");
                currentBufferPosition = 0;
            }
            return realPosition;
        }

        if (offset == -1)
        {
            realPosition--;
            currentBufferPosition--;
            if (currentBufferPosition < 0)
            {
                currentBuffer = currentBuffer!.Previous ?? throw new InvalidOperationException("Buffer corrupt!");
                currentBufferPosition = currentBuffer.Value.Length - 1;
            }

            return realPosition;
        }

        realPosition = (int)newPos;
        var localOffset = offset + currentBufferPosition;
        currentBufferPosition = 0;

        // Reverse
        while (localOffset < 0)
        {
            currentBuffer = currentBuffer!.Previous ?? throw new InvalidOperationException("Buffer corrupt!");
            localOffset += currentBuffer.Value.Length;
        }

        // Forward
        while (localOffset >= currentBuffer!.Value.Length)
        {
            localOffset -= currentBuffer.Value.Length;
            currentBuffer = currentBuffer.Next ?? throw new InvalidOperationException("Buffer corrupt!");
        }

        currentBufferPosition = (int)localOffset;
        return realPosition;
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        if (closed) throw new ObjectDisposedException(nameof(FifoStream));
        switch (origin)
        {
            case SeekOrigin.Current: return SeekCurrent(offset);
            case SeekOrigin.Begin:
            {
                currentBuffer = Buffers.First ?? throw new EndOfStreamException();
                currentBufferPosition = 0;
                realPosition = 0;
                if (offset != 0) return SeekCurrent(offset);
                return 0;
            }
            case SeekOrigin.End:
            {
                currentBuffer = Buffers.Last ?? throw new EndOfStreamException();
                currentBufferPosition = currentBuffer.Value.Length;
                realPosition = realLength;
                if (offset != 0) return SeekCurrent(offset);
                return realLength;
            }
            default: throw new NotImplementedException($"SeekOrigin {origin} undefined!");
        }
    }

    /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
    /// <param name="value">Not supported.</param>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <summary>Retrieves all data at the buffer as array.</summary>
    /// <returns>An array of bytes.</returns>
    public byte[] ToArray()
    {
        var result = new byte[realLength];
        var start = 0;
        var node = Buffers.First;

        while (node != null)
        {
            Buffer.BlockCopy(node.Value, 0, result, start, node.Value.Length);
            start += node.Value.Length;
            node = node.Next;
        }

        return result;
    }

    /// <summary>This always writes at the end of the stream and ignores the current position as position is the read position only!</summary>
    /// <remarks>Uses <see cref="AppendBuffer"/> to add data to the fifo.</remarks>
    /// <param name="buffer">An array of bytes.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
    /// <param name="count">The number of bytes to be written to the current stream.</param>
    public override void Write(byte[] buffer, int offset, int count) => AppendBuffer(buffer, offset, count);

    #endregion Public Methods
}
