using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Cave.IO;

/// <summary>
/// Provides a cache for <see cref="ArrayGetter"/> instances, allowing for efficient retrieval of fast array getters for different element types without
/// redundant dynamic method generation.
/// </summary>
public class ArrayGetterCache
{
    #region Private Fields

    readonly Dictionary<Type, ArrayGetter> cache = new();

    #endregion Private Fields

    #region Public Methods

    /// <summary>Gets a fast array getter for the specified element type, using a cache to avoid redundant dynamic method generation.</summary>
    [MethodImpl(256)]
    public ArrayGetter Get(Type elementType)
    {
        if (!cache.TryGetValue(elementType, out var getter))
        {
            getter = ArrayGetterFactory.CreateGetter(elementType);
            cache[elementType] = getter;
        }
        return getter;
    }

    #endregion Public Methods
}
