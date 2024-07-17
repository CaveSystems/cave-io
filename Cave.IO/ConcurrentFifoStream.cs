using System;
using System.IO;

namespace Cave.IO;

/// <summary>Provides a concurrent version of <see cref="FifoStream"/></summary>
public class ConcurrentFifoStream : IFifoStream
{
    #region Private Fields

    IFifoStream baseStream;

    #endregion Private Fields

    #region Private Methods

    void Locked(Action action) { lock (baseStream) action(); }

    TResult Locked<TResult>(Func<TResult> func) { lock (baseStream) return func(); }

    #endregion Private Methods

    #region Public Constructors

    /// <summary>Creates a new synchonized fifo stream.</summary>
    /// <param name="baseStream"></param>
    public ConcurrentFifoStream(IFifoStream baseStream) => this.baseStream = baseStream;

    #endregion Public Constructors

    #region Public Properties

    /// <inheritdoc/>
    public int Available => Locked(() => baseStream.Available);

    /// <inheritdoc/>
    public int BufferCount => Locked(() => baseStream.BufferCount);

    /// <inheritdoc/>
    public long Length => Locked(() => baseStream.Length);

    /// <inheritdoc/>
    public long Position { get => Locked(() => baseStream.Position); set => Locked(() => baseStream.Position = value); }

    #endregion Public Properties

    #region Public Indexers

    /// <inheritdoc/>
    public byte this[int index] => Locked(() => baseStream[index]);

    #endregion Public Indexers

    #region Public Methods

    /// <inheritdoc/>
    public void AppendBuffer(byte[] buffer, int offset, int count) => Locked(() => baseStream.AppendBuffer(buffer, offset, count));

    /// <inheritdoc/>
    public long AppendStream(Stream source) => Locked(() => baseStream.AppendStream(source));

    /// <inheritdoc/>

    public int AppendStream(Stream source, int count) => Locked(() => baseStream.AppendStream(source, count));

    /// <inheritdoc/>
    public void Clear() => Locked(baseStream.Clear);

    /// <inheritdoc/>
    public bool Contains(byte b) => Locked(() => baseStream.Contains(b));

    /// <inheritdoc/>
    public bool Contains(byte[] data) => Locked(() => baseStream.Contains(data));

    /// <inheritdoc/>
    public void Flush() => Locked(baseStream.Flush);

    /// <inheritdoc/>
    public int FreeBuffers() => Locked(baseStream.FreeBuffers);

    /// <inheritdoc/>
    public void FreeBuffers(int sizeToKeep) => Locked(() => baseStream.FreeBuffers(sizeToKeep));

    /// <inheritdoc/>
    public int IndexOf(byte b) => Locked(() => baseStream.IndexOf(b));

    /// <inheritdoc/>
    public int IndexOf(byte[] data) => Locked(() => baseStream.IndexOf(data));

    /// <inheritdoc/>
    public int PeekByte() => Locked(baseStream.PeekByte);

    /// <inheritdoc/>
    public void PutBuffer(byte[] buffer) => Locked(() => baseStream.PutBuffer(buffer));

    /// <inheritdoc/>
    public int Read(byte[] buffer, int offset, int count) => Locked(() => baseStream.Read(buffer, offset, count));

    /// <inheritdoc/>
    public int ReadByte() => Locked(baseStream.ReadByte);

    /// <inheritdoc/>
    public long Seek(long offset, SeekOrigin origin) => Locked(() => baseStream.Seek(offset, origin));

    /// <inheritdoc/>
    public byte[] ToArray() => Locked(baseStream.ToArray);

    /// <inheritdoc/>
    public void Write(byte[] buffer, int offset, int count) => Locked(() => baseStream.Write(buffer, offset, count));

    #endregion Public Methods
}
