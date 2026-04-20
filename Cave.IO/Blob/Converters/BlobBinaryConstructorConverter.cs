using System;
using System.Linq;
using System.Reflection;

namespace Cave.IO.Blob.Converters;

/// <summary>Converts types with <c>.ctor(byte[])</c> and <c>ToByteArray()</c>.</summary>
public class BlobBinaryConstructorConverter : IBlobConverter
{
    #region Public Methods

    /// <inheritdoc/>
    public virtual bool CanHandle(Type type) =>
        type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(m => m.Name == "ToByteArray" && m.ReturnType == typeof(byte[]) && m.GetParameters().Length == 0) &&
        type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(c => c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType == typeof(byte[]));

    /// <inheritdoc/>
    public virtual object ReadContent(IBlobReaderState state, BlobConverterBundle bundle)
    {
        var reader = state.Reader;
        var blob = reader.ReadBytes();
        if (bundle.State is not BlobBinaryConstructorConverterState myState) throw new InvalidOperationException($"{nameof(BlobBinaryConstructorConverter)} expected, got {bundle.State?.GetType()}!");
        return myState.Constructor.Invoke([blob]) ?? throw new InvalidOperationException($"Constructor {myState.Constructor} returned null!");
    }

    /// <inheritdoc/>
    public virtual void ReadInitialization(IBlobReaderState state, BlobConverterBundle bundle) => bundle.State = new BlobBinaryConstructorConverterState(bundle.Type);

    /// <inheritdoc/>
    public virtual void WriteContent(IBlobWriterState state, BlobConverterBundle bundle, object instance)
    {
        var writer = state.Writer;
        if (instance is null)
        {
            writer.Write((byte)0);
            return;
        }
        if (bundle.State is not BlobBinaryConstructorConverterState myState) throw new InvalidOperationException($"{nameof(BlobBinaryConstructorConverter)} expected, got {bundle.State?.GetType()}!");
        if (myState.ToByteArrayMethod.Invoke(instance, new object[0]) is not byte[] blob) throw new InvalidOperationException($"{instance} Instance.ToByteArray() does not return a byte[]!");
        writer.WritePrefixed(blob);
    }

    /// <inheritdoc/>
    public virtual void WriteInitialization(IBlobWriterState state, BlobConverterBundle bundle) => bundle.State = new BlobBinaryConstructorConverterState(bundle.Type);

    #endregion Public Methods
}
