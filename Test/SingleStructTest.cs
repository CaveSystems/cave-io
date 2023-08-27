

using NUnit.Framework;

using System;
using Cave.IO;

namespace Tests.Cave.IO
{
    [TestFixture]
    public class SingleStructTest
    {
        #region Public Methods

        [Test]
        public void ToInt64()
        {
            foreach (var value in new float[] { float.Epsilon, float.MaxValue, float.MinValue, float.NaN, float.NegativeInfinity, float.PositiveInfinity, 0f })
            {
                var b = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
                Assert.AreEqual(b, SingleStruct.ToInt32(value));
            }
        }

        [Test]
        public void ToSingle()
        {
            foreach (var value in new float[] { float.Epsilon, float.MaxValue, float.MinValue, float.NaN, float.NegativeInfinity, float.PositiveInfinity, 0f })
            {
                var a = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);
                var b = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
                Assert.AreEqual(value, SingleStruct.ToSingle(a));
                Assert.AreEqual(value, SingleStruct.ToSingle(b));

                IBitConverter bc = Endian.MachineType switch
                {
                    EndianType.BigEndian => new BitConverterBE(),
                    EndianType.LittleEndian => new BitConverterLE(),
                    _ => throw new NotSupportedException()
                };

                var x = bc.ToUInt32(bc.GetBytes(value), 0);
                var y = bc.ToInt32(bc.GetBytes(value), 0);
                Assert.AreEqual(value, SingleStruct.ToSingle(x));
                Assert.AreEqual(value, SingleStruct.ToSingle(y));
            }
        }

        [Test]
        public void ToUInt64()
        {
            foreach (var value in new float[] { float.Epsilon, float.MaxValue, float.MinValue, float.NaN, float.NegativeInfinity, float.PositiveInfinity, 0f })
            {
                var a = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);
                Assert.AreEqual(a, SingleStruct.ToUInt32(value));
            }
        }

        #endregion Public Methods
    }
}
