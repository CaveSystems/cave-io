using System;
using System.Collections.Generic;

namespace Cave.IO.Blob.Converters;

/// <summary>Converts primitive CLR types, strings, DateTime, TimeSpan, decimal, byte[] and enums to and from a blob representation.</summary>
/// <remarks>
/// Supports nullable underlying types. When <see cref="EnumAsStrings"/> is true, enums are (de)serialized using their name strings; otherwise their numeric
/// values are used.
/// </remarks>
public sealed class BlobPrimitiveConverter : BlobConverterBase
{
    #region Protected Methods

    /// <inheritdoc/>
    protected override object? GetCanHandleCache(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is Type underlying)
        {
            type = underlying;
        }

        return BlobSerializer.GetPrimitiveType(type, out var primitiveType) ? primitiveType : null;
    }

    #endregion Protected Methods

    #region Public Methods

    /// <inheritdoc/>
    public override IList<Type> GetContentTypes(Type type) => [];

    /// <inheritdoc/>
    public override object ReadContent(IBlobReaderState state, BlobConverterBundle bundle)
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
            BlobPrimitiveType.DateTimeOffset => new DateTimeOffset(reader.Read7BitEncodedInt64(), reader.ReadTimeSpan()),

            // enums (last allowed bucket)
            BlobPrimitiveType.Enum => EnumAsStrings ? Enum.Parse(bundle.Type, reader.ReadPrefixedString() ?? string.Empty) : Enum.ToObject(bundle.Type, reader.Read7BitEncodedUInt64()),

            _ => throw new NotSupportedException($"Type '{bundle.Type}' is not supported")
        };
        return value;
    }

    /// <inheritdoc/>
    public override void ReadInitialization(IBlobReaderState state, BlobConverterBundle bundle)
    {
        GetHandlingData(bundle.Type, out BlobPrimitiveType primitiveType);
        bundle.State = primitiveType;
    }

    /// <inheritdoc/>
    /// <param name="state">The writer state providing the underlying writer.</param>
    /// <param name="bundle">Converter bundle containing the target <see cref="Type"/>.</param>
    /// <param name="instance">The value to serialize.</param>
    /// <exception cref="NotSupportedException">Thrown when the target type is not supported by this converter.</exception>
    public override void WriteContent(IBlobWriterState state, BlobConverterBundle bundle, object instance)
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
            case BlobPrimitiveType.DateTimeOffset:
            {
                var dto = (DateTimeOffset)instance;
                writer.Write7BitEncoded64(dto.Ticks);
                writer.Write(dto.Offset);
                break;
            }

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
    public override void WriteInitialization(IBlobWriterState state, BlobConverterBundle bundle)
    {
        GetHandlingData(bundle.Type, out BlobPrimitiveType primitiveType);
        bundle.State = primitiveType;
    }

    /// <summary>
    /// Gets or sets a value indicating whether enums are serialized and deserialized as their name strings. When false, enums are handled by their numeric value.
    /// </summary>
    public static bool EnumAsStrings { get; set; }

    #endregion Public Methods
}
