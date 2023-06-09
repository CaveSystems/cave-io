using System;

#nullable enable

namespace Cave.IO;

/// <summary>
/// Provides flags for overflow handling at <see cref="IRingBuffer{TValue}"/> implementations.
/// </summary>
[Flags]
public enum RingBufferOverflowFlags
{
    /// <summary>
    /// No specifal handling. The RingBuffer is unprotected, unchecked and will ignore overflows.
    /// </summary>
    None = 0,

    /// <summary>
    /// Prevent overflows by rejecting writes.
    /// </summary>
    Prevent = 1 << 0,

    /// <summary>
    /// Write to <see cref="Trace"/> on overflows.
    /// </summary>
    Trace = 1 << 1,

    /// <summary>
    /// Throw an <see cref="OverflowException"/> on overflows.
    /// </summary>
    Exception = 1 << 2,
}
