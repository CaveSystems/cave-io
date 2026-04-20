using System;
using Cave.Logging;

namespace Cave.IO.Blob;

/// <summary>
/// Represents shared state for a single blob serialization or deserialization operation.
/// </summary>
/// <remarks>
/// An instance implementing <see cref="IBlobState"/> is provided to <see cref="IBlobConverter"/>
/// implementations during the binary serialization pipeline. It exposes the owning
/// <see cref="BlobSerializer"/>, an optional <see cref="ILogger"/> for diagnostics, the
/// available <see cref="BlobConverterRegistry"/>, and helpers for resolving primitive type mappings.
/// </remarks>
public interface IBlobState
{
    /// <summary>
    /// Gets the registry of available blob converters for the current operation.
    /// Use this registry to look up converters for specific CLR types.
    /// </summary>
    BlobConverterRegistry Converters { get; }

    #region Public Methods

    /// <summary>
    /// Resolves the CLR <see cref="Type"/> that corresponds to the specified <see cref="BlobPrimitiveType"/>.
    /// </summary>
    /// <param name="typeCode">The primitive type code to resolve.</param>
    /// <returns>The CLR <see cref="Type"/> that maps to <paramref name="typeCode"/>.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="typeCode"/> does not map to a known CLR type.
    /// </exception>
    Type GetPrimitiveType(BlobPrimitiveType typeCode);

    /// <summary>
    /// Attempts to determine the <see cref="BlobPrimitiveType"/> for a given CLR <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The CLR type to evaluate.</param>
    /// <param name="primitiveType">
    /// When this method returns <see langword="true"/>, contains the matching <see cref="BlobPrimitiveType"/>;
    /// otherwise contains <see cref="BlobPrimitiveType.Unsupported"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="type"/> maps to a supported primitive type; otherwise <see langword="false"/>.
    /// </returns>
    bool GetPrimitiveType(Type type, out BlobPrimitiveType primitiveType);

    /// <summary>
    /// Closes the resource and releases any associated resources.
    /// </summary>
    /// <remarks>Call this method when the resource is no longer needed to ensure proper cleanup. After
    /// calling this method, further operations on the resource may throw exceptions.</remarks>
    void Close();

    #endregion Public Methods

    #region Properties

    /// <summary>
    /// Gets the optional <see cref="ILogger"/> used for diagnostic messages.
    /// May be <see langword="null"/> when logging is disabled or no debugger is attached.
    /// </summary>
    ILogger? Logger { get; }

    /// <summary>
    /// Gets the <see cref="BlobSerializer"/> that owns and controls the current operation.
    /// </summary>
    BlobSerializer Serializer { get; }

    #endregion Properties
}
