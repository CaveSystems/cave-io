using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using Cave;
using Cave.Collections;
using Cave.Collections.Generic;
using NUnit.Framework;

namespace Tests.Cave.IO;

public class TestClass : IEquatable<TestClass>
{
    public static TestClass Create(int i)
    {
        var result = new TestClass()
        {
            Values = new[] { i, i + 1, i + 2 },
            Uri = new("http://localhost/" + i),
            Version = new Version(1, 0, i % 1000),
            Address = IPAddress.Parse(new int[] { 127, ((i >> 16) & 0xff), ((i >> 8) & 0xff), (i & 0xff) }.Join('.')),
            DateTimeOffset = DateTimeOffset.Now,
            SubSet = i > 1000 ? null : new List<TestClass> { Create(i + 250), Create(i + 251) },
            String1 = "String1" + i,
            String2 = "String2" + i,
            String3 = "String3" + i,
            String4 = "String4" + i,
        };

        for (int n = i + 2; n > i - 2; n--)
        {
            result.Data[n] = n.ToString();
            result.Set.Add(n * 0.5);
        }
        return result;
    }

    public int[] Values { get; init; }

    public Dictionary<int, string> Data { get; init; } = new();

    public Uri Uri { get; init; }

    public Version Version { get; init; }

    public DateTimeOffset DateTimeOffset { get; init; }

    public IPAddress Address { get; init; }

    public Set<double> Set { get; init; } = new();

    public List<TestClass>? SubSet { get; init; }

    public UTF16BE String1 { get; init; }

    public UTF16LE String2 { get; init; }

    public UTF32BE String3 { get; init; }

    public UTF32LE String4 { get; init; }

    public UTF8 String5 { get; init; }

    public override bool Equals(object obj) => obj is TestClass testClass && Equals(testClass);

    public bool Equals(TestClass? other)
    {
        if (other == null) return false;
        if (!Values.SequenceEqual(other.Values)) return false;
        if (!Equals(Uri , other.Uri)) return false;
        if (!Equals(Version , other.Version)) return false;
        if (!Equals(DateTimeOffset , other.DateTimeOffset)) return false;
        if (!Equals(Address , other.Address)) return false;
        if (Set<double>.Xor(Set, other.Set).Any()) return false;
        if (!Data.OrderBy(kv => kv.Key).SequenceEqual(other.Data.OrderBy(kv => kv.Key))) return false;
        if ((SubSet == null) != (other.SubSet == null)) return false;
        if (SubSet != null && !SubSet.SequenceEqual(other.SubSet!)) return false;
        if (!Equals(String1 , other.String1)) return false;
        if (!Equals(String2 , other.String2)) return false;
        if (!Equals(String3 , other.String3)) return false;
        if (!Equals(String4 , other.String4)) return false;
        if (!Equals(String5 , other.String5)) return false;
        return true;
    }
}
