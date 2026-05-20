using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    #region Properties

    internal ArrayGetterCache ArrayGetterCache { get; } = new();

    #endregion Properties

    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="BlobSerializer"/> class.</summary>
    public BlobSerializer()
    {
        KnownConverters = new(knownConverters);
        KnownTypes = new(knownTypes);
    }

    #endregion Public Constructors

    #region Public Methods

    readonly Dictionary<Type, IBlobConverter> knownConverters = new();

    readonly Dictionary<string, Type> knownTypes = new();

    /// <summary>Resolves the CLR <see cref="Type"/> that corresponds to the specified <see cref="BlobPrimitiveType"/>.</summary>
    /// <param name="typeCode">The primitive type code to resolve.</param>
    /// <returns>The CLR <see cref="Type"/> that maps to <paramref name="typeCode"/>.</returns>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="typeCode"/> does not map to a known CLR type.</exception>
    public static Type GetPrimitiveType(BlobPrimitiveType typeCode)
    {
        return typeCode switch
        {
            BlobPrimitiveType.Bool => typeof(bool),
            BlobPrimitiveType.UInt8 => typeof(byte),
            BlobPrimitiveType.Int8 => typeof(sbyte),
            BlobPrimitiveType.Int16 => typeof(short),
            BlobPrimitiveType.UInt16 => typeof(ushort),
            BlobPrimitiveType.Int32 => typeof(int),
            BlobPrimitiveType.UInt32 => typeof(uint),
            BlobPrimitiveType.Int64 => typeof(long),
            BlobPrimitiveType.UInt64 => typeof(ulong),
            BlobPrimitiveType.Float32 => typeof(float),
            BlobPrimitiveType.Float64 => typeof(double),
            BlobPrimitiveType.Char => typeof(char),
            BlobPrimitiveType.String => typeof(string),
            BlobPrimitiveType.DateTime => typeof(DateTime),
            BlobPrimitiveType.TimeSpan => typeof(TimeSpan),
            BlobPrimitiveType.DateTimeOffset => typeof(DateTimeOffset),
            BlobPrimitiveType.Decimal => typeof(decimal),
            BlobPrimitiveType.ByteArray => typeof(byte[]),
            BlobPrimitiveType.Int8Array => typeof(sbyte[]),
            BlobPrimitiveType.Int16Array => typeof(short[]),
            BlobPrimitiveType.UInt16Array => typeof(ushort[]),
            BlobPrimitiveType.Int32Array => typeof(int[]),
            BlobPrimitiveType.UInt32Array => typeof(uint[]),
            BlobPrimitiveType.Int64Array => typeof(long[]),
            BlobPrimitiveType.UInt64Array => typeof(ulong[]),
            BlobPrimitiveType.FloatArray => typeof(float[]),
            BlobPrimitiveType.DoubleArray => typeof(double[]),
            _ => throw new NotSupportedException($"Unsupported primitive type code: {typeCode}")
        };
    }

    /// <summary>Attempts to determine the <see cref="BlobPrimitiveType"/> for a given CLR <see cref="Type"/>.</summary>
    /// <param name="type">The CLR type to evaluate.</param>
    /// <param name="primitiveType">
    /// When this method returns <see langword="true"/>, contains the matching <see cref="BlobPrimitiveType"/>; otherwise contains <see cref="BlobPrimitiveType.Unsupported"/>.
    /// </param>
    /// <returns><see langword="true"/> if <paramref name="type"/> maps to a supported primitive type; otherwise <see langword="false"/>.</returns>
    public static bool GetPrimitiveType(Type type, out BlobPrimitiveType primitiveType)
    {
        primitiveType = type switch
        {
            // primitives
            Type t when t == typeof(bool) => BlobPrimitiveType.Bool,
            Type t when t == typeof(byte) => BlobPrimitiveType.UInt8,
            Type t when t == typeof(sbyte) => BlobPrimitiveType.Int8,
            Type t when t == typeof(short) => BlobPrimitiveType.Int16,
            Type t when t == typeof(ushort) => BlobPrimitiveType.UInt16,
            Type t when t == typeof(int) => BlobPrimitiveType.Int32,
            Type t when t == typeof(uint) => BlobPrimitiveType.UInt32,
            Type t when t == typeof(long) => BlobPrimitiveType.Int64,
            Type t when t == typeof(ulong) => BlobPrimitiveType.UInt64,
            Type t when t == typeof(float) => BlobPrimitiveType.Float32,
            Type t when t == typeof(double) => BlobPrimitiveType.Float64,
            Type t when t == typeof(char) => BlobPrimitiveType.Char,

            // special allowed non-primitives
            Type t when t == typeof(string) => BlobPrimitiveType.String,
            Type t when t == typeof(DateTime) => BlobPrimitiveType.DateTime,
            Type t when t == typeof(TimeSpan) => BlobPrimitiveType.TimeSpan,
            Type t when t == typeof(DateTimeOffset) => BlobPrimitiveType.DateTimeOffset,
            Type t when t == typeof(decimal) => BlobPrimitiveType.Decimal,

            //array types
            Type t when t == typeof(byte[]) => BlobPrimitiveType.ByteArray,
            Type t when t == typeof(sbyte[]) => BlobPrimitiveType.Int8Array,
            Type t when t == typeof(short[]) => BlobPrimitiveType.Int16Array,
            Type t when t == typeof(ushort[]) => BlobPrimitiveType.UInt16Array,
            Type t when t == typeof(int[]) => BlobPrimitiveType.Int32Array,
            Type t when t == typeof(uint[]) => BlobPrimitiveType.UInt32Array,
            Type t when t == typeof(long[]) => BlobPrimitiveType.Int64Array,
            Type t when t == typeof(ulong[]) => BlobPrimitiveType.UInt64Array,
            Type t when t == typeof(float[]) => BlobPrimitiveType.FloatArray,
            Type t when t == typeof(double[]) => BlobPrimitiveType.DoubleArray,

            // last since it is more expensive to check
            Type t when t.IsEnum => BlobPrimitiveType.Enum,

            _ => BlobPrimitiveType.Unsupported
        };
        return (primitiveType != BlobPrimitiveType.Unsupported);
    }

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

    /// <summary>Prepares the serializer for handling the specified type by ensuring that an appropriate <see cref="IBlobConverter"/> is available.</summary>
    /// <remarks>
    /// This will speed up the first serialization or deserialization of the type by pre-resolving and caching the converter, but is not required for normal
    /// operation as converters are resolved on demand.
    /// </remarks>
    /// <param name="type">The type to prepare the serializer for.</param>
    /// <exception cref="InvalidOperationException">Thrown if no converter is found for the specified type.</exception>
    public void Prepare(Type type)
    {
        var converter = GetConverter(type);
        foreach (var contentType in converter.GetContentTypes(type))
        {
            try
            {
                Prepare(contentType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to prepare content type {contentType.ToShortName()} for type {type.ToShortName()}.", ex);
            }
        }
    }

    /// <summary>
    /// Prepares the serializer for handling the specified types by ensuring that appropriate <see cref="IBlobConverter"/> instances are available for each type.
    /// </summary>
    /// <remarks>
    /// This will speed up the first serialization or deserialization of the type by pre-resolving and caching the converter, but is not required for normal
    /// operation as converters are resolved on demand.
    /// </remarks>
    /// <param name="types">The types to prepare the serializer for.</param>
    public void Prepare(params Type[] types) => types.ForEach(Prepare);

    /// <summary>
    /// Prepares the serializer for handling the specified types by ensuring that appropriate <see cref="IBlobConverter"/> instances are available for each type.
    /// </summary>
    /// <remarks>
    /// This will speed up the first serialization or deserialization of the type by pre-resolving and caching the converter, but is not required for normal
    /// operation as converters are resolved on demand.
    /// </remarks>
    /// <param name="types">The types to prepare the serializer for.</param>
    public void Prepare(IEnumerable<Type> types) => types.ForEach(Prepare);

    /// <summary>Registers a custom <see cref="IBlobConverter"/> for a specific type, allowing it to be used during serialization and deserialization.</summary>
    /// <remarks>
    /// This can be used if classes or structs cannot be marked with attributes but the default factory converter does not work or no matching converter can be
    /// found. In a classical use case, the converter to be used would require an override with a modified CanHandle implementation. The lookup via the internal
    /// serializer dictionaries is much faster than the factory-based resolution. The deserializer does not need to register converters since the used converter
    /// is part of the serialized data for the types it expects to read, but doing so will speed up the deserialization process by avoiding the need for
    /// on-demand converter resolution.
    /// </remarks>
    /// <param name="type">The type for which the converter is being registered.</param>
    /// <param name="converter">The converter instance to register.</param>
    public void Register(Type type, IBlobConverter converter)
    {
        Logger?.Verbose($"Registering converter {converter.GetType().Name} for type {type.ToShortName()}");
        knownTypes.Add(type.GetPortableTypeName(), type);
        knownConverters.Add(type, converter);
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

    /// <summary>Gets the collection of known blob converters, keyed by their associated type.</summary>
    public ReadOnlyDictionary<Type, IBlobConverter> KnownConverters { get; }

    /// <summary>Gets the collection of known type mappings used for serialization or deserialization.</summary>
    public ReadOnlyDictionary<string, Type> KnownTypes { get; }

    #endregion Public Methods

    #region Properties

    /// <summary>Gets or sets the factory responsible for creating <see cref="IBlobConverter"/> instances for specific types. Defaults to <see cref="BlobDefaultFactory"/>.</summary>
    public IBlobConverterFactory Factory { get; set; } = new BlobDefaultFactory();

    /// <summary>
    /// Gets or sets the logger used for diagnostic output during serialization and deserialization. Defaults to <see langword="null"/> unless a debugger is
    /// attached at construction time.
    /// </summary>
    public ILogger? Logger { get; set; }

    #endregion Properties

    /// <summary>Resolves the appropriate <see cref="IBlobConverter"/> for the specified type, using registered converters and the factory as needed.</summary>
    /// <param name="type">The type for which to retrieve a converter.</param>
    /// <returns>The resolved <see cref="IBlobConverter"/> instance.</returns>
    public IBlobConverter GetConverter(Type type)
    {
        if (knownConverters.TryGetValue(type, out var converter))
        {
            return converter;
        }
        if (Factory.TryCreateConverter(type, out converter))
        {
            Register(type, converter);
            return converter;
        }
        throw new InvalidOperationException($"No converter found for type {type.ToShortName()}!");
    }
}
