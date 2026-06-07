using System;
using System.Collections.Concurrent;
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
public class RingBufferTests
{
    #region Public Constructors

    static RingBufferTests()
    {
        ThreadPool.SetMaxThreads(1000, 1000);
        ThreadPool.SetMinThreads(100, 100);
    }

    #endregion Public Constructors

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
        var errors = new ConcurrentQueue<Exception>();
        var items = new int[] { 0, 2, 3, 6, 5, 4, 25, 79 };
        var rb = new RingBuffer<int>(16);
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

        Task CreateReader(IRingBufferCursor<int> cursor)
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    Interlocked.Increment(ref ready);
                    startSignal.WaitOne();

                    for (int i = 0; i < 1000; i++)
                    {
                        foreach (var expected in items)
                        {
                            var item = cursor.Read();
                            Assert.AreEqual(expected, item);
                            Assert.AreEqual(0, cursor.LostCount);
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Enqueue(ex);
                }
            }, TaskCreationOptions.LongRunning);
        }

        Task CreateWriter()
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    Interlocked.Increment(ref ready);
                    startSignal.WaitOne();

                    for (int i = 0; i < 1000; i++)
                    {
                        items.ForEach(item => rb.Write(item));
                        if (rb.WriteCount % 1000 == 0) Thread.Sleep(0);
                    }
                }
                catch (Exception ex)
                {
                    errors.Enqueue(ex);
                }
            }, TaskCreationOptions.LongRunning);
        }


        var tasks = new List<Task>();
        foreach (var cursor in cursors)
        {
            tasks.Add(CreateReader(cursor));
        }

        var writerTask = CreateWriter();
        tasks.Add(writerTask);

        var watch = StopWatch.StartNew();
        while (ready != 11)
        {
            Thread.Sleep(1);
            if (watch.ElapsedSeconds > 20) Assert.Fail("Tasks did not startup in time!");
        }

        Trace.WriteLine($"{watch.Elapsed.FormatTime()} Ready");
        startSignal.Set();
        watch.Reset();
        if (!writerTask.Wait(10_000) || errors.Count > 0)
        {
            if (!errors.IsEmpty)
            {
                throw new AggregateException(errors.ToArray());
            }
            Assert.Fail("Writer did not complete in time!");
        }
        var readerTasks = tasks.Where(t => t != writerTask).ToArray();
        if (!Task.WaitAll(readerTasks, 10_000))
        {
            Assert.Fail($"{readerTasks.Count(t => !t.IsCompleted)} readers did not complete in time!");
        }
        Trace.WriteLine($"{watch.Elapsed.FormatTime()} Done");
    }


    [Test]
    public void MultiWriterTest()
    {
        const int count = 10000;
        var fifo = new RingBuffer<int>();
        int ready = 0;
        var startSignal = new ManualResetEvent(false);

        void Writer(int startValue)
        {
            Interlocked.Increment(ref ready);
            startSignal.WaitOne();
            Parallel.For(startValue * count, (startValue + 1) * count, (n) =>
            {
                if (!fifo.Write(n)) throw new Exception("This shall not happen!");
                if (fifo.Available > fifo.Capacity * 4 / 5) Thread.Sleep(1);
            });
        }
        void Reader()
        {
            var list = new List<int>(count * 10);
            Interlocked.Increment(ref ready);
            for (int i = 0; i < list.Capacity; i++)
            {
                list.Add(fifo.Read());
            }
            list.Sort();
            CollectionAssert.AreEqual(new Counter(0, list.Capacity), list);
        }
        var tasks = new Counter(0, 10).Select((start) => Task.Factory.StartNew(() => Writer(start), TaskCreationOptions.LongRunning)).ToList();
        tasks.Add(Task.Factory.StartNew(() => Reader()));
        var watch = StopWatch.StartNew();
        while (ready != 11)
        {
            Thread.Sleep(1);
            if (watch.ElapsedSeconds > 20) Assert.Fail("Tasks did not startup in time!");
        }
        Trace.WriteLine($"{watch.Elapsed.FormatTime()} Ready");
        startSignal.Set();
        watch.Reset();
        if (!Task.WaitAll(tasks.ToArray(), 10 * 1000))
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
    public void OverFlowTest()
    {
        var buf = new RingBuffer<long>(2);
        for (int i = 0; i < 10; i++) buf.Write(i);

        //overflow position
        Assert.AreEqual(6, buf.LostCount);
        Assert.AreEqual(0, buf.ReadCount);
        Assert.AreEqual(4, buf.Available);
        Assert.AreEqual(10, buf.WriteCount);
        Assert.AreEqual(0, buf.RejectedCount);
        Assert.AreEqual(0, buf.Space);

        foreach (var expected in new long[] { 6, 7, 8, 9 })
        {
            Assert.IsTrue(buf.TryRead(out long value));
            Assert.AreEqual(expected, value);
        }

        Assert.AreEqual(4, buf.Space);
        Assert.AreEqual(6, buf.LostCount);
        Assert.AreEqual(4, buf.ReadCount);
        Assert.AreEqual(0, buf.Available);
        Assert.AreEqual(10, buf.WriteCount);
        Assert.AreEqual(0, buf.RejectedCount);

        {
            var ok = buf.TryRead(out long value);
            Assert.IsFalse(ok);
        }

        var expectedLostCount = 6;
        var expectedReadCount = 4;
        var expectedWriteCount = 10;
        for (int i = 0; i < 10; i++)
        {
            //buffer empty
            {
                var ok = buf.TryRead(out long value);
                Assert.IsFalse(ok);
            }
            //overflow buffer
            for (int n = 0; n < 10; n++)
            {
                buf.Write(-n);
                Assert.IsTrue(buf.Available > 0);
                Assert.AreEqual(++expectedWriteCount, buf.WriteCount);
            }
            Assert.AreEqual(4, buf.Available);
            //overwrite buffer
            for (int n = 0; n < 4; n++) buf.Write(i);
            expectedWriteCount += 4;
            expectedLostCount += 10;
            Assert.AreEqual(4, buf.Available);
            Assert.AreEqual(expectedWriteCount, buf.WriteCount);
            Assert.AreEqual(expectedLostCount, buf.LostCount);
            Assert.AreEqual(expectedReadCount, buf.ReadCount);
            //read 4 items
            for (int n = 0; n < 4; n++)
            {
                Assert.IsTrue(buf.TryRead(out long value));
                Assert.AreEqual((long)i, value);
            }
            expectedReadCount += 4;
            Assert.AreEqual(0, buf.Available);
            Assert.AreEqual(expectedWriteCount, buf.WriteCount);
            Assert.AreEqual(expectedLostCount, buf.LostCount);
            Assert.AreEqual(expectedReadCount, buf.ReadCount);
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
            Assert.AreEqual(256 - i, buf.Available);
            Assert.AreEqual((long)i, buf.ReadCount);
            var read = buf.TryRead(out var value);
            Assert.AreEqual((buf.WritePosition + i + 1) % 256, buf.ReadPosition);
            Assert.IsTrue(read);
            Assert.AreEqual(1000 - 256 + i, value);
        }

        {
            Assert.IsFalse(buf.TryRead(out var value));
            Assert.AreEqual(default(long), value);
        }
    }

    [Test]
    public void OverFlowLappingTest()
    {
        // buffer capacity = 2^2 = 4
        var buf = new RingBuffer<long>(2);

        // 4x Write (normal) → buf full
        for (int i = 0; i < 4; i++) buf.Write(i);
        Assert.AreEqual(4, buf.WriteCount);
        Assert.AreEqual(0, buf.ReadCount);
        Assert.AreEqual(0, buf.LostCount);
        Assert.AreEqual(4, buf.Available);
        Assert.AreEqual(0, buf.Space);

        // Write 5 (overflow) → item0 lost
        buf.Write(4);
        Assert.AreEqual(5, buf.WriteCount);
        Assert.AreEqual(0, buf.ReadCount);
        Assert.AreEqual(1, buf.LostCount);
        Assert.AreEqual(4, buf.Available);

        // Write 6 (overflow) → item1 lost
        buf.Write(5);
        Assert.AreEqual(6, buf.WriteCount);
        Assert.AreEqual(0, buf.ReadCount);
        Assert.AreEqual(2, buf.LostCount);
        Assert.AreEqual(4, buf.Available);

        // first TryRead → lapping detected, readCount pulled to writeCount-Capacity=2
        // oldest valid items: item2, item3, item4, item5
        Assert.IsTrue(buf.TryRead(out long v0));
        Assert.AreEqual(2L, v0);
        Assert.AreEqual(1, buf.ReadCount);
        Assert.AreEqual(3, buf.Available);
        Assert.AreEqual(2, buf.LostCount);

        Assert.IsTrue(buf.TryRead(out long v1));
        Assert.AreEqual(3L, v1);

        Assert.IsTrue(buf.TryRead(out long v2));
        Assert.AreEqual(4L, v2);

        Assert.IsTrue(buf.TryRead(out long v3));
        Assert.AreEqual(5L, v3);

        // buffer empty
        Assert.AreEqual(6, buf.WriteCount);
        Assert.AreEqual(4, buf.ReadCount);
        Assert.AreEqual(2, buf.LostCount);
        Assert.AreEqual(0, buf.Available);
        Assert.AreEqual(4, buf.Space);

        Assert.IsFalse(buf.TryRead(out long empty));
        Assert.AreEqual(default(long), empty);
    }

    #endregion Public Methods
}
