using System;
using Cave.IO.Blob.Converters;

namespace Cave.IO.Blob;

/// <summary>Marks a class or struct as a target for the <see cref="BlobReflectionConverter"/> and configures the serialization behavior via <see cref="BlobConverterFlags"/>.</summary>
/// <remarks>This attribute can be applied to classes and structs and is inherited by derived types. It can only be applied once per type.</remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
public class BlobReflectionConverterAttribute : Attribute
{
    #region Properties

    /// <summary>
    /// Gets or sets the <see cref="BlobConverterFlags"/> that determine which members (fields, properties, public, private) are included during serialization.
    /// </summary>
    public BlobConverterFlags Source { get; set; }

    #endregion Properties
}
