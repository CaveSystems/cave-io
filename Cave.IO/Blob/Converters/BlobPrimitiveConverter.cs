using System;

namespace Cave.IO.Blob.Converters;

/// <summary>Converts primitive CLR types, strings, DateTime, TimeSpan, decimal, byte[] and enums to and from a blob representation.</summary>
/// <remarks>
/// Supports nullable underlying types. When <see cref="EnumAsStrings"/> is true, enums are (de)serialized using their name strings; otherwise their numeric
/// values are used.
/// </remarks>
public sealed class BlobPrimitiveConverter : IBlobConverter
{
    #region Public Methods

    /// <inheritdoc/>
    /// <param name="type">The type to check for handling capability.</param>
    /// <returns>
    /// True if the type is a primitive, string, DateTime, TimeSpan, decimal, byte array, or enum; otherwise false. Nullable types are unwrapped before checking.
    /// </returns>
    public bool CanHandle(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is Type underlying)
        {
            type = underlying;
        }

        return type.IsPrimitive
            || type == typeof(string)
            || type == typeof(DateTime)
            || type == typeof(TimeSpan)
            || type == typeof(decimal)
            || type == typeof(byte[])
            || type.IsEnum;
    }

    /// <inheritdoc/>
    /// <param name="state">The reader state providing the underlying reader.</param>
    /// <param name="bundle">Converter bundle containing the target <see cref="Type"/>.</param>
    /// <returns>The deserialized value as an object.</returns>
    /// <exception cref="NotSupportedException">Thrown when the target type is not supported by this converter.</exception>
    public object ReadContent(IBlobReaderState state, BlobConverterBundle bundle)
    {
        var reader = state.Reader;
        var value = bundle.State switch
        {
            // primitives
            BlobPrimitiveType.Bool => reader.ReadBool(),
            BlobPrimitiveType.UInt8 => reader.ReadByte(),
            BlobPrimitiveType.Int8 => reader.ReadInt8(),
            BlobPrimitiveType.Int16 => reader.ReadInt16(),
            BlobPrimitiveType.UInt16 => reader.ReadUInt16(),
            BlobPrimitiveType.Int32 => reader.Read7BitEncodedInt32(),
            BlobPrimitiveType.UInt32 => reader.Read7BitEncodedUInt32(),
            BlobPrimitiveType.Int64 => reader.Read7BitEncodedInt64(),
            BlobPrimitiveType.UInt64 => reader.Read7BitEncodedUInt64(),
            BlobPrimitiveType.Float32 => reader.ReadSingle(),
            BlobPrimitiveType.Float64 => reader.ReadDouble(),
            BlobPrimitiveType.Char => reader.ReadChar(),

            // special allowed non-primitives
            BlobPrimitiveType.String => reader.ReadPrefixedString() ?? string.Empty,
            BlobPrimitiveType.Decimal => reader.ReadDecimal(),
            BlobPrimitiveType.DateTime => reader.ReadDateTime(),
            BlobPrimitiveType.TimeSpan => reader.ReadTimeSpan(),
            BlobPrimitiveType.ByteArray => reader.ReadBytes() ?? [],

            // enums (last allowed bucket)
            BlobPrimitiveType.Enum => EnumAsStrings ? Enum.Parse(bundle.Type, reader.ReadPrefixedString() ?? string.Empty) : Enum.ToObject(bundle.Type, reader.Read7BitEncodedUInt64()),

            _ => throw new NotSupportedException($"Type '{bundle.Type}' is not supported")
        };
        return value;
    }

    /// <inheritdoc/>
    /// <param name="state">The reader state.</param>
    /// <param name="bundle">The converter bundle for the type to initialize.</param>
    /// <remarks>This converter does not require initialization; method is provided to satisfy the interface.</remarks>
    public void ReadInitialization(IBlobReaderState state, BlobConverterBundle bundle)
    {
        var elementType = Nullable.GetUnderlyingType(bundle.Type) is Type underlying ? underlying : bundle.Type;
        bundle.State = state.GetPrimitiveType(elementType, out var primitiveType) ? primitiveType : throw new InvalidOperationException($"Type '{bundle.Type}' is not a supported primitive type!");
    }

    /// <inheritdoc/>
    /// <param name="state">The writer state providing the underlying writer.</param>
    /// <param name="bundle">Converter bundle containing the target <see cref="Type"/>.</param>
    /// <param name="instance">The value to serialize.</param>
    /// <exception cref="NotSupportedException">Thrown when the target type is not supported by this converter.</exception>
    public void WriteContent(IBlobWriterState state, BlobConverterBundle bundle, object instance)
    {
        var writer = state.Writer;
        switch (bundle.State)
        {
            // primitives
            case BlobPrimitiveType.Bool: writer.Write((bool)instance); break;
            case BlobPrimitiveType.UInt8: writer.Write((byte)instance); break;
            case BlobPrimitiveType.Int8: writer.Write((sbyte)instance); break;
            case BlobPrimitiveType.Int16: writer.Write((short)instance); break;
            case BlobPrimitiveType.UInt16: writer.Write((ushort)instance); break;
            case BlobPrimitiveType.Int32: writer.Write7BitEncoded32((int)instance); break;
            case BlobPrimitiveType.UInt32: writer.Write7BitEncoded32((uint)instance); break;
            case BlobPrimitiveType.Int64: writer.Write7BitEncoded64((long)instance); break;
            case BlobPrimitiveType.UInt64: writer.Write7BitEncoded64((ulong)instance); break;
            case BlobPrimitiveType.Float32: writer.Write((float)instance); break;
            case BlobPrimitiveType.Float64: writer.Write((double)instance); break;
            case BlobPrimitiveType.Char: writer.Write((char)instance); break;
            // special allowed types
            case BlobPrimitiveType.String: writer.WritePrefixed((string)instance); break;
            case BlobPrimitiveType.Decimal: writer.Write((decimal)instance); break;
            case BlobPrimitiveType.DateTime: writer.Write((DateTime)instance); break;
            case BlobPrimitiveType.TimeSpan: writer.Write((TimeSpan)instance); break;
            case BlobPrimitiveType.ByteArray: writer.WritePrefixed((byte[])instance); break;

            // enums
            case BlobPrimitiveType.Enum:
            {
                if (EnumAsStrings)
                {
                    writer.WritePrefixed(instance.ToString()); break;
                }
                else
                {
                    writer.Write7BitEncoded64(Convert.ToUInt64(instance)); break;
                }
            }

            default: throw new NotSupportedException($"Type '{bundle.Type}' is not supported");
        }
    }

    /// <inheritdoc/>
    /// <param name="state">The writer state.</param>
    /// <param name="bundle">The converter bundle for the type to initialize.</param>
    /// <remarks>This converter does not require initialization; method is provided to satisfy the interface.</remarks>
    public void WriteInitialization(IBlobWriterState state, BlobConverterBundle bundle)
    {
        var elementType = Nullable.GetUnderlyingType(bundle.Type) is Type underlying ? underlying : bundle.Type;
        bundle.State = state.GetPrimitiveType(elementType, out var primitiveType) ? primitiveType : throw new InvalidOperationException($"Type '{bundle.Type}' is not a supported primitive type!");
    }

    /// <summary>When true, enums are serialized and deserialized as their name strings. When false, enums are handled by their numeric value.</summary>
    public bool EnumAsStrings { get; set; }

    #endregion Public Methods
}
