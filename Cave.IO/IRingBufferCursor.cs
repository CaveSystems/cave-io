#nullable enable

using System.Collections.Generic;

namespace Cave.IO;

/// <summary>Provides an interface for <see cref="IRingBuffer{TValue}"/> read cursors.</summary>
/// <typeparam name="TValue">Type stored at the ringbuffer</typeparam>
public interface IRingBufferCursor<TValue>
{
    #region Public Properties

    /// <summary>Gets the number of items available for reading.</summary>
    int Available { get; }

    /// <summary>Gets the number of items lost due to overflows.</summary>
    long LostCount { get; }

    /// <summary>Gets the number of successful <see cref="Read"/> or <see cref="TryRead(out TValue)"/> calls.</summary>
    long ReadCount { get; }

    /// <summary>Gets the current read position [0..Capacity-1].</summary>
    int ReadPosition { get; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Reads an item from the buffer. This blocks until an item could be read.</summary>
    /// <returns>Returns the item read from the buffer.</returns>
    TValue Read();

    /// <summary>Reads up to <paramref name="count"/> items from the buffer.</summary>
    /// <param name="count">Number of items to read. (Optional: If unset or &lt;= 0 this will read all available items.)</param>
    /// <returns>Returns the items read from the buffer.</returns>
    IList<TValue> ReadList(int count = 0);

    /// <summary>Tries to read an item from the buffer.</summary>
    /// <param name="value">Returns the item read from the buffer or default if none available.</param>
    /// <returns>Returns true if an item could be read, false otherwise.</returns>
    bool TryRead(out TValue value);

    #endregion Public Methods
}
