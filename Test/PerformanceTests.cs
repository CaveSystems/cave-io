#if DEBUG && (NET8_0 || NET48 || NET35)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cave;
using Cave.IO;
using NUnit.Framework;

namespace Tests.Cave.IO;

[TestFixture]
public class PerformanceTests
{
    record TestSignal : BaseRecord
    {
        public void SetExit() => Exit = true;
        public bool Exit { get; private set; }
    }

    #region Private Methods

    //change this to >5s for better results, but it will take much longer to run and will cause timeouts in our CI pipeline
    const int testSeconds = 1;

    static string FormatBytes(long bytes) => bytes.FormatBinarySize();

    static void LinkedListAddLastWithLimit(TestSignal signal, LinkedList<int> list, int limit, int value)
    {
        var spin = new SpinWait();
        for (int i = 0; !signal.Exit; i++)
        {
            if (Monitor.TryEnter(list, 10))
            {
                try
                {
                    if (list.Count < limit) { list.AddLast(value); return; }
                }
                finally { Monitor.Exit(list); }
            }
            spin.SpinOnce();
        }
    }

    static bool LinkedListTryRemoveFirst(TestSignal signal, LinkedList<int> list)
    {
        var spin = new SpinWait();
        for (int i = 0; !signal.Exit; i++)
        {
            if (Monitor.TryEnter(list, 10))
            {
                try
                {
                    if (list.Count > 0) { list.RemoveFirst(); return true; }
                    break;
                }
                finally { Monitor.Exit(list); }
            }
            spin.SpinOnce();
        }
        return false;
    }

    static string MdHeader() =>
        $"| {"Buffer",-28} | {"memory",9} | {"elapsed",12} | {"writes/s",18} | {"try-reads/s",18} | {"obj-reads/s",18} | {"r/w-%",9} |\n" +
        $"| {new string('-', 28)} | {new string('-', 9)} | {new string('-', 12)} | {new string('-', 18)} | {new string('-', 18)} | {new string('-', 18)} | {new string('-', 9)} |";

    static string MdRow(PerformanceMeasureResult r) =>
        $"| {r.Name,-28} | {FormatBytes(r.MemoryBytes),9} | {r.ElapsedMs,10:N1}ms | {r.WritesPerSec,18:N0} | {r.TryReadsPerSec,18:N0} | {r.ObjReadsPerSec,18:N0} | {r.ReadWritePercent,8:N1}% |" +
        (r.Notes.Length > 0 ? $" {r.Notes}" : "");

    static PerformanceMeasureResult MeasureMultiThread(string name, Action<TestSignal, int> write, Func<TestSignal, bool> read, int writer, int reader, Func<string>? notes = null)
    {
        long writeCount = 0, tryReadCount = 0, objReadCount = 0;
        var durationMs = testSeconds * 1000;
        var startSignal = new ManualResetEvent(false);
        var tasks = new Task[reader + writer];
        var signal = new TestSignal();

        for (int t = 0; t < writer; t++)
        {
            tasks[reader + t] = Task.Factory.StartNew(() =>
            {
                startSignal.WaitOne();
                var sw = Stopwatch.StartNew();
                var signal = new TestSignal();
                while (!signal.Exit && sw.ElapsedMilliseconds < durationMs) write(signal, (int)Interlocked.Increment(ref writeCount));
            }, TaskCreationOptions.LongRunning);
        }
        for (int r = 0; r < reader; r++)
        {
            tasks[r] = Task.Factory.StartNew(() =>
            {
                startSignal.WaitOne();
                var sw = Stopwatch.StartNew();
                while (!signal.Exit && sw.ElapsedMilliseconds < durationMs)
                {
                    Interlocked.Increment(ref tryReadCount);
                    if (read(signal)) Interlocked.Increment(ref objReadCount);
                }
            }, TaskCreationOptions.LongRunning);
        }

        using var mem = new MeasureMemory();
        var elapsed = Stopwatch.StartNew();
        startSignal.Set();
        if (!Task.WaitAll(tasks, durationMs))
        {
            // looks like some threads are still running, signal them to stop and wait until hard timeout (2x duration) reached
            signal.SetExit();
            if (!Task.WaitAll(tasks, durationMs)) throw new TimeoutException($"{name} is very long blocking in this scenario (> {elapsed.ElapsedMilliseconds - durationMs}ms).");
        }

        elapsed.Stop();
        mem.Stop();
        return new(name,
            mem.GetMemory(),
            Interlocked.Read(ref writeCount),
            Interlocked.Read(ref tryReadCount),
            Interlocked.Read(ref objReadCount),
            elapsed.Elapsed.TotalMilliseconds,
            notes?.Invoke() ?? "");
    }

    static PerformanceMeasureResult MeasureSingleThread(string name, long memoryBytes, Action<TestSignal, int> write, Func<TestSignal, bool> read, Func<string>? notes = null) =>
        MeasureMultiThread(name, write, read, 1, 1, notes);

    static void PrintSorted(List<PerformanceMeasureResult> rows, int writerCount, int readerCount)
    {
        foreach (var r in rows.OrderByDescending(x => x.WeightedScore(writerCount, readerCount)))
            Console.WriteLine(MdRow(r));
    }

    static void QueueEnqueueWithLimit(TestSignal signal, Queue<int> queue, int limit, int value)
    {
        for (int i = 0; !signal.Exit; i++)
        {
            if (Monitor.TryEnter(queue, 10))
            {
                try
                {
                    if (queue.Count < limit) { queue.Enqueue(value); return; }
                }
                finally { Monitor.Exit(queue); }
            }
            if (i < 10) Thread.SpinWait(1);
            else if (i < 20) Thread.Sleep(0);
        }
    }

    static bool QueueTryDequeue(TestSignal signal, Queue<int> queue)
    {
        for (int i = 0; !signal.Exit; i++)
        {
            if (Monitor.TryEnter(queue, 10))
            {
                try
                {
                    if (queue.Count > 0) { queue.Dequeue(); return true; }
                    break;
                }
                finally { Monitor.Exit(queue); }
            }
            if (i < 10) Thread.SpinWait(1);
            else if (i < 20) Thread.Sleep(0);
        }
        return false;
    }

    #endregion Private Methods

    static void ConcurrentQueueEnqueueWithLimit(TestSignal signal, ConcurrentQueue<int> queue, int limit, int value)
    {
        var spin = new SpinWait();
        for (int i = 0; !signal.Exit; i++)
        {
            if (queue.Count < limit) { queue.Enqueue(value); return; }
            spin.SpinOnce();
        }
    }

    #region Public Methods

    [Test]
    [TestCase(1, 31)]
    [TestCase(3, 29)]
    [TestCase(7, 7)]
    [TestCase(17, 17)]
    [TestCase(29, 3)]
    [TestCase(31, 1)]
    public void MultiThreadPerformanceTest(int writerCount, int readerCount)
    {
        Console.WriteLine($"# Performance Test ({testSeconds}s) — {writerCount} Writer, {readerCount} Reader\n");
        Console.WriteLine(MdHeader());
        var rows = new List<PerformanceMeasureResult>();

        try
        {
            var buf = new Queue<int>();
            rows.Add(MeasureMultiThread("Queue<int> + lock", (s, i) => QueueEnqueueWithLimit(s, buf, 1 << 20, i), (s) => QueueTryDequeue(s, buf), writerCount, readerCount));
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }
        try
        {
            var buf = new LinkedList<int>();
            rows.Add(MeasureMultiThread("LinkedList<int> + lock", (s, i) => LinkedListAddLastWithLimit(s, buf, 1 << 20, i), (s) => LinkedListTryRemoveFirst(s, buf), writerCount, readerCount));
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }
        try
        {
            var buf = new ConcurrentQueue<int>();
            rows.Add(MeasureMultiThread("ConcurrentQueue<int>", (s, i) => ConcurrentQueueEnqueueWithLimit(s, buf, 1 << 20, i), (s) => buf.TryDequeue(out _), writerCount, readerCount));
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }
        try
        {
            var buf = new RingBuffer<int>(20);
            rows.Add(MeasureMultiThread("RingBuffer<int>(20)", (s, i) => buf.Write(i), (s) => buf.TryRead(out _), writerCount, readerCount, () => $"lost: {buf.LostCount:N0}"));
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }
        try
        {
            var buf = new CircularBuffer<int>(20);
            rows.Add(MeasureMultiThread("CircularBuffer<int>(20)", (s, i) => buf.Write(i), (s) => buf.TryRead(out _), writerCount, readerCount, () => $"rejected: {buf.RejectedCount:N0}"));
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }
        try
        {
            var buf = new Fifo<int>();
            rows.Add(MeasureMultiThread("Fifo<int>", (s, i) => buf.Enqueue(i), (s) => buf.TryDequeue(out _), writerCount, readerCount));
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }
        try
        {
            var buf = new ShardedRingBuffer<int>(6, 20);
            rows.Add(MeasureMultiThread("ShardedRingBuf(6,20)", (s, i) => buf.Write(i), (s) => buf.TryRead(out _), writerCount, readerCount, () => $"lost: {buf.LostCount:N0}"));
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }
        try
        {
            var buf = new ShardedRingBuffer<int>(6, 20) { OverflowHandling = RingBufferOverflowFlags.Prevent };
            rows.Add(MeasureMultiThread("ShardedRingBuf(6,20)+Prev", (s, i) => buf.Write(i), (s) => buf.TryRead(out _), writerCount, readerCount, () => $"rejected: {buf.RejectedCount:N0}"));
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }

        PrintSorted(rows, writerCount, readerCount);
        Assert.Pass();
    }

    [Test]

    public void SingleThreadPerformanceTest()
    {
        MultiThreadPerformanceTest(1, 1);
        Assert.Pass();
    }

    #endregion Public Methods
}
#endif
