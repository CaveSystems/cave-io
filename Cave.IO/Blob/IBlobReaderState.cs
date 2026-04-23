using System;

namespace Cave.IO.Blob;

/// <summary>Represents read state for blob deserialization.</summary>
/// <remarks>
/// <para><see cref="IBlobReaderState"/> extends <see cref="IBlobState"/> with reader-specific members used to deserialize values from a binary blob stream.</para>
/// <para>Implementations are usually created by <see cref="BlobSerializer"/> and passed to <see cref="IBlobConverter"/> instances during deserialization.</para>
/// </remarks>
public interface IBlobReaderState : IBlobState
{
    #region Public Methods

    /// <summary>Reads the next value from the binary stream.</summary>
    /// <typeparam name="TContent">The expected value type.</typeparam>
    /// <param name="instance">Receives the deserialized value, or <see langword="null"/> if a null reference was encoded.</param>
    void Read<TContent>(out TContent? instance);

    /// <summary>Reads the next value from the binary stream.</summary>
    /// <returns>The deserialized value, or <see langword="null"/> if a null reference was encoded.</returns>
    object? Read();

    /// <summary>Reads a <see cref="Type"/> definition from the binary stream.</summary>
    /// <returns>The resolved <see cref="Type"/>.</returns>
    /// <exception cref="System.IO.InvalidDataException">Thrown if the stream does not contain a valid type name.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the encoded type cannot be resolved or generic type metadata is invalid.</exception>
    Type ReadTypeDefitition();

    /// <summary>Closes the reader and validates the end marker.</summary>
    void Close();

    /// <summary>Closes the reader and optionally validates the end marker.</summary>
    /// <param name="validateEndMark">If <see langword="true"/>, validates the end marker.</param>
    void Close(bool validateEndMark);

    /// <summary>Reads or resolves the next converter bundle from the binary stream.</summary>
    /// <param name="knownId">An optional converter identifier. If <see langword="null"/>, the identifier is read from the stream.</param>
    /// <returns>The resolved <see cref="BlobConverterBundle"/>.</returns>
    BlobConverterBundle ReadConverter(uint? knownId = null);

    #endregion Public Methods

    #region Properties

    /// <summary>Gets a value indicating whether the end of the binary stream has been reached.</summary>
    bool IsCompleted { get; }

    /// <summary>Gets the <see cref="DataReader"/> used for binary input.</summary>
    DataReader Reader { get; }

    #endregion Properties
}
