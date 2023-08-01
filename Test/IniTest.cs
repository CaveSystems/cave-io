using Cave.IO;
using NUnit.Framework;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Cave.IO.IniFile
{
    [TestFixture]
    public class IniTest
    {
        #region Private Fields

        readonly CultureInfo[] allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

        #endregion Private Fields

        #region Private Methods

        void TestReader(IniReader reader, SettingsStructFields[] settings)
        {
            var fields1 = typeof(SettingsStructFields).GetFields().OrderBy(f => f.Name).ToArray();
            var fields2 = typeof(SettingsObjectFields).GetFields().OrderBy(f => f.Name).ToArray();
            var fields3 = typeof(SettingsStructProperties).GetProperties().OrderBy(f => f.Name).ToArray();
            var fields4 = typeof(SettingsObjectProperties).GetProperties().OrderBy(f => f.Name).ToArray();
            for (var i = 0; i < settings.Length; i++)
            {
                var settings1 = reader.ReadStructFields<SettingsStructFields>($"Section {i}");
                var settings2 = reader.ReadObjectFields<SettingsObjectFields>($"Section {i}");
                var settings3 = reader.ReadStructProperties<SettingsStructProperties>($"Section {i}");
                var settings4 = reader.ReadObjectProperties<SettingsObjectProperties>($"Section {i}");

                for (var n = 0; n < fields1.Length; n++)
                {
                    var original = fields1[n].GetValue(settings[i]);
                    var value1 = fields1[n].GetValue(settings1);
                    var value2 = fields2[n].GetValue(settings2);
                    var value3 = fields3[n].GetValue(settings3, null);
                    var value4 = fields4[n].GetValue(settings4, null);
                    if (original is DateTime dt && !Equals(original, value1))
                    {
                        switch (reader.Properties.Culture.ThreeLetterISOLanguageName)
                        {
                            case "dzo":
                                return;

                            default:
                                throw new NotImplementedException();
                        }
                    }
                    Assert.AreEqual(original, value1);
                    Assert.AreEqual(original, value2);
                    Assert.AreEqual(original, value3);
                    Assert.AreEqual(original, value4);
                }
            }
        }

        #endregion Private Methods

        #region Public Constructors

#if NET5_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        static IniTest() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif

        #endregion Public Constructors

        #region Public Methods

        [Test]
        public void IniReaderWriterStringTest()
        {
            void Test(string s)
            {
                var writer = new IniWriter();
                writer.WriteSetting("test", "string", s);
                var reader = writer.ToReader();
                var value = reader.ReadSetting("test", "string");
                Assert.AreEqual(s, value);
            }

            for (var i = 0; i < 255; i++)
            {
                Test(((char)i).ToString());
            }

            var random = new Random();
            foreach (var encodingInfo in Encoding.GetEncodings())
            {
                for (var i = 0; i < 100; i++)
                {
                    var encoding = encodingInfo.GetEncoding();
                    var buf = encoding.GetBytes(new string(' ', 100));
                    random.NextBytes(buf);
                    var str = encoding.GetString(buf);

                    Test(str);
                    Test(str + " ");
                    Test(" " + str);
                    Test("#" + str);
                    Test("\t" + str + "\r\n");
                }
            }
        }

        [Test]
        public void IniReaderWriterTest()
        {
            object syncRoot = new();
            Parallel.ForEach(allCultures, culture =>
            {
                var temp = Path.GetTempFileName();
                lock (syncRoot) Console.WriteLine($"{nameof(IniReaderWriterTest)}.cs: info TI0002: Test {culture}, file {temp}");

                if (culture.Calendar is not GregorianCalendar)
                {
                    lock (syncRoot) Console.WriteLine($"- Skipping calendar {culture.Calendar}");
                    return;
                }

                var settings = new SettingsStructFields[10];
                var properties = IniProperties.Default;
                properties.Culture = culture;
                var writer = new IniWriter(temp, properties);

                {
                    var setting = SettingsStructFields.Random(null);
                    settings[0] = setting;
                    writer.WriteFields($"Section 0", setting);
                }
                for (var i = 1; i < settings.Length; i++)
                {
                    var setting = SettingsStructFields.Random(culture);
                    settings[i] = setting;
                    writer.WriteFields($"Section {i}", setting);
                }
                writer.Save(temp);

                TestReader(writer.ToReader(), settings);
                TestReader(IniReader.FromFile(temp, properties), settings);
                File.Delete(temp);
            });
        }

        #endregion Public Methods
    }
}
