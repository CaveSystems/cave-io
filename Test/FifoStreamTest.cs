﻿

using NUnit.Framework;

using System.Linq;
using System.Threading.Tasks;
using Cave.IO;

namespace Tests.Cave.IO;

[TestFixture]
public class FifoStreamTest
{
    #region Public Methods

    [Test]
    public void Test1()
    {
        var b = new byte[256];
        var fifo = new FifoStream();
        fifo.PutBuffer(b);

        //write buffer after add
        for (var i = 0; i < b.Length; i++) b[i] = (byte)i;

        //test content
        Assert.IsTrue(b.SequenceEqual(fifo.ToArray()));

        Assert.AreEqual(0, fifo.ReadByte());
        Assert.AreEqual(255, fifo.Available);
        Assert.AreEqual(255, fifo[254]);

        for (var i = 1; i < b.Length; i++)
        {
            for (var n = i; n < b.Length; n++)
            {
                Assert.AreEqual(n, fifo[n - i]);
            }

            Assert.AreEqual(255, fifo[fifo.Available - 1]);
            Assert.AreEqual(i, fifo.ReadByte());
        }
    }

    [Test]
    public void Test2()
    {
        const int items = 256;
        var fifo = new FifoStream();
        for (var i = 0; i < items; i++) fifo.WriteByte((byte)i);

        Assert.AreEqual(0, fifo.ReadByte());
        Assert.AreEqual(255, fifo.Available);
        Assert.AreEqual(255, fifo[254]);

        for (var i = 1; i < items; i++)
        {
            for (var n = i; n < items; n++)
            {
                Assert.AreEqual(n, fifo[n - i]);
            }

            Assert.AreEqual(255, fifo[fifo.Available - 1]);
            Assert.AreEqual(i, fifo.ReadByte());
        }
    }

    #endregion Public Methods
}
