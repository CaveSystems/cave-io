using System;

namespace Cave.IO;

/// <summary>Provides <see cref="EventArgs"/> for <see cref="Exception"/> handling of background threads using an <see cref="EventHandler"/>.</summary>
/// <remarks>Initializes a new instance of the <see cref="ExceptionEventArgs"/> class.</remarks>
/// <param name="ex">The <see cref="Exception"/> that was encountered.</param>
public class ExceptionEventArgs(Exception ex) : EventArgs
{
    #region Public Properties

    /// <summary>Gets the <see cref="Exception"/> that was encountered.</summary>
    public Exception Exception { get; } = ex;

    #endregion Public Properties
}
