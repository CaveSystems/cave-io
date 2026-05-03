using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cave.IO.Blob.Converters;
using Cave.Logging;

namespace Cave.IO.Blob;

/// <summary>
/// Default <see cref="IBlobConverterFactory"/> for <see cref="BlobSerializer"/>. Resolves and creates <see cref="IBlobConverter"/> instances for CLR types.
/// </summary>
/// <remarks>
/// Converter resolution order:
/// <list type="number">
/// <item>
/// <description><see cref="IBlobConvertible"/> types via <see cref="BlobConvertibleConverter"/>.</description>
/// </item>
/// <item>
/// <description>Primitive types via <see cref="BlobPrimitiveConverter"/>.</description>
/// </item>
/// <item>
/// <description>Types with a <c>Parse</c> method via <see cref="BlobStringParseConverter"/>.</description>
/// </item>
/// <item>
/// <description>Dictionary-like types via <see cref="BlobDictionaryConverter"/>.</description>
/// </item>
/// <item>
/// <description>Enumerable types via <see cref="BlobEnumerableConverter"/>.</description>
/// </item>
/// <item>
/// <description>All remaining types via <see cref="BlobReflectionConverter"/>.</description>
/// </item>
/// </list>
/// </remarks>
public class BlobDefaultFactory : IBlobConverterFactory
{
    #region Public Methods

    /// <inheritdoc/>
    public virtual bool TryCreateConverter(BlobSerializer serializer, Type type, [MaybeNullWhen(false)] out IBlobConverter converter)
    {
        if (serializer.KnownConverters.TryGetValue(type, out converter))
        {
            Logger?.Verbose($"FastPath: Selecting known converter {converter.GetType().Name} for type {type.ToShortName()}");
            return true;
        }

        if (Converters.FirstOrDefault(Converters => Converters.CanHandle(type)) is IBlobConverter result)
        {
            converter = result;
        }
        else if (FallbackConverter.CanHandle(type))
        {
            converter = FallbackConverter;
        }
        else
        {
            Logger?.Warning($"No converter found for type {type.ToShortName()}");
            return false;
        }

        Logger?.Debug($"Selecting converter {converter.GetType().Name} for type {type.ToShortName()}");
        serializer.KnownTypes.Add(type.GetPortableTypeName(), type);
        serializer.KnownConverters.Add(type, converter);
        return true;
    }

    #endregion Public Methods

    #region Properties

    /// <summary>Initializes a new instance of the BlobDefaultFactory class with a predefined set of blob converters.</summary>
    /// <remarks>The default converters support a variety of common data types, enabling flexible blob serialization and deserialization out of the box.</remarks>
    public BlobDefaultFactory() => Converters =
        [
            new BlobPrimitiveConverter(),
            new BlobConvertibleConverter(),
            new BlobStringParseConverter(),
            new BlobBinaryConstructorConverter(),
            new BlobMarshalStructConverter(),
            new BlobDictionaryConverter(),
            new BlobEnumerableConverter(),
        ];

    /// <summary>Gets the collection of additional converters used for blob serialization and deserialization.</summary>
    /// <remarks>
    /// Use this collection to register custom converters that extend or override the default blob conversion behavior. Converters are applied in the order they
    /// appear in the collection. If no converter can handle a type, the <see cref="FallbackConverter"/> is used.
    /// </remarks>
    public ICollection<IBlobConverter> Converters { get; set; }

    /// <summary>Gets or sets the fallback converter used when no specific blob converter is available.</summary>
    /// <remarks>
    /// The fallback converter is used as a last resort to handle blob conversions that are not supported by other registered converters. Assign a custom
    /// implementation to change the default fallback behavior.
    /// </remarks>
    public IBlobConverter FallbackConverter { get; set; } = new BlobReflectionConverter();

    /// <summary>Gets or sets the logger used for converter selection. Created converters do not use this logger.</summary>
    public ILogger? Logger { get; set; }

    #endregion Properties
}
