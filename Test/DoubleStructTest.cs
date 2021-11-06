using System;
using Cave;
using NUnit.Framework;
using Cave.IO;

namespace Test.Cave.IO
{
    [TestFixture]
    public class DoubleStructTest
    {
        #region Public Methods

        [Test]
        public void Test_DoubleStruct_ToDouble()
        {
            foreach (var value in new double[]
            {
                double.Epsilon,
                double.MaxValue,
                double.MinValue,
                double.NaN,
                double.NegativeInfinity,
                double.PositiveInfinity,
                0d
            })
            {
                var a = BitConverter.ToUInt64(BitConverter.GetBytes(value), 0);
                var b = BitConverter.ToInt64(BitConverter.GetBytes(value), 0);
                Assert.AreEqual(value, DoubleStruct.ToDouble(a));
                Assert.AreEqual(value, DoubleStruct.ToDouble(b));

                IBitConverter bc = Endian.MachineType switch
                {
                    EndianType.BigEndian => new BitConverterBE(),
                    EndianType.LittleEndian => new BitConverterLE(),
                    _ => throw new NotSupportedException()
                };

                var x = bc.ToUInt64(bc.GetBytes(value), 0);
                var y = bc.ToInt64(bc.GetBytes(value), 0);
                Assert.AreEqual(value, DoubleStruct.ToDouble(x));
                Assert.AreEqual(value, DoubleStruct.ToDouble(y));
            }
        }

        [Test]
        public void Test_DoubleStruct_ToInt64()
        {
            foreach (var value in new double[] { double.Epsilon, double.MaxValue, double.MinValue, double.NaN, double.NegativeInfinity, double.PositiveInfinity, 0d })
            {
                var b = BitConverter.ToInt64(BitConverter.GetBytes(value), 0);
                Assert.AreEqual(b, DoubleStruct.ToInt64(value));
            }
        }

        [Test]
        public void Test_DoubleStruct_ToUInt64()
        {
            foreach (var value in new double[] { double.Epsilon, double.MaxValue, double.MinValue, double.NaN, double.NegativeInfinity, double.PositiveInfinity, 0d })
            {
                var a = BitConverter.ToUInt64(BitConverter.GetBytes(value), 0);
                Assert.AreEqual(a, DoubleStruct.ToUInt64(value));
            }
        }

        #endregion Public Methods
    }
}
