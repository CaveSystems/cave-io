using System;
using System.Diagnostics;
using System.IO;
using Cave;
using Cave.IO;
using Cave.IO.Blob;
using NUnit.Framework;

namespace Tests.Cave.IO;

[TestFixture]
public class BlobSerializerTest
{
    #region Public Methods

    class Sink : Stream
    {
        long length;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public override long Length => length;

        public override long Position { get => length; set => throw new NotSupportedException(); }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) { length += count; }
    }

    [Test]
    public void PerfTestWrite10s()
    {
        var serializer = new BlobSerializer();
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
        Console.WriteLine($"bytes/obj: ~{(ms.Length / totalObjects):N0}");
        Console.WriteLine("----------------------------");
        Console.WriteLine("Small object single thread");
    }

    [Test]
    public void PerfTestWriteRead10s()
    {
        var serializer = new BlobSerializer();
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
        }

        writer.Close();
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
        Console.WriteLine($"bytes/obj: ~{(size / totalObjects):N0}");
        Console.WriteLine("--------------------------------");
        Console.WriteLine("Small object single thread with ");
        Console.WriteLine("sequential write,read,equals.");
    }

    [Test]
    public void TestBigBlockWithSystemUri()
    {
        var serializer = new BlobSerializer();

        var ms = new MemoryStream();
        {
            var writer = serializer.StartWriting(ms);
            for (var i = 0; i < 1000; i++)
            {
                var test = TestStruct.Create(i);
                writer.Write(test);
            }
            writer.Close();
        }
        ms.Position = 0;
        {
            var reader = serializer.StartReading(ms);
            for (var i = 0; i < 1000; i++)
            {
                reader.Read(out TestStruct read);
                var test = TestStruct.Create(i);
                Assert.AreEqual(test, read);
            }
            reader.Close();
        }
    }


    [Test]
    public void TestBigBlockWithSystemUriAlternate()
    {
        var ms = new MemoryStream();
        {
            for (var i = 0; i < 1000; i++)
            {
                var test = TestStruct.Create(i);
                var serializer = new BlobSerializer();
                serializer.Serialize(ms, test);
            }
        }
        ms.Position = 0;
        {
            for (var i = 0; i < 1000; i++)
            {
                var serializer = new BlobSerializer();
                serializer.Deserialize(ms, out TestStruct read);
                var test = TestStruct.Create(i);
                Assert.AreEqual(test, read);
            }
        }
    }

    [Test]
    public void TestClassFields()
    {
        var serializer = new BlobSerializer();
        for (var i = 0; i < 1000; i++)
        {
            using var stream = new MemoryStream();
            var test = SettingsObjectFields.Random();

            var writer = serializer.StartWriting(stream);
            writer.Write(test);
            writer.Close();
            stream.Position = 0;
            var reader = serializer.StartReading(stream);
            reader.Read(out SettingsObjectFields roundtrip);
            reader.Close();

            Assert.AreEqual(test, roundtrip);
        }
    }

    [Test]
    public void TestClassProperties()
    {
        var serializer = new BlobSerializer();
        for (var i = 0; i < 1000; i++)
        {
            using var stream = new MemoryStream();
            var test = SettingsObjectProperties.Random();

            var writer = serializer.StartWriting(stream);
            writer.Write(test);
            writer.Close();
            stream.Position = 0;
            var reader = serializer.StartReading(stream);
            reader.Read(out SettingsObjectProperties roundtrip);
            reader.Close();

            Assert.AreEqual(test, roundtrip);
        }
    }

    [Test]
    public void TestNull()
    {
        var serializer = new BlobSerializer();
        var stream = new MemoryStream();
        serializer.Serialize(stream, null);
        stream.Position = 0;
        serializer.Deserialize<object?>(stream, out var roundtrip);
        Assert.AreEqual(null, roundtrip);
    }

    [Test]
    public void TestStructFields()
    {
        for (var i = 0; i < 1000; i++)
        {
            var serializer = new BlobSerializer();
            var stream = new MemoryStream();
            var test = SettingsStructFields.Random();
            serializer.Serialize(stream, test);
            stream.Position = 0;
            serializer.Deserialize<SettingsStructFields>(stream, out var roundtrip);
            Assert.AreEqual(test, roundtrip);
        }
    }

    [Test]
    public void TestStructProperties()
    {
        for (var i = 0; i < 1000; i++)
        {
            var serializer = new BlobSerializer();
            var stream = new MemoryStream();
            var test = SettingsStructProperties.Random();
            serializer.Serialize(stream, test);
            stream.Position = 0;
            serializer.Deserialize<SettingsStructProperties>(stream, out var roundtrip);
            Assert.AreEqual(test, roundtrip);
        }
    }

    #endregion Public Methods
}
