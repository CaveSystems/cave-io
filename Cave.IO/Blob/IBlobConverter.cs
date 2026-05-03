using System;
using System.Collections.Generic;

namespace Cave.IO.Blob;

/// <summary>Defines a converter that can serialize and deserialize a specific type to and from a binary format using the provided BlobReaderState and BlobWriterState.</summary>
public interface IBlobConverter
{
    #region Public Methods

    /// <summary>Determines whether this converter can handle the specified type.</summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the converter can handle the type; otherwise, false.</returns>
    bool CanHandle(Type type);

    /// <summary>Retrieves a collection of content types needed to be serialized or deserialized by this converter.</summary>
    /// <remarks>This method can be used to get a list of types that the converter needs to handle during serialization or deserialization.</remarks>
    /// <param name="type">The type to check. This has to be checked against <see cref="CanHandle(Type)"/> first!</param>
    /// <returns>A list of <see cref="Type"/> objects representing the content types needed by this converter.</returns>
    IList<Type> GetContentTypes(Type type);

    /// <summary>Reads the content of the specified type from the binary format using the provided BlobReaderState.</summary>
    /// <param name="state">The BlobReaderState to use for reading.</param>
    /// <param name="bundle">The BlobConverterBundle to use for reading.</param>
    /// <returns>The deserialized instance.</returns>
    object ReadContent(IBlobReaderState state, BlobConverterBundle bundle);

    /// <summary>Reads the initialization data for the specified type from the binary format using the provided BlobReaderState.</summary>
    /// <param name="state">The BlobReaderState to use for reading.</param>
    /// <param name="bundle">The BlobConverterBundle to use for reading.</param>
    void ReadInitialization(IBlobReaderState state, BlobConverterBundle bundle);

    /// <summary>Writes the content of the specified instance to the binary format using the provided BlobWriterState.</summary>
    /// <param name="state">The BlobWriterState to use for writing.</param>
    /// <param name="bundle">The BlobConverterBundle to use for writing.</param>
    /// <param name="instance">The instance to write to.</param>
    void WriteContent(IBlobWriterState state, BlobConverterBundle bundle, object instance);

    /// <summary>Writes the initialization data for the specified type to the binary format using the provided BlobWriterState.</summary>
    /// <param name="writerState">The BlobWriterState to use for writing.</param>
    /// <param name="bundle">The BlobConverterBundle to use for writing.</param>
    void WriteInitialization(IBlobWriterState writerState, BlobConverterBundle bundle);

    #endregion Public Methods
}
