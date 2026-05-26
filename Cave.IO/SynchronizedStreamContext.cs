using System;
using System.IO;

namespace Cave.IO;

/// <summary>Provides a synchronized context for a stream.</summary>
/// <remarks>
/// This class ensures that access to the underlying stream is synchronized, preventing concurrent read/write operations from causing data corruption. The lock
/// is held for the duration of the context's lifetime.
/// </remarks>
/// <example>
/// <code>
///using var context = new MessageContext(TcpAsyncClient.Stream);
///context.Writer.WriteLine("Hello World!");
/// </code>
/// </example>
public class SynchronizedStreamContext(Stream stream, TimeSpan timeout, StringEncoding encoding = StringEncoding.UTF_8, NewLineMode newLineMode = NewLineMode.LF, EndianType endian = EndianType.LittleEndian)
    : SynchronizedContext(stream, timeout)
{
    #region Properties

    /// <summary>Gets the <see cref="DataReader"/> for reading data from the stream.</summary>
    public DataReader Reader => field ??= new DataReader(stream, encoding, newLineMode, endian);

    /// <summary>Gets the underlying stream.</summary>
    public Stream Stream => stream;

    /// <summary>Gets the <see cref="DataWriter"/> for writing data to the stream.</summary>
    public DataWriter Writer => field ??= new DataWriter(stream, encoding, newLineMode, endian);

    #endregion Properties
}
