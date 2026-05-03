using System;
using System.Collections.Generic;

namespace Cave.IO.Blob.Converters;

/// <summary>Base class for blob converters providing a caching mechanism for supported types to optimize the expensive reflection-based type handling checks.</summary>
public abstract class BlobConverterBase : IBlobConverter
{
    #region Fields

    /// <summary>
    /// Cache for supported types. The key is the type, and the value is either converter-specific data or <see langword="null"/> if the type is not supported.
    /// </summary>
    readonly Dictionary<Type, object?> supportedTypes = new();

    #endregion Fields

    #region Protected Methods

    /// <summary>
    /// Implemented in derived classes to perform the actual reflection-based checks to determine if the converter can handle the specified type. The result is
    /// stored in the <see cref="supportedTypes"/> cache for future reference.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>The converter-specific data if the type can be handled; otherwise, <see langword="null"/>.</returns>
    protected abstract object? GetCanHandleCache(Type type);

    /// <summary>Attempts to get the converter-specific data for the specified type.</summary>
    /// <typeparam name="TContent">The type of the converter-specific data.</typeparam>
    /// <param name="type">The type to check.</param>
    /// <param name="content">The converter-specific data if found; otherwise, the default value of <typeparamref name="TContent"/>.</param>
    /// <returns><c>true</c> if the converter-specific data is found; otherwise, <c>false</c>.</returns>
    protected void GetHandlingData<TContent>(Type type, out TContent content)
    {
        if (supportedTypes.TryGetValue(type, out var data) && data is TContent result)
        {
            content = result;
            return;
        }
        throw new InvalidOperationException($"The converter does not support the type '{type.FullName}'.");
    }

    #endregion Protected Methods

    #region Public Methods

    /// <inheritdoc/>
    /// <remarks>
    /// To speed up the expensive reflection methods needed to determine if a type can be handled, this method uses the <see cref="supportedTypes"/> dictionary
    /// as a cache.
    /// </remarks>
    public bool CanHandle(Type type)
    {
        if (!supportedTypes.TryGetValue(type, out var data))
        {
            data = GetCanHandleCache(type);
            supportedTypes[type] = data;
        }
        return data != null;
    }

    /// <inheritdoc/>
    public abstract IList<Type> GetContentTypes(Type type);

    /// <inheritdoc/>
    public abstract object ReadContent(IBlobReaderState state, BlobConverterBundle bundle);

    /// <inheritdoc/>
    public abstract void ReadInitialization(IBlobReaderState state, BlobConverterBundle bundle);

    /// <inheritdoc/>
    public abstract void WriteContent(IBlobWriterState state, BlobConverterBundle bundle, object instance);

    /// <inheritdoc/>
    public abstract void WriteInitialization(IBlobWriterState writerState, BlobConverterBundle bundle);

    #endregion Public Methods
}
