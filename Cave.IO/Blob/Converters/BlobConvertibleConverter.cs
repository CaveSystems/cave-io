using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Cave.IO.Blob.Converters;

/// <summary>
/// An <see cref="IBlobConverter"/> that delegates serialization to types that implement <see cref="IBlobConvertible"/>, allowing them to fully control their
/// own binary representation.
/// </summary>
/// <remarks>
/// This converter requires no initialization data or state in the binary stream. On write, it calls <see cref="IBlobConvertible.ToBlob"/> and writes the
/// resulting byte array as a length-prefixed block. On read, it creates a new instance via <see cref="TypeActivator.CreateFast(Type)"/>, reads the
/// length-prefixed byte array, and passes it to <see cref="IBlobConvertible.FromBlob"/>.
/// </remarks>
sealed class BlobConvertibleConverter : BlobConverterBase
{
    #region Protected Methods

    protected override object? GetCanHandleCache(Type type) => IBlobConvertibleType.IsAssignableFrom(type) ? type : null;

    #endregion Protected Methods

    #region Properties

    /// <summary>
    /// Gets the <see cref="Type"/> object representing the <see cref="IBlobConvertible"/> interface, used for fast type-compatibility checks during serialization.
    /// </summary>
    public static Type IBlobConvertibleType { get; } = typeof(IBlobConvertible);

    #endregion Properties

    #region Public Methods

    /// <inheritdoc/>
    public override IList<Type> GetContentTypes(Type type) => [];

    /// <inheritdoc/>
    public override object ReadContent(IBlobReaderState state, BlobConverterBundle bundle)
    {
        var instance = (IBlobConvertible)TypeActivator.CreateFast(bundle.Type);
        var blob = state.Reader.ReadBytes() ?? throw new InvalidDataException($"Could not read blob for type {bundle.Type.Name}.");
        return instance.FromBlob(blob);
    }

    /// <inheritdoc/>
    public override void ReadInitialization(IBlobReaderState state, BlobConverterBundle bundle) { }

    /// <inheritdoc/>
    public override void WriteContent(IBlobWriterState state, BlobConverterBundle bundle, object instance)
    {
        var convertible = (IBlobConvertible)instance;
        state.Writer.WritePrefixed(convertible.ToBlob());
    }

    /// <inheritdoc/>
    public override void WriteInitialization(IBlobWriterState state, BlobConverterBundle bundle) { }

    #endregion Public Methods
}
