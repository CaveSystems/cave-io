using System;
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

    #region Properties

    /// <inheritdoc/>
    public BlobSerializer Serializer { get; }

    /// <inheritdoc/>
    public ILogger? Logger => Serializer.Logger;

    #endregion Properties
}
