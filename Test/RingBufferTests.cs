using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cave;
using Cave.Collections;
using Cave.IO;
using NUnit.Framework;

namespace Tests.Cave.IO;

[TestFixture]
public class RingBufferTests
{
    #region Public Methods

    [Test]
    public void CircularBufferTest()
    {
        var buf = new CircularBuffer<long>();
        Parallel.For(0, 1000, n => buf.Write(n));

        Assert.AreEqual(1000, buf.WritePosition);
        Assert.AreEqual(1000, buf.WriteCount);
        Assert.AreEqual(0, buf.RejectedCount);
        Assert.AreEqual(0, buf.LostCount);

        var items = new List<long>(1000);
        for (int i = 0; i < 1000; i++)
        {
            Assert.AreEqual(1000 - i, buf.Available);
            Assert.AreEqual((long)i, buf.ReadCount);
            Assert.AreEqual(i, buf.ReadPosition);
            var read = buf.TryRead(out var value);
            Assert.IsTrue(read);
            Assert.IsTrue(value >= 0 && value < 1000);
            items.Add(value);
        }

        {
            Assert.IsFalse(buf.TryRead(out var value));
            Assert.AreEqual(default(long), value);
        }

        CollectionAssert.AreEqual(new Counter(0, 1000), items.OrderBy(n => n));
    }

    [Test]
    public void OverFlowRejectTest()
    {
        var buf = new CircularBuffer<long>(8);
        Parallel.For(0, 1000, n => buf.Write(n));

        //overflow position
        Assert.AreEqual(0, buf.WritePosition);
        Assert.AreEqual(256, buf.WriteCount);
        Assert.AreEqual(744, buf.RejectedCount);
        Assert.AreEqual(0, buf.LostCount);

        for (int i = 0; i < 256; i++)
        {
            Assert.AreEqual(256 - i, buf.Available);
            Assert.AreEqual((long)i, buf.ReadCount);
            Assert.AreEqual(i, buf.ReadPosition);
            var read = buf.TryRead(out var value);
            Assert.IsTrue(read);
            Assert.IsTrue(value >= 0 && value < 1000);
        }

        {
            Assert.IsFalse(buf.TryRead(out var value));
            Assert.AreEqual(default(long), value);
        }
    }

    [Test]
    public void OverFlowWriteTest()
    {
        var buf = new RingBuffer<long>(8);
        for (int i = 0; i < 1000; i++) buf.Write(i);

        //overflow position
        Assert.AreEqual(1000 % 256, buf.WritePosition);
        Assert.AreEqual(1000, buf.WriteCount);
        Assert.AreEqual(744, buf.LostCount);
        Assert.AreEqual(0, buf.RejectedCount);

        for (int i = 0; i < 256; i++)
        {
            Assert.AreEqual(1000 - i, buf.Available);
            Assert.AreEqual((long)i, buf.ReadCount);
            Assert.AreEqual(i, buf.ReadPosition);
            var read = buf.TryRead(out var value);
            Assert.IsTrue(read);
            Assert.IsTrue(value >= 0 && value < 1000);
        }

        {
            Assert.IsFalse(buf.TryRead(out var value));
            Assert.AreEqual(default(long), value);
        }
    }

    #endregion Public Methods
}
