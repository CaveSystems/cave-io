using System;
using System.Text;

namespace Cave.IO
{
    /// <summary>Extensions to the <see cref="StringEncoding"/> enum.</summary>
    public static class StringEncodingExtensions
    {
        #region Public Methods

        /// <summary>Returns whether the encoding is dead (true) or not (false).</summary>
        /// <param name="encoding">Encoding to check.</param>
        /// <returns>Returns true for dead encodings.</returns>
        public static bool IsDead(this Encoding encoding)
        {
            if (encoding is null) throw new ArgumentNullException(nameof(encoding));
            return encoding.CodePage is >= 0xDEA0 and < 0xDF00;
        }

        /// <summary>Converts an encoding instance by codepage to the corresponding <see cref="StringEncoding"/> enum value.</summary>
        /// <param name="encoding">The encoding to convert.</param>
        /// <returns>Returns an enum value for the <see cref="Encoding.CodePage"/>.</returns>
        public static StringEncoding ToStringEncoding(this Encoding encoding)
        {
            if (encoding is null) throw new ArgumentNullException(nameof(encoding));
            return encoding.CodePage switch
            {
                (int)StringEncoding.UTF_16 => StringEncoding.UTF16,
                (int)StringEncoding.UTF_32 => StringEncoding.UTF32,
                (int)StringEncoding.UTF_8 => StringEncoding.UTF8,
                (int)StringEncoding.US_ASCII => StringEncoding.ASCII,
                _ => (StringEncoding)encoding.CodePage
            };
        }

        #endregion Public Methods
    }
}
