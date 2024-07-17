using System;
using System.IO;

namespace Cave.IO;

/// <summary>
/// Provides a fifo buffer for byte[] blocks readable as stream. New buffers can be appended to the end of the stream and read like a stream. The performance of
/// this class is best with medium sized buffers (1kiB - 64kiB).
/// </summary>
public interface IFifoStream
{
    #region Public Properties

    /// <summary>Gets the number of bytes available from the current read position to the end of the stream.</summary>
    int Available { get; }

    /// <summary>Gets the number of buffers in the stream.</summary>
    int BufferCount { get; }

    /// <summary>Gets provides the current length of the stream.</summary>
    long Length { get; }

    /// <summary>Gets or sets the current read position.</summary>
    long Position { get; set; }

    #endregion Public Properties

    #region Public Indexers

    /// <summary>Gets the byte at the specified index.</summary>
    /// <param name="index">Index in range [0.. <see cref="Available"/>]</param>
    /// <returns>Returns the byte value.</returns>
    byte this[int index] { get; }

    #endregion Public Indexers

    #region Public Methods

    /// <summary>appends a buffer at the end of the stream (always copies the buffer).</summary>
    /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
    /// <param name="count">The number of bytes to be written to the current stream.</param>
    void AppendBuffer(byte[] buffer, int offset, int count);

    /// <summary>Appends a whole stream to the end of the stream.</summary>
    /// <param name="source">The source stream.</param>
    /// <returns>The number of bytes written.</returns>
    long AppendStream(Stream source);

    /// <summary>Appends a byte buffer of the specified length from the specified Source stream to the end of the stream.</summary>
    /// <param name="source">The source stream.</param>
    /// <param name="count">The number of bytes to append.</param>
    /// <returns>The number of bytes written.</returns>
    int AppendStream(Stream source, int count);

    /// <summary>Clears the buffer.</summary>
    void Clear();

    /// <summary>Determines whether the buffer contains the specified byte.</summary>
    /// <param name="b">The byte.</param>
    /// <returns><c>true</c> if the buffer contains the specified byte; otherwise, <c>false</c>.</returns>
    bool Contains(byte b);

    /// <summary>Determines whether the buffer contains the specified data.</summary>
    /// <param name="data">The data.</param>
    /// <returns><c>true</c> if the buffer contains the specified data; otherwise, <c>false</c>.</returns>
    bool Contains(byte[] data);

    /// <summary>Flushes all buffers to the stream (not used in most implementations).</summary>
    void Flush();

    /// <summary>Removes all buffers in front of the current position.</summary>
    /// <returns>Bytes freed.</returns>
    int FreeBuffers();

    /// <summary>removes all buffers in front of the current position but keeps at least the specified number of bytes.</summary>
    /// <param name="sizeToKeep">The number of bytes to keep at the buffer.</param>
    void FreeBuffers(int sizeToKeep);

    /// <summary>Determines whether the buffer contains the specified byte.</summary>
    /// <param name="b">The byte.</param>
    /// <returns>the index (a value &gt;=0) if the buffer contains the specified byte; otherwise, -1.</returns>
    int IndexOf(byte b);

    /// <summary>Determines whether the buffer contains the specified data.</summary>
    /// <param name="data">The data.</param>
    /// <returns>the index (a value &gt;=0) if the buffer contains the specified bytes; otherwise, -1.</returns>
    int IndexOf(byte[] data);

    /// <summary>Peeks at the next byte in the buffer. Returns -1 if no more data available.</summary>
    /// <returns>The next byte if available.</returns>
    int PeekByte();

    /// <summary>Puts a buffer to the end of the stream without copying.</summary>
    /// <param name="buffer">The byte buffer to add.</param>
    void PutBuffer(byte[] buffer);

    /// <summary>Reads some bytes at the current position from the stream.</summary>
    /// <param name="buffer">An array of bytes.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
    /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
    /// <returns>The total number of bytes read into the buffer.</returns>
    int Read(byte[] buffer, int offset, int count);

    /// <summary>Reads the next byte in the buffer (much faster than <see cref="Read"/>). Returns -1 if no more data available.</summary>
    /// <returns>The next byte if available.</returns>
    int ReadByte();

    /// <summary>Moves the read / write position in the stream.</summary>
    /// <param name="offset">A byte offset relative to the origin parameter.</param>
    /// <param name="origin">A value indicating the reference point used to obtain the new position.</param>
    /// <returns>The new position within the current stream.</returns>
    long Seek(long offset, SeekOrigin origin);

    /// <summary>Retrieves all data at the buffer as array (peek).</summary>
    /// <returns>An array of bytes.</returns>
    byte[] ToArray();

    /// <summary>This always writes at the end of the stream and ignores the current position as position is the read position only!</summary>
    /// <remarks>Uses <see cref="AppendBuffer"/> to add data to the fifo.</remarks>
    /// <param name="buffer">An array of bytes.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
    /// <param name="count">The number of bytes to be written to the current stream.</param>
    void Write(byte[] buffer, int offset, int count);

    #endregion Public Methods
}
