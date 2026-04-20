using System;
using System.Diagnostics.CodeAnalysis;

namespace Cave.IO.Blob;

/// <summary>
/// Defines a factory for creating and resolving <see cref="IBlobConverter"/> instances appropriate for serializing and deserializing types to and from a binary
/// blob format.
/// </summary>
public interface IBlobConverterFactory
{
    #region Public Methods

    /// <summary>
    /// Attempts to resolve the most appropriate <see cref="IBlobConverter"/> for the specified <paramref name="type"/>. The factory evaluates available
    /// converters in order of precedence: convertible, primitive, string-parse, dictionary, enumerable, and finally reflection-based conversion.
    /// </summary>
    /// <param name="serializer">The <see cref="BlobSerializer"/> providing the serialization context.</param>
    /// <param name="type">The <see cref="Type"/> for which a converter should be resolved.</param>
    /// <param name="converter">When this method returns <see langword="true"/>, contains the resolved <see cref="IBlobConverter"/>; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a suitable converter was found or created; otherwise <see langword="false"/>.</returns>
    bool TryCreateConverter(BlobSerializer serializer, Type type, [MaybeNullWhen(false)] out IBlobConverter converter);

    #endregion Public Methods
}
