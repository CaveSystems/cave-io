using NUnit.Framework;

using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using Cave.IO;
using System.IO;
using System.Diagnostics;
using Test;
using System.Collections.Generic;
using Cave;
using System.Threading;

namespace Tests.Cave.IO;

[TestFixture]
public class TestInifile
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

    static TestInifile() => Program.Init();

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

        var random = new Random(123);
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
        ThreadPool.SetMaxThreads(1000, 1000);
        ThreadPool.SetMinThreads(100, 100);

        List<Exception> errors = new();
        Parallel.ForEach(allCultures, culture =>
        {
            var temp = Path.GetTempFileName();
            try
            {
                var settings = new SettingsStructFields[10];
                var properties = IniProperties.Default;
                properties.Culture = culture;

                if (properties.Culture.Calendar is not GregorianCalendar)
                {
                    try { new IniWriter(temp, properties); }
                    catch (NotSupportedException ex) { return; }
                    Assert.Fail($"Calendar {properties.Culture.Calendar} should not be supported!");
                    return;
                }
                var writer = new IniWriter(temp, properties);

                {
                    var setting = SettingsStructFields.Random(null);
                    settings[0] = setting;
                    writer.WriteFields($"Section 0", setting);
                }
                for (var i = 1; i < settings.Length; i++)
                {
                    var setting = SettingsStructFields.Random(properties.Culture);
                    settings[i] = setting;
                    writer.WriteFields($"Section {i}", setting);
                }
                writer.Save(temp);

                TestReader(writer.ToReader(), settings);
                TestReader(IniReader.FromFile(temp, properties), settings);
            }
            catch (Exception ex)
            {
                lock (errors) errors.Add(ex);
            }
            finally
            {
                File.Delete(temp);
            }
        });
        if (errors.Count > 0)
        {
            Assert.Fail("IniReaderWriterTest failed:\n" + errors.Select(e => $"{e}").JoinNewLine());
        }
    }

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
