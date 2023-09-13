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
using System.Drawing;

namespace Tests.Cave.IO;

[TestFixture]
public class FifoTests
{
    #region Public Methods

    [Test]
    public void FifoTest()
    {
        const int count = 100000;
        var buf = new Fifo<long>();
        Parallel.For(0, count, n => buf.Enqueue(n));
        Assert.AreEqual(count, buf.Available);

        var items = new List<long>(count);
        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(count - i, buf.Available);
            Assert.AreEqual((long)i, buf.ReadCount);
            var read = buf.TryDequeue(out var value);
            Assert.IsTrue(read);
            Assert.IsTrue(value >= 0 && value < count);
            items.Add(value);
        }

        {
            Assert.IsFalse(buf.TryDequeue(out var value));
            Assert.AreEqual(default(long), value);
        }

        CollectionAssert.AreEqual(new Counter(0, count), items.OrderBy(n => n));
    }

    [Test]
    public void MultiWriterTest()
    {
        const int count = 10000;
        var fifo = new Fifo<int>();
        int ready = 0;
        var startSignal = new ManualResetEvent(false);

        void Writer(int startValue)
        {
            Interlocked.Increment(ref ready);
            startSignal.WaitOne();
            Parallel.For(startValue * count, (startValue + 1) * count, fifo.Enqueue);
        }
        void Reader()
        {
            var list = new List<int>(count * 10);
            Interlocked.Increment(ref ready);
            for (int i = 0; i < list.Capacity; i++)
            {
                list.Add(fifo.Dequeue());
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
        if (!Task.WaitAll(tasks.ToArray(), 60 * 1000))
        {
            Assert.Fail($"{tasks.Count(t => !t.IsCompleted)} tasks did not complete in time!");
        }
        Trace.WriteLine($"{watch.Elapsed.FormatTime()} Done");
    }

    #endregion Public Methods
}
