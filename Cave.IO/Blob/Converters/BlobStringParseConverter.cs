using System;
using System.Collections.Generic;
using System.Globalization;

namespace Cave.IO.Blob.Converters;

/// <summary>
/// An <see cref="IBlobConverter"/> that serializes and deserializes types that expose a <c>Parse</c> method. On write, the value is converted to a string via
/// <see cref="IFormattable"/> or <see cref="object.ToString()"/>; on read, the stored string is passed to the resolved <c>Parse</c> method via reflection.
/// </summary>
/// <remarks>
/// Static <c>Parse</c> overloads are preferred over instance-based ones. Overloads that accept an <see cref="IFormatProvider"/> are preferred over those that
/// accept only a <see cref="string"/>. All string conversions use <see cref="CultureInfo.InvariantCulture"/> to ensure culture-independent round-tripping.
/// </remarks>
public class BlobStringParseConverter : BlobConverterBase
{
    /// <summary>
    /// Gets a value indicating whether constructor usage is allowed.
    /// </summary>
    /// <remarks>If set to <c>true</c>, the converter will attempt to use a constructor that accepts a string if no suitable <c>Parse</c> method is found.</remarks>
    public bool AllowConstructor { get; }

    /// <summary>Initializes a new instance of the <see cref="BlobStringParseConverter"/> class.</summary>
    /// <param name="allowConstructor">If set to <c>true</c>, the converter will attempt to use a constructor that accepts a string if no suitable <c>Parse</c> method is found.</param>
    public BlobStringParseConverter(bool allowConstructor = false) => AllowConstructor = allowConstructor;

    #region Protected Methods

    /// <inheritdoc/>
    protected override object? GetCanHandleCache(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is Type underlying)
        {
            type = underlying;
        }
        var state = new BlobStringParseConverterData(type, AllowConstructor);
        return state.IsValid ? state : null;
    }

    #endregion Protected Methods

    #region Public Methods

    /// <inheritdoc/>
    public override IList<Type> GetContentTypes(Type type) => [];

    /// <inheritdoc/>
    public override object ReadContent(IBlobReaderState state, BlobConverterBundle bundle)
    {
        var reader = state.Reader;
        var text = reader.ReadPrefixedString() ?? throw new InvalidOperationException("Expected a prefixed string for parsing.");
        if (bundle.State is not BlobStringParseConverterData myState) throw new InvalidOperationException("Invalid state for string parse converter.");
        return myState.Parse(text);
    }

    /// <inheritdoc/>
    public override void ReadInitialization(IBlobReaderState state, BlobConverterBundle bundle)
    {
        GetHandlingData(bundle.Type, out BlobStringParseConverterData data);
        bundle.State = data with { RoundtripTest = true, Mode = default };
        if (!data.IsValid) throw new InvalidOperationException($"Could not find matching parse function or constructor for type {bundle.Type}.");
    }

    /// <inheritdoc/>
    public override void WriteContent(IBlobWriterState state, BlobConverterBundle bundle, object instance)
    {
        var writer = state.Writer;
        if (instance is null)
        {
            writer.Write((byte)0);
            return;
        }

        if (bundle.State is not BlobStringParseConverterData myState) throw new InvalidOperationException("Invalid state for string parse converter.");
        string text;
        if (myState.RoundtripTest)
        {
            myState.RoundtripCheck(instance, out text);
            myState.RoundtripTest = false;
        }
        else
        {
            text = myState.GetString(instance);
        }
        writer.WritePrefixed(text);
    }

    /// <inheritdoc/>
    public override void WriteInitialization(IBlobWriterState state, BlobConverterBundle bundle)
    {
        GetHandlingData(bundle.Type, out BlobStringParseConverterData data);
        bundle.State = data with { RoundtripTest = true, Mode = default };
        if (!data.IsValid) throw new InvalidOperationException($"Could not find matching parse function or constructor for type {bundle.Type}.");
    }

    #endregion Public Methods
}

/// <summary>Generic blob string parse converter that validates roundtrip conversion for a specific type.</summary>
/// <typeparam name="TType">The type to convert.</typeparam>
public class BlobStringParseConverter<TType> : BlobStringParseConverter
{
    #region Public Constructors

    /// <summary>Initializes a new instance with a test value to validate roundtrip conversion capability.</summary>
    /// <param name="roundtripTestValue">Value used to verify successful roundtrip conversion.</param>
    /// <param name="allowConstructor">If set to <c>true</c>, the converter will attempt to use a constructor that accepts a string if no suitable <c>Parse</c> method is found.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="roundtripTestValue"/> is null.</exception>
    public BlobStringParseConverter(TType roundtripTestValue, bool allowConstructor)
    {
        if (roundtripTestValue is null) throw new ArgumentNullException(nameof(roundtripTestValue), "Roundtrip test value cannot be null.");
        var type = typeof(TType);
        var data = new BlobStringParseConverterData(type, allowConstructor);
        data.RoundtripCheck(roundtripTestValue, out _);
        SetHandleData(type, data);
    }

    #endregion Public Constructors
}
