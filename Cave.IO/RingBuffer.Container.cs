
namespace Cave.IO;

partial class RingBuffer<TValue>
{
    #region Private Classes

    sealed class Container(TValue value)
    {
        #region Public Fields

        public readonly TValue Value = value;

        #endregion Public Fields
    }

    #endregion Private Classes
}
