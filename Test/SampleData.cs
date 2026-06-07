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

public static class SampleData
{
    public static RootRecord Create(int seed = 1234)
    {
        var rnd = new Random(seed);

        var doubles = new List<double>(10_000);
        for (int i = 0; i < doubles.Capacity; i++) doubles.Add((rnd.NextDouble() - 0.5) * 1e6);

        var floats = new List<float>(20_000);
        for (int i = 0; i < floats.Capacity; i++) floats.Add((float)((rnd.NextDouble() - 0.5) * 1e4));

        var map = new Dictionary<int, double>(5_000);
        for (int i = 0; i < 5_000; i++) map[i] = rnd.NextDouble();

        var payload = new Level5Payload(
            Bool: true,
            Byte: 1,
            SByte: -1,
            Short: -123,
            UShort: 123,
            Int: 42,
            UInt: 42U,
            Long: 42L,
            ULong: 42UL,
            Char: 'X',
            Float: 1.23f,
            Double: 4.56,
            Decimal: 7.89m,
            DateTime: DateTime.UtcNow,
            TimeSpan: TimeSpan.FromMinutes(5),
            Text: "serializer-test",
            Doubles: doubles,
            Floats: floats,
            Map: map,
            FloatArray: floats.ToArray(),
            DoubleArray: doubles.ToArray(),
            IntArray: new Counter(-100, 200).ToArray(),
            LongArray: new Counter(-100, 200).Select(i => i * (long)int.MaxValue).ToArray(),
            UIntArray: new Counter(0, 200).Select(i => (uint)i).ToArray(),
            ULongArray: new Counter(0, 200).Select(i => (ulong)i * (ulong)int.MaxValue).ToArray(),
            ShortArray: new Counter(-100, 200).Select(i => (short)i).ToArray(),
            UShortArray: new Counter(0, 200).Select(i => (ushort)i).ToArray(),
            new TestRecordWithInvalidStringRoundtrip(rnd.Next().ToString())
        );

        var l4 = new Level4Node(Guid.NewGuid(), payload, -1.0, 1.0);
        var l3 = new Level3Node("L3", new List<string> { "A", "B", "C" }, l4);
        var l2a = new Level2Node(rnd.Next(), true, l3);
        var l2b = new Level2Node(rnd.Next(), false, l3);
        return new RootRecord(Guid.NewGuid(), DateTime.UtcNow, l2a, l2b);
    }
}
