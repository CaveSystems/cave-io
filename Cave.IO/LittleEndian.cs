namespace Cave.IO;

/// <summary>Gets little endian extensions</summary>
public static class LittleEndian
{
    #region Public Properties

    /// <summary>Gets the little endian bit converter instance.</summary>
    public static BitConverterLE Converter { get; } = new();

    /// <summary>Gets a value indicating whether the current machine is little endian (true) or not (false)</summary>
    public static bool IsNative { get; } = (Endian.MachineType == EndianType.LittleEndian);

    #endregion Public Properties
}
