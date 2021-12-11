using System;

namespace Cave.IO
{
    /// <summary>
    /// Attribute for marking fields or properties to be ignored by ini file serialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class IniIgnoreAttribute : Attribute
    {
    }
}
