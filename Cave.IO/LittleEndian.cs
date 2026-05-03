using System;
using System.Runtime.CompilerServices;

namespace Cave.IO;

/// <summary>Gets little endian extensions</summary>
public static class LittleEndian
{
    #region Public Properties

    /// <summary>Gets the little endian bit converter instance.</summary>
    [Obsolete("Use LittleEndian static class (performance)")]
    public static BitConverterLE Converter { get; } = new();

    /// <summary>Gets a value indicating whether the current machine is little endian (true) or not (false)</summary>
    public static bool IsNative { get; } = (Endian.MachineType == EndianType.LittleEndian);

    #endregion Public Properties

    #region Public Methods

    /// <summary>Gets the bytes of a 7 bit encoded integer.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] Get7BitEncodedBytes(ulong value)
    {
        var buffer = new byte[10];
        var index = 0;
        while (value >= 0x80)
        {
            buffer[index++] = (byte)((value & 0x7F) | 0x80);
            value >>= 7;
        }

        buffer[index++] = (byte)value;
        return buffer[..index];
    }

    /// <summary>Gets the bytes of a 7 bit encoded integer.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] Get7BitEncodedBytes(long value) => Get7BitEncodedBytes(unchecked((ulong)value));

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(decimal value)
    {
        var bits = decimal.GetBits(value);
        var result = new byte[16];
        var index = 0;
        for (var i = 0; i < 4; i++)
        {
            var v = bits[i];
            result[index++] = (byte)v;
            result[index++] = (byte)(v >> 8);
            result[index++] = (byte)(v >> 16);
            result[index++] = (byte)(v >> 24);
        }

        return result;
    }

    [MethodImpl(256)]
    public static byte[] GetBytes(ushort value) => [(byte)value, (byte)(value >> 8)];

    [MethodImpl(256)]
    public static byte[] GetBytes(uint value) => [(byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24)];

    [MethodImpl(256)]
    public static byte[] GetBytes(ulong value) => [(byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24), (byte)(value >> 32), (byte)(value >> 40), (byte)(value >> 48), (byte)(value >> 56)];

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(bool value) => value ? [1] : [0];

    /// <summary>Gets the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(byte value) => [value];

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(sbyte value) => unchecked([(byte)value]);

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(short value) => unchecked(GetBytes((ushort)value));

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(int value) => unchecked(GetBytes((uint)value));

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(long value) => unchecked(GetBytes((ulong)value));

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(float value) => GetBytes(SingleStruct.ToUInt32(value));

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(double value) => GetBytes(DoubleStruct.ToUInt64(value));

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(DateTime value) => GetBytes(value.Ticks);

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(TimeSpan value) => GetBytes(value.Ticks);

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    [MethodImpl(256)]
    public static bool ToBoolean(byte[] data, int index) => data[index] != 0;

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    [MethodImpl(256)]
    public static byte ToByte(byte[] data, int index) => data[index];

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    [MethodImpl(256)]
    public static DateTime ToDateTime(byte[] data, int index) => new(ToInt64(data, index));

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    [MethodImpl(256)]
    public static decimal ToDecimal(byte[] data, int index)
    {
        unchecked
        {
            var lo = (int)ToUInt32(data, index);
            var mid = (int)ToUInt32(data, index + 4);
            var hi = (int)ToUInt32(data, index + 8);
            var flags = (int)ToUInt32(data, index + 12);
            return new decimal([lo, mid, hi, flags]);
        }
    }

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    [MethodImpl(256)]
    public static double ToDouble(byte[] data, int index) => DoubleStruct.ToDouble(ToUInt64(data, index));

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    [MethodImpl(256)]
    public static short ToInt16(byte[] data, int index) => unchecked((short)ToUInt16(data, index));

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    [MethodImpl(256)]
    public static int ToInt32(byte[] data, int index) => unchecked((int)ToUInt32(data, index));

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    [MethodImpl(256)]
    public static long ToInt64(byte[] data, int index) => unchecked((long)ToUInt64(data, index));

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    [MethodImpl(256)]
    public static sbyte ToSByte(byte[] data, int index) => unchecked((sbyte)data[index]);

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    [MethodImpl(256)]
    public static float ToSingle(byte[] data, int index) => SingleStruct.ToSingle(ToUInt32(data, index));

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    [MethodImpl(256)]
    public static TimeSpan ToTimeSpan(byte[] data, int index) => new(ToInt64(data, index));

    [MethodImpl(256)]
    public static ushort ToUInt16(byte[] data, int index) => (ushort)(data[index] | (data[index + 1] << 8));

    [MethodImpl(256)]
    public static uint ToUInt32(byte[] data, int index) => (uint)(data[index] | (data[index + 1] << 8) | (data[index + 2] << 16) | (data[index + 3] << 24));

    [MethodImpl(256)]
    public static ulong ToUInt64(byte[] data, int index) =>
        data[index] | ((ulong)data[index + 1] << 8) | ((ulong)data[index + 2] << 16) | ((ulong)data[index + 3] << 24) |
        ((ulong)data[index + 4] << 32) | ((ulong)data[index + 5] << 40) | ((ulong)data[index + 6] << 48) | ((ulong)data[index + 7] << 56);

    #endregion Public Methods
}
