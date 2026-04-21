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

    /// <summary>Reads a 7 bit encoded value from the specified Stream.</summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <returns>Returns the read value.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static int Read7BitEncodedInt32(Stream stream) => unchecked((int)Read7BitEncodedUInt32(stream));

    /// <summary>Reads a 7 bit encoded value from the specified Stream.</summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <returns>Returns the read value.</returns>
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

    /// <summary>Reads a 8 bit prefixed and shifted value from the specified Stream.</summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <returns>Returns the read value.</returns>
    [MethodImpl((MethodImplOptions)256)]
    public static int? Read8BitPrefixedInt32(Stream stream) => unchecked((int?)Read8BitPrefixedUInt32(stream));

    /// <summary>Reads a 8 bit prefixed and shifted value from the specified Stream.</summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <returns>Returns the read value.</returns>
    public static uint? Read8BitPrefixedUInt32(Stream stream)
    {
        unchecked
        {
            var count = stream.ReadByte();
            if (count == 0) return null;
            if (--count == 0) return 0;
            if (count > 4) throw new InvalidDataException("8Bit prefixed 64 bit integer may not exceed 8 bytes!");

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

    /// <summary>Writes the specified value 7 bit encoded to the specified Stream.</summary>
    /// <param name="stream">The <see cref="Stream"/> to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>Returns the number of bytes written.</returns>
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
        return Write7BitEncoded(writer.BaseStream, value);
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
