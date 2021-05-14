using System.IO;
using NUnit.Framework;
using Cave.IO;

namespace Test.Cave.IO
{
    [TestFixture]
    public class MarshalStructTest
    {
        #region Public Methods

        [Test]
        public void Test_MarshalStruct_1()
        {
            var l_Test = InteropTestStruct.Create(1);

            var data = MarshalStruct.GetBytes(l_Test);
            var result1 = MarshalStruct.GetStruct<InteropTestStruct>(data);
            Assert.AreEqual(l_Test, result1);

            var stream = new MemoryStream();
            MarshalStruct.Write(stream, l_Test);
            stream.Position = 0;
            var result2 = MarshalStruct.Read<InteropTestStruct>(stream);
            Assert.AreEqual(l_Test, result2);

            var buffer = new byte[100000];
            MarshalStruct.Write(l_Test, buffer, 1024);
            var result3 = MarshalStruct.Read<InteropTestStruct>(buffer, 1024);
            Assert.AreEqual(l_Test, result3);
        }

        #endregion Public Methods
    }
}
