using System;

namespace Cave.IO.Blob.Converters;

/// <summary>Handles serialization of marshal-compatible structures using <see cref="MarshalStruct"/>.</summary>
public class BlobMarshalStructConverter : IBlobConverter
{
    #region Public Methods

    /// <inheritdoc/>
    public virtual bool CanHandle(Type type) => MarshalStruct.IsBlittable(type);

    /// <inheritdoc/>
    public virtual object ReadContent(IBlobReaderState state, BlobConverterBundle bundle)
    {
        var type = bundle.Type;
        var binSize = state.Reader.Read7BitEncodedInt32();
        if (binSize == 0) return null!;
        var realSize = MarshalStruct.SizeOf(type);
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
    public virtual void ReadInitialization(IBlobReaderState state, BlobConverterBundle bundle) { }

    /// <inheritdoc/>
    public virtual void WriteContent(IBlobWriterState state, BlobConverterBundle bundle, object instance)
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
    public virtual void WriteInitialization(IBlobWriterState state, BlobConverterBundle bundle) { }

    #endregion Public Methods
}
