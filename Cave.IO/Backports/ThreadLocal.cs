using System;
using System.Collections.Generic;
using System.Threading;

namespace Cave.IO;

#if NET20 || NET35 || NET40 || NET45

/// <summary>Backport of System.Threading.ThreadLocal{T} for frameworks prior to .NET 4.0.</summary>
/// <typeparam name="T">Value type.</typeparam>
sealed class ThreadLocal<T>
{
    #region Private Fields

    readonly Func<T> valueFactory;
    readonly Dictionary<int, T> values = new Dictionary<int, T>();
    readonly object rwLock = new object();

    #endregion Private Fields

    #region Public Constructors

    /// <summary>Initializes a new instance with a value factory.</summary>
    /// <param name="valueFactory">Factory called once per thread on first access.</param>
    public ThreadLocal(Func<T> valueFactory)
    {
        if (valueFactory == null) throw new ArgumentNullException("valueFactory");
        this.valueFactory = valueFactory;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets or sets the value for the current thread.</summary>
    public T Value
    {
        get
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            lock (rwLock)
            {
                T val;
                if (values.TryGetValue(id, out val)) return val;
            }
            // not yet created for this thread
            var newVal = valueFactory();
            lock (rwLock)
            {
                // double-check after acquiring write lock
                T existing;
                if (values.TryGetValue(id, out existing)) return existing;
                values[id] = newVal;
                return newVal;
            }
        }
        set
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            lock (rwLock) { values[id] = value; }
        }
    }

    #endregion Public Properties
}

#endif
