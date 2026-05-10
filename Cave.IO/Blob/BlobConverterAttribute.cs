using System;

namespace Cave.IO.Blob;

/// <summary>
/// Obsolete attribute to mark a class as a blob converter. Use <see cref="BlobReflectionConverterAttribute"/> instead.
/// </summary>
[Obsolete("Use BlobReflectionConverterAttribute instead.")]
public class BlobConverterAttribute : BlobReflectionConverterAttribute
{
}
