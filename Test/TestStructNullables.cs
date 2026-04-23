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

public struct TestStructNullables
{
    #region Public Fields

    public byte[] Arr;

    public byte? B;

    public char? C;

    public ConnectionString? ConStr;

    public double? D;

    public DateTime? Date;

    public decimal? Dec;

    public float? F;

    public int? I;

    public long? ID;

    public short? S;

    public sbyte? SB;

    public string? Text;

    public TimeSpan? Time;

    public uint? UI;

    public Uri? Uri;

    public ushort? US;

    #endregion Public Fields

    #region Public Methods

    public static TestStructNullables Create(int i)
    {
        var t = new TestStructNullables
        {
            Arr = i % 10 == 1 ? null : BitConverter.GetBytes((long)i),
            B = i % 11 == 1 ? null : (byte)(i & 0xFF),
            SB = i % 12 == 1 ? null : (sbyte)(-i / 10),
            US = i % 13 == 1 ? null : (ushort)i,
            C = i % 14 == 1 ? null : (char)i,
            I = i % 15 == 1 ? null : i,
            F = i % 16 == 1 ? null : (500 - i) * 0.5f,
            D = i % 17 == 1 ? null : (500 - i) * 0.5d,
            Date = i % 18 == 0 ? null : new DateTime(1980 + Math.Abs(i % 1000), 12, 31, 23, 59, 48, Math.Abs(i % 1000), (i % 2) == 1 ? DateTimeKind.Local : DateTimeKind.Utc),
            Time = i % 19 == 0 ? null : TimeSpan.FromSeconds(i),
            S = i % 21 == 0 ? null : (short)(i - 500),
            UI = i % 23 == 0 ? null : (uint)i,
            Text = i % 7 == 0 ? null : i.ToString(),
            Dec = i % 25 == 0 ? null : 0.005m * (i - 500),
            Uri = i % 7 == 0 ? null : new Uri("http://localhost/" + i),
            ConStr = i % 17 == 0 ? null : "http://localhost/" + i
        };
        return t;
    }

    public static bool operator !=(TestStructNullables left, TestStructNullables right) => !(left == right);

    public static bool operator ==(TestStructNullables left, TestStructNullables right) => left.Equals(right);

    public override bool Equals(object obj) => obj is TestStructNullables other &&
        DefaultComparer.Equals(Arr, other.Arr) &&
        Equals(B, other.B) &&
        Equals(C, other.C) &&
        Equals(ConStr, other.ConStr) &&
        Equals(D, other.D) &&
        Equals(Date?.ToUniversalTime(), other.Date?.ToUniversalTime()) &&
        Equals(Dec, other.Dec) &&
        Equals(F, other.F) &&
        Equals(I, other.I) &&
        Equals(S, other.S) &&
        Equals(SB, other.SB) &&
        Equals(Text, other.Text) &&
        Equals(Time, other.Time) &&
        Equals(UI, other.UI) &&
        Equals(Uri, other.Uri) &&
        Equals(US, other.US);

    public override int GetHashCode() => ID.GetHashCode();

    public override string ToString() => new object[] { Arr, B, C, ConStr, D, Date, Dec, F, I, S, SB, Text, Time, UI, Uri, US }.Join(';');

    #endregion Public Methods
}
