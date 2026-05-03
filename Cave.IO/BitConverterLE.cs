using System;

namespace Cave.IO;

/// <summary>Provides an alternate <see cref="BitConverter"/> class providing additional functionality.</summary>
/// <remarks>Use <see cref="LittleEndian.Converter"/> for an instance.</remarks>
[Obsolete("Use LittleEndian or BigEndian static classes (performance)")]
public class BitConverterLE : BitConverterBase
{
    #region Public Methods

    /// <inheritdoc/>
    public override byte[] GetBytes(ushort value) => LittleEndian.GetBytes(value);

    /// <inheritdoc/>
    public override byte[] GetBytes(uint value) => LittleEndian.GetBytes(value);

    /// <inheritdoc/>
    public override byte[] GetBytes(ulong value) => LittleEndian.GetBytes(value);

    /// <inheritdoc/>
    public override byte[] GetBytes(decimal value) => LittleEndian.GetBytes(value);   

    /// <inheritdoc/>
    public override ushort ToUInt16(byte[] data, int index) => LittleEndian.ToUInt16(data, index);

    /// <inheritdoc/>
    public override uint ToUInt32(byte[] data, int index) => LittleEndian.ToUInt32(data, index);

    /// <inheritdoc/>
    public override ulong ToUInt64(byte[] data, int index) => LittleEndian.ToUInt64(data, index);

    #endregion Public Methods
}
