using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cave.IO.Blob.Converters;

/// <summary>Holds per-type data for positional records.</summary>
internal sealed record BlobPositionalRecordConverterData : BaseRecord
{
    #region Fields

    /// <summary>Record constructor.</summary>
    internal readonly ConstructorInfo Constructor;

    /// <summary>Distinct element types.</summary>
    internal readonly IList<Type> ElementTypes;

    /// <summary>Ordered member metadata.</summary>
    internal IList<BlobPositionalRecordConverterMember> Members = [];

    /// <summary>Constructor parameters.</summary>
    internal readonly ParameterInfo[] Parameters;

    /// <summary>Parameter count.</summary>
    internal readonly uint ParameterCount;

    /// <summary>Readable properties in constructor order.</summary>
    internal readonly PropertyInfo[] Properties;

    /// <summary>Serialized member count.</summary>
    internal uint SerializedMemberCount;

    #endregion Fields

    #region Public Constructors

    /// <summary>Initializes a new instance.</summary>
    /// <param name="type">Target type.</param>
    /// <param name="constructor">Record constructor.</param>
    /// <param name="parameters">Constructor parameters.</param>
    /// <param name="properties">Readable properties in constructor order.</param>
    public BlobPositionalRecordConverterData(Type type, ConstructorInfo constructor, ParameterInfo[] parameters, PropertyInfo[] properties)
    {
        Constructor = constructor;
        Parameters = parameters;
        Properties = properties;
        ParameterCount = (uint)parameters.Length;
        SerializedMemberCount = ParameterCount;
        ElementTypes = properties.Select(p => p.PropertyType).Distinct().ToArray();
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    public override string ToString() => $"{ParameterCount} parameters, {ElementTypes.Count} element types";

    #endregion Public Methods
}
