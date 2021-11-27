using System;
using System.Runtime.InteropServices;

namespace Test.Cave.IO
{
    [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Unicode)]
    public struct InteropTestStruct : IEquatable<InteropTestStruct>
    {
        #region Public Fields

        public byte B;

        int underlyingChar;

        public char C { get => (char)underlyingChar; set => underlyingChar = value; }

        public double D;

        public float F;

        public int I;

        public long ID;

        public short S;

        public sbyte SB;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Text;

        public uint UI;

        public ushort US;

        #endregion Public Fields

        #region Public Methods

        public static InteropTestStruct Create(int i)
        {
            var t = new InteropTestStruct()
            {
                B = (byte)(i & 0xFF),
                SB = (sbyte)(-i / 10),
                US = (ushort)i,
                C = (char)i,
                I = i,
                F = (500 - i) * 0.5f,
                D = (500 - i) * 0.5d,
                S = (short)(i - 500),
                UI = (uint)i,
                Text = i.ToString(),
            };
            return t;
        }

        public bool Equals(InteropTestStruct other) =>
            (other.ID == ID) &&
            (other.B == B) &&
            (other.SB == SB) &&
            (other.C == C) &&
            (other.S == S) &&
            (other.US == US) &&
            (other.I == I) &&
            (other.UI == UI) &&
            (other.D == D) &&
            (other.F == F) &&
            (other.Text == Text);

        public override bool Equals(object obj) => obj is InteropTestStruct @struct && Equals(@struct);

        public override int GetHashCode() =>
            ID.GetHashCode() ^
            B.GetHashCode() ^
            SB.GetHashCode() ^
            C.GetHashCode() ^
            S.GetHashCode() ^
            US.GetHashCode() ^
            I.GetHashCode() ^
            UI.GetHashCode() ^
            D.GetHashCode() ^
            F.GetHashCode() ^
            (Text?.GetHashCode() ?? 0);

        public override string ToString() => Text.ToString() + " Hash:" + GetHashCode();

        public static bool operator ==(InteropTestStruct left, InteropTestStruct right) => left.Equals(right);

        public static bool operator !=(InteropTestStruct left, InteropTestStruct right) => !(left == right);

        #endregion Public Methods
    }
}
