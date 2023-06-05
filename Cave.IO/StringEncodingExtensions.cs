using System;
using System.Text;

namespace Cave.IO
{
    /// <summary>Extensions to the <see cref="StringEncoding" /> enum.</summary>
    public static class StringEncodingExtensions
    {
        #region Public Methods

        /// <summary>Returns whether the encoding is dead (true) or not (false).</summary>
        /// <param name="encoding">Encoding to check.</param>
        /// <returns>Returns true for dead encodings.</returns>
        public static bool IsDead(this Encoding encoding)
        {
            if (encoding is null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }
            return encoding.CodePage is >= 0xDEA0 and < 0xDF00;
        }

        /// <summary>Converts an encoding instance by codepage to the corresponding <see cref="StringEncoding" /> enum value.</summary>
        /// <param name="encoding">The encoding to convert.</param>
        /// <returns>Returns an enum value for the <see cref="Encoding.CodePage" />.</returns>
        public static StringEncoding ToStringEncoding(this Encoding encoding)
        {
            if (encoding is null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }
            return (StringEncoding)encoding.CodePage;
        }

        /// <summary>Creates a new encoding instance for the specified <paramref name="encoding" />.</summary>
        /// <param name="encoding">The encoding to create.</param>
        /// <returns>Returns a new <see cref="Encoding" /> instance.</returns>
        public static Encoding Create(this StringEncoding encoding) =>
            encoding switch
            {
                StringEncoding.Undefined => throw new InvalidOperationException($"{nameof(StringEncoding)} {encoding} is undefined!"),
                StringEncoding.ASCII => new CheckedASCIIEncoding(),
                StringEncoding.UTF8 => Encoding.UTF8,
                StringEncoding.UTF16 => Encoding.Unicode,
                StringEncoding.UTF32 => Encoding.UTF32,
                StringEncoding.US_ASCII => new CheckedASCIIEncoding(),
                StringEncoding.UTF_8 => Encoding.UTF8,
                StringEncoding.UTF_16 => Encoding.Unicode,
                StringEncoding.UTF_32 => Encoding.UTF32,
                _ => Encoding.GetEncoding((int)encoding)
            };

        #endregion Public Methods
    }
}
