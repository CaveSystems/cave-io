using System;
using System.Diagnostics;

namespace Cave.IO.Blob;

/// <summary>Bundle for a blob converter and its metadata.</summary>
/// <remarks>Holds converter instance, unique id, target type, and optional state for blob serialization.</remarks>
[DebuggerDisplay("BlobConverterBundle Id = {Id}, Type = {Type.ToShortName()}, Converter = {Converter}, State = {State}")]
public sealed class BlobConverterBundle
{
    #region Fields

    /// <summary>Blob converter instance.</summary>
    public readonly IBlobConverter Converter;

    /// <summary>Unique id for this bundle.</summary>
    public readonly uint Id;

    /// <summary>Target type for the converter.</summary>
    public readonly Type Type;

    /// <summary>Optional state for this bundle.</summary>
    public object? State;

    #endregion Fields

    #region Public Constructors

    /// <summary>Creates a new <see cref="BlobConverterBundle"/>.</summary>
    /// <param name="id">Unique id.</param>
    /// <param name="type">Target type.</param>
    /// <param name="converter">Blob converter instance.</param>
    public BlobConverterBundle(uint id, Type type, IBlobConverter converter)
    {
        Id = id;
        Type = type;
        Converter = converter;
    }

    #endregion Public Constructors

    #region Public Methods

    /// <summary>Returns string with id, type, and converter info.</summary>
    /// <returns>Short info string.</returns>
    public override string ToString() => $"BlobConverterBundle Id = {Id}, Type = {Type.ToShortName()}, Converter = {Converter.GetType().ToShortName()}";

    #endregion Public Methods
}
