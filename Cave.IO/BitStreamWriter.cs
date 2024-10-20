using System;
using System.IO;

namespace Cave.IO;

/// <summary>Bit Stream Reader Class for Bitstreams of the form: byte0[bit0,bit1,bit2,bit3,bit4,bit5,bit6,bit7] byte1[bit8,bit9,bit10,bit11,...].</summary>
/// <remarks>Initializes a new instance of the <see cref="BitStreamWriter"/> class.</remarks>
/// <param name="stream">The stream to write to.</param>
public class BitStreamWriter(Stream stream)
{
    #region Private Fields

    int bufferedByte;
    int position;

    #endregion Private Fields

    bool isClosed;

    #region Public Properties

    /// <summary>Gets the BaseStream.</summary>
    public Stream BaseStream { get; private set; } = stream;

    /// <summary>Gets the length in bits.</summary>
    public long Length => (BaseStream.Length * 8) + position;

    /// <summary>Gets the current bitposition.</summary>
    public long Position => (BaseStream.Position * 8) + position;

    #endregion Public Properties

    #region Public Methods

    /// <summary>Closes the writer and the underlying stream.</summary>
    public void Close()
    {
        if (!isClosed)
        {
            isClosed = true;
            Flush();
#if NETSTANDARD13
            BaseStream?.Dispose();
#else
            BaseStream?.Close();
#endif
        }
    }

    /// <summary>Flushes the buffered bits to the stream and closes the writer (not the underlying stream).</summary>
    public void Flush()
    {
        isClosed = true;
        if (position > 0)
        {
            BaseStream.WriteByte((byte)bufferedByte);
        }
    }

    /// <summary>writes a bit to the buffer.</summary>
    /// <param name="bit">The bit.</param>
    public void WriteBit(bool bit)
    {
        if (isClosed) throw new InvalidOperationException("Stream already closed!");
        if (bit)
        {
            var bitmask = 1 << (7 - position);
            bufferedByte |= bitmask;
        }

        if (++position > 7)
        {
            BaseStream.WriteByte((byte)bufferedByte);
            bufferedByte = 0;
            position = 0;
        }
    }

    /// <summary>writes some bits.</summary>
    /// <param name="bits">The bits to write.</param>
    /// <param name="count">Number of bits to write.</param>
    public void WriteBits(long bits, int count)
    {
        if (isClosed) throw new InvalidOperationException("Stream already closed!");
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (Math.Abs(count) > 63)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        for (var i = count - 1; i > -1; i--)
        {
            WriteBit(((bits >> i) & 1) != 0);
        }
    }

    /// <summary>writes some bits.</summary>
    /// <param name="bits">The bits to write.</param>
    /// <param name="count">Number of bits to write.</param>
    public void WriteBits(int bits, int count)
    {
        if (isClosed) throw new InvalidOperationException("Stream already closed!");
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (Math.Abs(count) > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        for (var i = count - 1; i > -1; i--)
        {
            WriteBit(((bits >> i) & 1) != 0);
        }
    }

    /// <summary>writes some bits (todo: optimize me).</summary>
    /// <param name="count">Number of bits to write.</param>
    /// <param name="bit">The bit to write count times.</param>
    public void WriteBits(int count, bool bit)
    {
        if (isClosed) throw new InvalidOperationException("Stream already closed!");
        for (var i = 0; i < count; i++)
        {
            WriteBit(bit);
        }
    }

    #endregion Public Methods

    #region overrides

    /// <summary>Gets a hash code for this object.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => base.GetHashCode();

    /// <summary>Gets the name of the class and the current state.</summary>
    /// <returns>The class name and the current state.</returns>
    public override string ToString()
    {
        var result = base.ToString();
        if (BaseStream != null)
        {
            if (BaseStream.CanSeek)
            {
                result += " [" + Position + "/" + Length + "]";
            }
            else
            {
                result += " opened";
            }
        }
        else
        {
            result += " closed";
        }

        return result;
    }

    #endregion overrides
}
