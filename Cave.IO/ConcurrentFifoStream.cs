using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Cave.IO;

/// <summary>Provides a concurrent version of <see cref="FifoStream"/></summary>
/// <remarks>Creates a new synchonized fifo stream.</remarks>
/// <param name="baseStream"></param>
public class ConcurrentFifoStream(IFifoStream baseStream) : IFifoStream
{
    #region Private Methods

    [MethodImpl(256)]
    void Locked(Action action) { lock (baseStream) action(); }

    [MethodImpl(256)]
    TResult Locked<TResult>(Func<TResult> func) { lock (baseStream) return func(); }

    #endregion Private Methods

    #region Public Properties

    /// <inheritdoc/>
    public int Available
    {
        [MethodImpl(256)]
        get => Locked(() => baseStream.Available);
    }

    /// <inheritdoc/>
    public int BufferCount
    {
        [MethodImpl(256)]
        get => Locked(() => baseStream.BufferCount);
    }

    /// <inheritdoc/>
    public long Length
    {
        [MethodImpl(256)]
        get => Locked(() => baseStream.Length);
    }

    /// <inheritdoc/>
    public long Position
    {
        [MethodImpl(256)]
        get => Locked(() => baseStream.Position);
        [MethodImpl(256)]
        set => Locked(() => baseStream.Position = value);
    }

    #endregion Public Properties

    #region Public Indexers

    /// <inheritdoc/>
    public byte this[int index]
    {
        [MethodImpl(256)]
        get => Locked(() => baseStream[index]);
    }

    #endregion Public Indexers

    #region Public Methods

    /// <inheritdoc/>
    [MethodImpl(256)]
    public void AppendBuffer(byte[] buffer, int offset, int count) => Locked(() => baseStream.AppendBuffer(buffer, offset, count));

    /// <inheritdoc/>
    [MethodImpl(256)]
    public long AppendStream(Stream source) => Locked(() => baseStream.AppendStream(source));

    /// <inheritdoc/>
    [MethodImpl(256)]
    public int AppendStream(Stream source, int count) => Locked(() => baseStream.AppendStream(source, count));

    /// <inheritdoc/>
    [MethodImpl(256)]
    public void Clear() => Locked(baseStream.Clear);

    /// <inheritdoc/>
    [MethodImpl(256)]
    public bool Contains(byte b) => Locked(() => baseStream.Contains(b));

    /// <inheritdoc/>
    [MethodImpl(256)]
    public bool Contains(byte[] data) => Locked(() => baseStream.Contains(data));

    /// <inheritdoc/>
    [MethodImpl(256)]
    public void Flush() => Locked(baseStream.Flush);

    /// <inheritdoc/>
    [MethodImpl(256)]
    public int FreeBuffers() => Locked(baseStream.FreeBuffers);

    /// <inheritdoc/>
    [MethodImpl(256)]
    public void FreeBuffers(int sizeToKeep) => Locked(() => baseStream.FreeBuffers(sizeToKeep));

    /// <inheritdoc/>
    [MethodImpl(256)]
    public int IndexOf(byte b) => Locked(() => baseStream.IndexOf(b));

    /// <inheritdoc/>
    [MethodImpl(256)]
    public int IndexOf(byte[] data) => Locked(() => baseStream.IndexOf(data));

    /// <inheritdoc/>
    [MethodImpl(256)]
    public int PeekByte() => Locked(baseStream.PeekByte);

    /// <inheritdoc/>
    [MethodImpl(256)]
    public void PutBuffer(byte[] buffer) => Locked(() => baseStream.PutBuffer(buffer));

    /// <inheritdoc/>
    [MethodImpl(256)]
    public int Read(byte[] buffer, int offset, int count) => Locked(() => baseStream.Read(buffer, offset, count));

    /// <inheritdoc/>
    [MethodImpl(256)]
    public int ReadByte() => Locked(baseStream.ReadByte);

    /// <inheritdoc/>
    [MethodImpl(256)]
    public long Seek(long offset, SeekOrigin origin) => Locked(() => baseStream.Seek(offset, origin));

    /// <inheritdoc/>
    [MethodImpl(256)]
    public byte[] ToArray() => Locked(baseStream.ToArray);

    /// <inheritdoc/>
    [MethodImpl(256)]
    public void Write(byte[] buffer, int offset, int count) => Locked(() => baseStream.Write(buffer, offset, count));

    /// <inheritdoc/>
    [MethodImpl(256)]
    public long FastCopyTo(Stream stream) => Locked(() => baseStream.FastCopyTo(stream));

    #endregion Public Methods
}
