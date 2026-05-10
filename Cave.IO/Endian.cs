using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Cave.IO;

/// <summary>Provides Endian Tools.</summary>
public static class Endian
{
    #region Fields

    static EndianType? machineType;

    #endregion Fields

    #region Public Properties

    static EndianType CheckMachineType()
    {
        var bytes = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };
        const ulong BigEndianValue = 0x123456789ABCDEF0;
        const ulong LittleEndianValue = 0xF0DEBC9A78563412;
        ulong value;
        unsafe
        {
            fixed (byte* ptr = &bytes[0])
            {
                value = *(ulong*)ptr;
            }
        }

        if (value == LittleEndianValue)
        {
            return EndianType.LittleEndian;
        }

        if (value == BigEndianValue)
        {
            return EndianType.BigEndian;
        }

        return EndianType.None;
    }

    /// <summary>Gets the machine endian type.</summary>
    [ExcludeFromCodeCoverage]
    public static EndianType MachineType
    {
        get
        {
            return machineType ??= CheckMachineType();
        }
    }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Swaps the endian type of the specified data.</summary>
    /// <param name="data">The data.</param>
    /// <param name="bytes">The bytes to swap (2..x).</param>
    /// <returns>The swapped data.</returns>
    [Obsolete("Use newer Swap methods for specific data types. (Performance)")]
    public static byte[] Swap(byte[] data, int bytes)
    {
        var result = new byte[data.Length];
        bytes--;
        for (var i = 0; i < data.Length;)
        {
            var e = i + bytes;
            for (var n = 0; n <= bytes; n++, i++, e--)
            {
                result[e] = data[i];
            }
        }

        return result;
    }

    /// <summary>Swaps the byte order of a value.</summary>
    /// <param name="value">Value to swap the byte order of.</param>
    /// <returns>Byte order-swapped value.</returns>
    [MethodImpl(256)]
    public static ushort Swap(ushort value) => (ushort)((value >> 8) | ((value & 0xFF) << 8));

    /// <summary>Swaps the byte order of a value.</summary>
    /// <param name="value">Value to swap the byte order of.</param>
    /// <returns>Byte order-swapped value.</returns>
    [MethodImpl(256)]
    public static uint Swap(uint value) => (value >> 24) | ((value & 0xFF00) << 8) | ((value >> 8) & 0xFF00) | (value << 24);

    /// <summary>Swaps the byte order of a value.</summary>
    /// <param name="value">Value to swap the byte order of.</param>
    /// <returns>Byte order-swapped value.</returns>
    [MethodImpl(256)]
    public static ulong Swap(ulong value) =>
        (value >> 56) | (0xFF00 & (value >> 40)) | (0xFF0000 & (value >> 24)) | (0xFF000000 & (value >> 8)) |
        ((value & 0xFF000000) << 8) | ((value & 0xFF0000) << 24) | ((value & 0xFF00) << 40) | (value << 56);

    /// <summary>Swaps the byte order of 16-bit values within a block of memory.</summary>
    /// <param name="src">Pointer to the source memory block.</param>
    /// <param name="dst">Pointer to the destination memory block.</param>
    /// <param name="count">The number of 16-bit values to swap.</param>
    [MethodImpl(256)]
    public static unsafe void Swap16(void* src, void* dst, int count)
    {
        var pairs = count >> 2;
        var rest = count & 3;
        var s64 = (ulong*)src;
        var d64 = (ulong*)dst;
        for (var i = 0; i < pairs; i++)
        {
            d64[i] = Swap16Pair(s64[i]);
        }
        if (rest != 0)
        {
            var s16 = (ushort*)src + (pairs * 4);
            var d16 = (ushort*)dst + (pairs * 4);
            for (var i = 0; i < rest; i++)
            {
                var v = s16[i];
                d16[i] = (ushort)((v << 8) | (v >> 8));
            }
        }
    }

    /// <summary>Swaps the byte order of 16-bit pairs within a 64-bit value.</summary>
    /// <param name="value">Value to swap the byte order of.</param>
    /// <returns>Byte order-swapped value.</returns>
    [MethodImpl(256)]
    public static ulong Swap16Pair(ulong value) => ((value & 0x00FF00FF00FF00FFUL) << 8) | ((value & 0xFF00FF00FF00FF00UL) >> 8);

    /// <summary>Swaps the byte order of 32-bit values within a block of memory.</summary>
    /// <param name="src">Pointer to the source memory block.</param>
    /// <param name="dst">Pointer to the destination memory block.</param>
    /// <param name="count">The number of 32-bit values to swap.</param>
    [MethodImpl(256)]
    public static unsafe void Swap32(void* src, void* dst, int count)
    {
        var pairs = count >> 1;
        var rest = count & 1;
        var s64 = (ulong*)src;
        var d64 = (ulong*)dst;
        for (var i = 0; i < pairs; i++)
        {
            d64[i] = Swap32Pair(s64[i]);
        }
        if (rest != 0)
        {
            var s32 = (uint*)src + (pairs * 2);
            var d32 = (uint*)dst + (pairs * 2);
            var v = *s32;
            v = ((v & 0x000000FFU) << 24) | ((v & 0x0000FF00U) << 8) | ((v & 0x00FF0000U) >> 8) | ((v & 0xFF000000U) >> 24);
            *d32 = v;
        }
    }

    /// <summary>Swaps the byte order of 32-bit pairs within a 64-bit value.</summary>
    /// <param name="value">Value to swap the byte order of.</param>
    /// <returns>Byte order-swapped value.</returns>
    [MethodImpl(256)]
    public static ulong Swap32Pair(ulong value)
    {
        ulong a = (uint)(value >> 32);
        ulong b = (uint)(value);
        a = ((a & 0x000000FFUL) << 24) | ((a & 0x0000FF00UL) << 8) | ((a & 0x00FF0000UL) >> 8) | ((a & 0xFF000000UL) >> 24);
        b = ((b & 0x000000FFUL) << 24) | ((b & 0x0000FF00UL) << 8) | ((b & 0x00FF0000UL) >> 8) | ((b & 0xFF000000UL) >> 24);
        return (a << 32) | b;
    }

    /// <summary>Swaps the byte order of 64-bit values within a block of memory.</summary>
    /// <param name="src">Pointer to the source memory block.</param>
    /// <param name="dst">Pointer to the destination memory block.</param>
    /// <param name="count">The number of 64-bit values to swap.</param>
    [MethodImpl(256)]
    public static unsafe void Swap64(void* src, void* dst, int count)
    {
        var s64 = (ulong*)src;
        var d64 = (ulong*)dst;
        for (var i = 0; i < count; i++)
        {
            d64[i] = Swap(s64[i]);
        }
    }

    #endregion Public Methods
}
