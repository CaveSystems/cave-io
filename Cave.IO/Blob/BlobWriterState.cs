using System;

namespace Cave.IO.Blob;

/// <summary>Provides the state for an active blob serialization (write) operation.</summary>
/// <remarks>
/// <see cref="BlobWriterState"/> extends <see cref="BlobState"/> with writer-specific functionality and implements <see cref="IBlobWriterState"/>.
/// It maintains <see cref="IBlobState.Converters"/> to cache resolved converters and their assigned numeric identifiers,
/// and tracks which identifiers have already been emitted to avoid writing duplicate type definitions.
/// </remarks>
sealed class BlobWriterState : BlobState, IBlobWriterState
{
    #region Internal Methods

    /// <summary>Writes the binary stream header, including the format tag and version number, to the output stream.</summary>
    internal void Initialize()
    {
        Logger?.Debug($"Start writing binary blob version {Version}.");
        Writer.WriteZeroTerminated("BIN");
        Writer.Write7BitEncoded32(Version);
    }

    #endregion Internal Methods

    /// <summary>Gets the binary format version written to the stream header.</summary>
    internal int Version { get; } = 1;

    /// <summary>Initializes a new instance of the <see cref="BlobWriterState"/> class.</summary>
    /// <param name="serializer">The <see cref="BlobSerializer"/> that owns this state.</param>
    /// <param name="writer">The <see cref="DataWriter"/> used to write binary data to the target stream.</param>
    public BlobWriterState(BlobSerializer serializer, DataWriter writer) : base(serializer) => Writer = writer;

    /// <inheritdoc/>
    public override void Close()
    {
        Writer.Write7BitEncoded64(ulong.MaxValue);
        Writer.WriteZeroTerminated("END");
        Converters.Reset();
        Logger?.Debug($"Finished writing binary blob version {Version}.");
    }

    /// <inheritdoc/>
    public BlobConverterBundle WriteConverter(Type type)
    {
        if (Converters.TryGet(type, out var bundle))
        {
            // already emitted converter
            Logger?.Verbose($"Reusing converter {bundle.Id} for type {type.ToShortName()}.");
            Writer.Write7BitEncoded64(bundle.Id);
            return bundle;
        }
        // new converter
        if (!Serializer.Factory.TryCreateConverter(Serializer, type, out var converter))
        {
            throw new InvalidOperationException($"No converter found for type {type.FullName}!");
        }

        var id = Converters.RequestId();
        Writer.Write7BitEncoded64(id);
        WriteTypeDefition(type);
        bundle = new BlobConverterBundle(id, type, converter);
        Converters.Add(bundle);
        Logger?.Debug($"Created new converter {bundle.Id} {bundle.Converter.GetType().ToShortName()} for type {type.ToShortName()}.");
        converter.WriteInitialization(this, bundle);
        return bundle;
    }

    /// <inheritdoc/>
    public void Write(object? instance)
    {
        if (instance == null)
        {
            Logger?.Debug($"Write null");
            Writer.Write7BitEncoded32(0);
            return;
        }

        var type = instance.GetType();
        var bundle = WriteConverter(type);
        bundle.Converter.WriteContent(this, bundle, instance);
    }

    /// <inheritdoc/>
    public void WriteTypeDefition(Type type)
    {
        if (GetPrimitiveType(type, out var primitiveType))
        {
            Writer.Write7BitEncoded32((uint)primitiveType);
            if (primitiveType == BlobPrimitiveType.Enum)
            {
                Writer.WritePrefixed($"{type.Namespace}.{type.Name}");
            }
            return;
        }
        Logger?.Debug($"Write type definition for type {type.ToShortName()}");
        Writer.Write((byte)0);
        Writer.WritePrefixed($"{type.Namespace}.{type.Name}");
        var genericArgs = type.GetGenericArguments();
        Writer.Write7BitEncoded32(genericArgs.Length);
        foreach (var arg in genericArgs)
        {
            WriteTypeDefition(arg);
        }
    }

    /// <inheritdoc/>
    public DataWriter Writer { get; }
}
