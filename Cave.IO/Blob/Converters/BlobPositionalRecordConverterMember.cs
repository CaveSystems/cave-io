using System.Reflection;

namespace Cave.IO.Blob.Converters;

/// <summary>Metadata for a positional record member.</summary>
internal sealed class BlobPositionalRecordConverterMember
{
    #region Fields

    /// <summary>Converter bundle for the member value.</summary>
    internal readonly BlobConverterBundle Bundle;

    /// <summary>Constructor parameter index.</summary>
    internal readonly int ParameterIndex;

    /// <summary>Constructor parameter.</summary>
    internal readonly ParameterInfo Parameter;

    /// <summary>Readable property.</summary>
    internal readonly PropertyInfo Property;

    #endregion Fields

    #region Public Constructors

    /// <summary>Creates a new instance.</summary>
    /// <param name="parameter">Constructor parameter.</param>
    /// <param name="parameterIndex">Constructor parameter index.</param>
    /// <param name="property">Readable property.</param>
    /// <param name="bundle">Converter bundle.</param>
    public BlobPositionalRecordConverterMember(ParameterInfo parameter, int parameterIndex, PropertyInfo property, BlobConverterBundle bundle)
    {
        Parameter = parameter;
        ParameterIndex = parameterIndex;
        Property = property;
        Bundle = bundle;
    }

    #endregion Public Constructors
}
