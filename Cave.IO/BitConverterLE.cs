using System;

namespace Cave.IO;

/// <summary>Provides an alternate <see cref="BitConverter"/> class providing additional functionality.</summary>
/// <remarks>Use <see cref="LittleEndian.Converter"/> for an instance.</remarks>
public class BitConverterLE : BitConverterBase
{
    #region Public Methods

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    public override byte[] GetBytes(ushort value) => [(byte)(value % 256), (byte)(value / 256)];

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    public override byte[] GetBytes(uint value)
    {
        var result = new byte[4];
        for (var i = 0; i < 4; i++)
        {
            result[i] = (byte)(value % 256);
            value /= 256;
        }

        return result;
    }

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    public override byte[] GetBytes(ulong value)
    {
        var result = new byte[8];
        for (var i = 0; i < 8; i++)
        {
            result[i] = (byte)(value % 256);
            value /= 256;
        }

        return result;
    }

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">data is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">out of range.</exception>
    public override ushort ToUInt16(byte[] data, int index)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if ((index < 0) || (index >= (data.Length - 1)))
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return unchecked((ushort)(data[index] + (data[index + 1] * 256)));
    }

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">data is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">out of range.</exception>
    public override uint ToUInt32(byte[] data, int index)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        uint result = data[index];
        uint multiplier = 1;
        for (var i = 1; i < 4; i++)
        {
            multiplier *= 256;
            result += data[index + i] * multiplier;
        }

        return result;
    }

    /// <summary>Returns a value converted from the specified data at a specified index.</summary>
    /// <param name="data">The data as byte array.</param>
    /// <param name="index">The index.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">data is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">out of range.</exception>
    public override ulong ToUInt64(byte[] data, int index)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        ulong result = data[index];
        ulong multiplier = 1;
        for (var i = 1; i < 8; i++)
        {
            multiplier *= 256;
            result += data[index + i] * multiplier;
        }

        return result;
    }

    #endregion Public Methods
}
