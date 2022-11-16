using System.IO;
using Cave.IO;
using NUnit.Framework;

namespace Tests.Cave.IO
{
    [TestFixture]
    public class MarshalStructTest
    {
        #region Public Methods

        [Test]
        public void Roundtrip()
        {
            for (var i = 1; i < 1000; i++)
            {
                var test = InteropTestStruct.Create(i);

                var data = MarshalStruct.GetBytes(test);
                var result1 = MarshalStruct.GetStruct<InteropTestStruct>(data);
                Assert.AreEqual(test, result1);

                var stream = new MemoryStream();
                MarshalStruct.Write(stream, test);
                stream.Position = 0;
                var result2 = MarshalStruct.Read<InteropTestStruct>(stream);
                Assert.AreEqual(test, result2);

                var buffer = new byte[100000];
                MarshalStruct.Write(test, buffer, 1024);
                var result3 = MarshalStruct.Read<InteropTestStruct>(buffer, 1024);
                Assert.AreEqual(test, result3);
            }
        }

        #endregion Public Methods
    }
}
