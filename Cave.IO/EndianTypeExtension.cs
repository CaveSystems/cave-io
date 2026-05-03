using System;
using System.Runtime.CompilerServices;

namespace Cave.IO;

/// <summary>Provides extensions to the <see cref="EndianType"/> enum.</summary>
public static class EndianTypeExtension
{
    #region Public Methods

    /// <summary>Obtains the bytes of a 7 bit encoded integer.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] Get7BitEncodedBytes(this EndianType type, ulong value) => type == EndianType.BigEndian ? BigEndian.Get7BitEncodedBytes(value) : LittleEndian.Get7BitEncodedBytes(value);

    /// <summary>Obtains the bytes of a 7 bit encoded integer.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] Get7BitEncodedBytes(this EndianType type, long value) => type == EndianType.BigEndian ? BigEndian.Get7BitEncodedBytes(value) : LittleEndian.Get7BitEncodedBytes(value);

    /// <summary>Gets the <see cref="IBitConverter"/> instance for the specified <paramref name="endianType"/>.</summary>
    /// <param name="endianType">Endian type</param>
    /// <returns>Returns the <see cref="IBitConverter"/> instance.</returns>
    /// <exception cref="NotImplementedException">EndianType {endianType} not implemented!</exception>
    [MethodImpl(256)]
    public static IBitConverter GetBitConverter(this EndianType endianType) =>
        endianType switch
        {
            EndianType.LittleEndian => LittleEndian.Converter,
            EndianType.BigEndian => BigEndian.Converter,
            _ => throw new NotImplementedException($"EndianType {endianType} not implemented!")
        };

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(this EndianType type, double value) => type == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value);

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(this EndianType type, int value) => type == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value);

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(this EndianType type, sbyte value) => type == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value);

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(this EndianType type, TimeSpan value) => type == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value);

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(this EndianType type, ulong value) => type == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value);

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(this EndianType type, ushort value) => type == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value);

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(this EndianType type, uint value) => type == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value);

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(this EndianType type, short value) => type == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value);

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(this EndianType type, long value) => type == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value);

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(this EndianType type, float value) => type == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value);

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(this EndianType type, decimal value) => type == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value);

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(this EndianType type, DateTime value) => type == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value);

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(this EndianType type, byte value) => type == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value);

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    [MethodImpl(256)]
    public static byte[] GetBytes(this EndianType type, bool value) => type == EndianType.BigEndian ? BigEndian.GetBytes(value) : LittleEndian.GetBytes(value);

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">data is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">out of range.</exception>
    [MethodImpl(256)]
    public static bool ToBoolean(this EndianType type, byte[] data, int index) => type == EndianType.BigEndian ? BigEndian.ToBoolean(data, index) : LittleEndian.ToBoolean(data, index);

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">data is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">out of range.</exception>
    [MethodImpl(256)]
    public static byte ToByte(this EndianType type, byte[] data, int index) => type == EndianType.BigEndian ? BigEndian.ToByte(data, index) : LittleEndian.ToByte(data, index);

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">data is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">out of range.</exception>
    [MethodImpl(256)]
    public static DateTime ToDateTime(this EndianType type, byte[] data, int index) => type == EndianType.BigEndian ? BigEndian.ToDateTime(data, index) : LittleEndian.ToDateTime(data, index);

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">data is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">out of range.</exception>
    [MethodImpl(256)]
    public static decimal ToDecimal(this EndianType type, byte[] data, int index) => type == EndianType.BigEndian ? BigEndian.ToDecimal(data, index) : LittleEndian.ToDecimal(data, index);

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">data is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">out of range.</exception>
    [MethodImpl(256)]
    public static double ToDouble(this EndianType type, byte[] data, int index) => type == EndianType.BigEndian ? BigEndian.ToDouble(data, index) : LittleEndian.ToDouble(data, index);

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">data is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">out of range.</exception>
    [MethodImpl(256)]
    public static short ToInt16(this EndianType type, byte[] data, int index) => type == EndianType.BigEndian ? BigEndian.ToInt16(data, index) : LittleEndian.ToInt16(data, index);

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">data is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">out of range.</exception>
    [MethodImpl(256)]
    public static int ToInt32(this EndianType type, byte[] data, int index) => type == EndianType.BigEndian ? BigEndian.ToInt32(data, index) : LittleEndian.ToInt32(data, index);

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">data is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">out of range.</exception>
    [MethodImpl(256)]
    public static long ToInt64(this EndianType type, byte[] data, int index) => type == EndianType.BigEndian ? BigEndian.ToInt64(data, index) : LittleEndian.ToInt64(data, index);

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">data is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">out of range.</exception>
    [MethodImpl(256)]
    public static sbyte ToSByte(this EndianType type, byte[] data, int index) => type == EndianType.BigEndian ? BigEndian.ToSByte(data, index) : LittleEndian.ToSByte(data, index);

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">data is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">out of range.</exception>
    [MethodImpl(256)]
    public static float ToSingle(this EndianType type, byte[] data, int index) => type == EndianType.BigEndian ? BigEndian.ToSingle(data, index) : LittleEndian.ToSingle(data, index);

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">data is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">out of range.</exception>
    [MethodImpl(256)]
    public static TimeSpan ToTimeSpan(this EndianType type, byte[] data, int index) => type == EndianType.BigEndian ? BigEndian.ToTimeSpan(data, index) : LittleEndian.ToTimeSpan(data, index);

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">data is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">out of range.</exception>
    [MethodImpl(256)]
    public static ushort ToUInt16(this EndianType type, byte[] data, int index) => type == EndianType.BigEndian ? BigEndian.ToUInt16(data, index) : LittleEndian.ToUInt16(data, index);

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">data is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">out of range.</exception>
    [MethodImpl(256)]
    public static uint ToUInt32(this EndianType type, byte[] data, int index) => type == EndianType.BigEndian ? BigEndian.ToUInt32(data, index) : LittleEndian.ToUInt32(data, index);

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="type">The endian type.</param>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">data is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">out of range.</exception>
    [MethodImpl(256)]
    public static ulong ToUInt64(this EndianType type, byte[] data, int index) => type == EndianType.BigEndian ? BigEndian.ToUInt64(data, index) : LittleEndian.ToUInt64(data, index);

    #endregion Public Methods
}
