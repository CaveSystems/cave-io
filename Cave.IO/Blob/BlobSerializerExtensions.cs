using Cave.IO.Blob.Converters;

namespace Cave.IO.Blob;

/// <summary>Provides extension methods for <see cref="BlobSerializer"/> to simplify registration of common converters.</summary>
public static class BlobSerializerExtensions
{
    #region Public Methods

    /// <summary>Provides a convenient method to register a <see cref="BlobReflectionConverter"/> for a specific type.</summary>
    /// <param name="serializer">The serializer instance to register the converter with.</param>
    /// <typeparam name="TType">The type for which to register the reflection converter.</typeparam>
    /// <param name="flags">Flags to control the behavior of the reflection converter.</param>
    public static void RegisterReflectionConverter<TType>(this BlobSerializer serializer, BlobConverterFlags flags = default)
    {
        var type = typeof(TType);
        serializer.Register(type, new BlobReflectionConverter(type, flags));
    }

    /// <summary>
    /// Provides a convenient method to register a <see cref="BlobStringParseConverter{TType}"/> for a specific type, using a provided test value to ensure
    /// correct round-trip.
    /// </summary>
    /// <remarks>This can be used for various framework types not providing a Parse(string) method. Examples are Uri, Version, BigInteger</remarks>
    /// <typeparam name="TType">The type for which to register the string parse converter.</typeparam>
    /// <param name="serializer">The serializer instance to register the converter with.</param>
    /// <param name="roundtripTestValue">Value used to verify successful roundtrip conversion.</param>
    /// <param name="allowConstructor">
    /// If set to <c>true</c>, the converter will attempt to use a constructor that accepts a string if no suitable <c>Parse</c> method is found.
    /// </param>
    public static void RegisterStringParseConverter<TType>(this BlobSerializer serializer, TType roundtripTestValue, bool allowConstructor = false)
    {
        var converter = new BlobStringParseConverter<TType>(roundtripTestValue, allowConstructor);
        serializer.Register(typeof(TType), converter);
    }

    #endregion Public Methods
}
