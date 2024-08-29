#define PARALLEL

using NUnit.Framework;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cave;
using Cave.IO;
using System.Threading;

namespace Tests.Cave.IO
{
    [TestFixture]
    public class DataReaderWriterTest
    {
        #region Private Methods

        string CreateString(StringEncoding encoding, byte[] randomBuffer)
        {
            var charArray = new char[128];
            for (var i = 0; i < charArray.Length; i++)
            {
                charArray[i] = (char)(127 - i);
            }
            string randomString;
            if (encoding is StringEncoding.ASCII or StringEncoding.US_ASCII)
            {
                return new(charArray);
            }

            //if charset supports > 127 characters
            var sb = new StringBuilder();
            sb.Append(charArray);
            for (var n = 0; n < randomBuffer.Length;)
            {
                int codepoint = (randomBuffer[n++] * 256 + randomBuffer[n++]);
                if (codepoint >= 0xD800) codepoint <<= 1;
                sb.Append(char.ConvertFromUtf32(codepoint));
            }
            randomString = sb.ToString();
            var randomStringBytes = encoding.Encode(randomString);
            if (encoding.Decode(randomStringBytes) != randomString)
            {
                //cannot roundtrip high surrogates
                //build a string with random characters supported by the specified encoding...
                randomString = encoding.Decode(randomBuffer).Replace("?", "");
                while (!encoding.CanRoundtrip(randomString))
                {
                    var newBuffer = encoding.Encode(randomString);
                    randomString = encoding.Decode(newBuffer).Replace("?", "");
                }
                _ = encoding.Encode(randomString, withRoundtripTest: true);
            }
            return randomString;
        }

        void TestReaderWriter(EncodingInfo encoding)
        {
            var stream = new MemoryStream();
            var writer = new DataWriter(stream, encoding.GetEncoding());
            var reader = new DataReader(stream, encoding.GetEncoding());
            TestReaderWriter(reader, writer);
        }

        void TestReaderWriter(DataReader reader, DataWriter writer)
        {
            var buffer = new byte[2 * 1024];
            new Random(123).NextBytes(buffer);
            var dateTime = DateTime.UtcNow;
            var timeSpan = new TimeSpan(Environment.TickCount);

            bool testStrings;
            string randomString = null;
            try
            {
                var lf = writer.LineFeed;
                testStrings = true;
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(NotSupportedException), ex.GetType());
                testStrings = false;
            }

            for (var i = int.MaxValue; i > 0; i >>= 1)
            {
                writer.Write7BitEncoded32(-i);
                writer.Write7BitEncoded32(i);
            }

            for (var i = long.MaxValue; i > 0; i >>= 1)
            {
                writer.Write7BitEncoded64(-i);
                writer.Write7BitEncoded64(i);
            }

            if (testStrings)
            {
                randomString = CreateString(writer.StringEncoding, buffer);
                TestStringsWrite(writer, randomString);
            }

            var position = writer.BaseStream.Position;
            writer.Write(true);
            writer.Write(false);
            writer.Write(dateTime);
            writer.Write(timeSpan);
            writer.Write(1.23456789m);
            writer.Write(1.23456);
            writer.Write(double.NaN);
            writer.Write(double.PositiveInfinity);
            writer.Write(double.NegativeInfinity);
            writer.Write(1.234f);
            writer.Write(float.NaN);
            writer.Write(float.PositiveInfinity);
            writer.Write(float.NegativeInfinity);
            writer.Write(12345678);
            writer.Write(int.MaxValue);
            writer.Write(int.MinValue);
            writer.Write(uint.MaxValue);
            writer.Write(uint.MinValue);
            writer.Write(long.MaxValue);
            writer.Write(long.MinValue);
            writer.Write(ulong.MaxValue);
            writer.Write(ulong.MinValue);
            writer.Write(short.MaxValue);
            writer.Write(short.MinValue);
            writer.Write(ushort.MaxValue);
            writer.Write(ushort.MinValue);
            writer.Write(byte.MaxValue);
            writer.Write(byte.MinValue);
            writer.Write(sbyte.MaxValue);
            writer.Write(sbyte.MinValue);

            writer.Write7BitEncoded64(long.MaxValue);
            writer.WritePrefixed(buffer);
            writer.WriteEpoch32(dateTime);
            writer.WriteEpoch64(dateTime);
            writer.Write(buffer);

            reader.BaseStream.Position = 0;
            var msg = $"{reader.StringEncoding}: Failed writer -> reader roundtrip!";

            for (var i = int.MaxValue; i > 0; i >>= 1)
            {
                Assert.AreEqual(-i, reader.Read7BitEncodedInt32(), msg);
                Assert.AreEqual(i, reader.Read7BitEncodedInt32(), msg);
            }

            for (var i = long.MaxValue; i > 0; i >>= 1)
            {
                Assert.AreEqual(-i, reader.Read7BitEncodedInt64(), msg);
                Assert.AreEqual(i, reader.Read7BitEncodedInt64(), msg);
            }

            if (testStrings)
            {
                TestStringsRead(reader, randomString);
            }

            Assert.AreEqual(position, reader.BaseStream.Position, $"{reader.StringEncoding}: End position during read does not match write. String decoding may have lost some bytes...");

            Assert.AreEqual(true, reader.ReadBool(), msg);
            Assert.AreEqual(false, reader.ReadBool(), msg);
            Assert.AreEqual(dateTime, reader.ReadDateTime(), msg);
            Assert.AreEqual(timeSpan, reader.ReadTimeSpan(), msg);
            Assert.AreEqual(1.23456789m, reader.ReadDecimal(), msg);
            Assert.AreEqual(1.23456, reader.ReadDouble(), msg);
            Assert.AreEqual(double.NaN, reader.ReadDouble(), msg);
            Assert.AreEqual(double.PositiveInfinity, reader.ReadDouble(), msg);
            Assert.AreEqual(double.NegativeInfinity, reader.ReadDouble(), msg);
            Assert.AreEqual(1.234f, reader.ReadSingle(), msg);
            Assert.AreEqual(float.NaN, reader.ReadSingle(), msg);
            Assert.AreEqual(float.PositiveInfinity, reader.ReadSingle(), msg);
            Assert.AreEqual(float.NegativeInfinity, reader.ReadSingle(), msg);
            Assert.AreEqual(12345678, reader.ReadInt32(), msg);
            Assert.AreEqual(int.MaxValue, reader.ReadInt32(), msg);
            Assert.AreEqual(int.MinValue, reader.ReadInt32(), msg);
            Assert.AreEqual(uint.MaxValue, reader.ReadUInt32(), msg);
            Assert.AreEqual(uint.MinValue, reader.ReadUInt32(), msg);
            Assert.AreEqual(long.MaxValue, reader.ReadInt64(), msg);
            Assert.AreEqual(long.MinValue, reader.ReadInt64(), msg);
            Assert.AreEqual(ulong.MaxValue, reader.ReadUInt64(), msg);
            Assert.AreEqual(ulong.MinValue, reader.ReadUInt64(), msg);
            Assert.AreEqual(short.MaxValue, reader.ReadInt16(), msg);
            Assert.AreEqual(short.MinValue, reader.ReadInt16(), msg);
            Assert.AreEqual(ushort.MaxValue, reader.ReadUInt16(), msg);
            Assert.AreEqual(ushort.MinValue, reader.ReadUInt16(), msg);
            Assert.AreEqual(byte.MaxValue, reader.ReadUInt8(), msg);
            Assert.AreEqual(byte.MinValue, reader.ReadUInt8(), msg);
            Assert.AreEqual(sbyte.MaxValue, reader.ReadInt8(), msg);
            Assert.AreEqual(sbyte.MinValue, reader.ReadInt8(), msg);

            Assert.AreEqual(long.MaxValue, reader.Read7BitEncodedInt64(), msg);
            CollectionAssert.AreEqual(buffer, reader.ReadBytes(), msg);
            var epoch = new DateTime(dateTime.Ticks - (dateTime.Ticks % TimeSpan.TicksPerSecond));
            Assert.AreEqual(epoch, reader.ReadEpoch32(), msg);
            Assert.AreEqual(epoch, reader.ReadEpoch64(), msg);
            CollectionAssert.AreEqual(buffer, reader.ReadBytes(buffer.Length), msg);
        }

        void TestReaderWriter(StringEncoding stringEncoding)
        {
            if (stringEncoding == 0)
            {
                return;
            }

            //little endian
            using (var stream = new MemoryStream())
            {
                var writer = new DataWriter(stream, stringEncoding);
                var reader = new DataReader(stream, stringEncoding);
                TestReaderWriter(reader, writer);
            }

            //big endian
            using (var stream = new MemoryStream())
            {
                var writer = new DataWriter(stream, stringEncoding, NewLineMode.CRLF, EndianType.BigEndian);
                var reader = new DataReader(stream, stringEncoding, NewLineMode.CRLF, EndianType.BigEndian);
                TestReaderWriter(reader, writer);
            }
        }

        void TestStringsRead(DataReader reader, string randomString)
        {
            var msg = $"{reader.StringEncoding}: Failed writer -> reader roundtrip!";
            Assert.AreEqual("Testline", reader.ReadLine(), msg + " Preamble test failed!");
            {
                var expected = reader.StringEncoding.Decode(reader.StringEncoding.Encode("Test.Too.Long!").GetRange(0, 8));
                Assert.AreEqual(expected, reader.ReadString(byteCount: 8).BeforeFirst('\0'), msg);
            }

            Assert.AreEqual(0x01020304, reader.ReadInt32(), msg);
            Assert.AreEqual(randomString[0], reader.ReadChar(), msg);
            Assert.AreEqual((byte)0, reader.ReadByte(), msg);
            Assert.AreEqual(randomString[1], reader.ReadChar(), msg);
            Assert.AreEqual((byte)1, reader.ReadByte(), msg);
            {
                var codepoints = randomString.CountCodepoints();
                var readString = reader.ReadChars(codepoints);
                CollectionAssert.AreEqual(randomString, readString, msg);
            }
            Assert.AreEqual((ushort)2, reader.ReadUInt16(), msg);
            {
                var codepoints = randomString.CountCodepoints();
                var readChars = reader.ReadChars(codepoints);
                CollectionAssert.AreEqual(randomString, readChars, msg);
            }
            Assert.AreEqual((uint)3, reader.ReadUInt32(), msg);

            Assert.AreEqual(randomString.Replace("\0", ""), reader.ReadZeroTerminatedString(65536), msg);
            Assert.AreEqual("", reader.ReadZeroTerminatedString(1024), msg);

            Assert.AreEqual(randomString, reader.ReadString(), msg);
            Assert.AreEqual("", reader.ReadString(), msg);
            Assert.AreEqual(null, reader.ReadPrefixedString(), msg);

            Assert.AreEqual(randomString.Replace(reader.LineFeed, ""), reader.ReadLine(), msg);
            Assert.AreEqual("", reader.ReadLine(), msg);

            switch (reader.NewLineMode)
            {
                case NewLineMode.CR:
                {
                    var expected = "\n\n\n";
                    var readLine = reader.ReadLine();
                    Assert.AreEqual(expected, readLine, msg);
                    break;
                }
                case NewLineMode.CRLF:
                case NewLineMode.LF:
                {
                    var expected = "\r\r\r";
                    var readLine = reader.ReadLine();
                    Assert.AreEqual(expected, readLine, msg);
                    break;
                }
                default: throw new NotSupportedException();
            }
        }

        void TestStringsWrite(DataWriter writer, string randomString)
        {
            if (writer.StringEncoding != StringEncoding.UTF_7)
            {
                writer.Write(writer.StringEncoding.GetByteOrderMark());
            }
            writer.WriteLine("Testline");
            var pos = writer.BaseStream.Position;
            writer.WriteString("Test.Too.Long!", byteCount: 8);
            Assert.AreEqual(8, writer.BaseStream.Position - pos);
            writer.Write(0x01020304);
            writer.Write(randomString[0]);
            writer.Write((byte)0);
            writer.Write(randomString[1]);
            writer.Write((byte)1);
            writer.Write(randomString);
            writer.Write((ushort)2);
            writer.Write(randomString.ToCharArray());
            writer.Write((uint)3);
            writer.WriteZeroTerminated(randomString.Replace("\0", ""));
            writer.WriteZeroTerminated("");
            Assert.Throws<ArgumentNullException>(() => writer.WriteZeroTerminated((string)null));
            writer.WritePrefixed(randomString);
            writer.WritePrefixed("");
            writer.WritePrefixed((string)null);
            writer.WriteLine(randomString.Replace(writer.LineFeed, ""));
            writer.WriteLine("");
            Assert.Throws<ArgumentNullException>(() => writer.WriteLine((string)null));
            switch (writer.NewLineMode)
            {
                case NewLineMode.CR:
                {
                    writer.WriteLine("\n\n\n");
                    break;
                }
                case NewLineMode.CRLF:
                case NewLineMode.LF:
                {
                    writer.WriteLine("\r\r\r");
                    break;
                }
                default: throw new NotSupportedException();
            }
        }

        #endregion Private Methods

        #region Public Fields

        public const string AceOfSpades = "\U0001F0A0";

        public const string TestString = "My Card: " + AceOfSpades;

        #endregion Public Fields

        #region Public Methods

        [Test]
        public void Iso2022Test()
        {
#if NET5_0_OR_GREATER
            Assert.Ignore("ISO 2022 is no longer supported with net > 5.0!");
#elif NETCOREAPP1_0_OR_GREATER
            Assert.Ignore("ISO 2022 is no longer supported with netcore!");
#else
            var buffer = new byte[1024];
            new Random(123).NextBytes(buffer);
            var sb = new StringBuilder();
            for (int codepoint = 1; codepoint < 0x10FFFF; codepoint = codepoint * 3 + 7)
            {
                sb.Append(char.ConvertFromUtf32(codepoint));
            }

            var encodings = new[]
            {
                StringEncoding.ISO_2022_KR,
                StringEncoding.ISO_2022_JP,
                StringEncoding.ISO_2022_JP_2,
                StringEncoding.CSISO2022JP,
            };
            foreach (var encoding in encodings)
            {
                using (var stream = new MemoryStream())
                {
                    var writer = new DataWriter(stream, encoding);
                    var reader = new DataReader(stream, encoding);
                    var codepoints = new List<string>();
                    for (int codepoint = 1; codepoint < 0x10FFFF; codepoint = codepoint * 3 + 7)
                    {
                        var character = char.ConvertFromUtf32(codepoint);
                        if (!encoding.CanRoundtrip(character)) continue;
                        var pos = stream.Position;
                        writer.Write(character);
                        codepoints.Add(character);
                        stream.Position = pos;
                        var test = reader.ReadChars(1);
                        CollectionAssert.AreEqual(character, test);
                    }
                    stream.Position = 0;
                    var real = codepoints.Join();
                    var roundtrip = reader.ReadChars(codepoints.Count);
                    CollectionAssert.AreEqual(real, roundtrip);

                    stream.SetLength(0);
                    //build a string with random characters supported by the specified encoding...
                    var randomString = writer.StringEncoding.Decode(buffer).Replace("\r", "").Replace("\n", "").Replace("\0", "");
                    while (!writer.StringEncoding.CanRoundtrip(randomString))
                    {
                        var newBuffer = writer.StringEncoding.Encode(randomString);
                        randomString = writer.StringEncoding.Decode(newBuffer);
                    }
                    var randomStringBytes = writer.StringEncoding.Encode(randomString, withRoundtripTest: true);
                    writer.WritePrefixed(randomString);
                    writer.WriteLine(randomString);
                    writer.WriteZeroTerminated(randomString);
                    reader.BaseStream.Position = 0;
                    Assert.AreEqual(randomString, reader.ReadPrefixedString());
                    Assert.AreEqual(randomString, reader.ReadLine());
                    Assert.AreEqual(randomString, reader.ReadZeroTerminatedString(64 * 1024));
                }
            }
#endif
        }

        [Test]
        public void TestAllStringEncodings()
        {
            var id = "T" + MethodBase.GetCurrentMethod().GetHashCode().ToString("x4");
            var encodings = Enum.GetValues(typeof(StringEncoding)).Cast<StringEncoding>();
#if !PARALLEL
            encodings.ForEach(stringEncoding =>
#else
            ThreadPool.SetMaxThreads(1000, 1000);
            ThreadPool.SetMinThreads(100, 100);
            Parallel.ForEach(encodings, stringEncoding =>
#endif
            {
                try
                {
                    TestReaderWriter(stringEncoding);
                }
                catch (Exception ex)
                {
                    if (ex is not NotSupportedException)
                    {
                        Console.Error.WriteLine($"StringEncoding: {stringEncoding}");
                        Console.Error.WriteLine(ex.ToString());
                        throw new AggregateException($"Error at StringEncoding {stringEncoding}", ex);
                    }

                    Assert.Ignore($"Test : info {id}: TestReaderWriter({stringEncoding}) not supported");
                }
            });
        }

        [Test]
        public void TestFrameworkEncoders()
        {
            var id = "T" + MethodBase.GetCurrentMethod().GetHashCode().ToString("x4");
            var encodings = Encoding.GetEncodings();
#if !PARALLEL
            encodings.ForEach(encoding =>
#else
            ThreadPool.SetMaxThreads(1000, 1000);
            ThreadPool.SetMinThreads(100, 100);
            Parallel.ForEach(encodings, encoding =>
#endif
            {
                TestReaderWriter(encoding);
            });
        }

        [Test]
        public void UnicodeTest()
        {
            var encodings = new[] { StringEncoding.UTF7, StringEncoding.UTF8, StringEncoding.UTF16, StringEncoding.UTF16BE, StringEncoding.UTF32, StringEncoding.UTF32BE, };
            foreach (var encoding in encodings)
            {
                using (var stream = new MemoryStream())
                {
                    var writer = new DataWriter(stream, encoding);
                    var reader = new DataReader(stream, encoding);
                    var codepoints = new List<string>();
                    for (int codepoint = 1; codepoint < 0x10FFFF; codepoint = codepoint * 3 + 7)
                    {
                        var character = char.ConvertFromUtf32(codepoint);
                        var pos = stream.Position;
                        writer.Write(character);
                        codepoints.Add(character);
                        stream.Position = pos;
                        var test = reader.ReadChars(1);
                        CollectionAssert.AreEqual(character, test);
                    }
                    stream.Position = 0;
                    var real = codepoints.Join();
                    var roundtrip = reader.ReadChars(codepoints.Count);
                    CollectionAssert.AreEqual(real, roundtrip);

                    {
                        stream.Position = 0;
                        writer.WriteZeroTerminated(TestString);
                        stream.Position = 0;
                        var test = reader.ReadZeroTerminatedString(1024);
                        Assert.AreEqual(TestString, test);
                    }
                }
            }
        }

        [Test]
        public void Utf7AceOfSpadesTest()
        {
            using (var stream = new MemoryStream())
            {
                var writer = new DataWriter(stream, StringEncoding.UTF_7);
                var reader = new DataReader(stream, StringEncoding.UTF_7);
                writer.WriteLine((UTF7)AceOfSpades);
                writer.WriteZeroTerminated((UTF7)AceOfSpades);
                writer.Write((UTF7)AceOfSpades);
                writer.Write(AceOfSpades.ToCharArray());
                stream.Position = 0;
                Assert.AreEqual(AceOfSpades, reader.ReadLine());
                Assert.AreEqual(AceOfSpades, reader.ReadZeroTerminatedString(128));
                Assert.AreEqual(AceOfSpades, reader.ReadUTF7(1));
                Assert.AreEqual(AceOfSpades, reader.ReadChars(1));
            }
        }

        [Test]
        public void Utf7Test()
        {
            using (var stream = new MemoryStream())
            {
                var writer = new DataWriter(stream, StringEncoding.UTF_7);
                var reader = new DataReader(stream, StringEncoding.UTF_7);
                var codepoints = new List<string>();
                for (int codepoint = 1; codepoint < 0x10FFFF; codepoint = codepoint * 3 + 7)
                {
                    var character = char.ConvertFromUtf32(codepoint);
                    var pos = stream.Position;
                    writer.Write(character);
                    codepoints.Add(character);
                    stream.Position = pos;
                    var test = reader.ReadChars(1);
                    CollectionAssert.AreEqual(character, test);
                }
                stream.Position = 0;
                CollectionAssert.AreEqual(codepoints.Join(), reader.ReadChars(codepoints.Count));
            }
        }

        [Test]
        public void Utf8AceOfSpadesTest()
        {
            using (var stream = new MemoryStream())
            {
                var writer = new DataWriter(stream);
                var reader = new DataReader(stream);
                writer.WriteLine((UTF8)AceOfSpades);
                writer.WriteZeroTerminated((UTF8)AceOfSpades);
                writer.Write((UTF8)AceOfSpades);
                writer.Write(AceOfSpades.ToCharArray());
                stream.Position = 0;
                Assert.AreEqual(AceOfSpades, reader.ReadLine());
                Assert.AreEqual(AceOfSpades, reader.ReadZeroTerminatedString(128));
                Assert.AreEqual(AceOfSpades, reader.ReadString(4));
                Assert.AreEqual(AceOfSpades, reader.ReadChars(1));
            }
        }

        [Test]
        public void Utf8ReaderWriterTest() => TestReaderWriter(StringEncoding.UTF_8);

        [Test]
        public void Utf8Test()
        {
            using (var stream = new MemoryStream())
            {
                var writer = new DataWriter(stream);
                var reader = new DataReader(stream);
                var codepoints = new List<string>();
                for (int codepoint = 1; codepoint < 0x10FFFF; codepoint = codepoint * 3 + 7)
                {
                    var character = char.ConvertFromUtf32(codepoint);
                    var pos = stream.Position;
                    writer.Write(character);
                    codepoints.Add(character);
                    stream.Position = pos;
                    var test = reader.ReadChars(1);
                    CollectionAssert.AreEqual(character, test);
                }
                stream.Position = 0;
                CollectionAssert.AreEqual(codepoints.Join(), reader.ReadChars(codepoints.Count));
            }
        }

        #endregion Public Methods
    }
}
