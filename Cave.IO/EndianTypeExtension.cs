using System;

namespace Cave.IO;

/// <summary>Provides extensions to the <see cref="EndianType"/> enum.</summary>
public static class EndianTypeExtension
{
    #region Public Methods

    /// <summary>Gets the <see cref="IBitConverter"/> instance for the specified <paramref name="endianType"/>.</summary>
    /// <param name="endianType">Endian type</param>
    /// <returns>Returns the <see cref="IBitConverter"/> instance.</returns>
    /// <exception cref="NotImplementedException">EndianType {endianType} not implemented!</exception>
    public static IBitConverter GetBitConverter(this EndianType endianType) =>
        endianType switch
        {
            EndianType.LittleEndian => LittleEndian.Converter,
            EndianType.BigEndian => BigEndian.Converter,
            _ => throw new NotImplementedException($"EndianType {endianType} not implemented!")
        };

    #endregion Public Methods
}
