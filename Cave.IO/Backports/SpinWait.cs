using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Cave.IO;

#if NET20 || NET35

/// <summary>Backport of System.Threading.SpinWait for .NET 2.0 and 3.5.</summary>
public struct SpinWait
{
    #region Fields

    int counter;

    #endregion Fields

    #region Public Methods

    /// <summary>Performs a single spin cycle.</summary>
    [MethodImpl((MethodImplOptions)0x0100)]
    public void SpinOnce()
    {
        if (++counter < 20)
        {
            Thread.SpinWait(counter);
        }
        else if (++counter < 40)
        {
            Thread.Sleep(0);
        }
        else
        {
            Thread.Sleep(1);
        }
    }

    #endregion Public Methods

    #region Properties

    /// <summary>Gets a value indicating whether the next call to <see cref="SpinOnce"/> will enforce a context switch of the current thread.</summary>
    public bool NextSpinWillYield
    {
        [MethodImpl((MethodImplOptions)0x0100)]
        get => counter >= 19;
    }

    #endregion Properties
}

#endif
