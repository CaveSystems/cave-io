#if !DEBUG && (NET8_0 || NET48 || NET35)

using Cave;
using Cave.Collections;
using Cave.IO;
using Cave.IO.Blob;
using Cave.IO.Blob.Converters;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Tests.Cave.IO;

[TestFixture]
public class BlobSerializerPerformanceTests
{
    [Test]
    [Category("Performance")]
    public void PerfTestBigRecordWrite10s()
    {
        var serializer = new BlobSerializer();
        serializer.Prepare(typeof(RootRecord));
        using var ms = new Sink();
        var test1 = SampleData.Create(1);
        var test2 = SampleData.Create(2);
        var test3 = SampleData.Create(3);
        var test4 = SampleData.Create(4);
        var test5 = SampleData.Create(5);
        var writer = serializer.StartWriting(ms);
        var sw = StopWatch.StartNew();
        long count = 0;

        while (sw.Elapsed < TimeSpan.FromSeconds(10))
        {
            writer.Write(test1);
            writer.Write(test2);
            writer.Write(test3);
            writer.Write(test4);
            writer.Write(test5);
            count++;
        }

        writer.Close();
        sw.Stop();

        long totalObjects = 5L * count;
        double seconds = sw.Elapsed.TotalSeconds;
        double objectsPerSecond = totalObjects / seconds;

        Console.WriteLine("=== BlobSerializer Write ===");
        Console.WriteLine($"Count:      {totalObjects:N0} objects in {sw.Elapsed.FormatTime()}");
        Console.WriteLine($"Bytes:      {ms.Length:N0}");
        Console.WriteLine($"Size/s:     {(ms.Length / seconds).FormatBinarySize()}");
        Console.WriteLine($"Objects/s:  {objectsPerSecond:N0}");
        Console.WriteLine($"Time/obj:   {(seconds / totalObjects).FormatSeconds()}");
        Console.WriteLine($"bytes/obj: ~{(ms.Length / totalObjects):N0}");
        Console.WriteLine("----------------------------");
        Console.WriteLine("Big record single thread");
    }

    [Test]
    [Category("Performance")]
    public void PerfTestBigRecordRead10s()
    {
        var serializer = new BlobSerializer();
        serializer.Prepare(typeof(RootRecord));
        using var ms = new MemoryStream();
        var test1 = SampleData.Create(1);
        var test2 = SampleData.Create(2);
        var test3 = SampleData.Create(3);
        var test4 = SampleData.Create(4);
        var test5 = SampleData.Create(5);
        var writer = serializer.StartWriting(ms);
        writer.Write(test1);
        writer.Write(test2);
        writer.Write(test3);
        writer.Write(test4);
        writer.Write(test5);
        var streamResetPosition = ms.Position;
        writer.Write(test1);
        writer.Write(test2);
        writer.Write(test3);
        writer.Write(test4);
        writer.Write(test5);
        ms.Position = 0;

        var deserializer = new BlobSerializer();
        deserializer.Prepare(typeof(RootRecord));
        var reader = deserializer.StartReading(ms);
        reader.Read();
        reader.Read();
        reader.Read();
        reader.Read();
        reader.Read();

        var sw = StopWatch.StartNew();
        long count = 0;
        var bytesRead = 0L;
        while (sw.Elapsed < TimeSpan.FromSeconds(10))
        {
            ms.Position = streamResetPosition;
            reader.Read();
            reader.Read();
            reader.Read();
            reader.Read();
            reader.Read();
            count++;
            bytesRead += ms.Position - streamResetPosition;
        }

        sw.Stop();
        writer.Close();

        long totalObjects = 5L * count;
        double seconds = sw.Elapsed.TotalSeconds;
        double objectsPerSecond = totalObjects / seconds;

        Console.WriteLine("=== BlobSerializer Read ===");
        Console.WriteLine($"Count:      {totalObjects:N0} objects in {sw.Elapsed.FormatTime()}");
        Console.WriteLine($"Bytes:      {bytesRead:N0}");
        Console.WriteLine($"Size/s:     {(bytesRead / seconds).FormatBinarySize()}");
        Console.WriteLine($"Objects/s:  {objectsPerSecond:N0}");
        Console.WriteLine($"Time/obj:   {(seconds / totalObjects).FormatSeconds()}");
        Console.WriteLine($"bytes/obj: ~{(bytesRead / totalObjects):N0}");
        Console.WriteLine("----------------------------");
        Console.WriteLine("Big record single thread");
    }

    [Test]
    [Category("Performance")]
    public void PerfTestBigRecordWriteRead10s()
    {
        var serializer = new BlobSerializer();
        serializer.Prepare(typeof(RootRecord));
        using var fs = new FifoStream();
        var test1 = SampleData.Create(1);
        var test2 = SampleData.Create(2);
        var test3 = SampleData.Create(3);
        var test4 = SampleData.Create(4);
        var test5 = SampleData.Create(5);
        var writer = serializer.StartWriting(fs);
        var reader = serializer.StartReading(fs);
        var sw = StopWatch.StartNew();
        long count = 0;
        long size = 0;
        while (sw.Elapsed < TimeSpan.FromSeconds(10))
        {
            writer.Write(test1);
            writer.Write(test2);
            writer.Write(test3);
            writer.Write(test4);
            writer.Write(test5);
            size += fs.Length;
            count++;
            var result1 = reader.Read();
            var result2 = reader.Read();
            var result3 = reader.Read();
            var result4 = reader.Read();
            var result5 = reader.Read();
            fs.FreeBuffers();
            Assert.AreEqual(test1, result1);
            Assert.AreEqual(test2, result2);
            Assert.AreEqual(test3, result3);
            Assert.AreEqual(test4, result4);
            Assert.AreEqual(test5, result5);
            Assert.AreEqual(false, reader.IsCompleted);
        }
        writer.Close();
        reader.Read();
        reader.Close();
        sw.Stop();

        long totalObjects = 5L * count;
        double seconds = sw.Elapsed.TotalSeconds;
        double objectsPerSecond = totalObjects / seconds;

        Console.WriteLine("=== BlobSerializer Read/Write ===");
        Console.WriteLine($"Count:      {totalObjects:N0} objects in {sw.Elapsed.FormatTime()}");
        Console.WriteLine($"Bytes:      {size:N0}");
        Console.WriteLine($"Size/s:     {(size / seconds).FormatBinarySize()}");
        Console.WriteLine($"Objects/s:  {objectsPerSecond:N0}");
        Console.WriteLine($"Time/obj:   {(seconds / totalObjects).FormatSeconds()}");
        Console.WriteLine($"bytes/obj: ~{(size / totalObjects):N0}");
        Console.WriteLine("--------------------------------");
        Console.WriteLine("Big record single thread with ");
        Console.WriteLine("sequential write,read,equals.");
    }

    [Test]
    [Category("Performance")]
    public void PerfTestWrite10s()
    {
        var serializer = new BlobSerializer();
        serializer.RegisterStringParseConverter(new Uri("https://example.com/test"), allowConstructor: true);
        serializer.Prepare(typeof(TestStruct));
        serializer.Prepare(typeof(SettingsStructFields));
        serializer.Prepare(typeof(SettingsObjectFields));
        serializer.Prepare(typeof(SettingsStructProperties));
        serializer.Prepare(typeof(SettingsObjectProperties));
        using var ms = new Sink();
        var test1 = TestStruct.Create(111);
        var test2 = SettingsStructFields.Random();
        var test3 = SettingsObjectFields.Random();
        var test4 = SettingsStructProperties.Random();
        var test5 = SettingsObjectProperties.Random();
        var writer = serializer.StartWriting(ms);
        var sw = StopWatch.StartNew();
        long count = 0;

        while (sw.Elapsed < TimeSpan.FromSeconds(10))
        {
            writer.Write(test1);
            writer.Write(test2);
            writer.Write(test3);
            writer.Write(test4);
            writer.Write(test5);
            count++;
        }

        writer.Close();
        sw.Stop();

        long totalObjects = 5L * count;
        double seconds = sw.Elapsed.TotalSeconds;
        double objectsPerSecond = totalObjects / seconds;

        Console.WriteLine("=== BlobSerializer Write ===");
        Console.WriteLine($"Count:      {totalObjects:N0} objects in {sw.Elapsed.FormatTime()}");
        Console.WriteLine($"Bytes:      {ms.Length:N0}");
        Console.WriteLine($"Size/s:     {(ms.Length / seconds).FormatBinarySize()}");
        Console.WriteLine($"Objects/s:  {objectsPerSecond:N0}");
        Console.WriteLine($"Time/obj:   {(seconds / totalObjects).FormatSeconds()}");
        Console.WriteLine($"bytes/obj: ~{(ms.Length / totalObjects):N0}");
        Console.WriteLine("----------------------------");
        Console.WriteLine("Small object single thread");
    }

    [Test]
    [Category("Performance")]
    public void PerfTestWriteRead10s()
    {
        var serializer = new BlobSerializer();
        serializer.RegisterStringParseConverter(new Uri("https://example.com/test"), allowConstructor: true);
        serializer.Prepare(typeof(TestStruct));
        serializer.Prepare(typeof(SettingsStructFields));
        serializer.Prepare(typeof(SettingsObjectFields));
        serializer.Prepare(typeof(SettingsStructProperties));
        serializer.Prepare(typeof(SettingsObjectProperties));
        using var fs = new FifoStream();
        var test1 = TestStruct.Create(111);
        var test2 = SettingsStructFields.Random();
        var test3 = SettingsObjectFields.Random();
        var test4 = SettingsStructProperties.Random();
        var test5 = SettingsObjectProperties.Random();
        var writer = serializer.StartWriting(fs);
        var reader = serializer.StartReading(fs);
        var sw = StopWatch.StartNew();
        long count = 0;
        long size = 0;
        while (sw.Elapsed < TimeSpan.FromSeconds(10))
        {
            writer.Write(test1);
            writer.Write(test2);
            writer.Write(test3);
            writer.Write(test4);
            writer.Write(test5);
            size += fs.Length;
            count++;
            var result1 = reader.Read();
            var result2 = reader.Read();
            var result3 = reader.Read();
            var result4 = reader.Read();
            var result5 = reader.Read();
            fs.FreeBuffers();
            Assert.AreEqual(test1, result1);
            Assert.AreEqual(test2, result2);
            Assert.AreEqual(test3, result3);
            Assert.AreEqual(test4, result4);
            Assert.AreEqual(test5, result5);
            Assert.AreEqual(false, reader.IsCompleted);
        }
        writer.Close();
        reader.Read();
        reader.Close();
        sw.Stop();

        long totalObjects = 5L * count;
        double seconds = sw.Elapsed.TotalSeconds;
        double objectsPerSecond = totalObjects / seconds;

        Console.WriteLine("=== BlobSerializer Read/Write ===");
        Console.WriteLine($"Count:      {totalObjects:N0} objects in {sw.Elapsed.FormatTime()}");
        Console.WriteLine($"Bytes:      {size:N0}");
        Console.WriteLine($"Size/s:     {(size / seconds).FormatBinarySize()}");
        Console.WriteLine($"Objects/s:  {objectsPerSecond:N0}");
        Console.WriteLine($"Time/obj:   {(seconds / totalObjects).FormatSeconds()}");
        Console.WriteLine($"bytes/obj: ~{(size / totalObjects):N0}");
        Console.WriteLine("--------------------------------");
        Console.WriteLine("Small object single thread with ");
        Console.WriteLine("sequential write,read,equals.");
    }
}
#endif
