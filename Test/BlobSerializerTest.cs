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
public class BlobSerializerTests
{
    #region Public Methods

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
    public void TestBigBlockWithSystemUri()
    {
        var serializer = new BlobSerializer();
        serializer.RegisterStringParseConverter(new Uri("https://example.com/test"), allowConstructor: true);
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
                serializer.RegisterStringParseConverter(new Uri("https://example.com/test"), allowConstructor: true);
                serializer.Serialize(ms, test);
            }
        }
        ms.Position = 0;
        {
            for (var i = 0; i < 1000; i++)
            {
                var serializer = new BlobSerializer();
                serializer.RegisterStringParseConverter(new Uri("https://example.com/test"), allowConstructor: true);
                serializer.Deserialize(ms, out TestStruct read);
                var test = TestStruct.Create(i);
                Assert.AreEqual(test, read);
            }
        }
    }

    [Test]
    public void TestEnumerableClassWithProperties()
    {
        var obj1 = new EnumerableClassWithProperties();
        var obj2 = new EnumerableClassWithProperties();
        var fifo = new FifoStream();
        var serializer = new BlobSerializer();
        //test serialization with defining the converter explicitly
        //this allows usage without access to the source code of the class
        serializer.RegisterReflectionConverter<EnumerableClassWithProperties>(BlobConverterFlags.Private | BlobConverterFlags.Public | BlobConverterFlags.Fields | BlobConverterFlags.Properties);
        serializer.RegisterStringParseConverter(new Uri("https://example.com/test"), allowConstructor: true);

        serializer.Serialize(fifo, obj1);
        serializer.Serialize(fifo, obj2);
        serializer.Deserialize<EnumerableClassWithProperties>(fifo, out var rt1);
        serializer.Deserialize<EnumerableClassWithProperties>(fifo, out var rt2);
        Assert.AreEqual(obj1.SomeObject, rt1.SomeObject);
        Assert.AreEqual(obj1.SomeValue, rt1.SomeValue);
        Assert.AreEqual(obj1.Uri, rt1.Uri);
        Assert.That(obj1.SequenceEqual(rt1));
        Assert.AreEqual(obj2.SomeObject, rt2.SomeObject);
        Assert.AreEqual(obj2.SomeValue, rt2.SomeValue);
        Assert.AreEqual(obj2.Uri, rt2.Uri);
        Assert.That(obj2.SequenceEqual(rt2));
    }

    [Test]
    public void TestEnumerableClassWithPropertiesAndAttribute()
    {
        var obj1 = new EnumerableClassWithPropertiesAndAttribute();
        var obj2 = new EnumerableClassWithPropertiesAndAttribute();
        var fifo = new FifoStream();
        var serializer = new BlobSerializer();
        serializer.RegisterStringParseConverter(new Uri("https://example.com/test"), allowConstructor: true);
        serializer.Serialize(fifo, obj1);
        serializer.Serialize(fifo, obj2);
        serializer.Deserialize<EnumerableClassWithPropertiesAndAttribute>(fifo, out var rt1);
        serializer.Deserialize<EnumerableClassWithPropertiesAndAttribute>(fifo, out var rt2);
        Assert.AreEqual(obj1.SomeObject, rt1.SomeObject);
        Assert.AreEqual(obj1.SomeValue, rt1.SomeValue);
        Assert.AreEqual(obj1.Uri, rt1.Uri);
        Assert.That(obj1.SequenceEqual(rt1));
        Assert.AreEqual(obj2.SomeObject, rt2.SomeObject);
        Assert.AreEqual(obj2.SomeValue, rt2.SomeValue);
        Assert.AreEqual(obj2.Uri, rt2.Uri);
        Assert.That(obj2.SequenceEqual(rt2));
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
            serializer.RegisterStringParseConverter(new Uri("https://example.com/test"), allowConstructor: true);
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
            serializer.RegisterStringParseConverter(new Uri("https://example.com/test"), allowConstructor: true);
            var stream = new MemoryStream();
            var test = TestStructNullables.Create(i);
            serializer.Serialize(stream, test);
            stream.Position = 0;
            serializer.Deserialize<TestStructNullables>(stream, out var roundtrip);
            Assert.AreEqual(test, roundtrip);
        }
    }

    #endregion Public Methods
}
