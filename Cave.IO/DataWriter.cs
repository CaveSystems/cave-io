using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Cave.IO;

/// <summary>
/// Provides a new little endian binary writer implementation (a combination of streamwriter and binarywriter). This class is not threadsafe and does not buffer
/// anything nor needs flushing. You can access the basestream at any time with any mode of operation (read, write, seek, ...).
/// </summary>
public sealed class DataWriter
{
    internal static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);

    #region Private Fields

    bool closed;
    NewLineData? newLineData;
    NewLineMode newLineMode = NewLineMode.LF;
    StringEncoding stringEncoding;
    bool zeroTested;

    #endregion Private Fields

    #region Private Methods

    [MethodImpl((MethodImplOptions)256)]
    byte[] EncodeString(string text) => stringEncoding.Encode(text, withRoundtripTest: !DisableEncodingRoundtripTest);

    [MethodImpl((MethodImplOptions)256)]
    void ZeroTerminationTest()
    {
        if (!zeroTested)
        {
            if (!StringEncoding.CanRoundtrip("\0"))
            {
                throw new NotSupportedException($"Encoding {StringEncoding} does not support zero termination!");
            }
            zeroTested = true;
        }
    }

    #endregion Private Methods

    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="DataWriter"/> class.</summary>
    /// <param name="output">The stream to write to.</param>
    /// <param name="encoding">The encoding.</param>
    /// <param name="endian">The endian type.</param>
    /// <param name="newLineMode">New line mode.</param>
    /// <exception cref="ArgumentNullException">output.</exception>
    /// <exception cref="ArgumentException">Stream does not support writing or is already closed.;output.</exception>
    /// <exception cref="NotSupportedException">StringEncoding {0} not supported! or EndianType {0} not supported!.</exception>
    public DataWriter(Stream output, StringEncoding encoding = StringEncoding.UTF_8, NewLineMode newLineMode = NewLineMode.LF,
        EndianType endian = EndianType.LittleEndian)
    {
        BaseStream = output ?? throw new ArgumentNullException(nameof(output));
        NewLineMode = newLineMode;
        stringEncoding = encoding != StringEncoding.Undefined ? encoding : throw new ArgumentOutOfRangeException(nameof(encoding));
        EndianType = endian;
        if (!BaseStream.CanWrite)
        {
            throw new ArgumentException("Stream does not support writing or is already closed.", nameof(output));
        }
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets access to the base stream.</summary>
    public Stream BaseStream { get; }

    /// <summary>
    /// Disable the string roundtrip test when writing non unicode strings. This might improve write performance but will not tell you when encoding errors
    /// (missing codepage character mappings) occur.
    /// </summary>
    public bool DisableEncodingRoundtripTest { get; set; }

    /// <summary>Gets or sets the endian encoder type.</summary>
    /// <remarks>This can be used between all write calls.</remarks>
    /// <value>The endian encoder type.</value>
    public EndianType EndianType { get; set; }

    /// <summary>Gets or sets the new line mode used.</summary>
    /// <remarks>This can be used between all write calls.</remarks>
    public NewLineMode NewLineMode
    {
        get => newLineMode;
        set { newLineData = null; newLineMode = value; }
    }

    /// <summary>Gets or sets encoding to use for characters and strings. Setting this property updates <see cref="Encoding"/> automatically.</summary>
    /// <remarks>This can be used between all write calls.</remarks>
    public StringEncoding StringEncoding
    {
        get => stringEncoding;
        set { stringEncoding = value; zeroTested = false; newLineData = null; }
    }

    /// <summary>Gets the line feed string.</summary>
    public string LineFeed => (newLineData ??= new NewLineData(StringEncoding, newLineMode)).LineFeed;

    #endregion Public Properties

    #region Public Methods

    /// <summary>Closes the writer and the stream.</summary>
    public void Close()
    {
        if (!closed)
        {
            closed = true;
            BaseStream.Close();
            if (BaseStream is IDisposable disposable) disposable.Dispose();
        }
    }

    /// <summary>Flushes the stream.</summary>
    [MethodImpl((MethodImplOptions)256)]
    public void Flush() => BaseStream.Flush();

    /// <summary>Seeks at the base stream (this requires the stream to be seekable).</summary>
    /// <param name="offset">Offset to seek to.</param>
    /// <param name="origin">Origin to seek from.</param>
    /// <returns>A value of type SeekOrigin indicating the reference point used to obtain the new position.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public long Seek(int offset, SeekOrigin origin) => BaseStream.Seek(offset, origin);

    /// <summary>Writes the specified value directly to the stream.</summary>
    /// <param name="value">The value to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void Write(bool value) => BaseStream.WriteByte(value ? (byte)1 : (byte)0);

    /// <summary>Writes the specified value directly to the stream.</summary>
    /// <param name="value">The value to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void Write(byte value) => BaseStream.WriteByte(value);

    /// <summary>Writes the specified buffer directly to the stream.</summary>
    /// <param name="buffer">The buffer to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void Write(byte[] buffer) => BaseStream.Write(buffer, 0, buffer.Length);

    /// <summary>Writes a part of the specified buffer directly to the stream.</summary>
    /// <param name="buffer">The buffer to write.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
    /// <param name="count">The number of bytes to be written to the current stream.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void Write(byte[] buffer, int offset, int count) => BaseStream.Write(buffer, offset, count);

    /// <summary>Writes the specified character directly to the stream.</summary>
    /// <param name="c">The character to write.</param>
    /// <returns>The number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public int Write(char c) => Write(new[] { c });

    /// <summary>Writes the specified characters directly to the stream.</summary>
    /// <param name="chars">Array of characters to write.</param>
    /// <returns>The number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public int Write(char[] chars) => Write(new string(chars));

    /// <summary>Writes a part of the specified character array directly to the stream.</summary>
    /// <param name="chars">Array of characters to write.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
    /// <param name="count">The number of bytes to be written to the current stream.</param>
    /// <returns>Number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public int Write(char[] chars, int offset, int count)
    {
        var text = new string(chars, offset, count);
        var data = EncodeString(text);
        Write(data);
        return data.Length;
    }

    /// <summary>Writes the specified value directly to the stream.</summary>
    /// <param name="value">The value to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void Write(decimal value)
    {
        foreach (var decimalData in decimal.GetBits(value))
        {
            Write(decimalData);
        }
    }

    /// <summary>Writes the specified value directly to the stream.</summary>
    /// <param name="value">The value to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void Write(double value) => Write(EndianType == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value));

    /// <summary>Writes the specified value directly to the stream.</summary>
    /// <param name="value">The value to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void Write(short value) => Write(EndianType == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value));

    /// <summary>Writes the specified value directly to the stream.</summary>
    /// <param name="value">The value to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void Write(int value) => Write(EndianType == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value));

    /// <summary>Writes the specified value directly to the stream.</summary>
    /// <param name="value">The value to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void Write(long value) => Write(EndianType == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value));

    /// <summary>Writes the specified value directly to the stream.</summary>
    /// <param name="value">The value to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void Write(sbyte value) => Write(unchecked((byte)value));

    /// <summary>Writes the specified value directly to the stream.</summary>
    /// <param name="value">The value to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void Write(float value) => Write(EndianType == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value));

    /// <summary>Writes the specified value directly to the stream.</summary>
    /// <param name="value">The value to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void Write(Guid value)
    {
        var data = value.ToByteArray();
        if (data.Length != 16) throw new ArgumentException("Invalid byte array at guid.ToByteArray()!");
        Write(data);
    }

    /// <summary>Writes the specified <paramref name="text"/> to the stream.</summary>
    /// <remarks>This does not use the writers <see cref="StringEncoding"/>, instead the original <paramref name="text"/> unicode data is written.</remarks>
    /// <param name="text">Text to write.</param>
    /// <param name="byteCount">Optional: if set the specified number of bytes will be written (cropping and zero filling will be applied).</param>
    /// <returns>Returns the number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public int Write(IUnicode text, int byteCount = 0)
    {
        if (byteCount > 0)
        {
            if (text.Data.Length > byteCount)
            {
                text = text.FromArray(text.Data, 0, byteCount);
            }
        }
        Write(text.Data);
        if (byteCount > text.Data.Length)
        {
            Write(new byte[byteCount - text.Data.Length]);
        }
        return text.Data.Length;
    }

    /// <summary>Writes the specified <paramref name="text"/> to the stream.</summary>
    /// <param name="text">String to write.</param>
    /// <returns>Number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public int Write(string text)
    {
        if (text is null) throw new ArgumentNullException(nameof(text));
        var data = EncodeString(text);
        Write(data);
        return data.Length;
    }

    /// <summary>Writes the specified value directly to the stream.</summary>
    /// <param name="value">The value to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void Write(ushort value) => Write(EndianType == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value));

    /// <summary>Writes the specified value directly to the stream.</summary>
    /// <param name="value">The value to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void Write(uint value) => Write(EndianType == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value));

    /// <summary>Writes the specified value directly to the stream.</summary>
    /// <param name="value">The value to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void Write(ulong value) => Write(EndianType == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value));

    /// <summary>Writes the specified value directly to the stream.</summary>
    /// <param name="value">The value to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void Write(TimeSpan value) => Write(value.Ticks);

    /// <summary>Writes the specified datetime value with <see cref="DateTimeKind"/>.</summary>
    /// <param name="value">The value to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void Write(DateTime value)
    {
        Write7BitEncoded32((int)value.Kind);
        Write(value.Ticks);
    }

    /// <summary>Writes the specified 32 bit value to the stream 7 bit encoded (1-5 bytes).</summary>
    /// <param name="value">The value to write.</param>
    /// <returns>Number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public int Write7BitEncoded32(int value) => BitCoder32.Write7BitEncoded(this, value);

    /// <summary>Writes the specified 32 bit value to the stream 7 bit encoded (1-5 bytes).</summary>
    /// <param name="value">The value to write.</param>
    /// <returns>Number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public int Write7BitEncoded32(uint value) => BitCoder32.Write7BitEncoded(this, value);

    /// <summary>Writes the specified 64 bit value to the stream 7 bit encoded (1-9 bytes).</summary>
    /// <param name="value">The value to write.</param>
    /// <returns>Number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public int Write7BitEncoded64(long value) => BitCoder64.Write7BitEncoded(this, value);

    /// <summary>Writes the specified 64 bit value to the stream 7 bit encoded (1-9 bytes).</summary>
    /// <param name="value">The value to write.</param>
    /// <returns>Number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public int Write7BitEncoded64(ulong value) => BitCoder64.Write7BitEncoded(this, value);

    /// <summary>Writes an array of the specified struct type to the stream using the default marshaller prefixed by array length.</summary>
    /// <remarks>This function ignores <see cref="EndianType"/> and uses native marshalling!</remarks>
    /// <typeparam name="T">Type of each element.</typeparam>
    /// <param name="array">Array of elements.</param>
    /// <returns>Number of bytes written.</returns>
    public int WriteArray<T>(T[] array)
        where T : struct
    {
        if (array == null)
        {
            return Write7BitEncoded32(-1);
        }

        if (array.Length == 0)
        {
            return Write7BitEncoded32(0);
        }

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        ReadOnlySpan<byte> byteBuffer = MemoryMarshal.AsBytes(array.AsSpan());
        var headerLength = Write7BitEncoded32(byteBuffer.Length);
        BaseStream.Write(byteBuffer);
#else
        var size = Marshal.SizeOf(typeof(T));
        var byteCount = array.Length * size;
        var byteBuffer = new byte[byteCount];
        Buffer.BlockCopy(array, 0, byteBuffer, 0, byteCount);
        var headerLength = Write7BitEncoded32(byteBuffer.Length);
        BaseStream.Write(byteBuffer, 0, byteBuffer.Length);
#endif
        return headerLength + byteBuffer.Length;
    }

    /// <summary>Writes a 32bit linux epoch value (localtime).</summary>
    /// <remarks>This will not write timezone informaton and the reader will assume local time!</remarks>
    /// <param name="value">The value to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WriteEpoch32(DateTime value) => Write((uint)(value - UnixEpoch).TotalSeconds);

    /// <summary>Writes a 64bit linux epoch value (localtime).</summary>
    /// <remarks>This will not write timezone informaton and the reader will assume local time!</remarks>
    /// <param name="value">The value to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WriteEpoch64(DateTime value) => Write((ulong)(value - UnixEpoch).TotalSeconds);

    /// <summary>Writes the "new line" marking to the stream. This depends on the chosen <see cref="NewLineMode"/>.</summary>
    /// <returns>Number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public int WriteLine() => Write(LineFeed);

    /// <summary>Writes the specified string followed by a "new line" marking to the stream. This depends on the chosen <see cref="NewLineMode"/>.</summary>
    /// <param name="text">Text to write.</param>
    /// <returns>Number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public int WriteLine(string text)
    {
        if (text is null) throw new ArgumentNullException(nameof(text));
        var len = Write(text);
        return len + WriteLine();
    }

    /// <summary>Writes the specified unicode data followed by a "new line" marking to the stream. This depends on the chosen <see cref="NewLineMode"/>.</summary>
    /// <remarks>This does not use the writers <see cref="StringEncoding"/>, instead the original <paramref name="text"/> unicode data is written.</remarks>
    /// <param name="text">Text to write.</param>
    /// <returns>Number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public int WriteLine(IUnicode text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }
        return Write(text.Concat(LineFeed));
    }

    /// <summary>Writes a nullable Boolean value using a custom encoding.</summary>
    /// <remarks>
    /// The method encodes null as 0, <see langword="true"/> as 1, and <see langword="false"/> as -1. This encoding may be required for interoperability with
    /// specific data formats.
    /// </remarks>
    /// <param name="value">The nullable Boolean value to write. If null, a default value is written; otherwise, the Boolean value is encoded.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WriteNullable(bool? value) => Write((sbyte)(value switch { null => -1, true => 1, false => 0 }));

    /// <summary>Writes the specified buffer to the stream with length prefix.</summary>
    /// <param name="buffer">The buffer to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(byte[]? buffer)
    {
        if (buffer == null)
        {
            Write7BitEncoded32(-1);
        }
        else
        {
            Write7BitEncoded32(buffer.Length);
            BaseStream.Write(buffer, 0, buffer.Length);
        }
    }

    /// <summary>Writes the specified buffer to the stream with length prefix.</summary>
    /// <param name="buffer">The buffer to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(sbyte[]? buffer)
    {
        if (buffer == null)
        {
            Write7BitEncoded32(-1);
        }
        else
        {
            var bytes = new byte[buffer.Length];
            Buffer.BlockCopy(buffer, 0, bytes, 0, buffer.Length);
            WritePrefixed(bytes);
        }
    }

    /// <summary>Writes the specified buffer to the stream with length prefix.</summary>
    /// <param name="buffer">The buffer to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(float[]? buffer)
    {
        if (buffer == null)
        {
            Write7BitEncoded32(-1);
        }
        else
        {
            WritePrefixed(EndianType == EndianType.BigEndian ? BigEndian.GetBytes(buffer) : LittleEndian.GetBytes(buffer));
        }
    }

    /// <summary>Writes the specified buffer to the stream with length prefix.</summary>
    /// <param name="buffer">The buffer to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(double[]? buffer)
    {
        if (buffer == null)
        {
            Write7BitEncoded32(-1);
        }
        else
        {
            WritePrefixed(EndianType == EndianType.BigEndian ? BigEndian.GetBytes(buffer) : LittleEndian.GetBytes(buffer));
        }
    }

    /// <summary>Writes the specified buffer to the stream with length prefix.</summary>
    /// <param name="buffer">The buffer to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(ulong[]? buffer)
    {
        if (buffer == null)
        {
            Write7BitEncoded32(-1);
        }
        else
        {
            WritePrefixed(EndianType == EndianType.BigEndian ? BigEndian.GetBytes(buffer) : LittleEndian.GetBytes(buffer));
        }
    }

    /// <summary>Writes the specified buffer to the stream with length prefix.</summary>
    /// <param name="buffer">The buffer to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(long[]? buffer)
    {
        if (buffer == null)
        {
            Write7BitEncoded32(-1);
        }
        else
        {
            WritePrefixed(EndianType == EndianType.BigEndian ? BigEndian.GetBytes(buffer) : LittleEndian.GetBytes(buffer));
        }
    }

    /// <summary>Writes the specified buffer to the stream with length prefix.</summary>
    /// <param name="buffer">The buffer to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(uint[]? buffer)
    {
        if (buffer == null)
        {
            Write7BitEncoded32(-1);
        }
        else
        {
            WritePrefixed(EndianType == EndianType.BigEndian ? BigEndian.GetBytes(buffer) : LittleEndian.GetBytes(buffer));
        }
    }

    /// <summary>Writes the specified buffer to the stream with length prefix.</summary>
    /// <param name="buffer">The buffer to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(int[]? buffer)
    {
        if (buffer == null)
        {
            Write7BitEncoded32(-1);
        }
        else
        {
            WritePrefixed(EndianType == EndianType.BigEndian ? BigEndian.GetBytes(buffer) : LittleEndian.GetBytes(buffer));
        }
    }

    /// <summary>Writes the specified buffer to the stream with length prefix.</summary>
    /// <param name="buffer">The buffer to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(short[]? buffer)
    {
        if (buffer == null)
        {
            Write7BitEncoded32(-1);
        }
        else
        {
            WritePrefixed(EndianType == EndianType.BigEndian ? BigEndian.GetBytes(buffer) : LittleEndian.GetBytes(buffer));
        }
    }

    /// <summary>Writes the specified buffer to the stream with length prefix.</summary>
    /// <param name="buffer">The buffer to write.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(ushort[]? buffer)
    {
        if (buffer == null)
        {
            Write7BitEncoded32(-1);
        }
        else
        {
            WritePrefixed(EndianType == EndianType.BigEndian ? BigEndian.GetBytes(buffer) : LittleEndian.GetBytes(buffer));
        }
    }

    /// <summary>
    /// Writes the specified value with length prefix and little endian encoding to the stream. This uses 1..9 bytes and allows to write a null value.
    /// </summary>
    /// <param name="value">Value to write</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(float? value)
    {
        if (value is not float data)
        {
            Write((byte)0);
        }
        else
        {
            WritePrefixed(EndianType == EndianType.BigEndian ? BigEndian.GetBytes(data) : LittleEndian.GetBytes(data));
        }
    }

    /// <summary>
    /// Writes the specified value with length prefix and little endian encoding to the stream. This uses 1..9 bytes and allows to write a null value.
    /// </summary>
    /// <param name="value">Value to write</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(double? value)
    {
        if (value is not double data)
        {
            Write((byte)0);
        }
        else
        {
            WritePrefixed(EndianType == EndianType.BigEndian ? BigEndian.GetBytes(data) : LittleEndian.GetBytes(data));
        }
    }

    /// <summary>
    /// Writes the specified value with length prefix and little endian encoding to the stream. This uses 1..9 bytes and allows to write a null value.
    /// </summary>
    /// <param name="value">Value to write</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(decimal? value)
    {
        if (value is not decimal data)
        {
            Write((byte)0);
        }
        else
        {
            WritePrefixed(EndianType == EndianType.BigEndian ? BigEndian.GetBytes(data) : LittleEndian.GetBytes(data));
        }
    }

    /// <summary>
    /// Writes the specified value with length prefix and little endian encoding to the stream. This uses 1..9 bytes and allows to write a null value.
    /// </summary>
    /// <param name="value">Value to write</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(DateTime? value) => WritePrefixed(value?.Ticks);

    /// <summary>
    /// Writes the specified value with length prefix and little endian encoding to the stream. This uses 1..9 bytes and allows to write a null value.
    /// </summary>
    /// <param name="value">Value to write</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(TimeSpan? value) => WritePrefixed(value?.Ticks);

    /// <summary>
    /// Writes the specified value with length prefix and little endian encoding to the stream. This uses 1..9 bytes and allows to write a null value.
    /// </summary>
    /// <param name="value">Value to write</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(long? value)
    {
        if (value is not long value64) Write((byte)0);
        else BitCoder64.Write8BitPrefixed(this, value64);
    }

    /// <summary>
    /// Writes the specified value with length prefix and little endian encoding to the stream. This uses 1..9 bytes and allows to write a null value.
    /// </summary>
    /// <param name="value">Value to write</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(ulong? value)
    {
        if (value is not ulong value64) Write((byte)0);
        else BitCoder64.Write8BitPrefixed(this, value64);
    }

    /// <summary>
    /// Writes the specified value with length prefix and little endian encoding to the stream. This uses 1..5 bytes and allows to write a null value.
    /// </summary>
    /// <param name="value">Value to write</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(int? value)
    {
        if (value is not int value32) Write((byte)0);
        else BitCoder32.Write8BitPrefixed(this, value32);
    }

    /// <summary>
    /// Writes the specified value with length prefix and little endian encoding to the stream. This uses 1..5 bytes and allows to write a null value.
    /// </summary>
    /// <param name="value">Value to write</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(short? value)
    {
        if (value is not short value16) Write((byte)0);
        else BitCoder32.Write8BitPrefixed(this, (ushort)value16);
    }

    /// <summary>
    /// Writes the specified value with length prefix and little endian encoding to the stream. This uses 1..5 bytes and allows to write a null value.
    /// </summary>
    /// <param name="value">Value to write</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(sbyte? value)
    {
        if (value is not sbyte value8) Write((byte)0);
        else BitCoder32.Write8BitPrefixed(this, (byte)value8);
    }

    /// <summary>
    /// Writes the specified value with length prefix and little endian encoding to the stream. This uses 1..5 bytes and allows to write a null value.
    /// </summary>
    /// <param name="value">Value to write</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(byte? value)
    {
        if (value is not byte value8) Write((byte)0);
        else BitCoder32.Write8BitPrefixed(this, (byte)value8);
    }

    /// <summary>
    /// Writes the specified value with length prefix and little endian encoding to the stream. This uses 1..5 bytes and allows to write a null value.
    /// </summary>
    /// <param name="value">Value to write</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(uint? value)
    {
        if (value is not uint value32) Write((byte)0);
        else BitCoder32.Write8BitPrefixed(this, value32);
    }

    /// <summary>
    /// Writes the specified value with length prefix and little endian encoding to the stream. This uses 1 or 17 bytes and allows to write a null value.
    /// </summary>
    /// <param name="value">Value to write</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(Guid? value)
    {
        if (value is not Guid guid)
        {
            Write((byte)0);
        }
        else
        {
            Write((byte)16);
            Write(guid);
        }
    }

    /// <summary>Writes a part of the specified buffer to the stream with length prefix.</summary>
    /// <param name="buffer">The buffer to write.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
    /// <param name="count">The number of bytes to be written to the current stream.</param>
    [MethodImpl((MethodImplOptions)256)]
    public void WritePrefixed(byte[] buffer, int offset, int count)
    {
        Write7BitEncoded32(count);
        BaseStream.Write(buffer, offset, count);
    }

    /// <summary>Writes the specified string with length prefix directly to the stream.</summary>
    /// <param name="text">String to write.</param>
    /// <returns>Number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public int WritePrefixed(string? text)
    {
        if (text == null)
        {
            return Write7BitEncoded32(-1);
        }

        var data = EncodeString(text);
        var prefix = Write7BitEncoded32(data.Length);
        Write(data);
        return prefix + data.Length;
    }

    /// <summary>Writes the specified string with length prefix directly to the stream.</summary>
    /// <remarks>This does not use the writers <see cref="StringEncoding"/>, instead the original <paramref name="text"/> unicode data is written.</remarks>
    /// <param name="text">String to write.</param>
    /// <returns>Number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public int WritePrefixed(IUnicode text)
    {
        if (text == null)
        {
            return Write7BitEncoded32(-1);
        }

        var len = text.Data.Length;
        var prefix = Write7BitEncoded32(len);
        Write(text.Data);
        return prefix + len;
    }

    /// <summary>
    /// Writes the specified <paramref name="text"/> by obeying maximum space and filling the remaining space with zero character. If the text is too long
    /// characters will be left out from writing and incomplete characters or space left will always be zeroed.
    /// </summary>
    /// <param name="text">Text to write.</param>
    /// <param name="byteCount">Optional: if set the specified number of bytes will be written (cropping and zero filling will be applied).</param>
    /// <returns>
    /// Returns the number of bytes needed for the text not the number of bytes written. This may exceed the number of bytes written if the text exceeded the space.
    /// </returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="Exception"></exception>
    [MethodImpl((MethodImplOptions)256)]
    public int WriteString(string text, int byteCount = 0)
    {
        if (text is null) throw new ArgumentNullException(nameof(text));
        var data = EncodeString(text);
        var required = data.Length;
        if (byteCount > 0)
        {
            ZeroTerminationTest();
            while (data.Length > byteCount)
            {
                var remove = Math.Max(1, (data.Length - byteCount) / 4);
                text = text[..^remove];
                data = EncodeString(text);
            }
        }
        Write(data);
        if (data.Length < byteCount)
        {
            Write(new byte[byteCount - data.Length]);
        }

        return required;
    }

    /// <summary>Writes the specified struct directly to the stream using the default marshaller.</summary>
    /// <typeparam name="T">the struct.</typeparam>
    /// <param name="item">The value to write.</param>
    /// <returns>Number of bytes written.</returns>
    public int WriteStruct<T>(T item)
        where T : struct
    {
        var len = Marshal.SizeOf(item);
        var data = new byte[len];
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        Marshal.StructureToPtr(item, handle.AddrOfPinnedObject(), false);
        handle.Free();
        Write(data);
        return len;
    }

    /// <summary>
    /// Writes the specified <paramref name="text"/> by obeying maximum space and filling the remaining space with zero character. If the text is too long,
    /// characters will be left out from writing and incomplete characters or space left will always be zeroed.
    /// </summary>
    /// <param name="text">Text to write.</param>
    /// <param name="maxLength">Maximum number of bytes to write.</param>
    /// <returns>Returns the number of bytes written.</returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="Exception"></exception>
    public int WriteZeroTerminated(string text, int maxLength)
    {
        if (maxLength <= 0) return WriteZeroTerminated(text);
        ZeroTerminationTest();

        var data = EncodeString(text + '\0');
        while (data.Length >= maxLength)
        {
            var remove = data.Length - maxLength;
            text = text[..^remove];
            data = EncodeString(text + '\0');
        }
        Write(data);
        return data.Length;
    }

    /// <summary>Writes the specified <paramref name="text"/> zero terminated to the string.</summary>
    /// <param name="text">Text to write.</param>
    /// <returns>Returns the number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public int WriteZeroTerminated(string text)
    {
        if (text is null) throw new ArgumentNullException(nameof(text));
        ZeroTerminationTest();
        return Write(text) + Write('\0');
    }

    /// <summary>Writes the specified <paramref name="text"/> zero terminated to the string.</summary>
    /// <remarks>This does not use the writers <see cref="StringEncoding"/>, instead the original <paramref name="text"/> unicode data is written.</remarks>
    /// <param name="text">Text to write.</param>
    /// <returns>Returns the number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public int WriteZeroTerminated(IUnicode text)
    {
        if (text is null) throw new ArgumentNullException(nameof(text));
        ZeroTerminationTest();
        return Write(text.Concat("\0"));
    }

    #endregion Public Methods
}
