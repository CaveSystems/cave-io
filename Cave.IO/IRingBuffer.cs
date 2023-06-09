namespace Cave.IO;

#nullable enable

/// <summary>Provides a ring buffer interface for ring buffer implementations.</summary>
/// <typeparam name="TValue">Value type.</typeparam>
public interface IRingBuffer<TValue>
{
    #region Public Properties

    /// <summary>Gets or sets the desired overflow handling.</summary>
    RingBufferOverflowFlags OverflowHandling { get; set; }

    /// <summary>Gets the number of items available for reading.</summary>
    int Available { get; }

    /// <summary>Gets the number of items maximum present at the buffer.</summary>
    int Capacity { get; }

    /// <summary>Gets the number of items lost due to overflows.</summary>
    long LostCount { get; }

    /// <summary>Gets the number of successful <see cref="Read"/> or <see cref="TryRead(out TValue)"/> calls.</summary>
    long ReadCount { get; }

    /// <summary>Gets the current read position [0..Capacity-1].</summary>
    int ReadPosition { get; }

    /// <summary>Gets the number of rejected items (items that could not be queued).</summary>
    long RejectedCount { get; }

    /// <summary>Gets the number of successful <see cref="Write"/> calls.</summary>
    long WriteCount { get; }

    /// <summary>Gets the current write position [0..Capacity-1].</summary>
    int WritePosition { get; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Copies all the elements of the current one-dimensional array to the specified one-dimensional array starting at the specified destination array index.
    /// </summary>
    /// <returns>Returns a new array instance.</returns>
    void CopyTo(TValue[] array, int index);

    /// <summary>Reads an item from the buffer.</summary>
    /// <returns>Returns the item read from the buffer.</returns>
    TValue Read();

    /// <summary>Tries to read an item from the buffer.</summary>
    /// <param name="value">Returns the item read from the buffer or default if none available.</param>
    /// <returns>Returns true if an item could be read, false otherwise.</returns>
    bool TryRead(out TValue value);

    /// <summary>Gets the contents of the buffer as array.</summary>
    /// <returns>Returns a new array instance with the buffer contents.</returns>
    TValue[] ToArray();

    /// <summary>Writes an item to the buffer if there is space left.</summary>
    /// <param name="item">Item to write to the buffer.</param>
    /// <returns>Returns true if the item could be written, false otherwise.</returns>
    bool Write(TValue item);

    #endregion Public Methods
}
