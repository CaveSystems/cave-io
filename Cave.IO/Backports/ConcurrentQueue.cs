using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Cave.IO;

#if NET20 || NET35

/// <summary>Backport of System.Collections.Concurrent.ConcurrentQueue{TValue} for .NET 2.0 and 3.5. based on the <see cref="Fifo{TValue}"/> class.</summary>
/// <typeparam name="TValue"></typeparam>
public class ConcurrentQueue<TValue> : Fifo<TValue>, IEnumerable<TValue>
{
    #region Properties

    /// <summary>Gets the number of elements contained in the <see cref="ConcurrentQueue{TValue}"/>.</summary>
    public int Count
    {
        [MethodImpl(256)]
        get => Available;
    }

    /// <summary>Gets a value indicating whether the <see cref="ConcurrentQueue{TValue}"/> is empty.</summary>
    public bool IsEmpty
    {
        [MethodImpl(256)]
        get => Available == 0;
    }

    IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => (IEnumerator<TValue>)ToArray().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ToArray().GetEnumerator();

    #endregion Properties
}

#endif
