using NUnit.Framework;

using System;
using Cave.IO;

namespace Tests.Cave.IO
{
    [TestFixture]
    public class DoubleStructTest
    {
        #region Public Methods

        [Test]
        public void ToDouble()
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
                IBitConverter bc = Endian.MachineType.GetBitConverter();
                var x = bc.ToUInt64(bc.GetBytes(value), 0);
                var y = bc.ToInt64(bc.GetBytes(value), 0);
                Assert.AreEqual(value, DoubleStruct.ToDouble(x));
                Assert.AreEqual(value, DoubleStruct.ToDouble(y));
            }
        }

        [Test]
        public void ToInt64()
        {
            foreach (var value in new double[] { double.Epsilon, double.MaxValue, double.MinValue, double.NaN, double.NegativeInfinity, double.PositiveInfinity, 0d })
            {
                var b = BitConverter.ToInt64(BitConverter.GetBytes(value), 0);
                Assert.AreEqual(b, DoubleStruct.ToInt64(value));
            }
        }

        [Test]
        public void ToUInt64()
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
