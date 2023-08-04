using System;
using System.Globalization;
using System.Text;

namespace Tests.Cave.IO
{
    public class SettingsObjectProperties : IEquatable<SettingsObjectProperties>
    {
        #region Public Properties

        public bool SampleBool { get; set; }
        public DateTime SampleDateTime { get; set; }
        public decimal SampleDecimal { get; set; }
        public double SampleDouble { get; set; }
        public SettingEnum SampleEnum { get; set; }
        public SettingFlags SampleFlagEnum { get; set; }
        public float SampleFloat { get; set; }
        public short SampleInt16 { get; set; }
        public int SampleInt32 { get; set; }
        public long SampleInt64 { get; set; }
        public sbyte SampleInt8 { get; set; }
        public uint? SampleNullableUInt32 { get; set; }
        public string SampleString { get; set; }
        public TimeSpan SampleTimeSpan { get; set; }
        public ushort SampleUInt16 { get; set; }
        public uint SampleUInt32 { get; set; }
        public ulong SampleUInt64 { get; set; }
        public byte SampleUInt8 { get; set; }

        #endregion Public Properties

        #region Public Methods

        public static SettingsObjectProperties Random(CultureInfo culture = null)
        {
            var random = new Random(123);
            var len = random.Next(0, 90);
            char[] str;
            if (culture == null)
            {
                var buf = new byte[len * 2];
                random.NextBytes(buf);
                var text = Encoding.Unicode.GetString(buf);
                while (text.Length > 0 && text.StartsWith("\uFEFF")) text = text[1..];
                str = text.ToCharArray();
            }
            else
            {
                var encoding = Encoding.GetEncoding(culture.TextInfo.ANSICodePage);
                var buf = encoding.GetBytes(new string(' ', len));
                random.NextBytes(buf);
                str = encoding.GetString(buf).ToCharArray();
            }

            var dateTime = DateTime.Today.AddSeconds(random.Next(1, 60 * 60 * 24));
            return new SettingsObjectProperties
            {
                SampleString = new string(str),
                SampleBool = random.Next(1, 100) < 51,
                SampleDateTime = dateTime,
                SampleTimeSpan = TimeSpan.FromSeconds(random.NextDouble()),
                SampleDecimal = (decimal)random.NextDouble() / (decimal)random.NextDouble(),
                SampleDouble = random.NextDouble(),
                SampleEnum = (SettingEnum)random.Next(0, 9),
                SampleFlagEnum = (SettingFlags)random.Next(0, 1 << 10),
                SampleFloat = (float)random.NextDouble(),
                SampleInt16 = (short)random.Next(short.MinValue, short.MaxValue),
                SampleInt32 = random.Next() - random.Next(),
                SampleInt64 = (random.Next() - (long)random.Next()) * random.Next(),
                SampleInt8 = (sbyte)random.Next(sbyte.MinValue, sbyte.MaxValue),
                SampleUInt8 = (byte)random.Next(byte.MinValue, byte.MaxValue),
                SampleUInt16 = (ushort)random.Next(ushort.MinValue, ushort.MaxValue),
                SampleUInt32 = (uint)random.Next() + (uint)random.Next(),
                SampleUInt64 = ((ulong)random.Next() + (ulong)random.Next()) * (ulong)random.Next(),
                SampleNullableUInt32 = random.Next(1, 100) < 20 ? null : (uint?)random.Next()
            };
        }

        public bool Equals(SettingsObjectProperties other) =>
            Equals(other.SampleString, SampleString) &&
            Equals(other.SampleBool, SampleBool) &&
            Equals(other.SampleDateTime, SampleDateTime) &&
            Equals(other.SampleTimeSpan, SampleTimeSpan) &&
            Equals(other.SampleDecimal, SampleDecimal) &&
            Equals(other.SampleDouble, SampleDouble) &&
            Equals(other.SampleEnum, SampleEnum) &&
            Equals(other.SampleFlagEnum, SampleFlagEnum) &&
            Equals(other.SampleFloat, SampleFloat) &&
            Equals(other.SampleInt16, SampleInt16) &&
            Equals(other.SampleInt32, SampleInt32) &&
            Equals(other.SampleInt64, SampleInt64) &&
            Equals(other.SampleInt8, SampleInt8) &&
            Equals(other.SampleUInt8, SampleUInt8) &&
            Equals(other.SampleUInt16, SampleUInt16) &&
            Equals(other.SampleUInt32, SampleUInt32) &&
            Equals(other.SampleUInt64, SampleUInt64) &&
            Equals(other.SampleNullableUInt32, SampleNullableUInt32);

        public override bool Equals(object obj) => Equals(obj as SettingsObjectProperties);

        public override int GetHashCode()
        {
            var hashCode = 1836983495;
            hashCode = (hashCode * -1521134295) + SampleBool.GetHashCode();
            hashCode = (hashCode * -1521134295) + SampleDateTime.GetHashCode();
            hashCode = (hashCode * -1521134295) + SampleDecimal.GetHashCode();
            hashCode = (hashCode * -1521134295) + SampleDouble.GetHashCode();
            hashCode = (hashCode * -1521134295) + SampleEnum.GetHashCode();
            hashCode = (hashCode * -1521134295) + SampleFlagEnum.GetHashCode();
            hashCode = (hashCode * -1521134295) + SampleFloat.GetHashCode();
            hashCode = (hashCode * -1521134295) + SampleInt16.GetHashCode();
            hashCode = (hashCode * -1521134295) + SampleInt32.GetHashCode();
            hashCode = (hashCode * -1521134295) + SampleInt64.GetHashCode();
            hashCode = (hashCode * -1521134295) + SampleInt8.GetHashCode();
            hashCode = (hashCode * -1521134295) + SampleNullableUInt32.GetHashCode();
            hashCode = (hashCode * -1521134295) + SampleString?.GetHashCode() ?? 0;
            hashCode = (hashCode * -1521134295) + SampleTimeSpan.GetHashCode();
            hashCode = (hashCode * -1521134295) + SampleUInt16.GetHashCode();
            hashCode = (hashCode * -1521134295) + SampleUInt32.GetHashCode();
            hashCode = (hashCode * -1521134295) + SampleUInt64.GetHashCode();
            hashCode = (hashCode * -1521134295) + SampleUInt8.GetHashCode();
            return hashCode;
        }

        #endregion Public Methods
    }
}
