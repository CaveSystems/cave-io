using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cave;
using Cave.Collections;
using Cave.IO;

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
    public void CursorTest()
    {
        var items = new int[] { 0, 2, 3, 6, 5, 4, 25, 79 };
        var rb = new RingBuffer<int>();
        var cursors = new Counter(0, 10).Select(c => rb.GetCursor()).ToArray();
        void TestCursors(IRingBufferCursor<int> cursor)
        {
            Assert.AreEqual(0, cursor.ReadPosition);
            Assert.AreEqual(0, cursor.ReadCount);
            Assert.AreEqual(0, cursor.LostCount);
            Assert.AreEqual(0, cursor.Available);
            Assert.IsFalse(cursor.TryRead(out _));
        }
        cursors.ForEach(TestCursors);
        int ready = 0;
        var startSignal = new ManualResetEvent(false);
        void Readers()
        {
            Parallel.ForEach(cursors, cursor =>
            {
                Interlocked.Increment(ref ready);
                startSignal.WaitOne();
                for (int i = 0; i < 1000; i++)
                {
                    foreach (var expected in items)
                    {
                        var item = cursor.Read();
                        Assert.AreEqual(expected, item);
                    }
                }

                Assert.AreEqual(0, cursor.Available);
                Assert.AreEqual(8000 - rb.Capacity, rb.WritePosition);
                Assert.AreEqual(8000, rb.WriteCount);
                Assert.AreEqual(8000 - rb.Capacity, cursor.ReadPosition);
                Assert.AreEqual(8000, cursor.ReadCount);
                Assert.AreEqual(0, cursor.LostCount);
                Assert.AreEqual(0, cursor.Available);
            });
        }
        void Writer()
        {
            Interlocked.Increment(ref ready);
            startSignal.WaitOne();
            for (int i = 0; i < 1000; i++)
            {
                items.ForEach(item => rb.Write(item));
                if ((i % 100) == 0) Thread.Sleep(1);
            }
        }

        var tasks = new[]
        {
            Task.Factory.StartNew(Readers, TaskCreationOptions.LongRunning),
            Task.Factory.StartNew(Writer, TaskCreationOptions.LongRunning)
        };
        var watch = StopWatch.StartNew();
        while (ready != 11)
        {
            Thread.Sleep(1);
            if (watch.ElapsedSeconds > 20) Assert.Fail("Tasks did not startup in time!");
        }
        Trace.WriteLine($"{watch.Elapsed.FormatTime()} Ready");
        startSignal.Set();
        watch.Reset();
        if (!Task.WaitAll(tasks, 60 * 1000))
        {
            Assert.Fail($"{tasks.Count(t => !t.IsCompleted)} tasks did not complete in time!");
        }
        Trace.WriteLine($"{watch.Elapsed.FormatTime()} Done");
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
