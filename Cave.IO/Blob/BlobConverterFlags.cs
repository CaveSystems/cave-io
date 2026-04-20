using System;
using Cave.IO.Blob.Converters;

namespace Cave.IO.Blob;

/// <summary>Controls which members are included by the <see cref="BlobReflectionConverter"/> during serialization and deserialization.</summary>
/// <remarks>
/// Flags can be combined to fine-tune member selection. When set to <see cref="Default"/>, the converter resolves the effective flags at runtime: value types (
/// <see langword="struct"/>) use <see cref="Fields"/>, reference types ( <see langword="class"/>, <see langword="record"/>) use <see cref="Properties"/>; both
/// public and non-public members are included in either case. Each member is assigned a stable numeric identifier in the binary stream, which allows
/// deserialization to survive minor member renames via fuzzy name matching.
/// </remarks>
[Flags]
public enum BlobConverterFlags
{
    /// <summary>
    /// Default uses private and public fields for structures and private and public properties for classes. This is the default because it is the most likely
    /// to be stable across versions of the same type. It also allows to use immutable types with private fields and public properties.
    /// </summary>
    Default,

    /// <summary>Tells the <see cref="BlobReflectionConverter"/> to use fields for serialization. This is the default for structures.</summary>
    Fields = 1 << 0,

    /// <summary>Tells the <see cref="BlobReflectionConverter"/> to use properties for serialization. This is the default for classes and records.</summary>
    Properties = 1 << 1,

    /// <summary>Tells the <see cref="BlobReflectionConverter"/> to include public members for serialization. This is the default.</summary>
    Public = 1 << 2,

    /// <summary>Tells the <see cref="BlobReflectionConverter"/> to include private members for serialization. This is the default.</summary>
    Private = 1 << 3,
}
