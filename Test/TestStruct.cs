using System;
using System.Linq;
using Cave;

namespace Tests.Cave.IO
{
    public struct TestStruct
    {
        #region Public Fields

        public byte[] Arr;

        public byte B;

        public char C;

        public ConnectionString ConStr;

        public double D;

        public DateTime Date;

        public decimal Dec;

        public float F;

        public int I;

        public long ID;

        public short S;

        public sbyte SB;

        public string Text;

        public TimeSpan Time;

        public uint UI;

        public Uri Uri;

        public ushort US;

        #endregion Public Fields

        #region Public Methods

        public static TestStruct Create(int i)
        {
            var t = new TestStruct
            {
                Arr = BitConverter.GetBytes((long)i),
                B = (byte)(i & 0xFF),
                SB = (sbyte)(-i / 10),
                US = (ushort)i,
                C = (char)i,
                I = i,
                F = (500 - i) * 0.5f,
                D = (500 - i) * 0.5d,
                Date = new DateTime(1 + Math.Abs(i % 3000), 12, 31, 23, 59, 48, Math.Abs(i % 1000), (i % 2) == 1 ? DateTimeKind.Local : DateTimeKind.Utc),
                Time = TimeSpan.FromSeconds(i),
                S = (short)(i - 500),
                UI = (uint)i,
                Text = i.ToString(),
                Dec = 0.005m * (i - 500),
                Uri = new Uri("http://localhost/" + i),
                ConStr = "http://localhost/" + i
            };
            return t;
        }

        public static bool operator !=(TestStruct left, TestStruct right) => !(left == right);

        public static bool operator ==(TestStruct left, TestStruct right) => left.Equals(right);

        public override bool Equals(object obj) => obj is TestStruct other
            && Arr.SequenceEqual(other.Arr) &&
            Equals(B, other.B) &&
            Equals(C, other.C) &&
            Equals(ConStr, other.ConStr) &&
            Equals(D, other.D) &&
            Equals(Date.ToUniversalTime(), other.Date.ToUniversalTime()) &&
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
}
