using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    internal static class DeepEquals
    {
        public static bool ListEqual<T>(IList<T> a, IList<T> b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;

            var cmp = EqualityComparer<T>.Default;
            for (int i = 0; i < a.Count; i++)
            {
                if (!cmp.Equals(a[i], b[i])) return false;
            }
            return true;
        }

        public static bool DictionaryEqual<TKey, TValue>(IDictionary<TKey, TValue> a, IDictionary<TKey, TValue> b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;

            var cmp = EqualityComparer<TValue>.Default;
            foreach (var kv in a)
            {
                TValue other;
                if (!b.TryGetValue(kv.Key, out other)) return false;
                if (!cmp.Equals(kv.Value, other)) return false;
            }
            return true;
        }
    }

    public sealed record Level5Payload(
        // primitive types
        bool Bool,
        byte Byte,
        sbyte SByte,
        short Short,
        ushort UShort,
        int Int,
        uint UInt,
        long Long,
        ulong ULong,
        char Char,
        float Float,
        double Double,
        decimal Decimal,
        DateTime DateTime,
        TimeSpan TimeSpan,
        string Text,
        // large data
        List<double> Doubles,
        List<float> Floats,
        Dictionary<int, double> Map
    ) : BaseRecord
    {
        Level5Payload() : this(false, 0, 0, 0, 0, 0, 0, 0, 0, '\0', 0f, 0.0, 0m, default, default, null, null, null, null) { }

        public bool Equals(Level5Payload other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;

            return
                Bool == other.Bool &&
                Byte == other.Byte &&
                SByte == other.SByte &&
                Short == other.Short &&
                UShort == other.UShort &&
                Int == other.Int &&
                UInt == other.UInt &&
                Long == other.Long &&
                ULong == other.ULong &&
                Char == other.Char &&
                Float.Equals(other.Float) &&
                Double.Equals(other.Double) &&
                Decimal == other.Decimal &&
                DateTime.Equals(other.DateTime) &&
                TimeSpan.Equals(other.TimeSpan) &&
                Text == other.Text &&
                DeepEquals.ListEqual(Doubles, other.Doubles) &&
                DeepEquals.ListEqual(Floats, other.Floats) &&
                DeepEquals.DictionaryEqual(Map, other.Map);
        }

        public override int GetHashCode()
        {
            // stabil & schnell – reicht für Tests vollkommen
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Int;
                hash = hash * 31 + (Text?.GetHashCode() ?? 0);
                hash = hash * 31 + Doubles.Count;
                hash = hash * 31 + Floats.Count;
                hash = hash * 31 + Map.Count;
                return hash;
            }
        }
    }

    public sealed record Level4Node(Guid Id, Level5Payload Payload, double Min, double Max) : BaseRecord
    {
        Level4Node() : this(Guid.Empty, null, 0.0, 0.0) { }
    }

    public sealed record Level3Node(string Name, List<string> Tags, Level4Node Child) : BaseRecord
    {
        Level3Node() : this(null, null, null) { }

        public bool Equals(Level3Node other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            return Name == other.Name && Child.Equals(other.Child) && DeepEquals.ListEqual(Tags, other.Tags);
        }

        public override int GetHashCode() => (Name?.GetHashCode() ?? 0) ^ Tags.Count;
    }

    public sealed record Level2Node(int Index, bool Enabled, Level3Node Child) : BaseRecord
    {
        Level2Node() : this(0, false, null) { }
    }

    public sealed record RootRecord(Guid Id, DateTime CreatedUtc, Level2Node A, Level2Node B) : BaseRecord
    {
        RootRecord() : this(Guid.Empty, default, null, null) { }
    }


    public static class SampleData
    {
        public static RootRecord Create(int seed = 1234)
        {
            var rnd = new Random(seed);

            var doubles = new List<double>(80_000);
            for (int i = 0; i < doubles.Capacity; i++)
                doubles.Add((rnd.NextDouble() - 0.5) * 1e6);

            var floats = new List<float>(60_000);
            for (int i = 0; i < floats.Capacity; i++)
                floats.Add((float)((rnd.NextDouble() - 0.5) * 1e4));

            var map = new Dictionary<int, double>(20_000);
            for (int i = 0; i < 20_000; i++)
                map[i] = rnd.NextDouble();

            var payload = new Level5Payload(
                Bool: true,
                Byte: 1,
                SByte: -1,
                Short: -123,
                UShort: 123,
                Int: 42,
                UInt: 42U,
                Long: 42L,
                ULong: 42UL,
                Char: 'X',
                Float: 1.23f,
                Double: 4.56,
                Decimal: 7.89m,
                DateTime: DateTime.UtcNow,
                TimeSpan: TimeSpan.FromMinutes(5),
                Text: "serializer-test",
                Doubles: doubles,
                Floats: floats,
                Map: map
            );

            var l4 = new Level4Node(Guid.NewGuid(), payload, -1.0, 1.0);
            var l3 = new Level3Node("L3", new List<string> { "A", "B", "C" }, l4);
            var l2a = new Level2Node(1, true, l3);
            var l2b = new Level2Node(2, false, l3);

            return new RootRecord(Guid.NewGuid(), DateTime.UtcNow, l2a, l2b);
        }
    }

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
    public void BigRecordTest()
    {
        var serializer = new BlobSerializer();
        serializer.Prepare(typeof(RootRecord));
        using var ms = new FifoStream();
        var test = SampleData.Create();
        var writer = serializer.StartWriting(ms);
        writer.Write(test);
        writer.Close();
        ms.Position = 0;
        var reader = serializer.StartReading(ms);
        reader.Read(out RootRecord roundtrip);
        reader.Close();
        Assert.AreEqual(test, roundtrip);
    }

    [Test]
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
        Console.WriteLine($"bytes/obj: ~{(ms.Length / totalObjects):N0}");
        Console.WriteLine("----------------------------");
        Console.WriteLine("Small object single thread");
    }

    [Test]
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
        Console.WriteLine($"bytes/obj: ~{(bytesRead / totalObjects):N0}");
        Console.WriteLine("----------------------------");
        Console.WriteLine("Small object single thread");
    }

    [Test]
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
        Console.WriteLine($"bytes/obj: ~{(size / totalObjects):N0}");
        Console.WriteLine("--------------------------------");
        Console.WriteLine("Small object single thread with ");
        Console.WriteLine("sequential write,read,equals.");
    }

    [Test]
    public void PerfTestWrite10s()
    {
        var serializer = new BlobSerializer();
        serializer.Prepare(typeof(RootRecord));
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
            Assert.AreEqual(null, reader.Read());
            Assert.AreEqual(true, reader.IsCompleted);
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
    public void TestSettingsClassFields()
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
    public void TestSettingsClassProperties()
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
    public void TestSettingsStructFields()
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
    public void TestSettingsStructProperties()
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

    [Test]
    public void TestStructFields()
    {
        for (var i = 0; i < 1000; i++)
        {
            var serializer = new BlobSerializer();
            var stream = new MemoryStream();
            var test = TestStruct.Create(i);
            serializer.Serialize(stream, test);
            stream.Position = 0;
            serializer.Deserialize<TestStruct>(stream, out var roundtrip);
            Assert.AreEqual(test, roundtrip);
        }
    }

    [Test]
    public void TestStructNullableFields()
    {
        for (var i = 0; i < 1000; i++)
        {
            var serializer = new BlobSerializer();
            var stream = new MemoryStream();
            var test = TestStructNullables.Create(i);
            serializer.Serialize(stream, test);
            stream.Position = 0;
            serializer.Deserialize<TestStructNullables>(stream, out var roundtrip);
            Assert.AreEqual(test, roundtrip);
        }
    }

    [Test]
    public void TestRecordReflection()
    {
        var stream = new FifoStream();
        var serializer = new BlobSerializer();
        var writer = serializer.StartWriting(stream);
        var reader = serializer.StartReading(stream);
        for (var i = 1000; i >= 0; i--)
        {
            var test = TestClass.Create(i);
            writer.Write(test);
            reader.Read<TestClass>(out var roundtrip);
            Assert.AreEqual(0, stream.Available);
            Assert.AreEqual(test, roundtrip);
            Assert.AreEqual(false, reader.IsCompleted);
        }
        writer.Close();
        reader.Close();

    }
    #endregion Public Methods
}
