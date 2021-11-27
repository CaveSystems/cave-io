using System;
using System.IO;

namespace Cave.IO
{
    /// <summary>Bit Stream Reader Class for reversed bitstreams of the form: byte0[bit7,bit6,bit5,bit4,bit3,bit2,bit1,bit0] byte1[bit15,bit14,bit13,bit12,...].</summary>
    public class BitStreamReaderReverse
    {
        #region Private Fields

        int bufferedByte;
        int position = 8;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>Initializes a new instance of the <see cref="BitStreamReaderReverse"/> class.</summary>
        /// <param name="stream">The stream to read from.</param>
        public BitStreamReaderReverse(Stream stream) => BaseStream = stream;

        #endregion Public Constructors

        #region Public Properties

        /// <summary>Gets the BaseStream.</summary>
        public Stream BaseStream { get; }

        /// <summary>
        /// Gets a value indicating whether the end of stream is reached during bit reading. This can always be called, even if the stream cannot seek.
        /// </summary>
        public bool EndOfStream
        {
            get
            {
                if (position > 7)
                {
                    bufferedByte = BaseStream.ReadByte();
                    if (bufferedByte == -1)
                    {
                        return true;
                    }

                    position = 0;
                }

                return false;
            }
        }

        /// <summary>Gets the length in bits.</summary>
        public long Length => BaseStream.Length * 8;

        /// <summary>Gets or sets the current bitposition.</summary>
        public long Position
        {
            get
            {
                var pos = BaseStream.Position * 8;
                if (position < 8)
                {
                    pos += position - 8;
                }

                return pos;
            }
            set
            {
                BaseStream.Position = value / 8;
                var diff = value % 8;
                position = 8;
                if (diff == 0)
                {
                    return;
                }

                position = (int)diff;
                bufferedByte = BaseStream.ReadByte();
                if (bufferedByte == -1)
                {
                    throw new EndOfStreamException();
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>reads a bit from the buffer.</summary>
        /// <returns>A bit.</returns>
        public int ReadBit()
        {
            if (position > 7)
            {
                bufferedByte = BaseStream.ReadByte();
                if (bufferedByte == -1)
                {
                    throw new EndOfStreamException();
                }

                position = 0;
            }

            return (bufferedByte >> position++) & 1;
        }

        /// <summary>reads some bits.</summary>
        /// <param name="count">Number of bits to read.</param>
        /// <returns>Number of bits read.</returns>
        public int ReadBits32(int count)
        {
            if (Math.Abs(count) > 31)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var result = 0;
            while (count-- > 0)
            {
                var bit = ReadBit();
                result = (result << 1) | bit;
            }

            return result;
        }

        /// <summary>reads some bits.</summary>
        /// <param name="count">Number of bits to read.</param>
        /// <returns>Number of bits read.</returns>
        public long ReadBits64(int count)
        {
            if (Math.Abs(count) > 63)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            long result = 0;
            while (count-- > 0)
            {
                long bit = ReadBit();
                result = (result << 1) | bit;
            }

            return result;
        }

        #endregion Public Methods

        #region overrides

        /// <summary>Gets a hash code for this object.</summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() => base.GetHashCode();

        /// <summary>Gets the name of the class and the current state.</summary>
        /// <returns>The classname and current state.</returns>
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
}
