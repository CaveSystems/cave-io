using System;
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
public class BlobStringParseConverter : IBlobConverter
{
    #region Public Methods

    /// <summary>
    /// Determines and sets the optimal serialization mode based on the instance's capabilities.
    /// </summary>
    /// <param name="state">The converter state to update.</param>
    /// <param name="instance">The instance to analyze.</param>
    /// <returns>The determined serialization mode.</returns>
    BlobStringParseConverterMode InitMode(BlobStringParseConverterState state, object instance)
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
    public virtual bool CanHandle(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is Type underlying)
        {
            type = underlying;
        }
        return type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Any(m => m.Name == "Parse");
    }

    /// <inheritdoc/>
    public virtual object ReadContent(IBlobReaderState state, BlobConverterBundle bundle)
    {
        var reader = state.Reader;
        var text = reader.ReadPrefixedString() ?? throw new InvalidOperationException("Expected a prefixed string for parsing.");
        if (bundle.State is not BlobStringParseConverterState myState) throw new InvalidOperationException("Invalid state for string parse converter.");
        return myState.Parse(text);
    }

    /// <inheritdoc/>
    public virtual void ReadInitialization(IBlobReaderState state, BlobConverterBundle bundle) => bundle.State = new BlobStringParseConverterState(bundle.Type);

    /// <inheritdoc/>
    public virtual void WriteContent(IBlobWriterState state, BlobConverterBundle bundle, object instance)
    {
        var writer = state.Writer;
        if (instance is null)
        {
            writer.Write((byte)0);
            return;
        }
        if (bundle.State is not BlobStringParseConverterState myState) throw new InvalidOperationException("Invalid state for string parse converter.");
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
    public virtual void WriteInitialization(IBlobWriterState state, BlobConverterBundle bundle) => bundle.State = new BlobStringParseConverterState(bundle.Type);

    #endregion Public Methods
}
