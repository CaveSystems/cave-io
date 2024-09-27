
using System;

namespace Cave.IO;

/// <summary>Class to provide new line characters and bytes used for reading and writing strings.</summary>
public class NewLineData
{
    #region Internal Fields

    internal readonly byte[] Data;

    #endregion Internal Fields

    #region Public Constructors

    /// <summary>Creates a new instance of the <see cref="NewLineData"/> class.</summary>
    /// <param name="encoding">Encoding to use.</param>
    /// <param name="mode">New line mode</param>
    /// <exception cref="NotImplementedException"></exception>
    public NewLineData(StringEncoding encoding, NewLineMode mode)
    {
        var lineFeed = mode switch
        {
            NewLineMode.CR => ASCII.Strings.CR,
            NewLineMode.LF => ASCII.Strings.LF,
            NewLineMode.CRLF => ASCII.Strings.CRLF,
            _ => throw new NotImplementedException($"NewLineMode {mode} not implemented!")
        };
        Data = encoding.Encode(lineFeed, withRoundtripTest: true);
        LineFeed = lineFeed;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets the line feed characters.</summary>
    public string LineFeed { get; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Gets the line feed bytes</summary>
    /// <returns>Returns a new array of bytes containing the encoded line feed characters.</returns>
    public byte[] ToArray() => (byte[])Data.Clone();

    #endregion Public Methods
}
