using System;
using System.Reflection;

namespace Cave.IO.Blob.Converters;

/// <summary>Holds state for enumerable blob converters, including constructor and element converter information.</summary>
sealed class BlobEnumerableConverterState
{
    #region Fields

    /// <summary>Gets whether the target type accepts arrays of the element type.</summary>
    internal readonly bool AcceptArray;

    /// <summary>Gets the constructor for the target type.</summary>
    internal readonly ConstructorCache? Constructor;

    /// <summary>Gets the converter bundle for the element type.</summary>
    internal readonly BlobConverterBundle ElementConverterBundle;

    #endregion Fields

    #region Public Constructors

    /// <summary>Initializes a new instance with the specified type, constructor, and element converter bundle.</summary>
    /// <param name="type">Target enumerable type.</param>
    /// <param name="constructor">Constructor for the target type.</param>
    /// <param name="elementConverterBundle">Converter bundle for elements.</param>
    public BlobEnumerableConverterState(Type type, ConstructorInfo? constructor, BlobConverterBundle elementConverterBundle)
    {
        ElementConverterBundle = elementConverterBundle;
        Constructor = constructor is null ? null : new(constructor);
        AcceptArray = type.IsAssignableFrom(elementConverterBundle.Type.MakeArrayType());
        if (!AcceptArray && Constructor is null) throw new InvalidOperationException($"Type {type.FullName} does not accept an array and does not have a suitable constructor for deserialization!");
    }

    #endregion Public Constructors
}
