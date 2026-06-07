using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Cave.IO;

/// <summary>
/// Provides a fifo buffer for byte[] blocks readable as stream.
/// </summary>
/// <remarks>
/// New buffers can be added to the end of the stream and writing to the stream always appends to the end of the stream. The position is the read position only
/// and does not affect writing. The stream can be cleared with <see cref="Clear"/> and buffers in front of the current position can be freed with
/// <see cref="FreeBuffers()"/>. The stream can be used for example to buffer data from a network stream while processing it at the same time. The stream is not
/// thread safe, so external synchronization is required if used from multiple threads. This class is best with medium sized buffers (1kiB - 64kiB).
/// </remarks>
public sealed class FifoStream : Stream, IFifoStream
{
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
    public int Available
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => realLength - realPosition;
    }

    /// <summary>Gets the number of buffers in the stream.</summary>
    public int BufferCount
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => Buffers.Count;
    }

    /// <inheritdoc/>
    public override bool CanRead
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => true;
    }

    /// <inheritdoc/>
    public override bool CanSeek
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => true;
    }

    /// <inheritdoc/>
    public override bool CanWrite
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => true;
    }

    /// <inheritdoc/>
    public override long Length
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => realLength;
    }

    /// <inheritdoc/>
    public override long Position
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => realPosition;
        set => Seek(value, SeekOrigin.Begin);
    }

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
                index -= count;
                node = node.Next;
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    #endregion Public Indexers

    #region Private Methods

    static int[] PrepareLps(byte[] pattern)
    {
        var m = pattern.Length;
        var lps = new int[m];
        var len = 0;
        var i = 1;
        while (i < m)
        {
            if (pattern[i] == pattern[len]) { lps[i++] = ++len; }
            else if (len > 0) { len = lps[len - 1]; }
            else { lps[i++] = 0; }
        }
        return lps;
    }

    /// <summary>KMP search across linked buffer segments.</summary>
    int IndexOfKmp(byte[] pattern)
    {
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
                    if (++j == m) return globalIndex - (m - 1);
                }
                pos++;
                globalIndex++;
            }
            node = node.Next;
            pos = 0;
        }
        return -1;
    }

    /// <summary>Naive search using <see cref="Array.IndexOf{T}(T[], T, int, int)"/> per segment.</summary>
    int IndexOfNaive(byte[] data)
    {
        var index = 0;
        var checkIndex = 0;
        var node = currentBuffer;
        var pos = currentBufferPosition;
        while (node != null)
        {
            var buf = node.Value;
            for (; pos < buf.Length; pos++, index++)
            {
                if (buf[pos] == data[checkIndex])
                {
                    if (++checkIndex == data.Length) return (index - checkIndex) + 1;
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

    #endregion Private Methods

    #region Public Methods

    /// <summary>Appends a buffer at the end of the stream (always copies the buffer).</summary>
    /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
    /// <param name="count">The number of bytes to be written to the current stream.</param>
    public void AppendBuffer(byte[] buffer, int offset, int count)
    {
        if (closed) throw new ObjectDisposedException(nameof(FifoStream));
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (count == 0) return;
        var newBuffer = new byte[count];
        Buffer.BlockCopy(buffer, offset, newBuffer, 0, count);
        PutBuffer(newBuffer);
    }

    /// <summary>Appends a byte buffer of the specified length from the specified source stream to the end of the stream.</summary>
    /// <param name="source">The source stream.</param>
    /// <param name="count">The number of bytes to append.</param>
    /// <returns>The number of bytes written.</returns>
    public int AppendStream(Stream source, int count)
    {
        if (closed) throw new ObjectDisposedException(nameof(FifoStream));
        if (source == null) throw new ArgumentNullException(nameof(source));
        var buffer = new byte[count];
        var result = source.Read(buffer, 0, count);
        if (result != count) Array.Resize(ref buffer, result);
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
        // reuse a single read buffer; AppendBuffer copies into a new owned buffer each time
        const int ChunkSize = 64 * 1024;
        var chunk = new byte[ChunkSize];
        long result = 0;
        int count;
        while ((count = source.Read(chunk, 0, ChunkSize)) > 0)
        {
            AppendBuffer(chunk, 0, count);
            result += count;
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
    /// <remarks>Uses <see cref="IndexOf(byte)"/> internally.</remarks>
    /// <param name="b">The byte.</param>
    /// <returns><c>true</c> if the buffer contains the specified byte; otherwise, <c>false</c>.</returns>
    [MethodImpl((MethodImplOptions)0x0100)]
    public bool Contains(byte b) => IndexOf(b) > -1;

    /// <summary>Determines whether the buffer contains the specified data.</summary>
    /// <remarks>Uses <see cref="IndexOf(byte[])"/> internally.</remarks>
    /// <param name="data">The data.</param>
    /// <returns><c>true</c> if the buffer contains the specified data; otherwise, <c>false</c>.</returns>
    [MethodImpl((MethodImplOptions)0x0100)]
    public bool Contains(byte[] data) => IndexOf(data) > -1;

    /// <summary>Does nothing.</summary>
    [MethodImpl((MethodImplOptions)0x0100)]
    public override void Flush() { if (closed) throw new ObjectDisposedException(nameof(FifoStream)); }

    /// <summary>Removes all buffers in front of the current position.</summary>
    /// <returns>Bytes freed.</returns>
    public int FreeBuffers()
    {
        if (closed) throw new ObjectDisposedException(nameof(FifoStream));
        var bytesFreed = 0;
        while (Buffers.First is { } first && first.Value.Length <= realPosition)
        {
            var len = first.Value.Length;
            realPosition -= len;
            realLength -= len;
            Buffers.RemoveFirst();
            bytesFreed += len;
        }
        if (Buffers.First == null)
        {
            currentBufferPosition = 0;
            currentBuffer = null;
        }
        return bytesFreed;
    }

    /// <summary>Removes all buffers in front of the current position but keeps at least the specified number of bytes.</summary>
    /// <param name="sizeToKeep">The number of bytes to keep at the buffer.</param>
    public void FreeBuffers(int sizeToKeep)
    {
        if (closed) throw new ObjectDisposedException(nameof(FifoStream));
        while (Buffers.First is { } first && first.Value.Length <= realPosition)
        {
            var len = first.Value.Length;
            if (Available - len < sizeToKeep) break;
            realPosition -= len;
            realLength -= len;
            Buffers.RemoveFirst();
        }
        if (Buffers.First == null)
        {
            currentBufferPosition = 0;
            currentBuffer = null;
        }
    }

    /// <summary>Determines whether the buffer contains the specified byte.</summary>
    /// <param name="b">The byte.</param>
    /// <returns>The index (&gt;=0) of the first occurrence; otherwise, -1.</returns>
    public int IndexOf(byte b)
    {
        if (closed) throw new ObjectDisposedException(nameof(FifoStream));
        var index = 0;
        var node = currentBuffer;
        var pos = currentBufferPosition;
        while (node != null)
        {
            var buf = node.Value;
            var len = buf.Length;
            // Array.IndexOf is JIT-optimized and uses vectorized search on modern runtimes
            var found = Array.IndexOf(buf, b, pos, len - pos);
            if (found >= 0) return index + (found - pos);
            index += len - pos;
            node = node.Next;
            pos = 0;
        }
        return -1;
    }

    /// <summary>Finds the index of the specified byte pattern.</summary>
    /// <param name="pattern">The pattern to search for.</param>
    /// <returns>Returns the index (&gt;=0) of the first occurrence; otherwise, -1.</returns>
    public int IndexOf(byte[] pattern)
    {
        if (closed) throw new ObjectDisposedException(nameof(FifoStream));
        if (pattern == null) throw new ArgumentNullException(nameof(pattern));
        var m = pattern.Length;
        if (m <= 8 || Available < (m << 2)) return IndexOfNaive(pattern);
        return IndexOfKmp(pattern);
    }

    /// <summary>Returns up to <paramref name="maxSize"/> available bytes from the current position without advancing (peek).</summary>
    /// <param name="maxSize">Maximum bytes to return; 0 means all available.</param>
    /// <returns>A new byte array with the peeked data.</returns>
    public byte[] PeekArray(int maxSize = 0)
    {
        var resultLength = maxSize > 0 && maxSize < Available ? maxSize : Available;
        var result = new byte[resultLength];
        var start = 0;
        var node = currentBuffer;
        if (node != null)
        {
            var count = Math.Min(node.Value.Length - currentBufferPosition, resultLength);
            Buffer.BlockCopy(node.Value, currentBufferPosition, result, start, count);
            start += count;
            resultLength -= count;
            node = node.Next;
        }
        while (node != null && resultLength > 0)
        {
            var count = Math.Min(node.Value.Length, resultLength);
            Buffer.BlockCopy(node.Value, 0, result, start, count);
            start += count;
            resultLength -= count;
            node = node.Next;
        }
        return result;
    }

    /// <summary>Peeks at the next byte in the buffer without advancing. Returns -1 if no data is available.</summary>
    /// <returns>The next byte, or -1 if unavailable.</returns>
    [MethodImpl((MethodImplOptions)0x0100)]
    public int PeekByte()
    {
        if (closed || currentBuffer == null) return -1;
        return currentBuffer.Value[currentBufferPosition];
    }

    /// <summary>Puts a buffer to the end of the stream without copying.</summary>
    /// <param name="buffer">The byte buffer to add.</param>
    public void PutBuffer(byte[] buffer)
    {
        if (closed) throw new ObjectDisposedException(nameof(FifoStream));
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        Buffers.AddLast(buffer);
        realLength += buffer.Length;
        if (currentBuffer == null)
        {
            // fast path: no seek needed when read position is at the start
            if (realPosition == 0)
            {
                currentBuffer = Buffers.First;
                currentBufferPosition = 0;
            }
            else
            {
                // buffers were freed while at a non-zero position; relocate read cursor
                Seek(realPosition, SeekOrigin.Begin);
            }
        }
    }

    /// <summary>Reads bytes from the current position into the buffer.</summary>
    /// <param name="buffer">Destination array.</param>
    /// <param name="offset">Start offset in <paramref name="buffer"/>.</param>
    /// <param name="count">Maximum bytes to read.</param>
    /// <returns>The number of bytes read, or -1 if the stream is closed.</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (closed) return -1;
        count = Math.Min(count, Available);
        var resultSize = 0;
        while (count > 0 && currentBuffer != null)
        {
            var buf = currentBuffer.Value;
            var blockSize = Math.Min(buf.Length - currentBufferPosition, count);
            Buffer.BlockCopy(buf, currentBufferPosition, buffer, offset, blockSize);
            resultSize += blockSize;
            count -= blockSize;
            offset += blockSize;
            currentBufferPosition += blockSize;
            realPosition += blockSize;
            if (currentBufferPosition == buf.Length)
            {
                currentBufferPosition = 0;
                currentBuffer = currentBuffer.Next;
            }
        }
        return resultSize;
    }

    /// <inheritdoc/>
    public long FastCopyTo(Stream stream)
    {
        if (closed) return -1;
        if (currentBuffer == null) return 0;
        var resultSize = 0L;
        // first partial buffer
        if (currentBufferPosition > 0)
        {
            var size = currentBuffer.Value.Length - currentBufferPosition;
            stream.Write(currentBuffer.Value, currentBufferPosition, size);
            resultSize += size;
            realPosition += size;
            currentBufferPosition = 0;
            currentBuffer = currentBuffer.Next;
        }
        // remaining full buffers
        while (currentBuffer != null && Available > 0)
        {
            var buf = currentBuffer.Value;
            stream.Write(buf, 0, buf.Length);
            resultSize += buf.Length;
            realPosition += buf.Length;
            currentBufferPosition = 0;
            currentBuffer = currentBuffer.Next;
        }
        return resultSize;
    }

    /// <summary>Reads the next byte and advances the position. Returns -1 if no data is available.</summary>
    /// <returns>The next byte, or -1 if unavailable.</returns>
    [MethodImpl((MethodImplOptions)0x0100)]
    public override int ReadByte()
    {
        // inline PeekByte to avoid double null-check and call overhead
        if (closed || currentBuffer == null)
        {
            if (closed) throw new ObjectDisposedException(nameof(FifoStream));
            return -1;
        }
        var result = currentBuffer.Value[currentBufferPosition];
        realPosition++;
        if (++currentBufferPosition == currentBuffer.Value.Length)
        {
            currentBuffer = currentBuffer.Next;
            currentBufferPosition = 0;
        }
        return result;
    }

    /// <summary>Closes the stream.</summary>
    /// <remarks>Only <see cref="ToArray()"/> will be available after the stream is closed.</remarks>
    public override void Close()
    {
        base.Close();
        closed = true;
    }

    long SeekCurrent(long offset)
    {
        if (currentBuffer is null)
        {
            if (offset < 0 && realPosition == realLength)
            {
                // at end of stream, seek backwards: move to last buffer first
                currentBuffer = Buffers.Last ?? throw new InvalidOperationException("Buffer corrupt!");
                currentBufferPosition = currentBuffer.Value.Length;
            }
            else
            {
                // buffers were empty when seeking (e.g. after FreeBuffers+new data)
                Seek(realPosition + offset, SeekOrigin.Begin);
            }
        }
        var newPos = realPosition + offset;
        if (newPos < 0 || newPos > realLength) throw new ArgumentOutOfRangeException(nameof(offset));

        if (offset == 1)
        {
            realPosition++;
            if (++currentBufferPosition == currentBuffer!.Value.Length)
            {
                currentBuffer = currentBuffer.Next;
                currentBufferPosition = 0;
            }
            return realPosition;
        }

        if (offset == -1)
        {
            realPosition--;
            if (--currentBufferPosition < 0)
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
                return offset != 0 ? SeekCurrent(offset) : 0;
            }
            case SeekOrigin.End:
            {
                currentBuffer = Buffers.Last ?? throw new EndOfStreamException();
                currentBufferPosition = currentBuffer.Value.Length;
                realPosition = realLength;
                return offset != 0 ? SeekCurrent(offset) : realLength;
            }
            default: throw new NotImplementedException($"SeekOrigin {origin} undefined!");
        }
    }

    /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
    /// <param name="value">Not supported.</param>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <summary>Retrieves all data in the buffer as a new array (from position 0, regardless of read position).</summary>
    /// <returns>A new byte array containing all buffered data.</returns>
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

    /// <summary>Appends data at the end of the stream (ignores read position).</summary>
    /// <param name="buffer">Source array.</param>
    /// <param name="offset">Start offset in <paramref name="buffer"/>.</param>
    /// <param name="count">Number of bytes to write.</param>
    [MethodImpl((MethodImplOptions)0x0100)]
    public override void Write(byte[] buffer, int offset, int count) => AppendBuffer(buffer, offset, count);

    #endregion Public Methods
}
