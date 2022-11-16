using Cave.IO;
using NUnit.Framework;

namespace Tests.Cave.IO.IniFile
{
    [TestFixture]
    public class TestInifile
    {
        #region Public Methods

        [Test]
        public void Test()
        {
            var writer = new IniWriter();
            for (var i = 0; i < 100; i++)
            {
                var s = TestStruct.Create(i ^ (1 << (i % 32)));
                writer.WriteFields($"struct{i}", s);
            }
            var reader = writer.ToReader();
            for (var i = 0; i < 100; i++)
            {
                var expected = TestStruct.Create(i ^ (1 << (i % 32)));
                var current = reader.ReadStructFields<TestStruct>($"struct{i}");
                Assert.AreEqual(expected, current);
            }
        }

        [Test]
        public void TestReader()
        {
            var fileName = "c:\\temp\\lala 123\\file name.exe";
            var test =
                "[Service]\r\n" +
                $"Path1 = {fileName}\r\n" +
                $"Path2 = {fileName}\r\n";

            var ini = IniReader.Parse("ini", test);
            Assert.AreEqual(fileName, ini.ReadSetting("Service", "Path1"));
            Assert.AreEqual(fileName, ini.ReadSetting("Service", "Path2"));
        }

        #endregion Public Methods
    }
}
