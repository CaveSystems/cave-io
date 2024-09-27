using System;

namespace Cave.IO;

/// <summary>Attribute for marking fields or properties for serialization into ini file sections.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class IniSectionAttribute : Attribute
{
    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="IniSectionAttribute"/> class.</summary>
    public IniSectionAttribute() { }

    /// <summary>Initializes a new instance of the <see cref="IniSectionAttribute"/> class.</summary>
    /// <param name="name"></param>
    public IniSectionAttribute(string name) => Name = name ?? throw new ArgumentNullException(nameof(name));

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets the section name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the type of elements to be serialized.</summary>
    public IniSettingsType SettingsType { get; set; }

    #endregion Public Properties
}
