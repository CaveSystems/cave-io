using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cave;
using Cave.Collections;
using Cave.Collections.Generic;
using Cave.IO;
using NUnit.Framework;

namespace Tests.Cave.IO;

class MeasureMemory : IDisposable
{
    Thread thread;
    ManualResetEvent running = new ManualResetEvent(false);
    bool exit;
    long min;
    long max;

    public MeasureMemory()
    {
        thread = new Thread(Worker) { Name = nameof(MeasureMemory) };
        thread.Start();
        running.WaitOne();
    }

    void Worker()
    {
        min = GC.GetTotalMemory(true);
        for (var i = 0; i < 100; i++)
        {
            GC.Collect(2, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
            var mem = GC.GetTotalMemory(true);
            if (mem < min) { min = mem; i = 0; }
        }
        running.Set();
        while (!exit)
        {
            Thread.Sleep(1);
            var mem = GC.GetTotalMemory(true);
            if (mem > max) max = mem;
            if (mem < min || min == 0) min = mem;
        }
    }

    public void Stop()
    {
        if (!exit)
        {
            exit = true;
            thread.Join();
        }
    }

    public long GetMemory() => max - min;
    public void Dispose() => Stop();
}
