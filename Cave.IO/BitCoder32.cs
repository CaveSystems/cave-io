using System;
using System.Collections.Generic;
using System.IO;

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
        using var stream = new MemoryStream();
        Write7BitEncoded(stream, value);
        return stream.ToArray();
    }

    /// <summary>Gets the data of a 7 bit encoded value.</summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded value as byte array.</returns>
    public static byte[] Get7BitEncoded(int value)
    {
        unchecked
        {
            return Get7BitEncoded((uint)value);
        }
    }

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
    public static int GetByteCount7BitEncoded(int value)
    {
        unchecked
        {
            return GetByteCount7BitEncoded((uint)value);
        }
    }

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
    public static int GetByteCount8BitShifted(int value)
    {
        unchecked
        {
            return GetByteCount8BitShifted((uint)value);
        }
    }

    /// <summary>Reads a 7 bit encoded value from the specified Stream.</summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <returns>Returns the read value.</returns>
    public static int Read7BitEncodedInt32(Stream stream)
    {
        unchecked
        {
            return (int)Read7BitEncodedUInt32(stream);
        }
    }

    /// <summary>Reads a 7 bit encoded value from the specified Stream.</summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <returns>Returns the read value.</returns>
    public static uint Read7BitEncodedUInt32(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        unchecked
        {
            var b = stream.ReadByte();
            var count = 1;
            if (b == -1)
            {
                throw new EndOfStreamException();
            }

            var result = (uint)(b & 0x7F);
            var bitPos = 7;
            while (b > 0x7F)
            {
                b = stream.ReadByte();
                if (++count > 5)
                {
                    throw new InvalidDataException("7Bit encoded 32 bit integer may not exceed 5 bytes!");
                }

                if (b == -1)
                {
                    throw new EndOfStreamException();
                }

                var value = (uint)(b & 0x7F);
                result = (value << bitPos) | result;
                bitPos += 7;
            }

            return result;
        }
    }

    /// <summary>Reads a 8 bit prefixed and shifted value from the specified Stream.</summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <returns>Returns the read value.</returns>
    public static int? Read8BitPrefixedInt32(Stream stream)
    {
        unchecked
        {
            return (int?)Read8BitPrefixedUInt32(stream);
        }
    }

    /// <summary>Reads a 8 bit prefixed and shifted value from the specified Stream.</summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <returns>Returns the read value.</returns>
    public static uint? Read8BitPrefixedUInt32(Stream stream)
    {
        unchecked
        {
            var count = stream.ReadByte();
            if (count == 0) return null;
            var value = (uint)0;
            while (--count >= 0)
            {
                var b = stream.ReadByte();
                if (b < 0) throw new EndOfStreamException();
                value = (value << 8) | (uint)b;
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
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        unchecked
        {
            var i = 1;
            var b = (byte)(value & 0x7F);
            var data = value >> 7;
            while (data != 0)
            {
                stream.WriteByte((byte)(0x80 | b));
                i++;
                b = (byte)(data & 0x7F);
                data >>= 7;
            }

            stream.WriteByte(b);
            return i;
        }
    }

    /// <summary>Writes the specified value 7 bit encoded to the specified Stream.</summary>
    /// <param name="stream">The <see cref="Stream"/> to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>Returns the number of bytes written.</returns>
    public static int Write7BitEncoded(Stream stream, int value)
    {
        unchecked
        {
            return Write7BitEncoded(stream, (uint)value);
        }
    }

    /// <summary>Writes the specified value 7 bit encoded to the specified Stream.</summary>
    /// <param name="writer">The <see cref="DataWriter"/> to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>Returns the number of bytes written.</returns>
    public static int Write7BitEncoded(DataWriter writer, uint value)
    {
        if (writer == null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        unchecked
        {
            var i = 1;
            var b = (byte)(value & 0x7F);
            var data = value >> 7;
            while (data != 0)
            {
                writer.Write((byte)(0x80 | b));
                i++;
                b = (byte)(data & 0x7F);
                data >>= 7;
            }

            writer.Write(b);
            return i;
        }
    }

    /// <summary>Writes the specified value 7 bit encoded to the specified Stream.</summary>
    /// <param name="writer">The <see cref="DataWriter"/> to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>Returns the number of bytes written.</returns>
    public static int Write7BitEncoded(DataWriter writer, int value)
    {
        unchecked
        {
            return Write7BitEncoded(writer, (uint)value);
        }
    }

    /// <summary>Writes the specified value 8 bit prefixed to the specified Stream.</summary>
    /// <param name="writer">The <see cref="DataWriter"/> to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>Returns the number of bytes written.</returns>
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
    public static int Write8BitPrefixed(DataWriter writer, int value)
    {
        unchecked
        {
            return Write8BitPrefixed(writer, (uint)value);
        }
    }

    #endregion Public Methods
}
