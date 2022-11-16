using System;
using Cave.IO;
using NUnit.Framework;

namespace Tests.Cave.IO
{
    [TestFixture]
    public class BitConverterTests
    {
        #region Public Methods

        [Test]
        public void TestArgumentNull()
        {
            foreach (var bc in new BitConverterBase[]
            {
                new BitConverterBE(),
                new BitConverterLE()
            })
            {
                Assert.That(() => bc.ToBoolean(null, 0), Throws.InstanceOf<ArgumentNullException>());
                Assert.That(() => bc.ToByte(null, 0), Throws.InstanceOf<ArgumentNullException>());
                Assert.That(() => bc.ToDateTime(null, 0), Throws.InstanceOf<ArgumentNullException>());
                Assert.That(() => bc.ToDecimal(null, 0), Throws.InstanceOf<ArgumentNullException>());
                Assert.That(() => bc.ToDouble(null, 0), Throws.InstanceOf<ArgumentNullException>());
                Assert.That(() => bc.ToInt16(null, 0), Throws.InstanceOf<ArgumentNullException>());
                Assert.That(() => bc.ToInt32(null, 0), Throws.InstanceOf<ArgumentNullException>());
                Assert.That(() => bc.ToInt64(null, 0), Throws.InstanceOf<ArgumentNullException>());
                Assert.That(() => bc.ToSByte(null, 0), Throws.InstanceOf<ArgumentNullException>());
                Assert.That(() => bc.ToSingle(null, 0), Throws.InstanceOf<ArgumentNullException>());
                Assert.That(() => bc.ToTimeSpan(null, 0), Throws.InstanceOf<ArgumentNullException>());
                Assert.That(() => bc.ToUInt16(null, 0), Throws.InstanceOf<ArgumentNullException>());
                Assert.That(() => bc.ToUInt32(null, 0), Throws.InstanceOf<ArgumentNullException>());
                Assert.That(() => bc.ToUInt64(null, 0), Throws.InstanceOf<ArgumentNullException>());
            }
        }

        #endregion Public Methods
    }
}
