using System;
using System.ComponentModel.Design;
using Cave.Logging;

namespace Cave.IO.Blob;

/// <summary>Provides the shared base state for blob serialization and deserialization operations.</summary>
/// <remarks>
/// <see cref="BlobState"/> is the concrete base implementation of <see cref="IBlobState"/> and serves as the common foundation for <see
/// cref="BlobReaderState"/> and <see cref="BlobWriterState"/>. It manages per-converter state objects and exposes primitive type mappings between CLR <see
/// cref="Type"/> instances and <see cref="BlobPrimitiveType"/> codes.
/// </remarks>
abstract class BlobState : IBlobState
{
    public BlobConverterRegistry Converters { get; } = new();

    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="BlobState"/> class.</summary>
    /// <param name="serializer">The <see cref="BlobSerializer"/> that owns this state.</param>
    public BlobState(BlobSerializer serializer) => Serializer = serializer;

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    public Type GetPrimitiveType(BlobPrimitiveType typeCode)
    {
        return typeCode switch
        {
            BlobPrimitiveType.Bool => typeof(bool),
            BlobPrimitiveType.UInt8 => typeof(byte),
            BlobPrimitiveType.Int8 => typeof(sbyte),
            BlobPrimitiveType.Int16 => typeof(short),
            BlobPrimitiveType.UInt16 => typeof(ushort),
            BlobPrimitiveType.Int32 => typeof(int),
            BlobPrimitiveType.UInt32 => typeof(uint),
            BlobPrimitiveType.Int64 => typeof(long),
            BlobPrimitiveType.UInt64 => typeof(ulong),
            BlobPrimitiveType.Float32 => typeof(float),
            BlobPrimitiveType.Float64 => typeof(double),
            BlobPrimitiveType.Char => typeof(char),
            BlobPrimitiveType.String => typeof(string),
            BlobPrimitiveType.DateTime => typeof(DateTime),
            BlobPrimitiveType.TimeSpan => typeof(TimeSpan),
            BlobPrimitiveType.Decimal => typeof(decimal),
            BlobPrimitiveType.ByteArray => typeof(byte[]),
            _ => throw new NotSupportedException($"Unsupported primitive type code: {typeCode}")
        };
    }

    /// <inheritdoc/>
    public bool GetPrimitiveType(Type type, out BlobPrimitiveType primitiveType)
    {
        primitiveType = type switch
        {
            // primitives
            Type t when t == typeof(bool) => BlobPrimitiveType.Bool,
            Type t when t == typeof(byte) => BlobPrimitiveType.UInt8,
            Type t when t == typeof(sbyte) => BlobPrimitiveType.Int8,
            Type t when t == typeof(short) => BlobPrimitiveType.Int16,
            Type t when t == typeof(ushort) => BlobPrimitiveType.UInt16,
            Type t when t == typeof(int) => BlobPrimitiveType.Int32,
            Type t when t == typeof(uint) => BlobPrimitiveType.UInt32,
            Type t when t == typeof(long) => BlobPrimitiveType.Int64,
            Type t when t == typeof(ulong) => BlobPrimitiveType.UInt64,
            Type t when t == typeof(float) => BlobPrimitiveType.Float32,
            Type t when t == typeof(double) => BlobPrimitiveType.Float64,
            Type t when t == typeof(char) => BlobPrimitiveType.Char,

            // special allowed non-primitives
            Type t when t == typeof(string) => BlobPrimitiveType.String,
            Type t when t == typeof(DateTime) => BlobPrimitiveType.DateTime,
            Type t when t == typeof(TimeSpan) => BlobPrimitiveType.TimeSpan,
            Type t when t == typeof(decimal) => BlobPrimitiveType.Decimal,
            Type t when t == typeof(byte[]) => BlobPrimitiveType.ByteArray,

            Type t when t.IsEnum => BlobPrimitiveType.Enum,

            _ => BlobPrimitiveType.Unsupported
        };
        return (primitiveType != BlobPrimitiveType.Unsupported);
    }

    /// <inheritdoc/>
    public abstract void Close();

    #endregion Public Methods

    #region Properties

    /// <inheritdoc/>
    public BlobSerializer Serializer { get; }

    /// <inheritdoc/>
    public ILogger? Logger => Serializer.Logger;

    #endregion Properties
}
