namespace Cave.IO;

/// <summary>
/// Provides available types of ini settings serialization.
/// </summary>
public enum IniSettingsType
{
    /// <summary>
    /// Read / write fields of a struct / class
    /// </summary>
    Fields = 0,

    /// <summary>
    /// Read / write properties of a struct / class
    /// </summary>
    Properties = 1,
}
