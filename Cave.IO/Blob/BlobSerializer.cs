using System;
using System.Collections.Generic;
using System.IO;
using Cave.Logging;

namespace Cave.IO.Blob;

/// <summary>Provides functionality to serialize and deserialize object graphs to and from a binary blob format.</summary>
/// <remarks>
/// <para>
/// The <see cref="BlobSerializer"/> orchestrates the binary serialization pipeline by coordinating an <see cref="IBlobConverterFactory"/> and a set of
/// registered <see cref="IBlobConverter"/> instances. Objects that implement <see cref="IBlobConvertible"/> are handled natively; all other types are processed
/// via reflection-based or custom converters.
/// </para>
/// <para>When a debugger is attached, a <see cref="Logger"/> is automatically created to aid diagnostics.</para>
/// </remarks>
public sealed class BlobSerializer
{
    #region Public Methods

    /// <summary>Deserializes an object of type <typeparamref name="TContent"/> from a binary representation read from the given stream.</summary>
    /// <typeparam name="TContent">The expected type of the deserialized content.</typeparam>
    /// <remarks>To deserialize more than one object do not call this multiple times, instead use the <see cref="IBlobReaderState"/> returned by <see cref="StartReading"/>.</remarks>
    /// <param name="stream">The source <see cref="Stream"/> to read the binary data from.</param>
    /// <param name="instance">
    /// When this method returns, contains the deserialized instance of type <typeparamref name="TContent"/>, or <see langword="null"/> if the deserialized
    /// content is <see langword="null"/>.
    /// </param>
    public void Deserialize<TContent>(Stream stream, out TContent? instance)
    {
        var watch = Logger is null ? null : StopWatch.StartNew();
        var state = StartReading(stream);
        state.Read(out instance);
        state.Close();
        watch?.Stop();
        Logger?.Info($"Deserialized content of type {typeof(TContent).ToShortName()} in {watch?.Elapsed.FormatTime()}.");
    }

    /// <summary>Begins reading from the specified stream and returns a new blob reader state.</summary>
    /// <param name="stream">The stream to read from. Must be readable and positioned at the start of the data to process.</param>
    /// <returns>An object representing the state of the blob reader for the provided stream.</returns>
    public IBlobReaderState StartReading(Stream stream)
    {
        var state = new BlobReaderState(this, new(stream));
        state.Initialize();
        return state;
    }

    /// <summary>Begins a new write operation to the specified stream and returns a state object for managing the write process.</summary>
    /// <param name="stream">The target stream to which data will be written. The stream must be writable.</param>
    /// <returns>An object representing the state of the write operation, which can be used to manage and complete the writing process.</returns>
    public IBlobWriterState StartWriting(Stream stream)
    {
        var state = new BlobWriterState(this, new(stream));
        state.Initialize();
        return state;
    }

    /// <summary>Serializes the specified object instance to a binary representation and writes it to the given stream.</summary>
    /// <remarks>To serialize more than one object do not call this multiple times, instead use the <see cref="IBlobWriterState"/> returned by <see cref="StartWriting"/>.</remarks>
    /// <param name="stream">The target <see cref="Stream"/> to write the binary data to.</param>
    /// <param name="instance">The object instance to serialize.</param>
    public void Serialize(Stream stream, object instance)
    {
        var watch = Logger is null ? null : StopWatch.StartNew();
        var state = StartWriting(stream);
        state.Write(instance);
        state.Close();
        Logger?.Info($"Serialized content of type {instance.GetType().ToShortName()} in {watch?.Elapsed.FormatTime()}.");
    }

    #endregion Public Methods

    #region Properties

    /// <summary>
    /// Gets the <see cref="Type"/> object representing the <see cref="IBlobConvertible"/> interface, used for fast type-compatibility checks during serialization.
    /// </summary>
    public static Type IBlobConvertibleType { get; } = typeof(IBlobConvertible);

    /// <summary>
    /// Gets or sets the collection of explicitly registered <see cref="IBlobConverter"/> instances that are considered before the factory during converter resolution.
    /// </summary>
    public ICollection<IBlobConverter> Converters { get; set; } = new HashSet<IBlobConverter>();

    /// <summary>Gets or sets the factory responsible for creating <see cref="IBlobConverter"/> instances for specific types. Defaults to <see cref="BlobDefaultFactory"/>.</summary>
    public IBlobConverterFactory Factory { get; set; } = new BlobDefaultFactory();

    /// <summary>
    /// Gets or sets the logger used for diagnostic output during serialization and deserialization. Defaults to <see langword="null"/> unless a debugger is
    /// attached at construction time.
    /// </summary>
    public ILogger? Logger { get; set; }

    #endregion Properties
}
