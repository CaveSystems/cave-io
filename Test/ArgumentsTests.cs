using NUnit.Framework;

using System;
using Cave.IO;
using System.Linq;
using System.Security.Cryptography;
using Cave;

namespace Tests.Cave.IO;

[TestFixture]
    public class ArgumentsTests
    {
        [Test]
        public void Parse_Array_With_Command_And_Parameters()
        {
            var args = Arguments.FromArray(
                Arguments.ParseOptions.ContainsCommand,
                "mycmd", "file1.txt", "file2.txt"
            );

            Assert.That(args.Command, Is.EqualTo("mycmd"));
            Assert.That(args.Parameters.Count, Is.EqualTo(2));
            Assert.That(args.Parameters[0], Is.EqualTo("file1.txt"));
            Assert.That(args.Parameters[1], Is.EqualTo("file2.txt"));
        }

        [Test]
        public void Parse_Array_With_Options()
        {
            var args = Arguments.FromArray("--verbose", "-x", "file.txt");

            Assert.That(args.Options.Count, Is.EqualTo(2));
            Assert.That(args.Options.Contains("verbose"), Is.True);
            Assert.That(args.Options.Contains("x"), Is.True);

            Assert.That(args.Parameters.Count, Is.EqualTo(1));
            Assert.That(args.Parameters[0], Is.EqualTo("file.txt"));
        }

        [Test]
        public void Parse_String_With_Quotes()
        {
            var args = Arguments.FromString(
                Arguments.ParseOptions.ContainsCommand,
                "cmd \"file name.txt\" --opt=\"value with spaces\""
            );

            Assert.That(args.Command, Is.EqualTo("cmd"));
            Assert.That(args.Parameters.Count, Is.EqualTo(1));
            Assert.That(args.Parameters[0], Is.EqualTo("file name.txt"));

            Assert.That(args.Options.Count, Is.EqualTo(1));
            Assert.That(args.Options["opt"].Value, Is.EqualTo("value with spaces"));
        }

        [Test]
        public void Parse_String_Mismatched_Quotes_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                Arguments.FromString("cmd \"unterminated");
            });
        }

        [Test]
        public void IsOptionPresent_Works()
        {
            var args = Arguments.FromArray("--debug", "--level=5");

            Assert.That(args.IsOptionPresent("debug"), Is.True);
            Assert.That(args.IsOptionPresent("level"), Is.True);
            Assert.That(args.IsOptionPresent("missing"), Is.False);
        }

        [Test]
        public void IsParameterPresent_Works()
        {
            var args = Arguments.FromArray("param1", "param2");

            Assert.That(args.IsParameterPresent("param1"), Is.True);
            Assert.That(args.IsParameterPresent("param2"), Is.True);
            Assert.That(args.IsParameterPresent("nope"), Is.False);
        }

        [Test]
        public void ToArray_And_ToString_Work()
        {
            var args = Arguments.FromArray(
                Arguments.ParseOptions.ContainsCommand,
                "cmd", "file.txt", "--opt=value"
            );

            var arr = args.ToArray();
            Assert.That(arr[0], Is.EqualTo("cmd"));
            Assert.That(arr[1], Is.EqualTo("file.txt"));
            Assert.That(arr[2], Is.EqualTo("--opt=value"));

            var str = args.ToString();
            Assert.That(str, Is.EqualTo("cmd file.txt --opt=value"));
        }

        [Test]
        public void AreOptionsPresent_Works()
        {
            var args = Arguments.FromArray("--a", "--b", "--c");

            Assert.That(args.AreOptionsPresent("a", "b"), Is.True);
            Assert.That(args.AreOptionsPresent("a", "x"), Is.False);
        }

        [Test]
        public void GetInvalidOptions_Works()
        {
            var args = Arguments.FromArray("--a", "--b", "--c");

            var invalid = args.GetInvalidOptions("a", "c");

            Assert.That(invalid.Count, Is.EqualTo(1));
            Assert.That(invalid.First().Name, Is.EqualTo("b"));
        }

        [Test]
        public void GetInvalidParameters_Works()
        {
            var args = Arguments.FromArray("p1", "p2", "p3");

            var invalid = args.GetInvalidParameters("p1", "p3");

            Assert.That(invalid.Count, Is.EqualTo(1));
            Assert.That(invalid[0], Is.EqualTo("p2"));
        }
    }
