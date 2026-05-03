using System;
using Cave.Logging;

namespace Cave.IO.Blob;

/// <summary>Represents shared state for a single blob serialization or deserialization operation.</summary>
/// <remarks>
/// An instance implementing <see cref="IBlobState"/> is provided to <see cref="IBlobConverter"/> implementations during the binary serialization pipeline. It
/// exposes the owning <see cref="BlobSerializer"/>, an optional <see cref="ILogger"/> for diagnostics, the available <see cref="BlobConverterRegistry"/>, and
/// helpers for resolving primitive type mappings.
/// </remarks>
public interface IBlobState
{
    #region Properties

    /// <summary>Gets the registry of available blob converters for the current operation. Use this registry to look up converters for specific CLR types.</summary>
    BlobConverterRegistry Converters { get; }

    #endregion Properties

    #region Properties

    /// <summary>
    /// Gets the optional <see cref="ILogger"/> used for diagnostic messages. May be <see langword="null"/> when logging is disabled or no debugger is attached.
    /// </summary>
    ILogger? Logger { get; }

    /// <summary>Gets the <see cref="BlobSerializer"/> that owns and controls the current operation.</summary>
    BlobSerializer Serializer { get; }

    #endregion Properties
}
