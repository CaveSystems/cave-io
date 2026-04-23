using System;

namespace Cave.IO.Blob;

/// <summary>Represents write state for blob serialization.</summary>
/// <remarks>
/// <para><see cref="IBlobWriterState"/> extends <see cref="IBlobState"/> with writer-specific members used to serialize values into a binary blob stream.</para>
/// <para>Implementations are usually created by <see cref="BlobSerializer"/> and passed to <see cref="IBlobConverter"/> instances during serialization.</para>
/// </remarks>
public interface IBlobWriterState : IBlobState
{
    #region Public Methods

    /// <summary>Closes the resource and releases any associated resources.</summary>
    /// <remarks>
    /// Call this method when the resource is no longer needed to ensure proper cleanup. After calling this method, further operations on the resource may throw exceptions.
    /// </remarks>
    void Close();

    /// <summary>Writes the specified value to the binary stream.</summary>
    /// <param name="instance">The value to serialize, or <see langword="null"/> to write a null reference.</param>
    void Write(object? instance);

    /// <summary>Writes or resolves the converter bundle for the specified <see cref="Type"/>.</summary>
    /// <param name="type">The <see cref="Type"/> for which the converter bundle is required.</param>
    /// <returns>The resolved <see cref="BlobConverterBundle"/>.</returns>
    BlobConverterBundle WriteConverter(Type type);

    /// <summary>Writes a <see cref="Type"/> definition to the binary stream.</summary>
    /// <param name="type">The <see cref="Type"/> to write.</param>
    void WriteTypeDefition(Type type);

    #endregion Public Methods

    #region Properties

    /// <summary>Gets the <see cref="DataWriter"/> used for binary output.</summary>
    DataWriter Writer { get; }

    #endregion Properties
}
