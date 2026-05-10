using NUnit.Framework;

using System;
using Cave.IO;
using System.Linq;
using System.Security.Cryptography;
using Cave;

namespace Tests.Cave.IO;

[TestFixture]
public class EndianTypeTests
{
    [Test]
    public void EndianTypeEqualsConverterTest()
    {
        var converters = new BitConverterBase[] { LittleEndian.Converter, BigEndian.Converter };
        var types = new EndianType[] { EndianType.LittleEndian, EndianType.BigEndian };
        for (int i = 0; i < converters.Length; i++)
        {
            var bc = converters[i];
            var type = types[i];
            Assert.That(bc, Is.SameAs(type.GetBitConverter()));

            Assert.That(type.ToBoolean(type.GetBytes(true), 0), Is.True);
            Assert.That(type.ToByte(type.GetBytes((byte)123), 0), Is.EqualTo((byte)123));
            Assert.That(type.ToDateTime(type.GetBytes(DateTime.Now), 0), Is.EqualTo(DateTime.Now).Within(TimeSpan.FromSeconds(1)));
            Assert.That(type.ToDecimal(type.GetBytes(123.456m), 0), Is.EqualTo(123.456m));
            Assert.That(type.ToDouble(type.GetBytes(123.456), 0), Is.EqualTo(123.456).Within(0.000001));
            Assert.That(type.ToInt16(type.GetBytes((short)12345), 0), Is.EqualTo((short)12345));
            Assert.That(type.ToInt32(type.GetBytes(123456789), 0), Is.EqualTo(123456789));
            Assert.That(type.ToInt64(type.GetBytes(1234567890123456789L), 0), Is.EqualTo(1234567890123456789L));
            Assert.That(type.ToSByte(type.GetBytes((sbyte)-100), 0), Is.EqualTo((sbyte)-100));
            Assert.That(type.ToSingle(type.GetBytes(123.45f), 0), Is.EqualTo(123.45f).Within(0.0001f));
            Assert.That(type.ToTimeSpan(type.GetBytes(TimeSpan.FromHours(1)), 0), Is.EqualTo(TimeSpan.FromHours(1)));
            Assert.That(type.ToUInt16(type.GetBytes((ushort)54321), 0), Is.EqualTo((ushort)54321));
            Assert.That(type.ToUInt32(type.GetBytes((uint)987654321), 0), Is.EqualTo((uint)987654321));
            Assert.That(type.ToUInt64(type.GetBytes((ulong)9876543210123456789UL), 0), Is.EqualTo((ulong)9876543210123456789UL));

            Assert.That(type.Get7BitEncodedBytes(123456789U), Is.EqualTo(bc.Get7BitEncodedBytes(123456789U)));
            Assert.That(type.Get7BitEncodedBytes(0x123456789ABCDEF0UL), Is.EqualTo(bc.Get7BitEncodedBytes(0x123456789ABCDEF0UL)));
        }
    }
}

[TestFixture]
public class BitConverterTests
{
    #region Public Methods

    [Test]
    public void TestArgumentNull()
    {
        foreach (var bc in new BitConverterBase[] { LittleEndian.Converter, BigEndian.Converter })
        {
            Assert.That(() => bc.ToBoolean(null, 0), Throws.InstanceOf<NullReferenceException>());
            Assert.That(() => bc.ToByte(null, 0), Throws.InstanceOf<NullReferenceException>());
            Assert.That(() => bc.ToDateTime(null, 0), Throws.InstanceOf<NullReferenceException>());
            Assert.That(() => bc.ToDecimal(null, 0), Throws.InstanceOf<NullReferenceException>());
            Assert.That(() => bc.ToDouble(null, 0), Throws.InstanceOf<NullReferenceException>());
            Assert.That(() => bc.ToInt16(null, 0), Throws.InstanceOf<NullReferenceException>());
            Assert.That(() => bc.ToInt32(null, 0), Throws.InstanceOf<NullReferenceException>());
            Assert.That(() => bc.ToInt64(null, 0), Throws.InstanceOf<NullReferenceException>());
            Assert.That(() => bc.ToSByte(null, 0), Throws.InstanceOf<NullReferenceException>());
            Assert.That(() => bc.ToSingle(null, 0), Throws.InstanceOf<NullReferenceException>());
            Assert.That(() => bc.ToTimeSpan(null, 0), Throws.InstanceOf<NullReferenceException>());
            Assert.That(() => bc.ToUInt16(null, 0), Throws.InstanceOf<NullReferenceException>());
            Assert.That(() => bc.ToUInt32(null, 0), Throws.InstanceOf<NullReferenceException>());
            Assert.That(() => bc.ToUInt64(null, 0), Throws.InstanceOf<NullReferenceException>());
        }
    }

    [Test]
    public void TestValueRoundTrip()
    {
        foreach (var bc in new BitConverterBase[] { LittleEndian.Converter, BigEndian.Converter })
        {
            Assert.That(bc.ToBoolean(bc.GetBytes(true), 0), Is.True);
            Assert.That(bc.ToByte(bc.GetBytes((byte)123), 0), Is.EqualTo((byte)123));
            Assert.That(bc.ToDateTime(bc.GetBytes(DateTime.Now), 0), Is.EqualTo(DateTime.Now).Within(TimeSpan.FromSeconds(1)));
            Assert.That(bc.ToDecimal(bc.GetBytes(123.456m), 0), Is.EqualTo(123.456m));
            Assert.That(bc.ToDouble(bc.GetBytes(123.456), 0), Is.EqualTo(123.456).Within(0.000001));
            Assert.That(bc.ToInt16(bc.GetBytes((short)12345), 0), Is.EqualTo((short)12345));
            Assert.That(bc.ToInt32(bc.GetBytes(123456789), 0), Is.EqualTo(123456789));
            Assert.That(bc.ToInt64(bc.GetBytes(1234567890123456789L), 0), Is.EqualTo(1234567890123456789L));
            Assert.That(bc.ToSByte(bc.GetBytes((sbyte)-100), 0), Is.EqualTo((sbyte)-100));
            Assert.That(bc.ToSingle(bc.GetBytes(123.45f), 0), Is.EqualTo(123.45f).Within(0.0001f));
            Assert.That(bc.ToTimeSpan(bc.GetBytes(TimeSpan.FromHours(1)), 0), Is.EqualTo(TimeSpan.FromHours(1)));
            Assert.That(bc.ToUInt16(bc.GetBytes((ushort)54321), 0), Is.EqualTo((ushort)54321));
            Assert.That(bc.ToUInt32(bc.GetBytes((uint)987654321), 0), Is.EqualTo((uint)987654321));
            Assert.That(bc.ToUInt64(bc.GetBytes((ulong)9876543210123456789UL), 0), Is.EqualTo((ulong)9876543210123456789UL));
        }
    }

    [Test]
    public void Test7BitEncodingInt32()
    {
        var value = 123456789;
        var littleEndianBytes = LittleEndian.Converter.Get7BitEncodedBytes(value);
        var bigEndianBytes = BigEndian.Converter.Get7BitEncodedBytes(value);
        var hexLittleEndian = littleEndianBytes.ToHexString();
        var hexBigEndian = bigEndianBytes.ToHexString();
        Assert.That(hexLittleEndian, Is.EqualTo("959aef3a"));
        Assert.That(hexBigEndian, Is.EqualTo("959aef3a"));
        Assert.That(littleEndianBytes, Is.EqualTo(LittleEndian.Get7BitEncodedBytes(value)));
        Assert.That(bigEndianBytes, Is.EqualTo(BigEndian.Get7BitEncodedBytes(value)));
    }

    [Test]
    public void Test7BitEncodingUInt32()
    {
        var value = 123456789U;
        var littleEndianBytes = LittleEndian.Converter.Get7BitEncodedBytes(value);
        var bigEndianBytes = BigEndian.Converter.Get7BitEncodedBytes(value);
        var hexLittleEndian = littleEndianBytes.ToHexString();
        var hexBigEndian = bigEndianBytes.ToHexString();
        Assert.That(hexLittleEndian, Is.EqualTo("959aef3a"));
        Assert.That(hexBigEndian, Is.EqualTo("959aef3a"));
        Assert.That(littleEndianBytes, Is.EqualTo(LittleEndian.Get7BitEncodedBytes(value)));
        Assert.That(bigEndianBytes, Is.EqualTo(BigEndian.Get7BitEncodedBytes(value)));
    }

    [Test]
    public void Test7BitEncodingInt64()
    {
        var value = 0xFEDCBA9876543210L;
        var littleEndianBytes = LittleEndian.Converter.Get7BitEncodedBytes(value);
        var bigEndianBytes = BigEndian.Converter.Get7BitEncodedBytes(value);
        var hexLittleEndian = littleEndianBytes.ToHexString();
        var hexBigEndian = bigEndianBytes.ToHexString();
        Assert.That(hexLittleEndian, Is.EqualTo("90e4d0b287d3aeeefe01"));
        Assert.That(hexBigEndian, Is.EqualTo("90e4d0b287d3aeeefe01"));
        Assert.That(littleEndianBytes, Is.EqualTo(LittleEndian.Get7BitEncodedBytes(value)));
        Assert.That(bigEndianBytes, Is.EqualTo(BigEndian.Get7BitEncodedBytes(value)));
    }

    [Test]
    public void Test7BitEncodingUInt64()
    {
        var value = 0xFEDCBA9876543210UL;
        var littleEndianBytes = LittleEndian.Converter.Get7BitEncodedBytes(value);
        var bigEndianBytes = BigEndian.Converter.Get7BitEncodedBytes(value);
        var hexLittleEndian = littleEndianBytes.ToHexString();
        var hexBigEndian = bigEndianBytes.ToHexString();
        Assert.That(hexLittleEndian, Is.EqualTo("90e4d0b287d3aeeefe01"));
        Assert.That(hexBigEndian, Is.EqualTo("90e4d0b287d3aeeefe01"));
        Assert.That(littleEndianBytes, Is.EqualTo(LittleEndian.Get7BitEncodedBytes(value)));
        Assert.That(bigEndianBytes, Is.EqualTo(BigEndian.Get7BitEncodedBytes(value)));
    }

    [Test]
    public void EndianTestInt16()
    {
        var value = (short)0x1234;
        var littleEndianBytes = LittleEndian.Converter.GetBytes(value);
        var bigEndianBytes = BigEndian.Converter.GetBytes(value);
        Assert.That(littleEndianBytes, Is.EqualTo(new byte[] { 0x34, 0x12 }));
        Assert.That(bigEndianBytes, Is.EqualTo(new byte[] { 0x12, 0x34 }));
        Assert.That(littleEndianBytes, Is.EqualTo(LittleEndian.GetBytes(value)));
        Assert.That(bigEndianBytes, Is.EqualTo(BigEndian.GetBytes(value)));
    }


    [Test]
    public void EndianTestUInt16()
    {
        var value = (ushort)0x1234;
        var littleEndianBytes = LittleEndian.Converter.GetBytes(value);
        var bigEndianBytes = BigEndian.Converter.GetBytes(value);
        Assert.That(littleEndianBytes, Is.EqualTo(new byte[] { 0x34, 0x12 }));
        Assert.That(bigEndianBytes, Is.EqualTo(new byte[] { 0x12, 0x34 }));
        Assert.That(littleEndianBytes, Is.EqualTo(LittleEndian.GetBytes(value)));
        Assert.That(bigEndianBytes, Is.EqualTo(BigEndian.GetBytes(value)));
    }

    [Test]
    public void EndianTestInt32()
    {
        var value = 0x12345678;
        var littleEndianBytes = LittleEndian.Converter.GetBytes(value);
        var bigEndianBytes = BigEndian.Converter.GetBytes(value);
        Assert.That(littleEndianBytes, Is.EqualTo(new byte[] { 0x78, 0x56, 0x34, 0x12 }));
        Assert.That(bigEndianBytes, Is.EqualTo(new byte[] { 0x12, 0x34, 0x56, 0x78 }));
        Assert.That(littleEndianBytes, Is.EqualTo(LittleEndian.GetBytes(value)));
        Assert.That(bigEndianBytes, Is.EqualTo(BigEndian.GetBytes(value)));
    }

    [Test]
    public void EndianTestUInt32()
    {
        var value = 0x12345678U;
        var littleEndianBytes = LittleEndian.Converter.GetBytes(value);
        var bigEndianBytes = BigEndian.Converter.GetBytes(value);
        Assert.That(littleEndianBytes, Is.EqualTo(new byte[] { 0x78, 0x56, 0x34, 0x12 }));
        Assert.That(bigEndianBytes, Is.EqualTo(new byte[] { 0x12, 0x34, 0x56, 0x78 }));
        Assert.That(littleEndianBytes, Is.EqualTo(LittleEndian.GetBytes(value)));
        Assert.That(bigEndianBytes, Is.EqualTo(BigEndian.GetBytes(value)));
    }

    [Test]
    public void EndianTestInt64()
    {
        var value = 0x123456789ABCDEF0;
        var littleEndianBytes = LittleEndian.Converter.GetBytes(value);
        var bigEndianBytes = BigEndian.Converter.GetBytes(value);
        Assert.That(littleEndianBytes, Is.EqualTo(new byte[] { 0xF0, 0xDE, 0xBC, 0x9A, 0x78, 0x56, 0x34, 0x12 }));
        Assert.That(bigEndianBytes, Is.EqualTo(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 }));
        Assert.That(littleEndianBytes, Is.EqualTo(LittleEndian.GetBytes(value)));
        Assert.That(bigEndianBytes, Is.EqualTo(BigEndian.GetBytes(value)));
    }

    [Test]
    public void EndianTestUInt64()
    {
        var value = 0x123456789ABCDEF0UL;
        var littleEndianBytes = LittleEndian.Converter.GetBytes(value);
        var bigEndianBytes = BigEndian.Converter.GetBytes(value);
        Assert.That(littleEndianBytes, Is.EqualTo(new byte[] { 0xF0, 0xDE, 0xBC, 0x9A, 0x78, 0x56, 0x34, 0x12 }));
        Assert.That(bigEndianBytes, Is.EqualTo(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 }));
        Assert.That(littleEndianBytes, Is.EqualTo(LittleEndian.GetBytes(value)));
        Assert.That(bigEndianBytes, Is.EqualTo(BigEndian.GetBytes(value)));
    }

    static byte[] Test<TValue>(TValue value, Func<TValue, byte[]> convertToBytes, Func<byte[], TValue> convertFromBytes)
    {
        var bytes = convertToBytes(value);
        var result = convertFromBytes(bytes);
        Assert.That(result, Is.EqualTo(value));
        return bytes;
    }

    [Test]
    public void TestLittleEndianConverters()
    {
        var converter = LittleEndian.Converter;
        Assert.That(Test(true, converter.GetBytes, (bytes) => converter.ToBoolean(bytes, 0)).SequenceEqual(Test(true, LittleEndian.GetBytes, (bytes) => LittleEndian.ToBoolean(bytes, 0))));
        Assert.That(Test(false, converter.GetBytes, (bytes) => converter.ToBoolean(bytes, 0)).SequenceEqual(Test(false, LittleEndian.GetBytes, (bytes) => LittleEndian.ToBoolean(bytes, 0))));
        Assert.That(Test(byte.MinValue, converter.GetBytes, (bytes) => converter.ToByte(bytes, 0)).SequenceEqual(Test(byte.MinValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToByte(bytes, 0))));
        Assert.That(Test(byte.MaxValue, converter.GetBytes, (bytes) => converter.ToByte(bytes, 0)).SequenceEqual(Test(byte.MaxValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToByte(bytes, 0))));
        Assert.That(Test(sbyte.MinValue, converter.GetBytes, (bytes) => converter.ToSByte(bytes, 0)).SequenceEqual(Test(sbyte.MinValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToSByte(bytes, 0))));
        Assert.That(Test(sbyte.MaxValue, converter.GetBytes, (bytes) => converter.ToSByte(bytes, 0)).SequenceEqual(Test(sbyte.MaxValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToSByte(bytes, 0))));
        Assert.That(Test(short.MinValue, converter.GetBytes, (bytes) => converter.ToInt16(bytes, 0)).SequenceEqual(Test(short.MinValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToInt16(bytes, 0))));
        Assert.That(Test(short.MaxValue, converter.GetBytes, (bytes) => converter.ToInt16(bytes, 0)).SequenceEqual(Test(short.MaxValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToInt16(bytes, 0))));
        Assert.That(Test(ushort.MinValue, converter.GetBytes, (bytes) => converter.ToUInt16(bytes, 0)).SequenceEqual(Test(ushort.MinValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToUInt16(bytes, 0))));
        Assert.That(Test(ushort.MaxValue, converter.GetBytes, (bytes) => converter.ToUInt16(bytes, 0)).SequenceEqual(Test(ushort.MaxValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToUInt16(bytes, 0))));
        Assert.That(Test(int.MinValue, converter.GetBytes, (bytes) => converter.ToInt32(bytes, 0)).SequenceEqual(Test(int.MinValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToInt32(bytes, 0))));
        Assert.That(Test(int.MaxValue, converter.GetBytes, (bytes) => converter.ToInt32(bytes, 0)).SequenceEqual(Test(int.MaxValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToInt32(bytes, 0))));
        Assert.That(Test(uint.MinValue, converter.GetBytes, (bytes) => converter.ToUInt32(bytes, 0)).SequenceEqual(Test(uint.MinValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToUInt32(bytes, 0))));
        Assert.That(Test(uint.MaxValue, converter.GetBytes, (bytes) => converter.ToUInt32(bytes, 0)).SequenceEqual(Test(uint.MaxValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToUInt32(bytes, 0))));
        Assert.That(Test(long.MinValue, converter.GetBytes, (bytes) => converter.ToInt64(bytes, 0)).SequenceEqual(Test(long.MinValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToInt64(bytes, 0))));
        Assert.That(Test(long.MaxValue, converter.GetBytes, (bytes) => converter.ToInt64(bytes, 0)).SequenceEqual(Test(long.MaxValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToInt64(bytes, 0))));
        Assert.That(Test(ulong.MinValue, converter.GetBytes, (bytes) => converter.ToUInt64(bytes, 0)).SequenceEqual(Test(ulong.MinValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToUInt64(bytes, 0))));
        Assert.That(Test(ulong.MaxValue, converter.GetBytes, (bytes) => converter.ToUInt64(bytes, 0)).SequenceEqual(Test(ulong.MaxValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToUInt64(bytes, 0))));
        Assert.That(Test(TimeSpan.MinValue, converter.GetBytes, (bytes) => converter.ToTimeSpan(bytes, 0)).SequenceEqual(Test(TimeSpan.MinValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToTimeSpan(bytes, 0))));
        Assert.That(Test(TimeSpan.MaxValue, converter.GetBytes, (bytes) => converter.ToTimeSpan(bytes, 0)).SequenceEqual(Test(TimeSpan.MaxValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToTimeSpan(bytes, 0))));
        Assert.That(Test(DateTime.MinValue, converter.GetBytes, (bytes) => converter.ToDateTime(bytes, 0)).SequenceEqual(Test(DateTime.MinValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToDateTime(bytes, 0))));
        Assert.That(Test(DateTime.MaxValue, converter.GetBytes, (bytes) => converter.ToDateTime(bytes, 0)).SequenceEqual(Test(DateTime.MaxValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToDateTime(bytes, 0))));
        Assert.That(Test(decimal.MinValue, converter.GetBytes, (bytes) => converter.ToDecimal(bytes, 0)).SequenceEqual(Test(decimal.MinValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToDecimal(bytes, 0))));
        Assert.That(Test(decimal.MaxValue, converter.GetBytes, (bytes) => converter.ToDecimal(bytes, 0)).SequenceEqual(Test(decimal.MaxValue, LittleEndian.GetBytes, (bytes) => LittleEndian.ToDecimal(bytes, 0))));
    }

    [Test]
    public void TestBigEndianConverters()
    {
        var converter = BigEndian.Converter;
        Assert.That(Test(true, converter.GetBytes, (bytes) => converter.ToBoolean(bytes, 0)).SequenceEqual(Test(true, BigEndian.GetBytes, (bytes) => BigEndian.ToBoolean(bytes, 0))));
        Assert.That(Test(false, converter.GetBytes, (bytes) => converter.ToBoolean(bytes, 0)).SequenceEqual(Test(false, BigEndian.GetBytes, (bytes) => BigEndian.ToBoolean(bytes, 0))));
        Assert.That(Test(byte.MinValue, converter.GetBytes, (bytes) => converter.ToByte(bytes, 0)).SequenceEqual(Test(byte.MinValue, BigEndian.GetBytes, (bytes) => BigEndian.ToByte(bytes, 0))));
        Assert.That(Test(byte.MaxValue, converter.GetBytes, (bytes) => converter.ToByte(bytes, 0)).SequenceEqual(Test(byte.MaxValue, BigEndian.GetBytes, (bytes) => BigEndian.ToByte(bytes, 0))));
        Assert.That(Test(sbyte.MinValue, converter.GetBytes, (bytes) => converter.ToSByte(bytes, 0)).SequenceEqual(Test(sbyte.MinValue, BigEndian.GetBytes, (bytes) => BigEndian.ToSByte(bytes, 0))));
        Assert.That(Test(sbyte.MaxValue, converter.GetBytes, (bytes) => converter.ToSByte(bytes, 0)).SequenceEqual(Test(sbyte.MaxValue, BigEndian.GetBytes, (bytes) => BigEndian.ToSByte(bytes, 0))));
        Assert.That(Test(short.MinValue, converter.GetBytes, (bytes) => converter.ToInt16(bytes, 0)).SequenceEqual(Test(short.MinValue, BigEndian.GetBytes, (bytes) => BigEndian.ToInt16(bytes, 0))));
        Assert.That(Test(short.MaxValue, converter.GetBytes, (bytes) => converter.ToInt16(bytes, 0)).SequenceEqual(Test(short.MaxValue, BigEndian.GetBytes, (bytes) => BigEndian.ToInt16(bytes, 0))));
        Assert.That(Test(ushort.MinValue, converter.GetBytes, (bytes) => converter.ToUInt16(bytes, 0)).SequenceEqual(Test(ushort.MinValue, BigEndian.GetBytes, (bytes) => BigEndian.ToUInt16(bytes, 0))));
        Assert.That(Test(ushort.MaxValue, converter.GetBytes, (bytes) => converter.ToUInt16(bytes, 0)).SequenceEqual(Test(ushort.MaxValue, BigEndian.GetBytes, (bytes) => BigEndian.ToUInt16(bytes, 0))));
        Assert.That(Test(int.MinValue, converter.GetBytes, (bytes) => converter.ToInt32(bytes, 0)).SequenceEqual(Test(int.MinValue, BigEndian.GetBytes, (bytes) => BigEndian.ToInt32(bytes, 0))));
        Assert.That(Test(int.MaxValue, converter.GetBytes, (bytes) => converter.ToInt32(bytes, 0)).SequenceEqual(Test(int.MaxValue, BigEndian.GetBytes, (bytes) => BigEndian.ToInt32(bytes, 0))));
        Assert.That(Test(uint.MinValue, converter.GetBytes, (bytes) => converter.ToUInt32(bytes, 0)).SequenceEqual(Test(uint.MinValue, BigEndian.GetBytes, (bytes) => BigEndian.ToUInt32(bytes, 0))));
        Assert.That(Test(uint.MaxValue, converter.GetBytes, (bytes) => converter.ToUInt32(bytes, 0)).SequenceEqual(Test(uint.MaxValue, BigEndian.GetBytes, (bytes) => BigEndian.ToUInt32(bytes, 0))));
        Assert.That(Test(long.MinValue, converter.GetBytes, (bytes) => converter.ToInt64(bytes, 0)).SequenceEqual(Test(long.MinValue, BigEndian.GetBytes, (bytes) => BigEndian.ToInt64(bytes, 0))));
        Assert.That(Test(long.MaxValue, converter.GetBytes, (bytes) => converter.ToInt64(bytes, 0)).SequenceEqual(Test(long.MaxValue, BigEndian.GetBytes, (bytes) => BigEndian.ToInt64(bytes, 0))));
        Assert.That(Test(ulong.MinValue, converter.GetBytes, (bytes) => converter.ToUInt64(bytes, 0)).SequenceEqual(Test(ulong.MinValue, BigEndian.GetBytes, (bytes) => BigEndian.ToUInt64(bytes, 0))));
        Assert.That(Test(ulong.MaxValue, converter.GetBytes, (bytes) => converter.ToUInt64(bytes, 0)).SequenceEqual(Test(ulong.MaxValue, BigEndian.GetBytes, (bytes) => BigEndian.ToUInt64(bytes, 0))));
        Assert.That(Test(TimeSpan.MinValue, converter.GetBytes, (bytes) => converter.ToTimeSpan(bytes, 0)).SequenceEqual(Test(TimeSpan.MinValue, BigEndian.GetBytes, (bytes) => BigEndian.ToTimeSpan(bytes, 0))));
        Assert.That(Test(TimeSpan.MaxValue, converter.GetBytes, (bytes) => converter.ToTimeSpan(bytes, 0)).SequenceEqual(Test(TimeSpan.MaxValue, BigEndian.GetBytes, (bytes) => BigEndian.ToTimeSpan(bytes, 0))));
        Assert.That(Test(DateTime.MinValue, converter.GetBytes, (bytes) => converter.ToDateTime(bytes, 0)).SequenceEqual(Test(DateTime.MinValue, BigEndian.GetBytes, (bytes) => BigEndian.ToDateTime(bytes, 0))));
        Assert.That(Test(DateTime.MaxValue, converter.GetBytes, (bytes) => converter.ToDateTime(bytes, 0)).SequenceEqual(Test(DateTime.MaxValue, BigEndian.GetBytes, (bytes) => BigEndian.ToDateTime(bytes, 0))));
        Assert.That(Test(decimal.MinValue, converter.GetBytes, (bytes) => converter.ToDecimal(bytes, 0)).SequenceEqual(Test(decimal.MinValue, BigEndian.GetBytes, (bytes) => BigEndian.ToDecimal(bytes, 0))));
        Assert.That(Test(decimal.MaxValue, converter.GetBytes, (bytes) => converter.ToDecimal(bytes, 0)).SequenceEqual(Test(decimal.MaxValue, BigEndian.GetBytes, (bytes) => BigEndian.ToDecimal(bytes, 0))));
    }
    #endregion Public Methods
}
