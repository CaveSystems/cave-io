using System;
using System.Threading;

namespace Cave.IO;

/// <summary>Provides a disposable scope for synchronized access using Monitor locks.</summary>
public class SynchronizedContext : IDisposable
{
    #region Public Constructors

    /// <summary>Initializes a new instance and acquires a lock on the specified synchronization object within the given timeout period.</summary>
    /// <param name="syncRoot">The object used for synchronization.</param>
    /// <param name="timeout">The maximum time to wait for acquiring the lock.</param>
    /// <exception cref="TimeoutException">Thrown when the lock cannot be acquired within the specified timeout period.</exception>
    public SynchronizedContext(object syncRoot, TimeSpan timeout)
    {
        SyncRoot = syncRoot;
        if (timeout <= TimeSpan.Zero)
        {
            Monitor.Enter(syncRoot);
            return;
        }
        if (!Monitor.TryEnter(syncRoot, timeout))
        {
            throw new TimeoutException($"Failed to acquire the lock within the specified timeout of {timeout.FormatTime()}.");
        }
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    void IDisposable.Dispose()
    {
        Monitor.Exit(SyncRoot);
        GC.SuppressFinalize(this);
    }

    #endregion Public Methods

    #region Properties

    /// <summary>
    /// Gets an object that can be used to synchronize access to the resource. This object is used as the lock object for the Monitor.Enter and Monitor.Exit calls.
    /// </summary>
    public object SyncRoot { get; }

    #endregion Properties
}
