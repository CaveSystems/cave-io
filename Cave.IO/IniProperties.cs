using Cave.Security;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Cave.IO;

/// <summary>Provides properties for the <see cref="IniReader"/> and <see cref="IniWriter"/> classes.</summary>
public struct IniProperties : IEquatable<IniProperties>, IDisposable
{
    #region Public Fields

    CultureInfo culture;

    /// <summary>Gets or sets the string boxing character.</summary>
    public char BoxCharacter { get; set; }

    /// <summary>Default is case insensitive. Set this to true to match properties exactly.</summary>
    public bool CaseSensitive { get; set; }

    /// <summary>Gets / sets the <see cref="IniCompressionType"/>.</summary>
    public IniCompressionType Compression { get; set; }

    /// <summary>Gets / sets the culture used to en/decode values.</summary>
    public CultureInfo Culture
    {
        readonly get => culture ?? CultureInfo.InvariantCulture;
        set => culture = value;
    }

    /// <summary>Gets / sets the format of date time fields.</summary>
    public string DateTimeFormat { get; set; }

    /// <summary>Gets / sets the kind for date time fields.</summary>
    public DateTimeKind DateTimeKind { get; set; }

    /// <summary>Disable un/-escaping of ascii characters (backslash escaping).</summary>
    public bool DisableEscaping { get; set; }

    /// <summary>Gets / sets the <see cref="Encoding"/>.</summary>
    public Encoding Encoding { get; set; }

    /// <summary>
    /// Use simple synchronous encryption to protect from users eyes ? (This is not a security feature, use file system acl to protect from other users.)
    /// </summary>
    public SymmetricAlgorithm Encryption { get; set; }

    #endregion Public Fields

    #region Public Properties

    /// <summary>Gets <see cref="IniProperties"/> with default settings: Encoding=UTF8, Compression=None, InvariantCulture and no encryption.</summary>
    public static IniProperties Default
    {
        get
        {
            var result = new IniProperties
            {
                Culture = CultureInfo.InvariantCulture,
                Compression = IniCompressionType.None,
                Encoding = new UTF8Encoding(false),
                DateTimeFormat = StringExtensions.InteropDateTimeFormat,
                DateTimeKind = DateTimeKind.Local,
                BoxCharacter = '"',
            };
            return result;
        }
    }

    /// <summary>Gets a value indicating whether the properties are all set or not.</summary>
    public readonly bool Valid => Enum.IsDefined(typeof(IniCompressionType), Compression) && (Encoding != null) && (Culture != null) && (DateTimeKind is DateTimeKind.Local or DateTimeKind.Utc);

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Obtains <see cref="IniProperties"/> with default settings and simple encryption. (This is not a security feature, use file system acl to protect from
    /// other users.)
    /// </summary>
    /// <param name="password">Password to use.</param>
    /// <returns>Returns a new <see cref="IniProperties"/> instance.</returns>
    [Obsolete("This method is provided for compatibility reasons only.")]
    public static IniProperties Encrypted(string password)
    {
        var salt = new byte[16];
        for (byte i = 0; i < salt.Length; salt[i] = ++i)
        {
        }

        var pbkdf1 = new PasswordDeriveBytes(password, salt);
        var result = Default;
        result.Encryption = new RijndaelManaged { BlockSize = 128, };
#pragma warning disable CA5373
        result.Encryption.Key = pbkdf1.GetBytes(result.Encryption.KeySize / 8);
        result.Encryption.IV = pbkdf1.GetBytes(result.Encryption.BlockSize / 8);
#pragma warning restore CA5373
        (pbkdf1 as IDisposable)?.Dispose();
        return result;
    }

    /// <summary>
    /// Obtains <see cref="IniProperties"/> with default settings and simple encryption. (This is not a security feature, use file system acl to protect from
    /// other users.)
    /// </summary>
    /// <param name="password">Password to use.</param>
    /// <param name="salt">Salt to use.</param>
    /// <param name="iterations">Number of iterations.</param>
    /// <returns>Returns a new <see cref="IniProperties"/> instance.</returns>
    public static IniProperties Encrypted(string password, byte[] salt, int iterations = 20000)
    {
        var pbkdf2 = new PBKDF2(password, salt, iterations);
        var result = Default;

#if NET40_OR_GREATER || NETSTANDARD1_0_OR_GREATER || NET5_0_OR_GREATER
        result.Encryption = Aes.Create();
#else
        result.Encryption = new RijndaelManaged();
#endif
        result.Encryption.BlockSize = 256;
        result.Encryption.Key = pbkdf2.GetBytes(result.Encryption.KeySize / 8);
        result.Encryption.IV = pbkdf2.GetBytes(result.Encryption.BlockSize / 8);
        (pbkdf2 as IDisposable)?.Dispose();
        return result;
    }

    /// <summary>Implements the operator !=.</summary>
    /// <param name="properties1">The properties1.</param>
    /// <param name="properties2">The properties2.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator !=(IniProperties properties1, IniProperties properties2) => !properties1.Equals(properties2);

    /// <summary>Implements the operator ==.</summary>
    /// <param name="properties1">The properties1.</param>
    /// <param name="properties2">The properties2.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator ==(IniProperties properties1, IniProperties properties2) => properties1.Equals(properties2);

    /// <inheritdoc/>
    public readonly void Dispose()
    {
        if (Encryption is IDisposable disposable) disposable.Dispose();
    }

    /// <summary>Determines whether the specified <see cref="object"/>, is equal to this instance.</summary>
    /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
    /// <returns><c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
    public override readonly bool Equals(object obj) => obj is IniProperties other && Equals(other);

    /// <summary>Determines whether the specified <see cref="IniProperties"/>, is equal to this instance.</summary>
    /// <param name="other">The <see cref="IniProperties"/> to compare with this instance.</param>
    /// <returns><c>true</c> if the specified <see cref="IniProperties"/> is equal to this instance; otherwise, <c>false</c>.</returns>
    public readonly bool Equals(IniProperties other) => other.CaseSensitive == CaseSensitive
            && other.Compression == Compression
            && other.Culture == Culture
            && other.DateTimeFormat == DateTimeFormat
            && other.Encoding == Encoding
            && other.BoxCharacter == BoxCharacter
            && other.Encryption == Encryption;

    /// <summary>Returns a hash code for this instance.</summary>
    /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
    public override readonly int GetHashCode() => base.GetHashCode();

    #endregion Public Methods
}
