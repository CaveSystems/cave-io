#nullable enable

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace Cave.IO;

/// <summary>
/// Provides a new little endian binary reader implementation (a combination of streamreader and binaryreader). This class is not threadsafe and does not buffer
/// anything nor needs flushing. You can access the basestream at any time with any mode of operation (read, write, seek, ...).
/// </summary>
public sealed class DataReader
{
    #region Private Fields

    bool closed;
    IBitConverter endianDecoder;
    EndianType endianType;
    NewLineData? newLineData;
    NewLineMode newLineMode;
    StringEncoding stringEncoding;
    byte[]? zeroBytes;

    #endregion Private Fields

    #region Private Methods

    string DecodeString(byte[] block, int start = 0, int length = -1) => StringEncoding.Decode(block, start, length);

    char ReadCharUTF7()
    {
        var b = ReadByte();
        if (b == '+')
        {
            var i = 0;
            var buf = new byte[8];
            buf[i++] = b;
            do
            {
                buf[i++] = b = ReadByte();
            }
            while (b != '-');
            Array.Resize(ref buf, i);
            var chars = UTF7.Decode(buf);
            if (chars.Length > 1)
            {
                throw new InvalidDataException($"Cannot parse utf7 stateless with multiple encoded characters! (Got {chars} instead of single character!)");
            }

            return chars[0];
        }

        return (char)b;
    }

    /// <summary>Reads a string ending with the specified <paramref name="endMark"/> from the stream.</summary>
    /// <param name="endMark">End of string bytes</param>
    /// <param name="endChars">End of string characters</param>
    /// <param name="maximumBytes">The maximum number of bytes to read.</param>
    /// <returns>The string.</returns>
    string ReadUntil(byte[] endMark, string endChars, int maximumBytes = 64 * 1024)
    {
        var buf = new byte[maximumBytes];
        var endOffset = 0;

        switch (StringEncoding)
        {
            case (StringEncoding)3: //StringEncoding.UTF16:
            case StringEncoding.UTF_16:
            case StringEncoding.UTF_16BE:
            {
                ReadUntil(buf, ref endOffset, 2, false, endMark);
                break;
            }
            case (StringEncoding)4: //StringEncoding.UTF32:
            case StringEncoding.UTF_32:
            case StringEncoding.UTF_32BE:
            {
                ReadUntil(buf, ref endOffset, 4, false, endMark);
                break;
            }
            default:
            {
                ReadUntil(buf, ref endOffset, false, endMark);
                break;
            }
        }

        //check preamble
        var result = DecodeString(buf, 0, endOffset);
        var index = result.IndexOf(endChars, StringComparison.Ordinal);
        if (index > -1) result = result[0..index];
        return result;
    }

    #endregion Private Methods

    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="DataReader"/> class.</summary>
    /// <param name="input">The stream to read from.</param>
    /// <param name="encoding">The encoding. Use UTF_8, UTF_16 or UTF_32 whenever possible!</param>
    /// <param name="endian">The endian type.</param>
    /// <param name="newLine">New line mode.</param>
    /// <exception cref="ArgumentNullException">output.</exception>
    /// <exception cref="ArgumentException">Stream does not support writing or is already closed.;output.</exception>
    /// <exception cref="NotSupportedException">StringEncoding {0} not supported! or EndianType {0} not supported!.</exception>
    public DataReader(Stream input, StringEncoding encoding = StringEncoding.UTF_8, NewLineMode newLine = NewLineMode.LF, EndianType endian = EndianType.LittleEndian)
    {
        BaseStream = input ?? throw new ArgumentNullException(nameof(input));
        newLineMode = newLine;
        stringEncoding = encoding != StringEncoding.Undefined ? encoding : throw new ArgumentOutOfRangeException(nameof(encoding));
        endianType = endian;
        endianDecoder = endian switch
        {
            EndianType.LittleEndian => new BitConverterLE(),
            EndianType.BigEndian => new BitConverterBE(),
            _ => throw new NotImplementedException($"EndianType {endian} not implemented!")
        };
        if (!BaseStream.CanRead)
        {
            throw new ArgumentException("Stream does not support reading or is already closed.", nameof(input));
        }
    }

    /// <summary>Initializes a new instance of the <see cref="DataReader"/> class.</summary>
    /// <param name="input">The stream to read from.</param>
    /// <param name="newLine">New line mode.</param>
    /// <param name="encoding">Encoding to use for characters and strings.</param>
    /// <param name="endian">The endian type.</param>
    /// <exception cref="ArgumentNullException">output.</exception>
    /// <exception cref="ArgumentException">Stream does not support writing or is already closed.;output.</exception>
    /// <exception cref="NotSupportedException">StringEncoding {0} not supported! or EndianType {0} not supported!.</exception>
    [Obsolete("Encoding classes in net framework change during each framework revision. Use StringEncoding.UTF_XX whenever possible!")]
    public DataReader(Stream input, Encoding encoding, NewLineMode newLine = NewLineMode.LF, EndianType endian = EndianType.LittleEndian)
    {
        BaseStream = input ?? throw new ArgumentNullException(nameof(input));
        newLineMode = newLine;
        stringEncoding = encoding?.ToStringEncoding() ?? throw new ArgumentOutOfRangeException(nameof(encoding));
        endianType = endian;
        endianDecoder = endian switch
        {
            EndianType.LittleEndian => new BitConverterLE(),
            EndianType.BigEndian => new BitConverterBE(),
            _ => throw new NotImplementedException($"EndianType {endian} not implemented!")
        };
        if (!BaseStream.CanRead)
        {
            throw new ArgumentException("Stream does not support reading or is already closed.", nameof(input));
        }
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets the available bytes for reading. Attention: the BaseStream has to support the Length and Position properties.</summary>
    public long Available => BaseStream.Length - BaseStream.Position;

    /// <summary>Gets access to the base stream.</summary>
    public Stream BaseStream { get; }

    /// <summary>Gets or sets the Encoding to use for characters and strings. Setting this property updates <see cref="StringEncoding"/> automatically.</summary>
    /// <remarks>This can be used between all read calls.</remarks>
    [Obsolete("Use StringEncoding whenever possible.")]
    public Encoding Encoding
    {
        get => stringEncoding.Create();
        set => StringEncoding = value.ToStringEncoding();
    }

    /// <summary>Gets or sets the endian encoder type.</summary>
    /// <remarks>This can be used between all read calls.</remarks>
    /// <value>The endian encoder type.</value>
    public EndianType EndianType
    {
        get => endianType;
        set
        {
            endianType = value;
            endianDecoder = endianType switch
            {
                EndianType.LittleEndian => new BitConverterLE(),
                EndianType.BigEndian => new BitConverterBE(),
                _ => throw new NotImplementedException($"EndianType {endianType} not implemented!")
            };
        }
    }

    /// <summary>Gets the line feed string.</summary>
    public string LineFeed => (newLineData ??= new NewLineData(StringEncoding, newLineMode)).LineFeed;

    /// <summary>Gets or sets the new line mode used.</summary>
    /// <remarks>This can be used between all read calls.</remarks>
    public NewLineMode NewLineMode
    {
        get => newLineMode;
        set { newLineData = null; newLineMode = value; }
    }

    /// <summary>Gets or sets the Encoding to use for characters and strings. Setting this property updates <see cref="Encoding"/> automatically.</summary>
    /// <remarks>This can be used between all read calls.</remarks>
    public StringEncoding StringEncoding
    {
        get => stringEncoding;
        set { stringEncoding = value; newLineData = null; zeroBytes = null; }
    }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Closes the reader and the stream.</summary>
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
    public void Flush() => BaseStream.Flush();

    /// <summary>Reads a 7 bit encoded 32 bit value from the stream.</summary>
    /// <returns>The value.</returns>
    public int Read7BitEncodedInt32() => BitCoder32.Read7BitEncodedInt32(BaseStream);

    /// <summary>Reads a 7 bit encoded 64 bit value from the stream.</summary>
    /// <returns>The value.</returns>
    public long Read7BitEncodedInt64() => BitCoder64.Read7BitEncodedInt64(BaseStream);

    /// <summary>Reads a 7 bit encoded 32 bit value from the stream.</summary>
    /// <returns>The value.</returns>
    public uint Read7BitEncodedUInt32() => BitCoder32.Read7BitEncodedUInt32(BaseStream);

    /// <summary>Reads a 7 bit encoded 64 bit value from the stream.</summary>
    /// <returns>The value.</returns>
    public ulong Read7BitEncodedUInt64() => BitCoder64.Read7BitEncodedUInt64(BaseStream);

    /// <summary>Reads an array of the specified struct type from the stream using the default marshaller.</summary>
    /// <typeparam name="T">Type of each element.</typeparam>
    /// <returns>The struct array.</returns>
    public T[] ReadArray<T>()
        where T : struct
    {
        var count = Read7BitEncodedInt32();
        if (count < 0)
        {
            throw new InvalidDataException("Invalid length prefix while reading array!");
        }

        if (count == 0)
        {
            return new T[0];
        }

        var byteCount = Read7BitEncodedInt32();
        var bytes = ReadBytes(byteCount);
        if (bytes is T[] result)
        {
            if (result == null)
            {
                throw new PlatformNotSupportedException("Byte array conversion bug! Please update your mono framework!");
            }
        }
        else
        {
            result = new T[count];
            Buffer.BlockCopy(bytes, 0, result, 0, byteCount);
        }

        return result;
    }

    /// <summary>Reads an us ascii text from string.</summary>
    /// <param name="charCount">Character count to read.</param>
    /// <returns>Returns the read text.</returns>
    public string ReadASCII(int charCount) => ASCII.GetString(ReadBytes(charCount));

    /// <summary>Reads the specified value from the stream.</summary>
    /// <returns>The value.</returns>
    public bool ReadBool() => ReadByte() != 0;

    /// <summary>Reads the specified value from the stream.</summary>
    /// <returns>The value.</returns>
    public byte ReadByte()
    {
        var b = BaseStream.ReadByte();
        if (b < 0)
        {
            throw new EndOfStreamException();
        }

        return (byte)b;
    }

    /// <summary>Reads a byte buffer with length prefix from the stream.</summary>
    /// <exception cref="InvalidDataException">Thrown if a invalid 7bit encoded value is found.</exception>
    /// <returns>The value.</returns>
    public byte[]? ReadBytes()
    {
        var length = Read7BitEncodedInt32();
        if (length < 0)
        {
            if (length == -1)
            {
                return null;
            }

            throw new InvalidDataException("Invalid 7bit encoded value found!");
        }

        return ReadBytes(length);
    }

    /// <summary>Reads a buffer from the stream.</summary>
    /// <param name="count">Number of bytes to read.</param>
    /// <returns>The value.</returns>
    public byte[] ReadBytes(int count)
    {
        var result = new byte[count];
        var done = 0;
        while (done < count)
        {
            var read = BaseStream.Read(result, done, count - done);
            if (read == 0)
            {
                throw new EndOfStreamException();
            }

            done += read;
        }

        return result;
    }

    /// <summary>Reads a character from the stream.</summary>
    /// <remarks>
    /// This function works only for the BMP (basic multilingual plane) characters. Use ReadChar(1) instead if you want to read the full unicode codepoints.
    /// </remarks>
    /// <returns>A BMP Unicode character.</returns>
    public char ReadChar()
    {
        switch (StringEncoding)
        {
            case StringEncoding.UTF_7: return ReadCharUTF7();
            case (StringEncoding)1 or StringEncoding.US_ASCII:
            {
                var b = ReadByte();
                if (b > 127)
                {
                    throw new InvalidDataException($"Byte '{b}' is not a valid ASCII character!");
                }

                return (char)b;
            }
        }

        var result = ReadChars(1);
        if (result.Length > 1)
        {
            throw new InvalidOperationException("Decoded character requires a high and low surrogate pair! Use ReadChars(1)!");
        }

        return result[0];
    }

    /// <summary>Reads characters from the stream.</summary>
    /// <param name="codepoints">Number of unicode codepoints (not bytes) to read (one unicode codepoint may contain two csharp unicode characters).</param>
    /// <returns>The value.</returns>
    public string ReadChars(int codepoints)
    {
        switch (StringEncoding)
        {
            case (StringEncoding)1: /* StringEncoding.ASCII: */
            case StringEncoding.US_ASCII: return ReadASCII(codepoints);

            case (StringEncoding)2: /* StringEncoding.UTF8: */
            case StringEncoding.UTF_8: return ReadUTF8(codepoints);

            case (StringEncoding)3: /* StringEncoding.UTF16: */
            case StringEncoding.UTF_16: return ReadUTF16LE(codepoints);

            case StringEncoding.UTF_16BE: return ReadUTF16BE(codepoints);

            case (StringEncoding)4: /* StringEncoding.UTF32: */
            case StringEncoding.UTF_32: return ReadUTF32LE(codepoints);

            case StringEncoding.UTF_32BE: return ReadUTF32BE(codepoints);

            case StringEncoding.UTF_7: return ReadUTF7(codepoints);

            case StringEncoding.CSISO2022JP:
            case StringEncoding.ISO_2022_JP:
            case StringEncoding.ISO_2022_JP_2: return ReadISO2022(codepoints, true);

            case StringEncoding.ISO_2022_KR: return ReadISO2022(codepoints, false);

            case StringEncoding.HZ_GB_2312: return ReadHZ(codepoints);
        }

        var textDecoder = StringEncoding.Create();
        if (textDecoder.IsSingleByte)
        {
            return textDecoder.GetString(ReadBytes(codepoints));
        }

        var buffer = ReadBytes(codepoints);
        var decoder = textDecoder.GetDecoder();
        var chars = new char[codepoints * 4];
        decoder.Convert(buffer, 0, buffer.Length, chars, 0, chars.Length, false, out var bytesUsed, out var charsUsed, out var complete);
        if (bytesUsed != buffer.Length)
        {
            throw new InvalidDataException("Invalid additional data!");
        }

        while (true)
        {
            var resultCount = chars.CountCodepoints(charsUsed);
            if (decoder.FallbackBuffer.Remaining == 0 && complete && resultCount >= codepoints) break;

            var charCountLeft = codepoints - resultCount;
            buffer = ReadBytes(charCountLeft);

            decoder.Convert(buffer, 0, buffer.Length, chars, charsUsed, chars.Length - charsUsed, false, out bytesUsed, out var charsUsedNext, out complete);
            if (bytesUsed != buffer.Length)
            {
                throw new InvalidDataException("Invalid additional data!");
            }
            charsUsed += charsUsedNext;
        }

        return new(chars, 0, charsUsed);
    }

    /// <summary>Reads a DateTime value from the stream with <see cref="DateTimeKind"/>.</summary>
    /// <returns>The value.</returns>
    public DateTime ReadDateTime()
    {
        var kind = (DateTimeKind)Read7BitEncodedInt32();
        switch (kind)
        {
            case DateTimeKind.Local:
            case DateTimeKind.Unspecified:
            case DateTimeKind.Utc:
                break;

            default: throw new InvalidDataException("Invalid DateTimeKind!");
        }

        return new DateTime(ReadInt64(), kind);
    }

    /// <summary>Reads a value from the stream.</summary>
    /// <returns>The value.</returns>
    public decimal ReadDecimal()
    {
        var bits = new int[4];
        for (var i = 0; i < 4; i++)
        {
            bits[i] = ReadInt32();
        }

        return new decimal(bits);
    }

    /// <summary>Reads a value from the stream.</summary>
    /// <returns>The value.</returns>
    public double ReadDouble()
    {
        var bytes = ReadBytes(8);
        return endianDecoder.ToDouble(bytes, 0);
    }

    /// <summary>Reads a 32bit linux epoch value.</summary>
    /// <returns>The value.</returns>
    public DateTime ReadEpoch32() => new DateTime(1970, 1, 1) + TimeSpan.FromSeconds(ReadUInt32());

    /// <summary>Reads a 64bit linux epoch value.</summary>
    /// <returns>The value.</returns>
    public DateTime ReadEpoch64() => new DateTime(1970, 1, 1) + TimeSpan.FromSeconds(ReadUInt64());

    /// <summary>Reads a guid from the stream.</summary>
    /// <returns>The guid.</returns>
    public Guid ReadGuid() => new(ReadBytes(16));

    /// <summary>Reads a HZ encoded string.</summary>
    /// <param name="codepoints">Number of codepoints to read</param>
    /// <returns>Returns the decoded string.</returns>
    /// <exception cref="InvalidDataException"></exception>
    /// s
    public string ReadHZ(int codepoints)
    {
        var block = new byte[Math.Max(16, codepoints * 2)];
        var len = 0;
        byte currentByte;

        void ReadAndStoreByte()
        {
            if (len >= block!.Length) Array.Resize(ref block, block.Length * 2);
            block![len++] = currentByte = ReadByte();
        }

        var shift = 0;
        var charactercount = 0;
        while (true)
        {
            if (shift == 0 && charactercount == codepoints)
            {
                return StringEncoding.Decode(block, 0, len);
            }
            ReadAndStoreByte();
            if (shift == 0)
            {
                if (currentByte == '~')
                {
                    ReadAndStoreByte();
                    if (currentByte == '{') shift = 1;
                    else if (currentByte == '~') charactercount++;
                    else if (currentByte != '\n') throw new InvalidDataException();
                    continue;
                }
                charactercount++;
                continue;
            }
            if (shift == 1)
            {
                var firstByte = currentByte;
                ReadAndStoreByte();
                if (firstByte == '~' && currentByte == '}') shift = 0;
                else charactercount++;
                continue;
            }
        }
    }

    /// <summary>Reads a value directly from the stream.</summary>
    /// <returns>The value.</returns>
    public short ReadInt16()
    {
        var bytes = ReadBytes(2);
        return endianDecoder.ToInt16(bytes, 0);
    }

    /// <summary>Reads a value directly from the stream.</summary>
    /// <returns>The value.</returns>
    public int ReadInt32()
    {
        var bytes = ReadBytes(4);
        return endianDecoder.ToInt32(bytes, 0);
    }

    /// <summary>Reads a value directly from the stream.</summary>
    /// <returns>The value.</returns>
    public long ReadInt64()
    {
        var bytes = ReadBytes(8);
        return endianDecoder.ToInt64(bytes, 0);
    }

    /// <summary>Reads a value directly from the stream.</summary>
    /// <returns>The value.</returns>
    public sbyte ReadInt8()
    {
        unchecked
        {
            return (sbyte)ReadByte();
        }
    }

    public string ReadISO2022(int codepoints, bool requireReset)
    {
        var block = new byte[Math.Max(16, codepoints * 2)];
        var len = 0;
        var currentCode = string.Empty;
        var shift = false;
        byte currentByte;

        void ReadAndStoreByte()
        {
            if (len >= block!.Length) Array.Resize(ref block, block.Length * 2);
            block![len++] = currentByte = ReadByte();
        }

        bool TryReadCode()
        {
            var sb = new StringBuilder();
            do
            {
                ReadAndStoreByte();
                sb.Append((char)currentByte);
            }
            while (currentByte is (>= 0x20 and <= 0x2F));
            var code = sb.ToString();
            if (currentByte < 0x20)
            {
                // unhandled escape sequence -> ignore
                return false;
            }
            if (currentByte is not (>= 0x30 and <= 0x7E))
            {
                /* invalid escape sequence -> interpret as single byte characters */
                return false;
            }
            if (code.Length < 2) return false;

            switch (code[0])
            {
                case '$': // bytesPerCharacter = 2;
                case '&':
                case '(': // bytesPerCharacter = 1;
                    break;

                default: // unhandled escape sequence -> ignore
                    return false;
            }

            //this is a character plain change -> keep
            currentCode = code;
            shift = false;
            return true;
        }

        var encoding = StringEncoding.Create();
        while (true)
        {
            //read first byte
            ReadAndStoreByte();
            if (currentByte == 27)
            {
            ControlCode:
                //decode control code
                if (TryReadCode()) continue;
                if (currentByte == 27) goto ControlCode;
            }

            switch (currentByte)
            {
                case 0x0e: { shift = true; continue; }
                case 0x0f: { shift = false; break; }
                default: break; // no special handling...
            }

            //now at least first char is complete, test char count
            var charCount = encoding.GetCharCount(block, 0, len);
            if (charCount >= codepoints)
            {
                var result = StringEncoding.Decode(block, 0, len);
                if (result.CountCodepoints() > codepoints)
                {
                    Debugger.Break();
                    throw new InvalidDataException("Overflow!");
                }
                if (result.CountCodepoints() >= codepoints)
                {
                    //we are almost complete... iso2022 requires the string to end with a switch to ascii if a code was used
                    //we we are in shift, we need the return
                    if (!requireReset) currentCode = string.Empty;
                    if (shift || currentCode is not ("(B" or ""))
                    {
                        while (shift || currentCode is not ("(B" or ""))
                        {
                            ReadAndStoreByte();
                            switch (currentByte)
                            {
                                case 15: { shift = false; break; }
                                case 27: { TryReadCode(); break; }
                            }
                        }
                        result = StringEncoding.Decode(block, 0, len);
                    }
                    return result;
                }
            }
        }
    }

    /// <summary>Reads a string ending with [CR]LF from the stream.</summary>
    /// <param name="maximumBytes">The maximum number of bytes to read.</param>
    /// <returns>The string.</returns>
    public string ReadLine(int maximumBytes = 64 * 1024)
    {
        newLineData ??= new(stringEncoding, newLineMode);
        if (StringEncoding == StringEncoding.UTF_7)
        {
            return ReadUTF7(maximumBytes, newLineData.LineFeed);
        }

        return ReadUntil(newLineData.Data, newLineData.LineFeed, maximumBytes);
    }

    /// <summary>Reads a string with length prefix from the stream.</summary>
    /// <exception cref="InvalidDataException">Thrown if an invalid 7bit encoded value found.</exception>
    /// <returns>The string.</returns>
    public string? ReadPrefixedString()
    {
        var length = Read7BitEncodedInt32();
        if (length == 0) return string.Empty;
        if (length < 0)
        {
            if (length == -1)
            {
                return null;
            }

            throw new InvalidDataException("Invalid 7bit encoded value found!");
        }

        return ReadString(length);
    }

    /// <summary>Writes the specified value directly to the stream.</summary>
    /// <returns>The value.</returns>
    public float ReadSingle()
    {
        var bytes = ReadBytes(4);
        return endianDecoder.ToSingle(bytes, 0);
    }

    /// <summary>Reads a string of the specified byte count from the stream.</summary>
    /// <param name="byteCount">Number of bytes to read.</param>
    /// <returns>The string.</returns>
    public string ReadString(int byteCount) => ReadString(byteCount, 0);

    /// <summary>Reads a string of the specified byte count from the stream.</summary>
    /// <param name="byteCount">Number of bytes to read. (Optional, <paramref name="charCount"/> can be used instead.)</param>
    /// <param name="charCount">Number of characters to read. (Optional, <paramref name="byteCount"/> can be used instead.)</param>
    /// <returns>The string read from the specified block of (bytes or chars).</returns>
    public string ReadString(int byteCount = 0, int charCount = 0)
    {
        if (byteCount == 0)
        {
            if (charCount == 0) throw new ArgumentOutOfRangeException(nameof(charCount));
            return ReadChars(charCount);
        }
        if (charCount != 0) throw new ArgumentOutOfRangeException(nameof(charCount));

        var block = ReadBytes(byteCount);
        return DecodeString(block);
    }

    /// <summary>Reads a string with length prefix from the stream.</summary>
    /// <exception cref="InvalidDataException">Thrown if an invalid 7bit encoded value found.</exception>
    /// <returns>The string.</returns>
    [Obsolete("Use ReadPrefixedString()")]
    public string? ReadString() => ReadPrefixedString();

    /// <summary>Reads the specified struct from the stream using the default marshaller.</summary>
    /// <typeparam name="T">the struct.</typeparam>
    /// <returns>The struct.</returns>
    public T ReadStruct<T>()
        where T : struct
    {
        var size = MarshalStruct.SizeOf<T>();
        var buffer = ReadBytes(size);
        MarshalStruct.Copy(buffer, out T result);
        return result;
    }

    /// <summary>Reads a value from the stream.</summary>
    /// <returns>The value.</returns>
    public TimeSpan ReadTimeSpan() => new(ReadInt64());

    /// <summary>Reads a value directly from the stream.</summary>
    /// <returns>The value.</returns>
    public ushort ReadUInt16()
    {
        var bytes = ReadBytes(2);
        return endianDecoder.ToUInt16(bytes, 0);
    }

    /// <summary>Reads a value directly from the stream.</summary>
    /// <returns>The value.</returns>
    public uint ReadUInt32()
    {
        var bytes = ReadBytes(4);
        return endianDecoder.ToUInt32(bytes, 0);
    }

    /// <summary>Reads a value directly from the stream.</summary>
    /// <returns>The value.</returns>
    public ulong ReadUInt64()
    {
        var bytes = ReadBytes(8);
        return endianDecoder.ToUInt64(bytes, 0);
    }

    /// <summary>Reads a value directly from the stream.</summary>
    /// <returns>The value.</returns>
    public byte ReadUInt8() => ReadByte();

    /// <summary>Reads bytes from the stream until one of the specified end markers are found or buffer length is reached.</summary>
    /// <param name="data">An array of bytes.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
    /// <param name="removeMarker">If set, removes found mark from endMark array.</param>
    /// <param name="endMark">Array of ending markers.</param>
    public void ReadUntil(byte[] data, ref int offset, bool removeMarker, params byte[] endMark)
    {
        if (data is null) throw new ArgumentNullException(nameof(data));
        var maxCount = data.Length;
        var completed = false;
        var endMarkLast = endMark.Length - 1;
        while (!completed)
        {
            if (offset >= data.Length)
            {
                throw new InvalidDataException($"Refusing to read more than {maxCount} bytes at ReadUntil()!");
            }
            var b = data[offset] = ReadByte();
            if ((offset >= endMarkLast) && (b == endMark[endMarkLast]))
            {
                completed = true;
                var i = endMarkLast - 1;
                var n = offset - 1;
                while (i >= 0)
                {
                    if (data[n--] != endMark[i--])
                    {
                        completed = false;
                        break;
                    }
                }
            }

            offset += 1;
        }

        if (removeMarker)
        {
            offset -= endMark.Length;
        }
    }

    /// <summary>Reads blocks of bytes from the stream until one of the specified end markers are found or buffer length is reached.</summary>
    /// <param name="data">An array of bytes.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
    /// <param name="blockSize">Size of each block to be read.</param>
    /// <param name="removeMarker">If set, removes found mark from endMark array.</param>
    /// <param name="endMark">Array of ending markers.</param>
    public void ReadUntil(byte[] data, ref int offset, int blockSize, bool removeMarker, params byte[] endMark)
    {
        if (data is null) throw new ArgumentNullException(nameof(data));
        var endMarkLength = endMark.Length;
        var maxCount = data.Length % blockSize;
        var currentOffset = offset;

        {
            //first read
            var min = Math.Max(blockSize, endMark.Length);
            if (BaseStream.Read(data, currentOffset, min) != min) throw new EndOfStreamException();
            currentOffset += min;
        }

        bool EndMarkMatches()
        {
            var n = currentOffset - endMarkLength;
            for (var i = 0; i < endMarkLength;)
            {
                if (data[n++] != endMark[i++])
                {
                    return false;
                }
            }
            return true;
        }
        while (!EndMarkMatches())
        {
            if (currentOffset >= data.Length)
            {
                throw new InvalidDataException($"Refusing to read more than {maxCount} blocks at ReadUntil()!");
            }
            if (BaseStream.Read(data, currentOffset, blockSize) != blockSize) throw new EndOfStreamException();
            currentOffset += blockSize;
        }

        if (removeMarker)
        {
            currentOffset -= endMark.Length;
        }
        offset = currentOffset;
    }

    /// <summary>Reads an utf16 text from string.</summary>
    /// <param name="codepoints">Number of unicode codepoints (not bytes) to read (one unicode codepoint may contain two csharp unicode characters).</param>
    /// <returns>Returns the read text.</returns>
    /// <exception cref="InvalidDataException"></exception>
    public string ReadUTF16BE(int codepoints)
    {
        var buffer = new byte[4 * codepoints];
        int count = 0, points = 0, start = 0;
        for (; points < codepoints; points++)
        {
            var b1 = ReadByte();
            var b2 = ReadByte();
            buffer[count++] = b1;
            buffer[count++] = b2;
            if (b1 is > 0xD7 and < 0xDC)
            {
                // add low surrogate
                buffer[count++] = ReadByte();
                buffer[count++] = ReadByte();
            }

            // remove bom at beginning
            if ((count > 1) && (buffer[0] == 0xFE) && (buffer[1] == 0xFF))
            {
                start += 2;
                points--;
                continue;
            }
        }

        var result = Encoding.BigEndianUnicode.GetString(buffer, start, count);
        return result;
    }

    /// <summary>Reads an utf16 text from string.</summary>
    /// <param name="codepoints">Number of unicode codepoints (not bytes) to read (one unicode codepoint may contain two csharp unicode characters).</param>
    /// <returns>Returns the read text.</returns>
    /// <exception cref="InvalidDataException"></exception>
    public string ReadUTF16LE(int codepoints)
    {
        var buffer = new byte[4 * codepoints];
        int count = 0, points = 0, start = 0;
        while (true)
        {
            for (; points < codepoints; points++)
            {
                var b1 = ReadByte();
                var b2 = ReadByte();
                buffer[count++] = b1;
                buffer[count++] = b2;
                if (b2 is > 0xD7 and < 0xDC)
                {
                    // add second surrogate part
                    buffer[count++] = ReadByte();
                    buffer[count++] = ReadByte();
                }
            }

            // remove bom at beginning
            if ((count > 1) && (buffer[0] == 0xFF) && (buffer[1] == 0xFE))
            {
                start += 2;
                points--;
                continue;
            }

            break;
        }
        var result = Encoding.Unicode.GetString(buffer, start, count);
        return result;
    }

    /// <summary>Reads an utf32 text from string.</summary>
    /// <param name="codepoints">Character count to read.</param>
    /// <returns>Returns the read text.</returns>
    /// <exception cref="InvalidDataException"></exception>
    public string ReadUTF32BE(int codepoints) => new UTF32BE(ReadBytes(codepoints * 4)).ToString();

    /// <summary>Reads an utf32 text from string.</summary>
    /// <param name="codepoints">Number of unicode codepoints (not bytes) to read (one unicode codepoint may contain two csharp unicode characters).</param>
    /// <returns>Returns the read text.</returns>
    /// <exception cref="InvalidDataException"></exception>
    public string ReadUTF32LE(int codepoints) => new UTF32LE(ReadBytes(codepoints * 4)).ToString();

    /// <summary>Reads an utf7 text from string.</summary>
    /// <param name="codepoints">Number of unicode codepoints (not bytes) to read (one unicode codepoint may contain two csharp unicode characters).</param>
    /// <returns>Returns the read text.</returns>
    /// <exception cref="InvalidDataException"></exception>
    public string ReadUTF7(int codepoints)
    {
        byte[]? buf = null;
        var sb = new StringBuilder();
        //fast check first -> todo: implement this in a better way
        while (sb.Length < codepoints || sb.ToString().CountCodepoints() < codepoints)
        {
            var b = ReadByte();
            if (b == '+')
            {
                buf ??= new byte[128];

                var n = 0;
                buf[n++] = b;
                while (b != '-')
                {
                    if (n == buf.Length)
                    {
                        Array.Resize(ref buf, buf.Length << 1);
                    }

                    buf[n++] = b = ReadByte();
                }

                Array.Resize(ref buf, n);
                sb.Append(UTF7.Decode(buf));
            }
            else
            {
                sb.Append((char)b);
            }
        }

        return sb.ToString();
    }

    /// <summary>Reads an utf7 text from string.</summary>
    /// <param name="maxBytes">Maximum bytes to read.</param>
    /// <param name="endMarker">End of string marking.</param>
    /// <returns>Returns the read text.</returns>
    /// <exception cref="InvalidDataException"></exception>
    public string ReadUTF7(int maxBytes, string endMarker)
    {
        var result = string.Empty;
        byte[]? buf = null;
        while (!result.EndsWith(endMarker, StringComparison.Ordinal))
        {
            if (--maxBytes < 0)
            {
                throw new InvalidDataException("Exceeded maximum byte count during read.");
            }

            var b = ReadByte();
            if (b == '+')
            {
                buf ??= new byte[128];

                var n = 0;
                buf[n++] = b;
                while (b != '-')
                {
                    if (n == buf.Length)
                    {
                        Array.Resize(ref buf, buf.Length << 1);
                    }

                    if (--maxBytes < 0)
                    {
                        throw new InvalidDataException("Exceeded maximum byte count during read.");
                    }

                    buf[n++] = b = ReadByte();
                }
                Array.Resize(ref buf, n);
                result += UTF7.Decode(buf);
            }
            else
            {
                result += (char)b;
            }
        }

        return result.Substring(0, result.Length - endMarker.Length);
    }

    /// <summary>Reads an utf8 text from string.</summary>
    /// <param name="codepoints">Number of unicode codepoints (not bytes) to read (one unicode codepoint may contain two csharp unicode characters).</param>
    /// <returns>Returns the read text.</returns>
    public string ReadUTF8(int codepoints)
    {
        var sb = new StringBuilder(codepoints * 2);
        for (var i = 0; i < codepoints; i++)
        {
            //read first byte
            var b = ReadByte();

            //single byte ?
            if (b < 0x80)
            {
                sb.Append(char.ConvertFromUtf32(b));
                continue;
            }
            if (b < 0xC2) throw new InvalidDataException("Invalid codepoint at utf-8 encoding!");

            // 2nd byte
            var b2 = ReadByte();
            if ((b2 & 0x11000000) == 0x10000000) throw new InvalidDataException("Invalid multibyte value!");
            if (b < 0xE0)
            {
                var codepoint = (b & 0x1F) << 6 | (b2 & 0x3F);
                sb.Append(char.ConvertFromUtf32(codepoint));
                continue;
            }

            // 3rd byte
            var b3 = ReadByte();
            if ((b3 & 0x11000000) == 0x10000000) throw new InvalidDataException("Invalid multibyte value!");
            if (b < 0xF0)
            {
                var codepoint = ((b & 0xF) << 6 | (b2 & 0x3F)) << 6 | (b3 & 0x3F);
                //test bom
                if (codepoint == 0xEFBBBF && sb.Length == 0)
                {
                    continue;
                }
                sb.Append(char.ConvertFromUtf32(codepoint));
                continue;
            }

            // 4th byte
            var b4 = ReadByte();
            if ((b4 & 0x11000000) == 0x10000000) throw new InvalidDataException("Invalid multibyte value!");
            if (b < 0xF5)
            {
                var codepoint = (((b & 0x7) << 6 | (b2 & 0x3F)) << 6 | (b3 & 0x3F)) << 6 | (b4 & 0x3F);
                sb.Append(char.ConvertFromUtf32(codepoint));
                continue;
            }
            //invalid codepoint
            throw new InvalidDataException("Invalid codepoint at utf-8 encoding!");
        }

        return sb.ToString();
    }

    /// <summary>Reads a zero terminated string from the stream.</summary>
    /// <param name="byteCount">Fieldlength in bytes.</param>
    /// <returns>The string.</returns>
    public string ReadZeroTerminatedFixedLengthString(int byteCount)
    {
        var result = ReadString(byteCount);
        var i = result.IndexOf((char)0);
        if (i > -1)
        {
            result = result.Substring(0, i);
        }

        return result;
    }

    /// <summary>Reads a zero terminated string from the stream.</summary>
    /// <param name="maximumBytes">The number of bytes to write at maximum.</param>
    /// <returns>The string.</returns>
    public string ReadZeroTerminatedString(int maximumBytes)
    {
        const string zeroChars = "\0";
        if (StringEncoding is StringEncoding.UTF_7)
        {
            return ReadUTF7(maximumBytes, zeroChars);
        }

        if (zeroBytes == null)
        {
            zeroBytes = StringEncoding.Encode(zeroChars, withRoundtripTest: true);
        }

        return ReadUntil(zeroBytes, zeroChars, maximumBytes);
    }

    /// <summary>Seeks at the base stream (this requires the stream to be seekable).</summary>
    /// <param name="offset">Offset to seek to.</param>
    /// <param name="origin">Origin to seek from.</param>
    /// <returns>A value of type SeekOrigin indicating the reference point used to obtain the new position.</returns>
    public long Seek(long offset, SeekOrigin origin) => BaseStream.Seek(offset, origin);

    /// <summary>Skips some bytes at the base stream.</summary>
    /// <param name="count">Length to skip in bytes.</param>
    public void Skip(long count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (count == 0)
        {
            return;
        }

        Seek(count, SeekOrigin.Current);
    }

    #endregion Public Methods
}
