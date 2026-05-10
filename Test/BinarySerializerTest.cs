using NUnit.Framework;

using System;
using System.IO;
using Cave.IO;

namespace Tests.Cave.IO;

[TestFixture]
public class BinarySerializerTest
{
    #region Public Methods

    [Test]
    public void TestBigBlockWithSystemUri()
    {
        var serializer = new BinarySerializer
        {
            StructFlags = SerializerFlags.Fields | SerializerFlags.Public
        };
        serializer.UseToStringAndCctor(typeof(Uri));

        var data = new FifoBuffer();
        for (var i = 0; i < 1000; i++)
        {
            var test = TestStruct.Create(i);
            serializer.Serialize(test, out var buffer);
            data.Enqueue(buffer, true);
        }

        for (var i = 0; i < 1000; i++)
        {
            var block = data.Dequeue();
            var read = serializer.Deserialize<TestStruct>(block);
            var test = TestStruct.Create(i);
            Assert.AreEqual(test, read);
        }
    }

    [Test]
    public void TestCircularReferenceThrows()
    {
        var serializer = new BinarySerializer();
        var stream = new MemoryStream();
        var node = new BinarySerializerNode();
        node.Next = node;

        Assert.Throws<InvalidOperationException>(() => serializer.Serialize(node, stream));
    }

    [Test]
    public void TestClassFields()
    {
        for (var i = 0; i < 1000; i++)
        {
            var serializer = new BinarySerializer
            {
                ClassFlags = SerializerFlags.Fields | SerializerFlags.Public
            };
            var stream = new MemoryStream();
            var test = SettingsObjectFields.Random();

            serializer.Serialize(test, stream);
            stream.Position = 0;

            var read = serializer.Deserialize<SettingsObjectFields>(stream);
            Assert.AreEqual(test, read);
        }
    }

    [Test]
    public void TestClassFieldsDoNotDependOnStructFlags()
    {
        var serializer = new BinarySerializer
        {
            ClassFlags = SerializerFlags.Fields | SerializerFlags.NonPublic | SerializerFlags.Public,
            StructFlags = SerializerFlags.Fields | SerializerFlags.Public
        };
        var stream = new MemoryStream();
        var test = new BinarySerializerHiddenFieldClass(123, "abc");

        serializer.Serialize(test, stream);
        stream.Position = 0;

        var read = serializer.Deserialize<BinarySerializerHiddenFieldClass>(stream);
        Assert.AreEqual(test, read);
    }

    [Test]
    public void TestClassProperties()
    {
        for (var i = 0; i < 1000; i++)
        {
            var serializer = new BinarySerializer
            {
                ClassFlags = SerializerFlags.Properties | SerializerFlags.Public
            };
            var stream = new MemoryStream();
            var test = SettingsObjectProperties.Random();

            serializer.Serialize(test, stream);
            stream.Position = 0;

            var read = serializer.Deserialize<SettingsObjectProperties>(stream);
            Assert.AreEqual(test, read);
        }
    }

    [Test]
    public void TestClassPropertiesDoNotDependOnStructFlags()
    {
        var serializer = new BinarySerializer
        {
            ClassFlags = SerializerFlags.Properties | SerializerFlags.Public,
            StructFlags = SerializerFlags.None
        };
        var stream = new MemoryStream();
        var test = new BinarySerializerPropertyClass
        {
            Number = 42,
            Text = "property-test"
        };

        serializer.Serialize(test, stream);
        stream.Position = 0;

        var read = serializer.Deserialize<BinarySerializerPropertyClass>(stream);
        Assert.AreEqual(test, read);
    }

    [Test]
    public void TestCustomSerializer()
    {
        var serializer = new BinarySerializer();
        serializer.Serializers.Add(new BinarySerializerCustomTypeSerializer());

        var stream = new MemoryStream();
        var test = new BinarySerializerCustomType
        {
            Number = 77,
            Text = "custom"
        };

        serializer.Serialize(test, stream);
        stream.Position = 0;

        var read = serializer.Deserialize<BinarySerializerCustomType>(stream);
        Assert.AreEqual(test, read);
    }

    [Test]
    public void TestNull()
    {
        var serializer = new BinarySerializer();
        var stream = new MemoryStream();

        serializer.Serialize(null, stream);
        stream.Position = 0;

        var read = serializer.Deserialize<object>(stream);
        Assert.AreEqual(null, read);
    }

    [Test]
    public void TestStructFields()
    {
        for (var i = 0; i < 1000; i++)
        {
            var serializer = new BinarySerializer
            {
                StructFlags = SerializerFlags.Fields | SerializerFlags.Public
            };
            var stream = new MemoryStream();
            var test = SettingsStructFields.Random();

            serializer.Serialize(test, stream);
            stream.Position = 0;

            var read = serializer.Deserialize<SettingsStructFields>(stream);
            Assert.AreEqual(test, read);
        }
    }

    [Test]
    public void TestStructProperties()
    {
        for (var i = 0; i < 1000; i++)
        {
            var serializer = new BinarySerializer
            {
                StructFlags = SerializerFlags.Properties | SerializerFlags.Public
            };
            var stream = new MemoryStream();
            var test = SettingsStructProperties.Random();

            serializer.Serialize(test, stream);
            stream.Position = 0;

            var read = serializer.Deserialize<SettingsStructProperties>(stream);
            Assert.AreEqual(test, read);
        }
    }

    [Test]
    public void TestTypeNameAllowsDeserializeAsObject()
    {
        var serializer = new BinarySerializer
        {
            ClassFlags = SerializerFlags.Fields | SerializerFlags.Public | SerializerFlags.TypeName
        };
        var stream = new MemoryStream();
        var test = new BinarySerializerTypeNameClass
        {
            Number = 5,
            Text = "typed"
        };

        serializer.Serialize(test, stream);
        stream.Position = 0;

        var read = serializer.Deserialize(typeof(object), stream);
        Assert.IsInstanceOf(typeof(BinarySerializerTypeNameClass), read);
        Assert.AreEqual(test, read);
    }

    [Test]
    public void TestUseToStringAndCctor()
    {
        var serializer = new BinarySerializer();
        serializer.UseToStringAndCctor(typeof(BinarySerializerCtorClass));

        var stream = new MemoryStream();
        var test = new BinarySerializerCtorClass("ctor-value");

        serializer.Serialize(test, stream);
        stream.Position = 0;

        var read = serializer.Deserialize<BinarySerializerCtorClass>(stream);
        Assert.AreEqual(test, read);
    }

    [Test]
    public void TestUseToStringAndParse()
    {
        var serializer = new BinarySerializer();
        serializer.UseToStringAndParse(typeof(BinarySerializerParseClass));

        var stream = new MemoryStream();
        var test = new BinarySerializerParseClass
        {
            Text = "parse-value"
        };

        serializer.Serialize(test, stream);
        stream.Position = 0;

        var read = serializer.Deserialize<BinarySerializerParseClass>(stream);
        Assert.AreEqual(test, read);
    }

    #endregion Public Methods
}

public sealed class BinarySerializerCtorClass : IEquatable<BinarySerializerCtorClass>
{
    #region Public Constructors

    public BinarySerializerCtorClass() { }

    public BinarySerializerCtorClass(string text) => Text = text;

    #endregion Public Constructors

    #region Public Properties

    public string Text { get; set; }

    #endregion Public Properties

    #region Public Methods

    public bool Equals(BinarySerializerCtorClass other) => other != null && other.Text == Text;

    public override bool Equals(object obj) => Equals(obj as BinarySerializerCtorClass);

    public override int GetHashCode() => Text == null ? 0 : Text.GetHashCode();

    public override string ToString() => Text;

    #endregion Public Methods
}

public sealed class BinarySerializerCustomType : IEquatable<BinarySerializerCustomType>
{
    #region Public Properties

    public int Number { get; set; }

    public string Text { get; set; }

    #endregion Public Properties

    #region Public Methods

    public bool Equals(BinarySerializerCustomType other) => other != null && other.Number == Number && other.Text == Text;

    public override bool Equals(object obj) => Equals(obj as BinarySerializerCustomType);

    public override int GetHashCode()
    {
        var hashCode = Number.GetHashCode();
        hashCode = (hashCode * 397) ^ (Text == null ? 0 : Text.GetHashCode());
        return hashCode;
    }

    #endregion Public Methods
}

public sealed class BinarySerializerCustomTypeSerializer : IBinaryTypeSerializer
{
    #region Public Methods

    public bool CanDeserialize(Type type) => type == typeof(BinarySerializerCustomType);

    public bool CanSerialize(object item) => item is BinarySerializerCustomType;

    public object Deserialize(DataReader reader, Type type)
    {
        return new BinarySerializerCustomType
        {
            Number = reader.ReadInt32(),
            Text = reader.ReadPrefixedString()
        };
    }

    public void Serialize(DataWriter writer, object item)
    {
        var value = (BinarySerializerCustomType)item;
        writer.Write(value.Number);
        writer.WritePrefixed(value.Text);
    }

    #endregion Public Methods
}

public class BinarySerializerHiddenFieldClass : IEquatable<BinarySerializerHiddenFieldClass>
{
    #region Protected Fields

    protected int Number;

    #endregion Protected Fields

    #region Public Constructors

    public BinarySerializerHiddenFieldClass() { }

    public BinarySerializerHiddenFieldClass(int number, string text)
    {
        Number = number;
        Text = text;
    }

    #endregion Public Constructors

    #region Public Properties

    public string Text { get; set; }

    public int Value => Number;

    #endregion Public Properties

    #region Public Methods

    public bool Equals(BinarySerializerHiddenFieldClass other) => other != null && other.Number == Number && other.Text == Text;

    public override bool Equals(object obj) => Equals(obj as BinarySerializerHiddenFieldClass);

    public override int GetHashCode()
    {
        var hashCode = Number.GetHashCode();
        hashCode = (hashCode * 397) ^ (Text == null ? 0 : Text.GetHashCode());
        return hashCode;
    }

    #endregion Public Methods
}

public sealed class BinarySerializerNode
{
    #region Public Fields

    public BinarySerializerNode Next;

    #endregion Public Fields
}

public sealed class BinarySerializerParseClass : IEquatable<BinarySerializerParseClass>
{
    #region Public Properties

    public string Text { get; set; }

    #endregion Public Properties

    #region Public Methods

    public static BinarySerializerParseClass Parse(string text)
    {
        return new BinarySerializerParseClass
        {
            Text = text
        };
    }

    public bool Equals(BinarySerializerParseClass other) => other != null && other.Text == Text;

    public override bool Equals(object obj) => Equals(obj as BinarySerializerParseClass);

    public override int GetHashCode() => Text == null ? 0 : Text.GetHashCode();

    public override string ToString() => Text;

    #endregion Public Methods
}

public sealed class BinarySerializerPropertyClass : IEquatable<BinarySerializerPropertyClass>
{
    #region Public Properties

    public int Number { get; set; }

    public string Text { get; set; }

    #endregion Public Properties

    #region Public Methods

    public bool Equals(BinarySerializerPropertyClass other) => other != null && other.Number == Number && other.Text == Text;

    public override bool Equals(object obj) => Equals(obj as BinarySerializerPropertyClass);

    public override int GetHashCode()
    {
        var hashCode = Number.GetHashCode();
        hashCode = (hashCode * 397) ^ (Text == null ? 0 : Text.GetHashCode());
        return hashCode;
    }

    #endregion Public Methods
}

public sealed class BinarySerializerTypeNameClass : IEquatable<BinarySerializerTypeNameClass>
{
    #region Public Fields

    public int Number;

    public string Text;

    #endregion Public Fields

    #region Public Methods

    public bool Equals(BinarySerializerTypeNameClass other) => other != null && other.Number == Number && other.Text == Text;

    public override bool Equals(object obj) => Equals(obj as BinarySerializerTypeNameClass);

    public override int GetHashCode()
    {
        var hashCode = Number.GetHashCode();
        hashCode = (hashCode * 397) ^ (Text == null ? 0 : Text.GetHashCode());
        return hashCode;
    }

    #endregion Public Methods
}
