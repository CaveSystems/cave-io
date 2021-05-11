﻿using System;
using NUnit.Framework;
using Cave.IO;

namespace Test.Cave.IO
{
    [TestFixture]
    public class SingleStructTest
    {
        #region Public Methods

        [Test]
        public void Test_SingleStruct_ToInt64()
        {
            foreach (float value in new float[] { float.Epsilon, float.MaxValue, float.MinValue, float.NaN, float.NegativeInfinity, float.PositiveInfinity, 0f })
            {
                int b = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
                Assert.AreEqual(b, SingleStruct.ToInt32(value));
            }
        }

        [Test]
        public void Test_SingleStruct_ToSingle()
        {
            foreach (float value in new float[] { float.Epsilon, float.MaxValue, float.MinValue, float.NaN, float.NegativeInfinity, float.PositiveInfinity, 0f })
            {
                uint a = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);
                int b = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
                Assert.AreEqual(value, SingleStruct.ToSingle(a));
                Assert.AreEqual(value, SingleStruct.ToSingle(b));
            }
        }

        [Test]
        public void Test_SingleStruct_ToUInt64()
        {
            foreach (float value in new float[] { float.Epsilon, float.MaxValue, float.MinValue, float.NaN, float.NegativeInfinity, float.PositiveInfinity, 0f })
            {
                uint a = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);
                Assert.AreEqual(a, SingleStruct.ToUInt32(value));
            }
        }

        #endregion Public Methods
    }
}
