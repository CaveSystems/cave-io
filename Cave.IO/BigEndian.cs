using System;
using System.Runtime.CompilerServices;

namespace Cave.IO;

/// <summary>Gets big endian extensions</summary>
public static class BigEndian
{
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
            result[index++] = (byte)(v >> 24);
            result[index++] = (byte)(v >> 16);
            result[index++] = (byte)(v >> 8);
            result[index++] = (byte)v;
        }
        return result;
    }

    /// <summary>Retrieves the specified value as byte array.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The value as byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(ushort value) => unchecked([(byte)(value >> 8), (byte)value]);

    /// <summary>Retrieves the specified value as byte array.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The value as byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(uint value) => unchecked([(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value]);

    /// <summary>Retrieves the specified value as byte array.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The value as byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(ulong value) => unchecked([(byte)(value >> 56), (byte)(value >> 48), (byte)(value >> 40), (byte)(value >> 32), (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value]);

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

    /// <summary>Gets the specified value as byte array with little-endian output.</summary>
    /// <param name="values">The values as float array.</param>
    /// <returns>The byte array representation of the values.</returns>
    [MethodImpl(256)]
    public static unsafe byte[] GetBytes(float[] values)
    {
        if (values.Length == 0) return [];
        var result = new byte[values.Length * 4];
        if (Endian.MachineType == EndianType.LittleEndian)
        {
            Buffer.BlockCopy(values, 0, result, 0, result.Length);
            return result;
        }
        fixed (float* src = values)
        fixed (byte* dst = result)
        {
            Endian.Swap32(src, dst, values.Length);
        }
        return result;
    }

    /// <summary>Gets the specified value as byte array with little-endian output.</summary>
    /// <param name="values">The values as float array.</param>
    /// <returns>The byte array representation of the values.</returns>
    [MethodImpl(256)]
    public static unsafe byte[] GetBytes(uint[] values)
    {
        if (values.Length == 0) return [];
        var result = new byte[values.Length * 4];
        if (Endian.MachineType == EndianType.LittleEndian)
        {
            Buffer.BlockCopy(values, 0, result, 0, result.Length);
            return result;
        }
        fixed (uint* src = values)
        fixed (byte* dst = result)
        {
            Endian.Swap32(src, dst, values.Length);
        }
        return result;
    }

    /// <summary>Gets the specified value as byte array with little-endian output.</summary>
    /// <param name="values">The values as float array.</param>
    /// <returns>The byte array representation of the values.</returns>
    [MethodImpl(256)]
    public static unsafe byte[] GetBytes(int[] values)
    {
        if (values.Length == 0) return [];
        var result = new byte[values.Length * 4];
        if (Endian.MachineType == EndianType.LittleEndian)
        {
            Buffer.BlockCopy(values, 0, result, 0, result.Length);
            return result;
        }
        fixed (int* src = values)
        fixed (byte* dst = result)
        {
            Endian.Swap32(src, dst, values.Length);
        }
        return result;
    }

    /// <summary>Gets the specified value as byte array with little-endian output.</summary>
    /// <param name="values">The values as double array.</param>
    /// <returns>The byte array representation of the values.</returns>
    [MethodImpl(256)]
    public static unsafe byte[] GetBytes(double[] values)
    {
        if (values.Length == 0) return [];
        var result = new byte[values.Length * 8];

        if (Endian.MachineType == EndianType.LittleEndian)
        {
            Buffer.BlockCopy(values, 0, result, 0, result.Length);
            return result;
        }
        fixed (double* src = values)
        fixed (byte* dst = result)
        {
            Endian.Swap64(src, dst, values.Length);
        }
        return result;
    }

    /// <summary>Gets the specified value as byte array with little-endian output.</summary>
    /// <param name="values">The values as double array.</param>
    /// <returns>The byte array representation of the values.</returns>
    [MethodImpl(256)]
    public static unsafe byte[] GetBytes(ulong[] values)
    {
        if (values.Length == 0) return [];
        var result = new byte[values.Length * 8];

        if (Endian.MachineType == EndianType.LittleEndian)
        {
            Buffer.BlockCopy(values, 0, result, 0, result.Length);
            return result;
        }
        fixed (ulong* src = values)
        fixed (byte* dst = result)
        {
            Endian.Swap64(src, dst, values.Length);
        }
        return result;
    }

    /// <summary>Gets the specified value as byte array with little-endian output.</summary>
    /// <param name="values">The values as double array.</param>
    /// <returns>The byte array representation of the values.</returns>
    [MethodImpl(256)]
    public static unsafe byte[] GetBytes(long[] values)
    {
        if (values.Length == 0) return [];
        var result = new byte[values.Length * 8];

        if (Endian.MachineType == EndianType.LittleEndian)
        {
            Buffer.BlockCopy(values, 0, result, 0, result.Length);
            return result;
        }
        fixed (long* src = values)
        fixed (byte* dst = result)
        {
            Endian.Swap64(src, dst, values.Length);
        }
        return result;
    }

    /// <summary>Gets the specified value as byte array with little-endian output.</summary>
    /// <param name="values">The values as double array.</param>
    /// <returns>The byte array representation of the values.</returns>
    [MethodImpl(256)]
    public static unsafe byte[] GetBytes(ushort[] values)
    {
        if (values.Length == 0) return [];
        var result = new byte[values.Length * 2];

        if (Endian.MachineType == EndianType.LittleEndian)
        {
            Buffer.BlockCopy(values, 0, result, 0, result.Length);
            return result;
        }
        fixed (ushort* src = values)
        fixed (byte* dst = result)
        {
            Endian.Swap16(src, dst, values.Length);
        }
        return result;
    }

    /// <summary>Gets the specified value as byte array with little-endian output.</summary>
    /// <param name="values">The values as double array.</param>
    /// <returns>The byte array representation of the values.</returns>
    [MethodImpl(256)]
    public static unsafe byte[] GetBytes(short[] values)
    {
        if (values.Length == 0) return [];
        var result = new byte[values.Length * 2];

        if (Endian.MachineType == EndianType.LittleEndian)
        {
            Buffer.BlockCopy(values, 0, result, 0, result.Length);
            return result;
        }
        fixed (short* src = values)
        fixed (byte* dst = result)
        {
            Endian.Swap16(src, dst, values.Length);
        }
        return result;
    }

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

    /// <summary>Gets the specified byte array as a value array with little-endian output.</summary>
    /// <param name="buffer">The values as byte array.</param>
    /// <returns>The value array representation of the byte array.</returns>
    [MethodImpl(256)]
    public static unsafe double[] ToDoubleArray(byte[] buffer)
    {
        if (buffer.Length == 0) return [];
        var result = new double[buffer.Length / 8];

        if (Endian.MachineType == EndianType.LittleEndian)
        {
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            return result;
        }
        fixed (byte* src = buffer)
        fixed (double* dst = result)
        {
            Endian.Swap64(src, dst, result.Length);
        }
        return result;
    }

    /// <summary>Gets the specified byte array as a value array with little-endian output.</summary>
    /// <param name="buffer">The values as byte array.</param>
    /// <returns>The value array representation of the byte array.</returns>
    [MethodImpl(256)]
    public static unsafe float[] ToFloatArray(byte[] buffer)
    {
        if (buffer.Length == 0) return [];
        var result = new float[buffer.Length / 4];

        if (Endian.MachineType == EndianType.LittleEndian)
        {
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            return result;
        }
        fixed (byte* src = buffer)
        fixed (float* dst = result)
        {
            Endian.Swap32(src, dst, result.Length);
        }
        return result;
    }

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    [MethodImpl(256)]
    public static short ToInt16(byte[] data, int index) => unchecked((short)ToUInt16(data, index));

    /// <summary>Gets the specified byte array as a value array with little-endian output.</summary>
    /// <param name="buffer">The values as byte array.</param>
    /// <returns>The value array representation of the byte array.</returns>
    [MethodImpl(256)]
    public static unsafe short[] ToInt16Array(byte[] buffer)
    {
        if (buffer.Length == 0) return [];
        var result = new short[buffer.Length / 2];

        if (Endian.MachineType == EndianType.LittleEndian)
        {
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            return result;
        }
        fixed (byte* src = buffer)
        fixed (short* dst = result)
        {
            Endian.Swap16(src, dst, result.Length);
        }
        return result;
    }

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    [MethodImpl(256)]
    public static int ToInt32(byte[] data, int index) => unchecked((int)ToUInt32(data, index));

    /// <summary>Gets the specified byte array as a value array with little-endian output.</summary>
    /// <param name="buffer">The values as byte array.</param>
    /// <returns>The value array representation of the byte array.</returns>
    [MethodImpl(256)]
    public static unsafe int[] ToInt32Array(byte[] buffer)
    {
        if (buffer.Length == 0) return [];
        var result = new int[buffer.Length / 4];

        if (Endian.MachineType == EndianType.LittleEndian)
        {
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            return result;
        }
        fixed (byte* src = buffer)
        fixed (int* dst = result)
        {
            Endian.Swap32(src, dst, result.Length);
        }
        return result;
    }

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    [MethodImpl(256)]
    public static long ToInt64(byte[] data, int index) => unchecked((long)ToUInt64(data, index));

    /// <summary>Gets the specified byte array as a value array with little-endian output.</summary>
    /// <param name="buffer">The values as byte array.</param>
    /// <returns>The value array representation of the byte array.</returns>
    [MethodImpl(256)]
    public static unsafe long[] ToInt64Array(byte[] buffer)
    {
        if (buffer.Length == 0) return [];
        var result = new long[buffer.Length / 8];

        if (Endian.MachineType == EndianType.LittleEndian)
        {
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            return result;
        }
        fixed (byte* src = buffer)
        fixed (long* dst = result)
        {
            Endian.Swap64(src, dst, result.Length);
        }
        return result;
    }

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

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    [MethodImpl(256)]
    public static ushort ToUInt16(byte[] data, int index) => unchecked((ushort)((data[index] << 8) | data[index + 1]));

    /// <summary>Gets the specified byte array as a value array with little-endian output.</summary>
    /// <param name="buffer">The values as byte array.</param>
    /// <returns>The value array representation of the byte array.</returns>
    [MethodImpl(256)]
    public static unsafe ushort[] ToUInt16Array(byte[] buffer)
    {
        if (buffer.Length == 0) return [];
        var result = new ushort[buffer.Length / 2];

        if (Endian.MachineType == EndianType.LittleEndian)
        {
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            return result;
        }
        fixed (byte* src = buffer)
        fixed (ushort* dst = result)
        {
            Endian.Swap16(src, dst, result.Length);
        }
        return result;
    }

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    [MethodImpl(256)]
    public static uint ToUInt32(byte[] data, int index) => unchecked(((uint)data[index] << 24) | ((uint)data[index + 1] << 16) | ((uint)data[index + 2] << 8) | data[index + 3]);

    /// <summary>Gets the specified byte array as a value array with little-endian output.</summary>
    /// <param name="buffer">The values as byte array.</param>
    /// <returns>The value array representation of the byte array.</returns>
    [MethodImpl(256)]
    public static unsafe uint[] ToUInt32Array(byte[] buffer)
    {
        if (buffer.Length == 0) return [];
        var result = new uint[buffer.Length / 4];

        if (Endian.MachineType == EndianType.LittleEndian)
        {
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            return result;
        }
        fixed (byte* src = buffer)
        fixed (uint* dst = result)
        {
            Endian.Swap32(src, dst, result.Length);
        }
        return result;
    }

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    [MethodImpl(256)]
    public static ulong ToUInt64(byte[] data, int index) =>
            unchecked(((ulong)data[index] << 56) | ((ulong)data[index + 1] << 48) | ((ulong)data[index + 2] << 40) | ((ulong)data[index + 3] << 32) |
            ((ulong)data[index + 4] << 24) | ((ulong)data[index + 5] << 16) | ((ulong)data[index + 6] << 8) | data[index + 7]);

    /// <summary>Gets the specified byte array as a value array with little-endian output.</summary>
    /// <param name="buffer">The values as byte array.</param>
    /// <returns>The value array representation of the byte array.</returns>
    [MethodImpl(256)]
    public static unsafe ulong[] ToUInt64Array(byte[] buffer)
    {
        if (buffer.Length == 0) return [];
        var result = new ulong[buffer.Length / 8];

        if (Endian.MachineType == EndianType.LittleEndian)
        {
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            return result;
        }
        fixed (byte* src = buffer)
        fixed (ulong* dst = result)
        {
            Endian.Swap64(src, dst, result.Length);
        }
        return result;
    }

    #endregion Public Methods

    #region Properties

    /// <summary>Gets the big endian bit converter instance.</summary>
    [Obsolete("Use BigEndian static class (performance)")]
    public static BitConverterBE Converter { get; } = new();

    /// <summary>Gets a value indicating whether the current machine is little endian (true) or not (false)</summary>
    public static bool IsNative { get; } = (Endian.MachineType == EndianType.BigEndian);

    #endregion Properties
}
