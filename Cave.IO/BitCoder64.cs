using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Cave.IO;

/// <summary>Provides 7bit encoding of 64bit values (ulong, ulong).</summary>
public static class BitCoder64
{
    #region Public Methods

    /// <summary>Gets the data of a 7 bit encoded value.</summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded value as byte array.</returns>
    public static byte[] Get7BitEncoded(ulong value)
    {
        var buffer = new byte[10];
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
    public static byte[] Get7BitEncoded(long value) => Get7BitEncoded(unchecked((ulong)value));

    /// <summary>Gets the data of a 8 bit shifted value (using little endian encoding).</summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded value as byte array.</returns>
    public static byte[] Get8BitShifted(ulong value)
    {
        unchecked
        {
            var result = new byte[9];
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
    public static byte[] Get8BitShifted(long value) => Get8BitShifted(unchecked((ulong)value));

    /// <summary>Gets the number of bytes needed for the specified value.</summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>number of bytes needed.</returns>
    public static int GetByteCount7BitEncoded(ulong value)
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
    public static int GetByteCount7BitEncoded(long value) => GetByteCount7BitEncoded(unchecked((ulong)value));

    /// <summary>Gets the number of bytes needed for the specified value.</summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>number of bytes needed.</returns>
    public static int GetByteCount8BitEncoded(ulong value)
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
    public static int GetByteCount8BitEncoded(long value) => GetByteCount8BitEncoded(unchecked((ulong)value));

    /// <summary>Gets the number of bytes needed for the specified value.</summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>number of bytes needed.</returns>
    public static int GetByteCount8BitShifted(ulong value)
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
    public static int GetByteCount8BitShifted(long value) => GetByteCount8BitShifted(unchecked((ulong)value));

    /// <summary>Reads a 7 bit encoded value from the specified Stream.</summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <returns>Returns the read value.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static long Read7BitEncodedInt64(Stream stream) => unchecked((long)Read7BitEncodedUInt64(stream));

    /// <summary>Reads a 7 bit encoded value from the specified Stream.</summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <returns>Returns the read value.</returns>
    public static ulong Read7BitEncodedUInt64(Stream stream)
    {
        unchecked
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            var b = stream.ReadByte();
            if (b == -1) throw new EndOfStreamException();
            var result = (ulong)(b & 0x7F);
            var bitPos = 7;
            var count = 1;
            while ((b & 0x80) != 0)
            {
                b = stream.ReadByte();
                if (b == -1) throw new EndOfStreamException();
                if (++count > 10) throw new InvalidDataException("7Bit encoded 64 bit integer may not exceed 10 bytes!");
                result |= (ulong)(b & 0x7F) << bitPos;
                bitPos += 7;
            }
            return result;
        }
    }

    /// <summary>Reads a 8 bit prefixed and shifted value from the specified Stream.</summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <returns>Returns the read value.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static long? Read8BitPrefixedInt64(Stream stream) => unchecked((long?)Read8BitPrefixedUInt64(stream));

    /// <summary>Reads a 8 bit prefixed and shifted value from the specified Stream.</summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <returns>Returns the read value.</returns>
    public static ulong? Read8BitPrefixedUInt64(Stream stream)
    {
        var count = stream.ReadByte();
        if (count == 0) return null;
        if (--count == 0) return 0;
        if (count > 8) throw new InvalidDataException("8Bit prefixed 64 bit integer may not exceed 8 bytes!");

        var buffer = new byte[count];
        var read = stream.Read(buffer, 0, count);
        if (read != count) throw new EndOfStreamException();

        ulong value = 0;
        for (var i = 0; i < count; i++)
        {
            value |= (ulong)buffer[i] << (i * 8);
        }
        return value;
    }

    /// <summary>Writes the specified value 7 bit encoded to the specified Stream.</summary>
    /// <param name="stream">The <see cref="Stream"/> to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>Returns the number of bytes written.</returns>
    public static int Write7BitEncoded(Stream stream, ulong value)
    {
        unchecked
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            var buffer = new byte[10];
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
    public static int Write7BitEncoded(Stream stream, long value) => Write7BitEncoded(stream, unchecked((ulong)value));

    /// <summary>Writes the specified value 7 bit encoded to the specified Stream.</summary>
    /// <param name="writer">The <see cref="DataWriter"/> to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>Returns the number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static int Write7BitEncoded(DataWriter writer, ulong value)
    {
        if (writer == null)
        {
            throw new ArgumentNullException(nameof(writer));
        }
        return Write7BitEncoded(writer.BaseStream, value);
    }

    /// <summary>Writes the specified value 7 bit encoded to the specified Stream.</summary>
    /// <param name="writer">The <see cref="DataWriter"/> to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>Returns the number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static int Write7BitEncoded(DataWriter writer, long value) => Write7BitEncoded(writer, unchecked((ulong)value));

    /// <summary>Writes the specified value 8 bit prefixed to the specified Stream.</summary>
    /// <param name="writer">The <see cref="DataWriter"/> to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>Returns the number of bytes written.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static int Write8BitPrefixed(DataWriter writer, ulong value)
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
    public static int Write8BitPrefixed(DataWriter writer, long value) => Write8BitPrefixed(writer, unchecked((ulong)value));

    #endregion Public Methods
}
