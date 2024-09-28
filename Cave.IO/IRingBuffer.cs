using System.Collections.Generic;

namespace Cave.IO;


/// <summary>Provides a ring buffer interface for ring buffer implementations.</summary>
/// <typeparam name="TValue">Value type.</typeparam>
public interface IRingBuffer<TValue> : IRingBufferCursor<TValue>
{
    #region Public Properties

    /// <summary>Gets the number of items maximum present at the buffer.</summary>
    int Capacity { get; }

    /// <summary>Gets or sets the desired overflow handling.</summary>
    RingBufferOverflowFlags OverflowHandling { get; set; }

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
    /// <remarks>
    /// This is an unsorted dump of the current buffer state. Be aware that the state may have changed on completion already. There are no checks or race
    /// condition preventions at this function!
    /// </remarks>
    void CopyTo(TValue[] array, int index);

    /// <summary>Gets a single thread read cursor able to follow up on all writes up to <see cref="Capacity"/>.</summary>
    /// <returns>Returns a new single thread read cursor.</returns>
    IRingBufferCursor<TValue> GetCursor();

    /// <summary>Reads an item from the buffer. This blocks until an item could be read.</summary>
    /// <returns>Returns the item read from the buffer.</returns>
    /// <remarks>
    /// <para>
    /// Multitread read (fifo queue) function: This blocks until an item can be read. Any thread waiting for an item can be activated, there is no activation
    /// order for threads waiting for an item.
    /// </para>
    /// <para>
    /// This function uses a global cursor. All items read by this function can no longer be read by cursors! Multiple threads can read but each item can only
    /// be read once. If you need multiple readers reading all items of the ring buffer, use <see cref="GetCursor"/> and do not use this function.
    /// </para>
    /// </remarks>
    new TValue Read();

    /// <summary>Reads up to <paramref name="count"/> items from the buffer.</summary>
    /// <param name="count">Number of items to read. (Optional: If unset or &lt;= 0 this will read all available items.)</param>
    /// <returns>Returns the items read from the buffer.</returns>
    /// <remarks>
    /// <para>
    /// Multitread read (fifo queue) function: If multiple threads read from the buffer or the buffer does not contain enough items, this will return less than
    /// <paramref name="count"/> items.
    /// </para>
    /// <para>
    /// This function uses a global cursor. All items read by this function can no longer be read by cursors! Multiple threads can read but each item can only
    /// be read once. If you need multiple readers reading all items of the ring buffer, use <see cref="GetCursor"/> and do not use this function.
    /// </para>
    /// </remarks>
    new IList<TValue> ReadList(int count = 0);

    /// <summary>Gets all available items as array. This equals <see cref="ReadList(int)"/> without parameter but does not advance the read position.</summary>
    /// <returns>Returns a new array instance with the buffer contents.</returns>
    TValue[] ToArray();

    /// <summary>Tries to read an item from the buffer.</summary>
    /// <param name="value">Returns the item read from the buffer or default if none available.</param>
    /// <returns>Returns true if an item could be read, false otherwise.</returns>
    /// <remarks>
    /// <para>Multitread read (fifo queue) function: Since the ringbuffer is lockless the first thread seeing the item will get it.</para>
    /// <para>
    /// This function uses a global cursor. All items read by this function can no longer be read by cursors! Multiple threads can read but each item can only
    /// be read once. If you need multiple readers reading all items of the ring buffer, use <see cref="GetCursor"/> and do not use this function.
    /// </para>
    /// </remarks>
    new bool TryRead(out TValue value);

    /// <summary>Writes an item to the buffer if there is space left.</summary>
    /// <param name="item">Item to write to the buffer.</param>
    /// <returns>Returns true if the item could be written, false otherwise.</returns>
    bool Write(TValue item);

    #endregion Public Methods
}
