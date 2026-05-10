using System;
using Cave.IO.Blob.Converters;

namespace Cave.IO.Blob;

/// <summary>Marks a class or struct as a target for blob conversion using the <see cref="BlobEnumerableConverter"/>.</summary>
/// <remarks>This attribute can be applied to classes and structs and is not inherited by derived types. It can only be applied once per type and should not be used together with <see cref="BlobReflectionConverterAttribute"/>!</remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public class BlobEnumerableConverterAttribute : Attribute
{
}
