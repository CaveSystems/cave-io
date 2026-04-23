using System;
using Cave.IO.Blob.Converters;

namespace Cave.IO.Blob;

/// <summary>
/// Identifies the primitive data type of a blob field within the binary serialization format. Used by <see cref="BlobPrimitiveConverter"/> to tag stored
/// values, enabling correct deserialization when reading back blob content.
/// </summary>
public enum BlobPrimitiveType : uint
{
    /// <summary>The type is not supported or could not be determined. This is the default value.</summary>
    Unsupported = 0,

    /// <summary>Represents a <see cref="bool"/> value.</summary>
    Bool,

    /// <summary>Represents a signed 8-bit integer ( <see cref="sbyte"/>).</summary>
    Int8,

    /// <summary>Represents an unsigned 8-bit integer ( <see cref="byte"/>).</summary>
    UInt8,

    /// <summary>Represents a signed 16-bit integer ( <see cref="short"/>).</summary>
    Int16,

    /// <summary>Represents an unsigned 16-bit integer ( <see cref="ushort"/>).</summary>
    UInt16,

    /// <summary>Represents a signed 32-bit integer ( <see cref="int"/>), encoded using 7-bit variable-length encoding.</summary>
    Int32,

    /// <summary>Represents an unsigned 32-bit integer ( <see cref="uint"/>), encoded using 7-bit variable-length encoding.</summary>
    UInt32,

    /// <summary>Represents a signed 64-bit integer ( <see cref="long"/>), encoded using 7-bit variable-length encoding.</summary>
    Int64,

    /// <summary>Represents an unsigned 64-bit integer ( <see cref="ulong"/>), encoded using 7-bit variable-length encoding.</summary>
    UInt64,

    /// <summary>Represents a 32-bit IEEE 754 single-precision floating-point number ( <see cref="float"/>).</summary>
    Float32,

    /// <summary>Represents a 64-bit IEEE 754 double-precision floating-point number ( <see cref="double"/>).</summary>
    Float64,

    /// <summary>Represents a single Unicode character ( <see cref="char"/>).</summary>
    Char,

    /// <summary>Represents a length-prefixed UTF-8 encoded string ( <see cref="string"/>).</summary>
    String,

    /// <summary>Represents a length-prefixed sequence of raw bytes ( <see cref="byte"/>[]).</summary>
    ByteArray,

    /// <summary>Represents a <see cref="DateTime"/> value.</summary>
    DateTime,

    /// <summary>Represents a <see cref="TimeSpan"/> value.</summary>
    TimeSpan,

    /// <summary>Represents a <see cref="decimal"/> value.</summary>
    Decimal,

    /// <summary>Represents an enumeration value.</summary>
    Enum,

    /// <summary>Represents a <see cref="DateTimeOffset"/> value, which includes both a <see cref="DateTime"/> and an associated offset from UTC.</summary>
    DateTimeOffset,
}
