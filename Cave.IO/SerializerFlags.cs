using System;

namespace Cave.IO;

/// <summary>Provides available serializer settings flags.</summary>
[Flags]
public enum SerializerFlags
{
    /// <summary>No flags set.</summary>
    None = 0,

    /// <summary>Set this flag to de-/serialize private fields/properties</summary>
    NonPublic = 1 << 0,

    /// <summary>Set this flag to de-/serialize public fields/properties</summary>
    Public = 1 << 1,

    /// <summary>Set this flag to de-/serialize fields</summary>
    Fields = 1 << 2,

    /// <summary>Set this flag to de-/serialize properties</summary>
    Properties = 1 << 3,

    /// <summary>Set this flag to de-/serialize with type name</summary>
    TypeName = 1 << 4,

    /// <summary>Set this flag to de-/serialize with assembly name</summary>
    AssemblyName = 1 << 5,
}
