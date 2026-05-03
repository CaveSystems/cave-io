using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

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
    #region Protected Methods

    /// <inheritdoc/>
    protected override object? GetCanHandleCache(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is Type underlying)
        {
            type = underlying;
        }
        var state = new BlobStringParseConverterData(type);
        return state.IsValid ? state : null;

    }

    #endregion Protected Methods

    #region Public Methods

    /// <summary>Determines and sets the optimal serialization mode based on the instance's capabilities.</summary>
    /// <param name="state">The converter state to update.</param>
    /// <param name="instance">The instance to analyze.</param>
    /// <returns>The determined serialization mode.</returns>
    BlobStringParseConverterMode InitMode(BlobStringParseConverterData state, object instance)
    {
        try { if (instance is IFormattable formattable && formattable.ToString("R", CultureInfo.InvariantCulture) != null) return state.Mode = BlobStringParseConverterMode.FormattableRoundtrip; }
        catch { }
        try { if (instance is IFormattable formattable && formattable.ToString(null, CultureInfo.InvariantCulture) != null) return state.Mode = BlobStringParseConverterMode.Formattable; }
        catch { }
        try { if (instance is IConvertible convertible && convertible.ToString(CultureInfo.InvariantCulture) != null) return state.Mode = BlobStringParseConverterMode.Convertible; }
        catch { }
        return state.Mode = BlobStringParseConverterMode.Simple;
    }

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
        var mode = myState.Mode;
        if (mode == BlobStringParseConverterMode.Undefined) mode = InitMode(myState, instance);
        var text = mode switch
        {
            BlobStringParseConverterMode.FormattableRoundtrip => ((IFormattable)instance).ToString("R", CultureInfo.InvariantCulture),
            BlobStringParseConverterMode.Formattable => ((IFormattable)instance).ToString(null, CultureInfo.InvariantCulture),
            BlobStringParseConverterMode.Convertible => ((IConvertible)instance).ToString(CultureInfo.InvariantCulture),
            _ => instance.ToString(),
        };
        writer.WritePrefixed(text);

        if (myState.RoundtripTest)
        {
            var roundtrip = myState.Parse(text!);
            if (!Equals(roundtrip, instance))
            {
                throw new InvalidOperationException($"Roundtrip test failed. Original: {instance}, Roundtrip: {roundtrip}");
            }
            myState.RoundtripTest = false;
        }
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
