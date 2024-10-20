using System;
using System.Diagnostics.CodeAnalysis;

namespace Cave.IO;

/// <summary>Provides a id in binary form. This is much more memory efficient if storing a large amount of guids.</summary>
public sealed class BinaryGuid : IComparable<BinaryGuid>, IComparable
{
    #region Private Fields

    byte[] data = [];

    #endregion Private Fields

    #region Public Methods

    /// <summary>Performs an implicit conversion from <see cref="string"/> to <see cref="BinaryGuid"/>.</summary>
    /// <param name="guid">The unique identifier.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator BinaryGuid?(string? guid) => guid is null ? null : new Guid(guid);

    /// <summary>Performs an implicit conversion from <see cref="Guid"/> to <see cref="BinaryGuid"/>.</summary>
    /// <param name="id">The unique identifier.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator BinaryGuid(Guid id) => new() { data = id.ToByteArray() };

    /// <summary>Performs an implicit conversion from <see cref="BinaryGuid"/> to <see cref="Guid"/>.</summary>
    /// <param name="id">The unique identifier.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator Guid(BinaryGuid id) => id?.ToGuid() ?? Guid.Empty;

    /// <summary>Implements the operator !=.</summary>
    /// <param name="g1">The first instance.</param>
    /// <param name="g2">The second instance.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator !=(BinaryGuid? g1, BinaryGuid? g2) => !Equals(g1?.ToString(), g2?.ToString());

    /// <inheritdoc/>
    public static bool operator <(BinaryGuid? left, BinaryGuid? right) => left is null ? right is not null : left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator <=(BinaryGuid? left, BinaryGuid? right) => left is null || left.CompareTo(right) <= 0;

    /// <summary>Implements the operator ==.</summary>
    /// <param name="g1">The first instance.</param>
    /// <param name="g2">The second instance.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator ==(BinaryGuid? g1, BinaryGuid? g2) => Equals(g1?.ToString(), g2?.ToString());

    /// <inheritdoc/>
    public static bool operator >(BinaryGuid? left, BinaryGuid? right) => left is not null && left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator >=(BinaryGuid? left, BinaryGuid? right) => left is null ? right is null : left.CompareTo(right) >= 0;

    /// <summary>Parses the specified text.</summary>
    /// <param name="text">The text.</param>
    /// <returns>the binary GUID.</returns>
    public static BinaryGuid Parse(string text)
    {
        if (text is null) throw new ArgumentNullException(nameof(text));
        var guid = new Guid(text);
        return new BinaryGuid { data = guid.ToByteArray() };
    }

    /// <summary>Tries to parse the specified id.</summary>
    /// <param name="text">The text.</param>
    /// <param name="id">The unique identifier.</param>
    /// <returns>true if parsing was successful.</returns>
    public static bool TryParse(string text, [MaybeNullWhen(false)] out BinaryGuid id)
    {
#if NET20 || NET35
        try
        {
            id = new Guid(text);
            return true;
        }
        catch
        {
            id = null;
            return false;
        }
#else
        if (Guid.TryParse(text, out var g))
        {
            id = g;
            return true;
        }

        id = null;
        return false;
#endif
    }

    /// <inheritdoc/>
    public int CompareTo(object? other) => string.CompareOrdinal(ToString(), other?.ToString());

    /// <inheritdoc/>
    public int CompareTo(BinaryGuid? other) => string.CompareOrdinal(ToString(), other?.ToString());

    /// <summary>Determines whether the specified <see cref="object"/>, is equal to this instance.</summary>
    /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
    /// <returns><c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(obj, this))
        {
            return true;
        }

        if (obj is null)
        {
            return false;
        }

        return string.Equals(ToString(), obj.ToString(), StringComparison.Ordinal);
    }

    /// <summary>Returns a hash code for this instance.</summary>
    /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
    public override int GetHashCode() => new Guid(data).GetHashCode();

    /// <summary>Converts to an byte array.</summary>
    /// <returns>Returns the byte array.</returns>
    public byte[] ToArray() => (byte[])data.Clone();

    /// <summary>Gets the <see cref="Guid"/> representation of this instance.</summary>
    /// <returns>A new <see cref="Guid"/> representation of this instance.</returns>
    public Guid ToGuid() => new(data);

    /// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
    /// <returns>A <see cref="string"/> that represents this instance.</returns>
    public override string ToString() => new Guid(data).ToString();

    #endregion Public Methods
}
