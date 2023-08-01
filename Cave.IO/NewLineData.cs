#nullable enable

using System;

namespace Cave.IO;

public class NewLineData
{
    #region Internal Fields

    internal readonly byte[] Data;

    #endregion Internal Fields

    #region Public Constructors

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

    public string LineFeed { get; }

    #endregion Public Properties

    #region Public Methods

    public byte[] ToArray() => (byte[])Data.Clone();

    #endregion Public Methods
}
