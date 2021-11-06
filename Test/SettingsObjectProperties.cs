using System;
using System.Globalization;
using System.Text;

namespace Test.Cave.IO
{
    public class SettingsObjectProperties : IEquatable<SettingsObjectProperties>
    {
        #region Private Fields

        static readonly Random Random = new(Environment.TickCount);

        #endregion Private Fields

        #region Public Properties

        public bool SampleBool { get; set; }
        public DateTime SampleDateTime { get; set; }
        public decimal SampleDecimal { get; set; }
        public double SampleDouble { get; set; }
        public SettingEnum SampleEnum { get; set; }
        public SettingFlagEnum SampleFlagEnum { get; set; }
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

        public static SettingsObjectProperties RandomStruct(CultureInfo culture = null)
        {
            var len = Random.Next(0, 90);
            char[] str;
            if (culture == null)
            {
                var buf = new byte[len * 2];
                Random.NextBytes(buf);
                str = Encoding.Unicode.GetString(buf).ToCharArray();
            }
            else
            {
                var encoding = Encoding.GetEncoding(culture.TextInfo.ANSICodePage);
                var buf = encoding.GetBytes(new string(' ', len));
                Random.NextBytes(buf);
                str = encoding.GetString(buf).ToCharArray();
            }

            var dateTime = DateTime.Today.AddSeconds(Random.Next(1, 60 * 60 * 24));
            return new SettingsObjectProperties
            {
                SampleString = new string(str),
                SampleBool = Random.Next(1, 100) < 51,
                SampleDateTime = dateTime,
                SampleTimeSpan = TimeSpan.FromSeconds(Random.NextDouble()),
                SampleDecimal = (decimal)Random.NextDouble() / (decimal)Random.NextDouble(),
                SampleDouble = Random.NextDouble(),
                SampleEnum = (SettingEnum)Random.Next(0, 9),
                SampleFlagEnum = (SettingFlagEnum)Random.Next(0, 1 << 10),
                SampleFloat = (float)Random.NextDouble(),
                SampleInt16 = (short)Random.Next(short.MinValue, short.MaxValue),
                SampleInt32 = Random.Next() - Random.Next(),
                SampleInt64 = (Random.Next() - (long)Random.Next()) * Random.Next(),
                SampleInt8 = (sbyte)Random.Next(sbyte.MinValue, sbyte.MaxValue),
                SampleUInt8 = (byte)Random.Next(byte.MinValue, byte.MaxValue),
                SampleUInt16 = (ushort)Random.Next(ushort.MinValue, ushort.MaxValue),
                SampleUInt32 = (uint)Random.Next() + (uint)Random.Next(),
                SampleUInt64 = ((ulong)Random.Next() + (ulong)Random.Next()) * (ulong)Random.Next(),
                SampleNullableUInt32 = Random.Next(1, 100) < 20 ? null : (uint?)Random.Next()
            };
        }

        public bool Equals(SettingsObjectProperties other)
        {
            return
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
        }

        public override bool Equals(object obj) => Equals(obj as SettingsObjectProperties);

        #endregion Public Methods
    }
}
