using System;
using System.IO;

namespace Cave.IO.Blob.Converters;

/// <summary>
/// An <see cref="IBlobConverter"/> that delegates serialization to types that implement <see cref="IBlobConvertible"/>, allowing them to fully control their
/// own binary representation.
/// </summary>
/// <remarks>
/// This converter requires no initialization data or state in the binary stream. On write, it calls <see cref="IBlobConvertible.ToBlob"/> and writes the
/// resulting byte array as a length-prefixed block. On read, it creates a new instance via <see cref="Activator.CreateInstance(Type)"/>, reads the
/// length-prefixed byte array, and passes it to <see cref="IBlobConvertible.FromBlob"/>.
/// </remarks>
sealed class BlobConvertibleConverter : IBlobConverter
{
    #region Public Methods

    /// <inheritdoc/>
    public bool CanHandle(Type type) => BlobSerializer.IBlobConvertibleType.IsAssignableFrom(type);

    /// <inheritdoc/>
    public object ReadContent(IBlobReaderState state, BlobConverterBundle bundle)
    {
        var instance = (IBlobConvertible)Activator.CreateInstance(bundle.Type)!;
        var blob = state.Reader.ReadBytes() ?? throw new InvalidDataException($"Could not read blob for type {bundle.Type.Name}.");
        return instance.FromBlob(blob);
    }

    /// <inheritdoc/>
    public void ReadInitialization(IBlobReaderState state, BlobConverterBundle bundle) { }

    /// <inheritdoc/>
    public void WriteContent(IBlobWriterState state, BlobConverterBundle bundle, object instance)
    {
        var convertible = (IBlobConvertible)instance;
        state.Writer.WritePrefixed(convertible.ToBlob());
    }

    /// <inheritdoc/>
    public void WriteInitialization(IBlobWriterState state, BlobConverterBundle bundle) { }

    #endregion Public Methods
}
