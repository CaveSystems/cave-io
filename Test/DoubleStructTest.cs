using System;
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
            foreach (double value in new double[] { double.Epsilon, double.MaxValue, double.MinValue, double.NaN, double.NegativeInfinity, double.PositiveInfinity, 0d })
            {
                ulong a = BitConverter.ToUInt64(BitConverter.GetBytes(value), 0);
                long b = BitConverter.ToInt64(BitConverter.GetBytes(value), 0);
                Assert.AreEqual(value, DoubleStruct.ToDouble(a));
                Assert.AreEqual(value, DoubleStruct.ToDouble(b));
            }
        }

        [Test]
        public void Test_DoubleStruct_ToInt64()
        {
            foreach (double value in new double[] { double.Epsilon, double.MaxValue, double.MinValue, double.NaN, double.NegativeInfinity, double.PositiveInfinity, 0d })
            {
                long b = BitConverter.ToInt64(BitConverter.GetBytes(value), 0);
                Assert.AreEqual(b, DoubleStruct.ToInt64(value));
            }
        }

        [Test]
        public void Test_DoubleStruct_ToUInt64()
        {
            foreach (double value in new double[] { double.Epsilon, double.MaxValue, double.MinValue, double.NaN, double.NegativeInfinity, double.PositiveInfinity, 0d })
            {
                ulong a = BitConverter.ToUInt64(BitConverter.GetBytes(value), 0);
                Assert.AreEqual(a, DoubleStruct.ToUInt64(value));
            }
        }

        #endregion Public Methods
    }
}
