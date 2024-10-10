namespace Cave.IO;

/// <summary>
/// Gets big endian extensions
/// </summary>
public static class BigEndian
{
    /// <summary>Gets the big endian bit converter instance.</summary>
    public static BitConverterBE Converter { get; } = new();

    /// <summary>
    /// Gets a value indicating whether the current machine is little endian (true) or not (false)
    /// </summary>
    public static bool IsNative { get; } = (Endian.MachineType == EndianType.BigEndian);
}
