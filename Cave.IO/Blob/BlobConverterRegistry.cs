using System;
using System.Collections.Generic;

namespace Cave.IO.Blob;

/// <summary>Registry for managing blob converter bundles with lookup by ID or type.</summary>
public sealed class BlobConverterRegistry
{
    #region Fields

    /// <summary>Next identifier to assign for a new blob converter bundle. Starts at 1.</summary>
    uint nextId = 1;

    /// <summary>Internal storage for registered bundles indexed by (Id - 1).</summary>
    BlobConverterBundle[] states = new BlobConverterBundle[127];

    /// <summary>Map from bundle CLR type to its assigned identifier.</summary>
    readonly Dictionary<Type, uint> types = new();

    #endregion Fields

    #region Internal Methods

    /// <summary>Requests a new unique ID for a blob converter bundle.</summary>
    /// <returns>A unique identifier for a new blob converter bundle.</returns>
    /// <remarks>
    /// The method reserves the next sequential ID and will grow the internal storage array
    /// if the reserved ID would exceed the current capacity.
    /// </remarks>
    internal uint RequestId()
    {
        var id = nextId++;
        if (id > states.Length)
        {
            Array.Resize(ref states, states.Length * 2);
        }
        return id;
    }

    #endregion Internal Methods

    #region Public Methods

    /// <summary>Registers a blob converter bundle in the registry.</summary>
    /// <param name="state">The blob converter bundle to register.</param>
    /// <exception cref="ArgumentException">Thrown when the ID is invalid or already registered.</exception>
    public void Add(BlobConverterBundle state)
    {
        if (state.Id >= nextId || state.Id == 0) throw new ArgumentException($"State ID {state.Id} is out of range.");
        var index = state.Id - 1;
        if (states[index] != null) throw new ArgumentException($"State ID {state.Id} is already registered.");
        types.Add(state.Type, state.Id);
        states[index] = state;
    }

    /// <summary>Clears all registered blob converter bundles from the type registry.</summary>
    /// <remarks>Does not modify allocated IDs or the internal states array.</remarks>
    public void Reset()
    {
        types.Clear();
    }

    /// <summary>Retrieves a blob converter bundle by its ID.</summary>
    /// <param name="id">The ID of the blob converter bundle.</param>
    /// <param name="state">The retrieved blob converter bundle, if found.</param>
    /// <returns>True if the bundle was found; otherwise, false.</returns>
    public bool TryGet(uint id, out BlobConverterBundle state)
    {
        if (id > 0 && id < nextId)
        {
            state = states[(int)id - 1];
            return true;
        }
        else
        {
            state = null!;
            return false;
        }
    }

    /// <summary>Retrieves a blob converter bundle by its type.</summary>
    /// <param name="type">The type associated with the blob converter bundle.</param>
    /// <param name="state">The retrieved blob converter bundle, if found.</param>
    /// <returns>True if the bundle was found; otherwise, false.</returns>
    public bool TryGet(Type type, out BlobConverterBundle state)
    {
        if (types.TryGetValue(type, out var id))
        {
            state = states[(int)id - 1];
            return true;
        }
        else
        {
            state = null!;
            return false;
        }
    }

    #endregion Public Methods
}
