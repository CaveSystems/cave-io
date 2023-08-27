#nullable enable

namespace Cave.IO;

partial class RingBuffer<TValue>
{
    class Container
    {
        public Container(TValue value) => Value = value;

        public readonly TValue Value;
    }
}
