using System;
using System.Linq;
using System.Reflection;

namespace Cave.IO.Blob.Converters;

/// <summary>Stores reflection members required for binary conversion of a specific type.</summary>
sealed class BlobBinaryConstructorConverterState
{
    #region Fields

    /// <summary>Gets the <c>.ctor(byte[])</c> constructor of the type.</summary>
    public readonly ConstructorInfo Constructor;

    /// <summary>Gets the <c>ToBinary()</c> method of the type.</summary>
    public readonly MethodInfo ToByteArrayMethod;

    #endregion Fields

    #region Public Constructors

    /// <summary>Initializes reflection members for a type with <c>ToBinary()</c> and <c>.ctor(byte[])</c>.</summary>
    /// <param name="type">Type to analyze.</param>
    /// <exception cref="InvalidOperationException">Thrown when required conversion members are missing.</exception>
    public BlobBinaryConstructorConverterState(Type type)
    {
        ToByteArrayMethod =
            type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).
            FirstOrDefault(m => m.Name == "ToByteArray" && m.ReturnParameter.ParameterType == typeof(byte[]) && m.GetParameters().Length == 0) ??
            throw new InvalidOperationException("Could not find a valid ToByteArray() method!");
        Constructor =
            type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).
            FirstOrDefault(c => c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType == typeof(byte[])) ??
            throw new InvalidOperationException("Could not find a valid Constructor(byte[])!");
    }

    #endregion Public Constructors
}
