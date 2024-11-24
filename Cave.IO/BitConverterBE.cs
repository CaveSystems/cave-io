using System;

namespace Cave.IO;

/// <summary>Provides an alternate <see cref="BitConverter"/> class providing additional functionality.</summary>
/// <remarks>Use <see cref="BigEndian.Converter"/> for an instance.</remarks>
public class BitConverterBE : BitConverterBase
{
    #region Public Methods

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    public override byte[] GetBytes(ushort value) => unchecked(new[] { (byte)(value / 256), (byte)(value % 256) });

    /// <summary>Retrieves the specified value as byte array with the specified endiantype.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as encoded byte array.</returns>
    public override byte[] GetBytes(uint value)
    {
        var result = new byte[4];
        for (var i = 3; i >= 0; i--)
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
        for (var i = 7; i >= 0; i--)
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
        if (data is null) throw new ArgumentNullException(nameof(data));
        return unchecked((ushort)((data[index] * 256) + data[index + 1]));
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

        uint result = 0;
        for (var i = 0; i < 4; i++, index++)
        {
            result = (result * 256) + data[index];
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

        ulong result = 0;
        for (var i = 0; i < 8; i++, index++)
        {
            result = (result * 256) + data[index];
        }

        return result;
    }

    #endregion Public Methods
}
