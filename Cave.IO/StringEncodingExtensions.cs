using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Cave.IO;

/// <summary>Extensions to the <see cref="StringEncoding"/> enum.</summary>
public static class StringEncodingExtensions
{
    #region Public Methods

    /// <summary>
    /// Checks whether the encoding is able to roundtrip encode and decode of the specified string. This is always true for unicode but is dependant on the used
    /// characters for codepage encodings.
    /// </summary>
    /// <param name="encoding">StringEncoding</param>
    /// <param name="text">Text to test</param>
    /// <returns>Returns true if the roundtrip preserves all characters, false otherwise.</returns>
    public static bool CanRoundtrip(this StringEncoding encoding, string text)
    {
        try
        {
            switch (encoding)
            {
                case (StringEncoding)1: // StringEncoding.ASCII:
                case StringEncoding.US_ASCII: return ASCII.IsClean(text);

                case (StringEncoding)2: // StringEncoding.UTF8:
                case StringEncoding.UTF_8:
                case (StringEncoding)3: // StringEncoding.UTF16:
                case StringEncoding.UTF_16:
                case StringEncoding.UTF_16BE:
                case (StringEncoding)4: // StringEncoding.UTF32:
                case StringEncoding.UTF_32:
                case StringEncoding.UTF_32BE:
                case StringEncoding.UTF_7: return true;
                default: break;
            }

            var block = encoding.Encode(text);
            var roundtrip = encoding.Decode(block);
            return text == roundtrip;
        }
        catch { return false; }
    }

    /// <summary>Creates a new encoding instance for the specified <paramref name="encoding"/>.</summary>
    /// <param name="encoding">The encoding to create.</param>
    /// <returns>Returns a new <see cref="Encoding"/> instance.</returns>
    public static Encoding Create(this StringEncoding encoding) =>
        encoding switch
        {
            StringEncoding.Undefined => throw new InvalidOperationException($"{nameof(StringEncoding)} {encoding} is undefined!"),
            (StringEncoding)1 or StringEncoding.US_ASCII => new CheckedASCIIEncoding(),
            (StringEncoding)2 or StringEncoding.UTF_8 => Encoding.UTF8,
            (StringEncoding)3 or StringEncoding.UTF_16 => Encoding.Unicode,
            StringEncoding.UTF_16BE => Encoding.BigEndianUnicode,
            (StringEncoding)4 or StringEncoding.UTF_32 => Encoding.UTF32,
#pragma warning disable SYSLIB0001
            StringEncoding.UTF_7 => Encoding.UTF7,
#pragma warning restore SYSLIB0001
            _ => Encoding.GetEncoding((int)encoding)
        };

    /// <summary>Decodes the specified byte block to a csharp string and removes the byte order mark if present.</summary>
    /// <param name="encoding">StringEncoding</param>
    /// <param name="block">Byte array</param>
    /// <param name="start">Start index at the byte array</param>
    /// <param name="length">Number of bytes</param>
    /// <returns>Returns a new string instance.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string Decode(this StringEncoding encoding, byte[] block, int start = 0, int length = -1)
    {
        if (block is null) throw new ArgumentNullException(nameof(block));
        if (start > 0 || length > -1) block = block.GetRange(start, length);
        switch (encoding)
        {
            case (StringEncoding)1: // StringEncoding.ASCII:
            case StringEncoding.US_ASCII: return ASCII.GetString(block);

            case (StringEncoding)2: // StringEncoding.UTF8:
            case StringEncoding.UTF_8: return new UTF8(block).RemoveByteOrderMark().ToString()!;

            case (StringEncoding)3: // StringEncoding.UTF16:
            case StringEncoding.UTF_16: return new UTF16LE(block).RemoveByteOrderMark().ToString()!;

            case StringEncoding.UTF_16BE: return new UTF16BE(block).RemoveByteOrderMark().ToString()!;

            case (StringEncoding)4: // StringEncoding.UTF32:
            case StringEncoding.UTF_32: return new UTF32LE(block).RemoveByteOrderMark().ToString()!;

            case StringEncoding.UTF_32BE: return new UTF32BE(block).RemoveByteOrderMark().ToString()!;

            case StringEncoding.UTF_7: return UTF7.Decode(block);
            default: break;
        }
        var textEncoding = encoding.Create();
        var bom = textEncoding.GetPreamble();
        start = 0;
        if (bom != null && bom.Length > 0 && block.StartsWith(bom))
        {
            start = bom.Length;
            //block = block.GetRange(bom.Length);
        }
        var chars = new char[block.Length];
        var decoder = textEncoding.GetDecoder();
        decoder.Convert(block, start, block.Length - start, chars, 0, chars.Length, false, out var bytesUsed, out var charsUsed, out var completed);
        //return textEncoding.GetString(block);
        return new string(chars, 0, charsUsed);
    }

    /// <summary>Encodes the specified text to a byte buffer and optionally prefixes the string with the byte order mark.</summary>
    /// <param name="encoding">StringEncoding</param>
    /// <param name="text">Text to encode</param>
    /// <param name="withByteOrderMark">Write the byte order mark</param>
    /// <param name="withRoundtripTest">Perform a roundtrip test for the specified string. This will tell you missing character mappings at codepage encodings.</param>
    /// <returns>Returns a byte array containing the encoded data.</returns>
    /// <exception cref="NotSupportedException">Thrown for dead encodings</exception>
    public static byte[] Encode(this StringEncoding encoding, string text, bool withByteOrderMark = false, bool withRoundtripTest = false)
    {
        IUnicode ApplyBOM(IUnicode instance) => withByteOrderMark ? instance.AddByteOrderMark() : instance;

        switch (encoding)
        {
            case (StringEncoding)1: // StringEncoding.ASCII:
            case StringEncoding.US_ASCII: return ASCII.GetBytes(text);

            case (StringEncoding)2: // StringEncoding.UTF8:
            case StringEncoding.UTF_8: return ApplyBOM((UTF8)text).Data;

            case (StringEncoding)3: // StringEncoding.UTF16:
            case StringEncoding.UTF_16: return ApplyBOM((UTF16LE)text).Data;
            case StringEncoding.UTF_16BE: return ApplyBOM((UTF16BE)text).Data;

            case (StringEncoding)4: // StringEncoding.UTF32:
            case StringEncoding.UTF_32: return ApplyBOM((UTF32LE)text).Data;

            case StringEncoding.UTF_32BE: return ApplyBOM((UTF32BE)text).Data;

            case StringEncoding.UTF_7: return UTF7.Encode(text);
            default: break;
        }

        var encoder = encoding.Create();
        if (encoder.IsDead())
        {
            throw new NotSupportedException($"Encoding {encoding} does not support direct char writing!");
        }
        var result = encoder.GetBytes(text);
        if (withRoundtripTest)
        {
            var decoder = encoding.Create();
            var roundtrip = decoder.GetString(result);
            if (roundtrip != text)
            {
                throw new NotSupportedException("The specified string cannot be encoded and decoded without character loss!");
            }
        }
        if (withByteOrderMark)
        {
            var bom = encoder.GetPreamble();
            if (bom != null && bom.Length > 0)
            {
                return bom.Concat(result);
            }
        }
        return result;
    }

    /// <summary>Gets the byte order mark to use for the specified <paramref name="encoding"/>.</summary>
    /// <param name="encoding">Encoding to use.</param>
    /// <returns>Returns the bytes used as BOM.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static byte[] GetByteOrderMark(this StringEncoding encoding) => encoding switch
    {
        StringEncoding.Undefined => throw new InvalidOperationException($"{nameof(StringEncoding)} {encoding} is undefined!"),
        (StringEncoding)1 or StringEncoding.US_ASCII => new byte[0],
        (StringEncoding)2 or StringEncoding.UTF_8 => new UTF8().ByteOrderMark,
        (StringEncoding)3 or StringEncoding.UTF_16 => new UTF16LE().ByteOrderMark,
        StringEncoding.UTF_16BE => new UTF16BE().ByteOrderMark,
        (StringEncoding)4 or StringEncoding.UTF_32 => new UTF32LE().ByteOrderMark,
        StringEncoding.UTF_32BE => new UTF32BE().ByteOrderMark,
        StringEncoding.UTF_7 => throw new InvalidOperationException("UTF7 uses an embedded UTF16BE BOM."),
        _ => Encoding.GetEncoding((int)encoding).GetPreamble(),
    };

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

    /// <summary>Returns whether the encoding is dead (true) or not (false).</summary>
    /// <param name="encoding">Encoding to check.</param>
    /// <returns>Returns true for dead encodings.</returns>
    public static bool IsDead(this StringEncoding encoding) => (int)encoding is >= 0xDEA0 and < 0xDF00;

    /// <summary>Converts an encoding instance by codepage to the corresponding <see cref="StringEncoding"/> enum value.</summary>
    /// <param name="encoding">The encoding to convert.</param>
    /// <returns>Returns an enum value for the <see cref="Encoding.CodePage"/>.</returns>
    public static StringEncoding ToStringEncoding(this Encoding encoding)
    {
        if (encoding is null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }
        return (StringEncoding)encoding.CodePage;
    }

    #endregion Public Methods
}
