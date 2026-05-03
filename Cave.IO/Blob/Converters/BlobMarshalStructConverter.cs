using System;
using System.Collections.Generic;

namespace Cave.IO.Blob.Converters;

/// <summary>Handles serialization of marshal-compatible structures using <see cref="MarshalStruct"/>.</summary>
public class BlobMarshalStructConverter : BlobConverterBase
{
    #region Protected Methods

    /// <inheritdoc/>
    protected override object? GetCanHandleCache(Type type) => MarshalStruct.IsBlittable(type) ? MarshalStruct.SizeOf(type) : null;

    #endregion Protected Methods

    #region Public Methods

    /// <inheritdoc/>
    public override IList<Type> GetContentTypes(Type type) => [];

    /// <inheritdoc/>
    public override object ReadContent(IBlobReaderState state, BlobConverterBundle bundle)
    {
        GetHandlingData(bundle.Type, out int realSize);
        var type = bundle.Type;
        var binSize = state.Reader.Read7BitEncodedInt32();
        if (binSize == 0) return null!;
        var buffer = state.Reader.ReadBytes(binSize);
        if (realSize > buffer.Length)
        {
            //extended structure
            Array.Resize(ref buffer, realSize);
        }
        MarshalStruct.Copy(type, buffer, out var result);
        return result;
    }

    /// <inheritdoc/>
    public override void ReadInitialization(IBlobReaderState state, BlobConverterBundle bundle) { }

    /// <inheritdoc/>
    public override void WriteContent(IBlobWriterState state, BlobConverterBundle bundle, object instance)
    {
        var writer = state.Writer;
        if (instance is null)
        {
            writer.Write((byte)0);
            return;
        }
        MarshalStruct.Copy(instance, out var buffer);
        writer.WritePrefixed(buffer);
    }

    /// <inheritdoc/>
    public override void WriteInitialization(IBlobWriterState state, BlobConverterBundle bundle) { }

    #endregion Public Methods
}
