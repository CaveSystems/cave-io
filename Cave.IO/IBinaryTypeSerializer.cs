using System;

namespace Cave.IO
{
    /// <summary>
    /// Provides an interface for binary serialization.
    /// </summary>
    public interface IBinaryTypeSerializer
    {
        #region Public Methods

        /// <summary>
        /// Checks whether a type can be serialized using this serializer or not.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>Returns true if deserialization is available, false otherwise.</returns>
        public bool CanDeserialize(Type type);

        /// <summary>
        /// Checks whether an item can be serialized using this deserializer or not.
        /// </summary>
        /// <param name="item">Item to serialize</param>
        /// <returns>Returns true if serialization is available, false otherwise.</returns>
        public bool CanSerialize(object item);

        /// <summary>
        /// Deserializes the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="reader">Reader to read from.</param>
        /// <param name="type">Type to deserialize.</param>
        /// <returns>Returns a new object with the deserialized data.</returns>
        public object Deserialize(DataReader reader, Type type);

        /// <summary>
        /// Serializes the specified <paramref name="item"/>.
        /// </summary>
        /// <param name="writer">Writer to write to.</param>
        /// <param name="item">Item to serialize.</param>
        public void Serialize(DataWriter writer, object item);

        #endregion Public Methods
    }
}
