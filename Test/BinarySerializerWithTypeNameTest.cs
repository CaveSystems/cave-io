using System.IO;
using Cave.IO;
using NUnit.Framework;

namespace Test.Cave.IO
{
    [TestFixture]
    public class BinarySerializerWithTypeNameTest
    {
        #region Public Methods

        [Test]
        public void TestBigBlockWithSystemUri()
        {
            var serializer = new BinarySerializer();
            serializer.ClassFlags |= SerializerFlags.TypeName;
            serializer.StructFlags |= SerializerFlags.TypeName;
            serializer.UseToStringAndCctor(typeof(System.Uri));

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
        public void TestClassFields()
        {
            for (var i = 0; i < 1000; i++)
            {
                var serializer = new BinarySerializer();
                serializer.ClassFlags |= SerializerFlags.TypeName;
                serializer.StructFlags |= SerializerFlags.TypeName;
                var stream = new MemoryStream();
                var test = SettingsObjectFields.RandomStruct();
                serializer.Serialize(test, stream);
                stream.Position = 0;
                var read = serializer.Deserialize<SettingsObjectFields>(stream);
                Assert.AreEqual(test, read);
            }
        }

        [Test]
        public void TestClassProperties()
        {
            for (var i = 0; i < 1000; i++)
            {
                var serializer = new BinarySerializer();
                serializer.ClassFlags |= SerializerFlags.TypeName;
                serializer.StructFlags |= SerializerFlags.TypeName;
                var stream = new MemoryStream();
                var test = SettingsObjectProperties.RandomStruct();
                serializer.Serialize(test, stream);
                stream.Position = 0;
                var read = serializer.Deserialize<SettingsObjectProperties>(stream);
                Assert.AreEqual(test, read);
            }
        }

        [Test]
        public void TestNull()
        {
            var serializer = new BinarySerializer();
            serializer.ClassFlags |= SerializerFlags.TypeName;
            serializer.StructFlags |= SerializerFlags.TypeName;
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
                var serializer = new BinarySerializer();
                serializer.ClassFlags |= SerializerFlags.TypeName;
                serializer.StructFlags |= SerializerFlags.TypeName;
                var stream = new MemoryStream();
                var test = SettingsStructFields.RandomStruct();
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
                var serializer = new BinarySerializer();
                serializer.ClassFlags |= SerializerFlags.TypeName;
                serializer.StructFlags |= SerializerFlags.TypeName;
                var stream = new MemoryStream();
                var test = SettingsStructProperties.RandomStruct();
                serializer.Serialize(test, stream);
                stream.Position = 0;
                var read = serializer.Deserialize<SettingsStructProperties>(stream);
                Assert.AreEqual(test, read);
            }
        }

        #endregion Public Methods
    }
}
