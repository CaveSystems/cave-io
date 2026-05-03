using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Cave.IO.Blob;

/// <summary>Provides state for blob read operations.</summary>
sealed class BlobReaderState : BlobState, IBlobReaderState
{
    #region Internal Methods

    /// <summary>Reads and validates the stream header.</summary>
    /// <exception cref="InvalidDataException">Thrown if the <c>BIN</c> tag is missing.</exception>
    /// <exception cref="NotImplementedException">Thrown if the stream version is not supported.</exception>
    internal void Initialize()
    {
        var bin = Reader.ReadZeroTerminatedFixedLengthString(4);
        if (bin != "BIN") throw new InvalidDataException("Invalid binary format (missing BIN tag).");
        var version = Reader.Read7BitEncodedInt32();
        if (version != 1) throw new NotImplementedException($"Unkown version {version}!");
    }

    #endregion Internal Methods

    #region Properties

    /// <summary>Gets processed object identifiers.</summary>
    internal HashSet<int> ProcessedIds { get; } = new HashSet<int>();

    /// <summary>Gets the supported binary format version.</summary>
    internal int Version { get; } = 1;

    /// <inheritdoc/>
    public bool IsCompleted { get; private set; }

    #endregion Properties

    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="BlobReaderState"/> class.</summary>
    /// <param name="serializer">The owning serializer.</param>
    /// <param name="reader">The data reader.</param>
    public BlobReaderState(BlobSerializer serializer, DataReader reader) : base(serializer) => Reader = reader;

    #endregion Public Constructors

    #region Public Methods

    LinkedList<Assembly> assemblies = new();

    /// <inheritdoc/>
    public void Close() => Close(true);

    /// <inheritdoc/>
    public void Close(bool validateEndMark = true)
    {
        if (validateEndMark)
        {
            if (!IsCompleted)
            {
                if (Read() != null)
                {
                    throw new InvalidOperationException("Stream was not read to end (got object instead of end marker)!");
                }
                IsCompleted = true;
            }
            var end = Reader.ReadZeroTerminatedFixedLengthString(4);
            if (end != "END") throw new InvalidDataException("Invalid binary format (missing END tag).");
        }
    }

    /// <summary>Reads the next object.</summary>
    /// <returns>The object, or <see langword="null"/>.</returns>
    public object? Read()
    {
        if (IsCompleted) return null;
        var id = Reader.Read7BitEncodedUInt64();
        if (id == 0) return null;
        if (id > uint.MaxValue)
        {
            IsCompleted = true;
            return null;
        }
        var bundle = ReadConverter((uint)id);
        return bundle.Converter.ReadContent(this, bundle);
    }

    /// <summary>Reads content from the specified blob reader state and outputs an instance of the requested type.</summary>
    /// <typeparam name="TContent">The type of content to read from the blob.</typeparam>
    /// <param name="instance">
    /// When this method returns, contains the content read from the blob if it is of the specified type; otherwise, the default value for the type.
    /// </param>
    /// <exception cref="InvalidDataException">Thrown if the content read from the blob is not of the expected type and is not null.</exception>
    public void Read<TContent>(out TContent? instance)
    {
        var result = Read();
        instance = result is TContent content ? content : result is null ? default :
            throw new InvalidDataException($"Expected content of type {typeof(TContent).ToShortName()}, but got {result.GetType().ToShortName()}.");
    }

    /// <summary>Reads and resolves a converter bundle.</summary>
    /// <param name="knownId">The optional converter identifier.</param>
    /// <returns>The resolved converter bundle.</returns>
    /// <exception cref="InvalidDataException">Thrown if the converter data is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown if no converter is available.</exception>
    public BlobConverterBundle ReadConverter(uint? knownId = null)
    {
        var id = knownId ?? Reader.Read7BitEncodedUInt32();
        if (id == 0) throw new InvalidDataException("Invalid converter ID (0).");
        if (Converters.TryGet(id, out var entry))
        {
            return entry;
        }
        var nextId = Converters.RequestId();
        if (nextId != id)
        {
            throw new InvalidDataException("Invalid converter ID sequence.");
        }
        // No cached converter; read initialization for this type.
        var type = ReadTypeDefitition();
        var converter = Serializer.Converters.FirstOrDefault(c => c.CanHandle(type));
        if (converter is null && !Serializer.Factory.TryCreateConverter(Serializer, type, out converter))
        {
            throw new InvalidOperationException($"No converter found for type {type.Namespace}.{type.Name}.");
        }
        var bundle = new BlobConverterBundle(id, type, converter);
        Converters.Add(bundle);
        converter.ReadInitialization(this, bundle);
        return bundle;
    }

    /// <summary>Reads a type definition.</summary>
    /// <returns>The resolved type.</returns>
    /// <exception cref="InvalidDataException">Thrown if the type data is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the type cannot be resolved.</exception>
    public Type ReadTypeDefitition()
    {
        var typeCode = (BlobPrimitiveType)Reader.Read7BitEncodedUInt32();
        if (typeCode != 0 && typeCode != BlobPrimitiveType.Enum)
        {
            return BlobSerializer.GetPrimitiveType(typeCode);
        }
        var typeName = Reader.ReadPrefixedString() ?? throw new InvalidDataException("Invalid binary format (missing type name).");
        if (Serializer.KnownTypes.TryGetValue(typeName, out var knownType))
        {
            Logger?.Debug($"FastPath: Taking type {typeName} from known types.");
            return knownType;
        }

        Logger?.Debug($"Resolving type {typeName} via assembly scan...");
        if (assemblies.Count == 0)
        {
            assemblies = new LinkedList<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
        }
        for (var node = assemblies.First; node != null; node = node.Next)
        {
            var assembly = node.Value;
            var type = assembly.GetType(typeName, false);
            if (type != null)
            {
                //move to first position for faster access next time
                assemblies.Remove(node);
                assemblies.AddFirst(node);
                return type;
            }
        }

        throw new InvalidOperationException($"Could not resolve type {typeName}. Ensure the type is loaded or use the BlobSerializer.Prepare() function!");
    }

    #endregion Public Methods

    /// <summary>Gets the data reader.</summary>
    public DataReader Reader { get; }
}
