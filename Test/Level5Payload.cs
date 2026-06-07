using Cave;
using Cave.Collections;
using Cave.IO;
using Cave.IO.Blob;
using Cave.IO.Blob.Converters;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Tests.Cave.IO;

public sealed record Level5Payload(
    // primitive types
    bool Bool,
    byte Byte,
    sbyte SByte,
    short Short,
    ushort UShort,
    int Int,
    uint UInt,
    long Long,
    ulong ULong,
    char Char,
    float Float,
    double Double,
    decimal Decimal,
    DateTime DateTime,
    TimeSpan TimeSpan,
    string Text,
    // large data
    List<double> Doubles,
    List<float> Floats,
    Dictionary<int, double> Map,
    float[] FloatArray,
    double[] DoubleArray,
    int[] IntArray,
    long[] LongArray,
    uint[] UIntArray,
    ulong[] ULongArray,
    short[] ShortArray,
    ushort[] UShortArray,
    TestRecordWithInvalidStringRoundtrip Test
) : BaseRecord
{
    public bool Equals(Level5Payload other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;

        return
            Bool == other.Bool &&
            Byte == other.Byte &&
            SByte == other.SByte &&
            Short == other.Short &&
            UShort == other.UShort &&
            Int == other.Int &&
            UInt == other.UInt &&
            Long == other.Long &&
            ULong == other.ULong &&
            Char == other.Char &&
            Float.Equals(other.Float) &&
            Double.Equals(other.Double) &&
            Decimal == other.Decimal &&
            DateTime.Equals(other.DateTime) &&
            TimeSpan.Equals(other.TimeSpan) &&
            Text == other.Text &&
            DeepEquals.ListEqual(Doubles, other.Doubles) &&
            DeepEquals.ListEqual(Floats, other.Floats) &&
            DeepEquals.DictionaryEqual(Map, other.Map) &&
            DeepEquals.ListEqual(FloatArray, other.FloatArray) &&
            DeepEquals.ListEqual(DoubleArray, other.DoubleArray) &&
            DeepEquals.ListEqual(IntArray, other.IntArray) &&
            DeepEquals.ListEqual(LongArray, other.LongArray) &&
            DeepEquals.ListEqual(UIntArray, other.UIntArray) &&
            DeepEquals.ListEqual(ULongArray, other.ULongArray) &&
            DeepEquals.ListEqual(ShortArray, other.ShortArray) &&
            DeepEquals.ListEqual(UShortArray, other.UShortArray) &&
            Test.Equals(other.Test);
    }

    public override int GetHashCode()
    {
        var hash = DefaultHashingFunction.Create();
        hash.Add(Bool);
        hash.Add(Byte);
        hash.Add(SByte);
        hash.Add(Short);
        hash.Add(UShort);
        hash.Add(Int);
        hash.Add(UInt);
        hash.Add(Long);
        hash.Add(ULong);
        hash.Add(Char);
        hash.Add(Float);
        hash.Add(Double);
        hash.Add(Decimal);
        hash.Add(DateTime);
        hash.Add(TimeSpan);
        hash.Add(Text);
        hash.Add(Doubles);
        hash.Add(Floats);
        hash.Add(Map);
        hash.Add(FloatArray);
        hash.Add(DoubleArray);
        hash.Add(IntArray);
        hash.Add(LongArray);
        hash.Add(UIntArray);
        hash.Add(ULongArray);
        hash.Add(ShortArray);
        hash.Add(UShortArray);
        hash.Add(Test);
        return hash.ToHashCode();
    }
}
