using System;

namespace Cave.IO;

/// <summary>Provides a base class for bit converters.</summary>
/// <seealso cref="IBitConverter"/>
[Obsolete("Use LittleEndian or BigEndian static classes (performance)")]
public abstract class BitConverterBase : IBitConverter
{
    #region Public Methods

    /// <summary>Gets the bytes of a 7 bit encoded integer.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    public byte[] Get7BitEncodedBytes(ulong value)
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
    public byte[] Get7BitEncodedBytes(long value) => Get7BitEncodedBytes(unchecked((ulong)value));

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    public byte[] GetBytes(bool value) => value ? [1] : [0];

    /// <summary>Gets the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    public byte[] GetBytes(byte value) => [value];

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    public byte[] GetBytes(sbyte value) => unchecked(new[] { (byte)value });

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    public byte[] GetBytes(short value) => unchecked(GetBytes((ushort)value));

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    public byte[] GetBytes(int value) => unchecked(GetBytes((uint)value));

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    public byte[] GetBytes(long value) => unchecked(GetBytes((ulong)value));

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    public byte[] GetBytes(float value) => GetBytes(SingleStruct.ToUInt32(value));

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    public byte[] GetBytes(double value) => GetBytes(DoubleStruct.ToUInt64(value));

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    public byte[] GetBytes(DateTime value) => GetBytes(value.Ticks);

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    public byte[] GetBytes(TimeSpan value) => GetBytes(value.Ticks);

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    public abstract byte[] GetBytes(decimal value);

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The value as byte array.</returns>
    public abstract byte[] GetBytes(ushort value);

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The value as byte array.</returns>
    public abstract byte[] GetBytes(uint value);

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The value as byte array.</returns>
    public abstract byte[] GetBytes(ulong value);

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    public bool ToBoolean(byte[] data, int index) => data[index] != 0;

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    public byte ToByte(byte[] data, int index) => data[index];

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    public DateTime ToDateTime(byte[] data, int index) => new(ToInt64(data, index));

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    public decimal ToDecimal(byte[] data, int index)
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
    public double ToDouble(byte[] data, int index) => DoubleStruct.ToDouble(ToUInt64(data, index));

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    public short ToInt16(byte[] data, int index) => unchecked((short)ToUInt16(data, index));

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    public int ToInt32(byte[] data, int index) => unchecked((int)ToUInt32(data, index));

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    public long ToInt64(byte[] data, int index) => unchecked((long)ToUInt64(data, index));

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    public sbyte ToSByte(byte[] data, int index) => unchecked((sbyte)data[index]);

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    public float ToSingle(byte[] data, int index) => SingleStruct.ToSingle(ToUInt32(data, index));

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    public TimeSpan ToTimeSpan(byte[] data, int index) => new(ToInt64(data, index));

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The value as byte array.</returns>
    public abstract ushort ToUInt16(byte[] data, int index);

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The value as byte array.</returns>
    public abstract uint ToUInt32(byte[] data, int index);

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The value as byte array.</returns>
    public abstract ulong ToUInt64(byte[] data, int index);

    #endregion Public Methods
}
