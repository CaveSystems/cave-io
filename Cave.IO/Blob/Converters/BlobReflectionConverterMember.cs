using System;
using System.Reflection;

namespace Cave.IO.Blob.Converters;

/// <summary>Metadata for a serializable member (field or property).</summary>
internal sealed class BlobReflectionConverterMember
{
    #region Fields

    /// <summary>Converter bundle for (de)serializing the member value.</summary>
    internal readonly BlobConverterBundle Bundle;

    /// <summary>Reflection info for the member ( <see cref="FieldInfo"/> or <see cref="PropertyInfo"/>).</summary>
    internal readonly MemberInfo Member;

    /// <summary>Setter delegate for assigning deserialized values.</summary>
    internal readonly Action<object, object> Setter;

    #endregion Fields

    #region Public Constructors

    /// <summary>Creates a new <see cref="BlobReflectionConverterMember"/>.</summary>
    /// <param name="memberInfo">Reflection info for the member.</param>
    /// <param name="setMethod">Delegate to set the member value.</param>
    /// <param name="bundle">Converter bundle for the member.</param>
    public BlobReflectionConverterMember(MemberInfo memberInfo, Action<object, object> setMethod, BlobConverterBundle bundle)
    {
        Member = memberInfo;
        Setter = setMethod;
        Bundle = bundle;
    }

    #endregion Public Constructors
}
