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
    internal readonly BlobConverterBundle? ElementConverterBundle;

    internal readonly BlobEnumerableConverterData Data;

    #endregion Fields

    #region Public Constructors

    /// <summary>Initializes a new instance with the specified type, constructor, and element converter bundle.</summary>
    /// <param name="type">Target enumerable type.</param>
    /// <param name="elementConverterBundle">Converter bundle for elements.</param>
    /// <param name="data">Converter data for the enumerable type.</param>
    public BlobEnumerableConverterState(Type type, BlobConverterBundle? elementConverterBundle, BlobEnumerableConverterData data)
    {
        Data = data;
        ElementConverterBundle = elementConverterBundle;
        Constructor = data.Constructor is null ? null : new(data.Constructor);
        AcceptArray = type.IsAssignableFrom(data.ArrayType);
        if (!AcceptArray && Constructor is null) throw new InvalidOperationException($"Type {type.FullName} does not accept an array and does not have a suitable constructor for deserialization!");
        if (data.PrimitiveType == default && ElementConverterBundle is null) throw new ArgumentNullException(nameof(elementConverterBundle), $"Type {type.ToShortName()} requires an {nameof(ElementConverterBundle)}!");
    }

    #endregion Public Constructors
}
