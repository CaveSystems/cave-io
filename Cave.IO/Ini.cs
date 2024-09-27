using System;

namespace Cave.IO;

/// <summary>Provides access to common ini file.</summary>
public static class Ini
{
    #region Internal Methods

    internal static void CheckName(string value, string paramName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(paramName);
        }
        for (var i = 0; i < value.Length; i++)
        {
            switch (value[i])
            {
                case '#':
                case '[':
                case ']':
                    throw new ArgumentException($"Invalid name for {paramName} {value}!", paramName);
                default:
                    if (value[i] < 32)
                    {
                        throw new ArgumentException($"Invalid name for {paramName} {value}!", paramName);
                    }
                    break;
            }
        }
    }

    internal static string Escape(string value, IniProperties properties)
    {
        var box = value.IndexOfAny([properties.BoxCharacter, '#', ' ']) > -1;
        if (!properties.DisableEscaping)
        {
            value = value.EscapeUtf8();
        }

        box |= value.IndexOf('\\') > -1 || value.Trim() != value;
        if (box)
        {
            value = value.Box(properties.BoxCharacter);
        }
        return value;
    }

    internal static string Unescape(string value, IniProperties properties)
    {
        if (value.IsBoxed(properties.BoxCharacter, properties.BoxCharacter))
        {
            value = value.Unbox(properties.BoxCharacter);
        }

        if (!properties.DisableEscaping)
        {
            try { value = value.Unescape(); }
            catch { }
        }

        return value;
    }

    #endregion Internal Methods

    #region Public Properties

    /// <summary>Gets the platform specific extension of the configuration file.</summary>
    public static string PlatformExtension => Platform.Type switch
    {
        PlatformType.CompactFramework or PlatformType.Windows or PlatformType.Xbox => ".ini",
        PlatformType.Linux or PlatformType.BSD or PlatformType.Android or PlatformType.Solaris or PlatformType.UnknownUnix or PlatformType.Unknown or PlatformType.MacOS => ".conf",
        _ => ".conf",
    };

    #endregion Public Properties

    #region Public Methods

    /// <summary>Gets the local machine ini file.</summary>
    /// <value>The local machine ini file.</value>
    public static IniReader GetLocalMachineIniFile()
    {
        var fileName = MainAssembly.Get()?.GetName()?.Name ?? "main";
        var location = FileLocation.Create(root: RootLocation.AllUserConfig, fileName: fileName, extension: PlatformExtension);
        FileSystem.TouchFile(location);
        return IniReader.FromFile(location);
    }

    /// <summary>Gets the local user ini file.</summary>
    /// <value>The local user ini file.</value>
    public static IniReader GetLocalUserIniFile()
    {
        var fileName = MainAssembly.Get()?.GetName()?.Name ?? "main";
        var location = FileLocation.Create(root: RootLocation.LocalUserConfig, fileName: fileName, extension: PlatformExtension);
        FileSystem.TouchFile(location);
        return IniReader.FromFile(location);
    }

    /// <summary>Gets the program ini file.</summary>
    /// <value>The program ini file.</value>
    public static IniReader GetProgramIniFile()
    {
        var fileName = MainAssembly.Get()?.GetName()?.Name ?? "main";
        var location = FileLocation.Create(root: RootLocation.Program, fileName: fileName, extension: PlatformExtension);
        FileSystem.TouchFile(location);
        return IniReader.FromFile(location);
    }

    /// <summary>Gets the user ini file.</summary>
    /// <value>The user ini file.</value>
    public static IniReader GetUserIniFile()
    {
        var fileName = MainAssembly.Get()?.GetName()?.Name ?? "main";
        var location = FileLocation.Create(root: RootLocation.RoamingUserConfig, fileName: fileName, extension: PlatformExtension);
        FileSystem.TouchFile(location);
        return IniReader.FromFile(location);
    }

    #endregion Public Methods
}
