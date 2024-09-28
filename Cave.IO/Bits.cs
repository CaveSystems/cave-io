using System;
using System.Collections;
using System.Collections.Generic;

namespace Cave.IO;

/// <summary>Provides binary conversion routines.</summary>
/// <remarks>Initializes a new instance of the <see cref="Bits"/> class. Bits are counted from LSB to MSB.</remarks>
/// <param name="data">Binary data to initialize.</param>
/// <param name="bitCount">Bit count used at data.</param>
public class Bits(byte[] data, int bitCount) : IEnumerable<bool>
{
    #region Private Methods

    private IEnumerator<bool> GetBoolEnumerator()
    {
        var count = BitCount;
        for (var i = data.Length - 1; i >= 0; i--)
        {
            var b = data[i];
            for (var bit = 0x1; bit < 0x100; bit <<= 1)
            {
                if (--count < 0) break;
                yield return (b & bit) != 0;
            }
        }
    }

    #endregion Private Methods

    #region Public Properties

    /// <summary>Gets the number of bits</summary>
    public int BitCount => bitCount;

    /// <summary>Gets a copy of all data.</summary>
    public IList<byte> Data => data;

    #endregion Public Properties

    #region Public Indexers

    /// <summary>Gets or sets the bit at the specified index</summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool this[int index]
    {
        get => (data[^(index / 8)] & (1 << (index % 8))) != 0;
        set
        {
            var i = index / 8;
            var bit = (byte)(1 << (index % 8));
            if (value)
            {
                data[^i] |= bit;
            }
            else
            {
                data[^i] &= (byte)~bit;
            }
        }
    }

    #endregion Public Indexers

    #region Public Methods

    /// <summary>Implicitly converts an array to <see cref="Bits"/> data.</summary>
    /// <param name="data">The binary data.</param>
    public static implicit operator Bits(byte[] data) => data == null ? new Bits([], 0) : new Bits(data, data.Length * 8);

    /// <summary>Implicitly converts <see cref="Bits"/> data to an array.</summary>
    /// <param name="value">The binary data.</param>
    public static implicit operator byte[](Bits value) => [.. value.Data];

    /// <summary>Reflects 32 bits.</summary>
    /// <param name="x">The bits.</param>
    /// <returns>Returns a center reflection.</returns>
    public static uint Reflect32(uint x)
    {
        // move bits
        x = ((x & 0x55555555) << 1) | ((x >> 1) & 0x55555555);
        x = ((x & 0x33333333) << 2) | ((x >> 2) & 0x33333333);
        x = ((x & 0x0F0F0F0F) << 4) | ((x >> 4) & 0x0F0F0F0F);

        // move bytes
        x = (x << 24) | ((x & 0xFF00) << 8) | ((x >> 8) & 0xFF00) | (x >> 24);
        return x;
    }

    /// <summary>Reflects 64 bits.</summary>
    /// <param name="x">The bits.</param>
    /// <returns>Returns a center reflection.</returns>
    public static ulong Reflect64(ulong x)
    {
        // move bits
        x = ((x & 0x5555555555555555) << 1) | ((x >> 1) & 0x5555555555555555);
        x = ((x & 0x3333333333333333) << 2) | ((x >> 2) & 0x3333333333333333);
        x = ((x & 0x0F0F0F0F0F0F0F0F) << 4) | ((x >> 4) & 0x0F0F0F0F0F0F0F0F);

        // move bytes
        x = (x << 56) | ((x & 0xFF00) << 40) | ((x & 0xFF0000) << 24) | ((x & 0xFF000000) << 8) | ((x >> 8) & 0xFF000000) | ((x >> 24) & 0xFF0000) |
            ((x >> 40) & 0xFF00) | (x >> 56);
        return x;
    }

    /// <summary>Reflects 8 bits.</summary>
    /// <param name="b">The bits.</param>
    /// <returns>Returns a center reflection.</returns>
    public static byte Reflect8(byte b)
    {
        uint r = b;
        r = ((r & 0x55) << 1) | ((r >> 1) & 0x55);
        r = ((r & 0x33) << 2) | ((r >> 2) & 0x33);
        r = ((r & 0x0F) << 4) | ((r >> 4) & 0x0F);
        return (byte)r;
    }

    /// <summary>Converts a value int (309 = 0x135) to a binary long (100110101).</summary>
    /// <param name="value">The binary value as int.</param>
    /// <returns>The value as binary long.</returns>
    public static long ToBinary(int value)
    {
        long result = 0;
        var counter = 0;
        while (value != 0)
        {
            long bit = (value & 1) << counter++;
            result |= bit;
            value >>= 1;
        }

        return result;
    }

    /// <summary>Converts a binary value (100110101) to a "normal" value (0x135 = 309).</summary>
    /// <param name="binary">The binary value.</param>
    /// <returns>The value as Byte.</returns>
    public static byte ToByte(long binary) => (byte)ToInt32(binary);

    /// <summary>Converts a binary value (100110101) to a "normal" value (0x135 = 309).</summary>
    /// <param name="binary">The binary value.</param>
    /// <returns>The value as Byte.</returns>
    public static byte ToByte(string binary) => (byte)ToInt64(binary);

    /// <summary>Converts a binary value (100110101) to a "normal" value (0x135 = 309).</summary>
    /// <param name="binary">The binary value.</param>
    /// <returns>The value as Int16.</returns>
    public static short ToInt16(long binary) => (short)ToInt32(binary);

    /// <summary>Converts a binary value (100110101) to a "normal" value (0x135 = 309).</summary>
    /// <param name="binary">The binary value.</param>
    /// <returns>The value as Int16.</returns>
    public static short ToInt16(string binary) => (short)ToInt64(binary);

    /// <summary>Converts a binary value (100110101) to a "normal" int (0x135 = 309).</summary>
    /// <param name="binary">The binary value.</param>
    /// <returns>The value as Int32.</returns>
    public static int ToInt32(long binary)
    {
        var result = 0;
        var counter = 0;
        while (binary > 0)
        {
            var current = (int)(binary % 10);
            binary /= 10;
            if (current > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(binary));
            }

            result |= current << counter++;
        }

        return result;
    }

    /// <summary>Converts a binary value (100110101) to a "normal" value (0x135 = 309).</summary>
    /// <param name="binary">The binary value.</param>
    /// <returns>The value as UInt32.</returns>
    public static int ToInt32(string binary) => (int)ToInt64(binary);

    /// <summary>Converts a binary string ("100110101") to a "normal" int (0x135 = 309).</summary>
    /// <param name="binary">The binary value.</param>
    /// <returns>The value as Int64.</returns>
    public static long ToInt64(string binary)
    {
        if (binary == null)
        {
            throw new ArgumentNullException(nameof(binary));
        }

        if (binary.Length > 63)
        {
            throw new ArgumentOutOfRangeException(nameof(binary));
        }

        long result = 0;
        foreach (var c in binary)
        {
            switch (c)
            {
                case '0':
                    result <<= 1;
                    break;

                case '1':
                    result = (result << 1) | 1;
                    break;

                default: throw new ArgumentOutOfRangeException(nameof(binary));
            }
        }

        return result;
    }

    /// <summary>Converts a binary value (100110101) to a "normal" value (0x135 = 309).</summary>
    /// <param name="binary">The binary value.</param>
    /// <returns>The value as Sbyte.</returns>
    public static sbyte ToSByte(long binary) => (sbyte)ToInt32(binary);

    /// <summary>Converts a binary value (100110101) to a "normal" value (0x135 = 309).</summary>
    /// <param name="binary">The binary value.</param>
    /// <returns>The value as SByte.</returns>
    public static sbyte ToSByte(string binary) => (sbyte)ToInt64(binary);

    /// <summary>Converts a value int (309 = 0x135) to a binary string ("100110101").</summary>
    /// <param name="value">The binary value.</param>
    /// <returns>The value as binary string.</returns>
    public static string ToString(int value)
    {
        var result = new List<char>();
        while (value != 0)
        {
            if ((value & 1) == 0)
            {
                result.Add('0');
            }
            else
            {
                result.Add('1');
            }

            value >>= 1;
        }

        result.Reverse();
        return new string([.. result]);
    }

    /// <summary>Converts a binary value (100110101) to a "normal" value (0x135 = 309).</summary>
    /// <param name="binary">The binary value.</param>
    /// <returns>The value as UInt16.</returns>
    public static ushort ToUInt16(long binary) => (ushort)ToInt32(binary);

    /// <summary>Converts a binary value (100110101) to a "normal" value (0x135 = 309).</summary>
    /// <param name="binary">The binary value.</param>
    /// <returns>The value as UInt16.</returns>
    public static ushort ToUInt16(string binary) => (ushort)ToInt64(binary);

    /// <summary>Converts a binary value (100110101) to a "normal" value (0x135 = 309).</summary>
    /// <param name="binary">The binary value.</param>
    /// <returns>The value as UInt32.</returns>
    public static uint ToUInt32(long binary) => (uint)ToInt32(binary);

    /// <summary>Converts a binary value (100110101) to a "normal" value (0x135 = 309).</summary>
    /// <param name="binary">The binary value.</param>
    /// <returns>The value as UInt32.</returns>
    public static ushort ToUInt32(string binary) => (ushort)ToInt64(binary);

    /// <inheritdoc/>
    public IEnumerator<bool> GetEnumerator() => GetBoolEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetBoolEnumerator();

    #endregion Public Methods
}
