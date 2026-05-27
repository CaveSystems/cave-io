using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Cave.IO;

/// <summary>Provides 7bit encoding of 64bit values (uint, uint).</summary>
public static class BitCoder32
{
    #region Public Methods

    /// <summary>Gets the data of a 7 bit encoded value.</summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded value as byte array.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static byte[] Get7BitEncoded(uint value)
    {
        var buffer = new byte[5];
        var index = 0;
        while (value >= 0x80)
        {
            buffer[index++] = (byte)(value | 0x80);
            value >>= 7;
        }
        buffer[index++] = (byte)value;
        if (index != buffer.Length)
        {
            buffer = buffer[0..index];
        }
        return buffer;
    }

    /// <summary>Gets the data of a 7 bit encoded value.</summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded value as byte array.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static byte[] Get7BitEncoded(int value) => Get7BitEncoded(unchecked((uint)value));

    /// <summary>Gets the data of a 8 bit shifted value (using little endian encoding).</summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded value as byte array.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static byte[] Get8BitShifted(uint value)
    {
        unchecked
        {
            var result = new byte[5];
            byte i = 1;
            while (value > 0)
            {
                result[i++] = (byte)value;
                value >>= 8;
            }
            result[0] = i;
            return result[0..i];
        }
    }

    /// <summary>Gets the data of a 8 bit shifted value (using little endian encoding).</summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded value as byte array.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static byte[] Get8BitShifted(int value) => Get8BitShifted(unchecked((uint)value));

    /// <summary>Gets the number of bytes needed for the specified value.</summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>number of bytes needed.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static int GetByteCount7BitEncoded(uint value)
    {
        unchecked
        {
            var count = 0;
            do
            {
                count++;
                value >>= 7;
            }
            while (value != 0);

            return count;
        }
    }

    /// <summary>Gets the number of bytes needed for the specified value.</summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>number of bytes needed.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static int GetByteCount7BitEncoded(int value) => GetByteCount7BitEncoded(unchecked((uint)value));

    /// <summary>Gets the number of bytes needed for the specified value.</summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>number of bytes needed.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static int GetByteCount8BitShifted(uint value)
    {
        unchecked
        {
            var count = 0;
            do
            {
                count++;
                value >>= 8;
            }
            while (value != 0);

            return count;
        }
    }

    /// <summary>Gets the number of bytes needed for the specified value.</summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>number of bytes needed.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static int GetByteCount8BitShifted(int value) => GetByteCount8BitShifted(unchecked((uint)value));

    /// <summary>Reads a 7-bit encoded unsigned 32-bit integer from the stream.</summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The decoded unsigned 32-bit integer.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the integer is completely read.</exception>
    /// <exception cref="InvalidDataException">The encoded data exceeds 5 bytes.</exception>
    [MethodImpl((MethodImplOptions)256)]
    public static int Read7BitEncodedInt32(Stream stream) => unchecked((int)Read7BitEncodedUInt32(stream));

    /// <summary>Reads a 7-bit encoded 32-bit signed integer from the byte array.</summary>
    /// <param name="data">The byte array containing the encoded data.</param>
    /// <param name="offset">The current position in the array. Updated to the position after the read value.</param>
    /// <returns>The decoded 32-bit signed integer.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
    /// <exception cref="EndOfStreamException">Attempted to read beyond the end of the array.</exception>
    /// <exception cref="InvalidDataException">Encoded value exceeds 5 bytes.</exception>
    [MethodImpl((MethodImplOptions)256)]
    public static int Read7BitEncodedInt32(byte[] data, ref int offset) => unchecked((int)Read7BitEncodedUInt32(data, ref offset));

    /// <summary>Reads a 7-bit encoded unsigned 32-bit integer from the stream.</summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The decoded unsigned 32-bit integer.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the integer is completely read.</exception>
    /// <exception cref="InvalidDataException">The encoded data exceeds 5 bytes.</exception>
    [MethodImpl((MethodImplOptions)256)]
    public static uint Read7BitEncodedUInt32(Stream stream)
    {
        unchecked
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            var b = stream.ReadByte();
            if (b == -1) throw new EndOfStreamException();
            var result = (uint)(b & 0x7F);
            var bitPos = 7;
            var count = 1;
            while ((b & 0x80) != 0)
            {
                b = stream.ReadByte();
                if (b == -1) throw new EndOfStreamException();
                if (++count > 5) throw new InvalidDataException("7Bit encoded 32 bit integer may not exceed 5 bytes!");
                result |= (uint)(b & 0x7F) << bitPos;
                bitPos += 7;
            }
            return result;
        }
    }

    /// <summary>Reads a 7-bit encoded unsigned 32-bit integer from a byte array.</summary>
    /// <param name="data">Byte array containing the encoded data.</param>
    /// <param name="offset">Position in the array to start reading; updated to the next position after reading.</param>
    /// <returns>Decoded unsigned 32-bit integer value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
    /// <exception cref="EndOfStreamException">Attempted to read beyond the end of the array.</exception>
    /// <exception cref="InvalidDataException">Encoded value exceeds 5 bytes.</exception>
    [MethodImpl((MethodImplOptions)256)]
    public static uint Read7BitEncodedUInt32(byte[] data, ref int offset)
    {
        unchecked
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (offset >= data.Length) throw new EndOfStreamException();
            int b = data[offset++];
            var result = (uint)(b & 0x7F);
            var bitPos = 7;
            var count = 1;
            while ((b & 0x80) != 0)
            {
                if (offset >= data.Length) throw new EndOfStreamException();
                b = data[offset++];
                if (++count > 5) throw new InvalidDataException("7Bit encoded 32 bit integer may not exceed 5 bytes!");
                result |= (uint)(b & 0x7F) << bitPos;
                bitPos += 7;
            }
            return result;
        }
    }

    /// <summary>Decodes a value previously encoded with Get8BitShifted (0 = null, 1 = 0, N = N-1 LE bytes).</summary>
    public static int? Read8BitPrefixedInt32(byte[] data) => unchecked((int?)Read8BitPrefixedUInt32(data));

    /// <summary>Reads a 8 bit prefixed and shifted value from the specified Stream.</summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <returns>Returns the read value.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static int? Read8BitPrefixedInt32(Stream stream) => unchecked((int?)Read8BitPrefixedUInt32(stream));

    /// <summary>Decodes a value previously encoded with Get8BitShifted (0 = null, 1 = 0, N = N-1 LE bytes). Supports up to 64-bit values.</summary>
    public static long? Read8BitPrefixedInt64(byte[] data) => unchecked((long?)Read8BitPrefixedUInt64(data));

    /// <summary>Decodes a value previously encoded with Get8BitShifted (0 = null, 1 = 0, N = N-1 LE bytes).</summary>
    [MethodImpl((MethodImplOptions)256)]
    public static uint? Read8BitPrefixedUInt32(byte[] data)
    {
        unchecked
        {
            if (data == null || data.Length == 0) throw new ArgumentException("Data must not be empty.", nameof(data));
            var count = data[0];
            if (count == 0) return null;
            if (--count == 0) return 0;
            if (count > 4) throw new InvalidDataException("8Bit prefixed 32 bit integer may not exceed 5 bytes!");
            if (data.Length < count + 1) throw new EndOfStreamException();
            uint value = 0;
            switch (count)
            {
                case 4: value |= (uint)data[4] << 24; goto case 3;
                case 3: value |= (uint)data[3] << 16; goto case 2;
                case 2: value |= (uint)data[2] << 8; goto case 1;
                case 1: value |= data[1]; break;
            }
            return value;
        }
    }

    /// <summary>Reads a 8 bit prefixed and shifted value from the specified Stream.</summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <returns>Returns the read value.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static uint? Read8BitPrefixedUInt32(Stream stream)
    {
        unchecked
        {
            var count = stream.ReadByte();
            if (count == 0) return null;
            if (--count == 0) return 0;
            if (count > 4) throw new InvalidDataException("8Bit prefixed 32 bit integer may not exceed 5 bytes!");

            var buffer = new byte[count];
            var read = stream.Read(buffer, 0, count);
            if (read != count) throw new EndOfStreamException();

            uint value = 0;
            for (var i = 0; i < count; i++)
            {
                value |= (uint)buffer[i] << (i * 8);
            }
            return value;
        }
    }

    /// <summary>Decodes a value previously encoded with Get8BitShifted (0 = null, 1 = 0, N = N-1 LE bytes). Supports up to 64-bit values.</summary>
    [MethodImpl((MethodImplOptions)256)]
    public static ulong? Read8BitPrefixedUInt64(byte[] data)
    {
        unchecked
        {
            if (data == null || data.Length == 0) throw new ArgumentException("Data must not be empty.", nameof(data));
            var count = data[0];
            if (count == 0) return null;
            if (--count == 0) return 0UL;
            if (count > 8) throw new InvalidDataException("8Bit prefixed 64 bit integer may not exceed 9 bytes!");
            if (data.Length < count + 1) throw new EndOfStreamException();
            ulong value = 0;
            switch (count)
            {
                case 8: value |= (ulong)data[8] << 56; goto case 7;
                case 7: value |= (ulong)data[7] << 48; goto case 6;
                case 6: value |= (ulong)data[6] << 40; goto case 5;
                case 5: value |= (ulong)data[5] << 32; goto case 4;
                case 4: value |= (ulong)data[4] << 24; goto case 3;
                case 3: value |= (ulong)data[3] << 16; goto case 2;
                case 2: value |= (ulong)data[2] << 8; goto case 1;
                case 1: value |= data[1]; break;
            }
            return value;
        }
    }

    /// <summary>Writes the specified value 7 bit encoded to the specified Stream.</summary>
    /// <param name="stream">The <see cref="Stream"/> to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>Returns the number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static int Write7BitEncoded(Stream stream, uint value)
    {
        unchecked
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            var buffer = new byte[5];
            var index = 0;
            while (value >= 0x80)
            {
                buffer[index++] = (byte)(value | 0x80);
                value >>= 7;
            }
            buffer[index++] = (byte)value;
            stream.Write(buffer, 0, index);
            return index;
        }
    }

    /// <summary>Writes the specified value 7 bit encoded to the specified Stream.</summary>
    /// <param name="stream">The <see cref="Stream"/> to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>Returns the number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static int Write7BitEncoded(Stream stream, int value) => Write7BitEncoded(stream, unchecked((uint)value));

    /// <summary>Writes the specified value 7 bit encoded to the specified Stream.</summary>
    /// <param name="writer">The <see cref="DataWriter"/> to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>Returns the number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static int Write7BitEncoded(DataWriter writer, uint value)
    {
        if (writer == null)
        {
            throw new ArgumentNullException(nameof(writer));
        }
        return Write7BitEncoded(writer.Stream, value);
    }

    /// <summary>Writes the specified value 7 bit encoded to the specified Stream.</summary>
    /// <param name="writer">The <see cref="DataWriter"/> to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>Returns the number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static int Write7BitEncoded(DataWriter writer, int value) => Write7BitEncoded(writer, unchecked((uint)value));

    /// <summary>Writes the specified value 8 bit prefixed to the specified Stream.</summary>
    /// <param name="writer">The <see cref="DataWriter"/> to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>Returns the number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static int Write8BitPrefixed(DataWriter writer, uint value)
    {
        if (writer == null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        unchecked
        {
            var block = Get8BitShifted(value);
            writer.Write(block);
            return block.Length;
        }
    }

    /// <summary>Writes the specified value 7 bit encoded to the specified Stream.</summary>
    /// <param name="writer">The <see cref="DataWriter"/> to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>Returns the number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static int Write8BitPrefixed(DataWriter writer, int value) => Write8BitPrefixed(writer, unchecked((uint)value));

    #endregion Public Methods
}
