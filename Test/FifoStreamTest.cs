
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cave;
using Cave.Collections;
using Cave.IO;
using NUnit.Framework;

namespace Tests.Cave.IO;

[TestFixture]
public class FifoStreamTest
{
    #region Public Methods

    [Test]
    public void Test1()
    {
        var b = new byte[256];
        var fifo = new FifoStream();
        fifo.PutBuffer(b);

        //write buffer after add
        for (var i = 0; i < b.Length; i++) b[i] = (byte)i;

        //test content
        Assert.IsTrue(b.SequenceEqual(fifo.ToArray()));

        Assert.AreEqual(0, fifo.ReadByte());
        Assert.AreEqual(255, fifo.Available);
        Assert.AreEqual(255, fifo[254]);

        for (var i = 1; i < b.Length; i++)
        {
            for (var n = i; n < b.Length; n++)
            {
                Assert.AreEqual(n, fifo[n - i]);
            }

            Assert.AreEqual(255, fifo[fifo.Available - 1]);
            Assert.AreEqual(i, fifo.ReadByte());
        }
    }

    [Test]
    public void Test2()
    {
        const int items = 256;
        var fifo = new FifoStream();
        for (var i = 0; i < items; i++) fifo.WriteByte((byte)i);

        Assert.AreEqual(0, fifo.ReadByte());
        Assert.AreEqual(255, fifo.Available);
        Assert.AreEqual(255, fifo[254]);

        for (var i = 1; i < items; i++)
        {
            for (var n = i; n < items; n++)
            {
                Assert.AreEqual(n, fifo[n - i]);
            }

            Assert.AreEqual(255, fifo[fifo.Available - 1]);
            Assert.AreEqual(i, fifo.ReadByte());
        }
    }

    [Test]
    public void TestAppend()
    {
        var fifo = new FifoStream();
        var writer = new DataWriter(fifo);
        var reader = new DataReader(fifo);
        writer.WriteZeroTerminated("Hello World.");
        writer.WriteZeroTerminated("We will now perform some tests.");
        writer.WriteZeroTerminated("This is the end of the testdata.");
        writer.WriteZeroTerminated("Bye!");

        var buf = fifo.ToArray();
        fifo.AppendStream(new MemoryStream(buf));
        fifo.AppendBuffer(buf, 0, buf.Length);
        fifo.AppendBuffer(ASCII.GetBytes("12345"), 1, 3);
        fifo.PutBuffer(ASCII.GetBytes("678"));
        writer.Write((byte)0);
        for (int i = 0; i < 3; i++)
        {
            Assert.That(reader.ReadZeroTerminatedString(), Is.EqualTo("Hello World."));
            Assert.That(reader.ReadZeroTerminatedString(), Is.EqualTo("We will now perform some tests."));
            Assert.That(reader.ReadZeroTerminatedString(), Is.EqualTo("This is the end of the testdata."));
            Assert.That(reader.ReadZeroTerminatedString(), Is.EqualTo("Bye!"));
        }
        Assert.That(reader.ReadZeroTerminatedString(), Is.EqualTo("234678"));
    }

    [Test]
    public void TestCopyContinue()
    {
        var fifo = new FifoStream();
        var writer = new DataWriter(fifo);
        for (var i = 0; i < 10; i++)
        {
            writer.WriteZeroTerminated("111");
            var buffer = fifo.ReadAllBytes();
            Assert.That(buffer.Length, Is.EqualTo(4));
            Assert.That(buffer, Is.EqualTo(ASCII.GetBytes("111\0")));
            for (var j = 0; j < i + 2; j++)
            {
                writer.Write((byte)j);
            }
            using MemoryStream ms = new();
            fifo.CopyTo(ms);
            buffer = ms.ToArray();
            Assert.That(buffer.Length, Is.EqualTo(i + 2));
            Assert.That(buffer, Is.EqualTo(new Counter(0, i + 2).Select(j => (byte)j).ToArray()));
            if (i % 2 == 0)
            {
                fifo.FreeBuffers();
                Assert.That(fifo.Position, Is.EqualTo(0));
                Assert.That(fifo.Length, Is.EqualTo(0));
                Assert.That(fifo.Available, Is.EqualTo(0));
            }
            else
            {
                fifo.Position -= 2;
                Assert.That(fifo.ReadByte(), Is.EqualTo((byte)i));
                Assert.That(fifo.ReadByte(), Is.EqualTo((byte)(i + 1)));
            }
        }
    }

    [Test]
    public void TestSeekAndIndexOf()
    {
        var fifo = new FifoStream();
        var writer = new DataWriter(fifo);
        writer.WriteZeroTerminated("Hello World.");
        writer.WriteZeroTerminated("We will now perform some tests.");
        writer.WriteZeroTerminated("This is the end of the testdata.");
        writer.WriteZeroTerminated("Bye!");

        var reader = new DataReader(fifo);
        Assert.That(reader.ReadZeroTerminatedString(), Is.EqualTo("Hello World."));
        reader.Seek(-5, SeekOrigin.End);
        Assert.That(reader.ReadZeroTerminatedString(), Is.EqualTo("Bye!"));
        reader.Seek(-5, SeekOrigin.Current);
        Assert.That(reader.ReadZeroTerminatedString(), Is.EqualTo("Bye!"));
        reader.Seek(0, SeekOrigin.Begin);
        Assert.That(reader.ReadZeroTerminatedString(), Is.EqualTo("Hello World."));

        var nextNull = fifo.IndexOf((byte)0);
        Assert.That(nextNull, Is.GreaterThan(0));
        reader.Seek(nextNull, SeekOrigin.Current);
        var pos = fifo.Position;
        Assert.That(reader.ReadZeroTerminatedString(), Is.EqualTo(""));
        Assert.That(reader.ReadZeroTerminatedString(), Is.EqualTo("This is the end of the testdata."));

        fifo.Position = 0;
        var space = fifo.IndexOf((byte)' ');
        reader.Seek(space, SeekOrigin.Current);
        Assert.That(reader.ReadZeroTerminatedString(), Is.EqualTo(" World."));

        fifo.Position = 0;
        var search = ASCII.GetBytes("We will now ");
        var index = fifo.IndexOf(search);
        fifo.Position = index;
        Assert.That(reader.ReadZeroTerminatedString(), Is.EqualTo("We will now perform some tests."));

        //now we allow the fifo to throw away buffers after reading..
        fifo.Position = 0;
        Assert.That(fifo.Position, Is.EqualTo(0));
        Assert.That(reader.ReadZeroTerminatedString(), Is.EqualTo("Hello World."));
        fifo.FreeBuffers();
        Assert.That(fifo.Position, Is.EqualTo(0));
        Assert.That(fifo.Length, Is.EqualTo(70));
        Assert.That(reader.ReadZeroTerminatedString(), Is.EqualTo("We will now perform some tests."));
        Assert.That(fifo.Position, Is.EqualTo(32));
        Assert.That(fifo.Length, Is.EqualTo(70));
        fifo.FreeBuffers();
        Assert.That(fifo.Position, Is.EqualTo(0));
        Assert.That(fifo.Length, Is.EqualTo(38));

        fifo.Clear();
        Assert.That(fifo.Position, Is.EqualTo(0));
        Assert.That(fifo.Length, Is.EqualTo(0));

        fifo.Clear();
        fifo.PutBuffer(ASCII.GetBytes("12345"));
        Assert.That(fifo.Position, Is.EqualTo(0));
        Assert.That(fifo.Length, Is.EqualTo(5));
        Assert.That(fifo.Available, Is.EqualTo(5));
        reader.ReadBytes(5);
        Assert.That(fifo.Position, Is.EqualTo(5));
        Assert.That(fifo.Length, Is.EqualTo(5));
        Assert.That(fifo.Available, Is.EqualTo(0));
    }

    #endregion Public Methods
}
