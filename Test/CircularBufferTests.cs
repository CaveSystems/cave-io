using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cave;
using Cave.Collections;
using Cave.IO;
using NUnit.Framework;

namespace Tests.Cave.IO;

[TestFixture]
public class CircularBufferTests
{
    #region Public Methods

    [Test]
    public void OverFlowRejectNoExceptionTest()
    {
        // CircularBuffer rejects overflow by default, no exception
        var buf = new CircularBuffer<long>(2) { OverflowExceptions = false };

        // fill buffer (capacity=4)
        for (int i = 0; i < 4; i++)
        {
            var written = buf.Write(i);
            Assert.IsTrue(written);
        }

        Assert.AreEqual(4, buf.WriteCount);
        Assert.AreEqual(4, buf.Available);
        Assert.AreEqual(0, buf.RejectedCount);
        Assert.AreEqual(0, buf.LostCount);
        Assert.AreEqual(0, buf.Space);

        // overflow: 6 more writes → all rejected
        for (int i = 4; i < 10; i++)
        {
            var written = buf.Write(i);
            Assert.IsFalse(written, $"Write({i}) should have been rejected");
        }

        Assert.AreEqual(4, buf.WriteCount);
        Assert.AreEqual(4, buf.Available);
        Assert.AreEqual(6, buf.RejectedCount);
        Assert.AreEqual(0, buf.LostCount);
        Assert.AreEqual(0, buf.Space);

        // read all 4 items in order (no lapping → original order preserved)
        for (long expected = 0; expected < 4; expected++)
        {
            Assert.IsTrue(buf.TryRead(out long value));
            Assert.AreEqual(expected, value);
        }

        Assert.AreEqual(4, buf.ReadCount);
        Assert.AreEqual(0, buf.Available);
        Assert.AreEqual(4, buf.Space);
        Assert.IsFalse(buf.TryRead(out _));
    }

    [Test]
    public void OverFlowRejectWithExceptionTest()
    {
        // CircularBuffer with OverflowExceptions=true throws on overflow
        var buf = new CircularBuffer<long>(2) { OverflowExceptions = true };

        // fill buffer (capacity=4)
        for (int i = 0; i < 4; i++)
        {
            Assert.IsTrue(buf.Write(i));
        }

        Assert.AreEqual(4, buf.WriteCount);
        Assert.AreEqual(0, buf.Space);

        // overflow write must throw
        Assert.Throws<InternalBufferOverflowException>(() => buf.Write(99));

        // state unchanged after rejected write
        Assert.AreEqual(4, buf.WriteCount);
        Assert.AreEqual(4, buf.Available);
        Assert.AreEqual(1, buf.RejectedCount);
        Assert.AreEqual(0, buf.LostCount);

        // read all 4 original items
        for (long expected = 0; expected < 4; expected++)
        {
            Assert.IsTrue(buf.TryRead(out long value));
            Assert.AreEqual(expected, value);
        }

        Assert.AreEqual(4, buf.ReadCount);
        Assert.AreEqual(0, buf.Available);
        Assert.IsFalse(buf.TryRead(out _));
    }

    [Test]
    public void ParallelWriteRejectTest()
    {
        // concurrent writes: only Capacity writes succeed, rest rejected
        var buf = new CircularBuffer<long>(8) { OverflowExceptions = false };
        Parallel.For(0, 1000, n => buf.Write(n));

        Assert.AreEqual(256, buf.WriteCount);
        Assert.AreEqual(744, buf.RejectedCount);
        Assert.AreEqual(0, buf.LostCount);
        Assert.AreEqual(256, buf.Available);
        Assert.AreEqual(0, buf.Space);

        for (int i = 0; i < 256; i++)
        {
            Assert.IsTrue(buf.TryRead(out long value));
            Assert.IsTrue(value >= 0 && value < 1000);
        }

        Assert.AreEqual(256, buf.ReadCount);
        Assert.AreEqual(0, buf.Available);
        Assert.IsFalse(buf.TryRead(out _));
    }

    #endregion Public Methods
}
