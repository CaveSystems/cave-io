namespace Cave.IO.Blob;

/// <summary>Defines a contract for types that are capable of serializing themselves into a binary blob format and deserializing themselves from it.</summary>
public interface IBlobConvertible
{
    #region Public Methods

    /// <summary>Deserializes the specified byte array (blob) and returns the reconstructed object.</summary>
    /// <param name="blob">The byte array containing the serialized data, previously produced by <see cref="ToBlob"/>.</param>
    /// <returns>The object reconstructed from the blob data.</returns>
    object FromBlob(byte[] blob);

    /// <summary>Serializes the current instance into a binary byte array (blob).</summary>
    /// <returns>A <see cref="byte"/> array containing the serialized representation of the current instance.</returns>
    byte[] ToBlob();

    #endregion Public Methods
}
