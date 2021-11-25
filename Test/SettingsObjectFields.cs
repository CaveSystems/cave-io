using System;
using System.Globalization;
using System.Text;

namespace Test.Cave.IO
{
    public class SettingsObjectFields : IEquatable<SettingsObjectFields>
    {
        #region Private Fields

        static readonly Random random = new(Environment.TickCount);

        #endregion Private Fields

        #region Public Fields

        public bool SampleBool;
        public DateTime SampleDateTime;
        public decimal SampleDecimal;
        public double SampleDouble;
        public SettingEnum SampleEnum;
        public SettingFlagEnum SampleFlagEnum;
        public float SampleFloat;
        public short SampleInt16;
        public int SampleInt32;
        public long SampleInt64;
        public sbyte SampleInt8;
        public uint? SampleNullableUInt32;
        public string SampleString;
        public TimeSpan SampleTimeSpan;
        public ushort SampleUInt16;
        public uint SampleUInt32;
        public ulong SampleUInt64;
        public byte SampleUInt8;

        #endregion Public Fields

        #region Public Methods

        public static SettingsObjectFields RandomStruct(CultureInfo culture = null)
        {
            var len = random.Next(0, 90);
            char[] str;
            if (culture == null)
            {
                var buf = new byte[len * 2];
                random.NextBytes(buf);
                str = Encoding.Unicode.GetString(buf).ToCharArray();
            }
            else
            {
                var encoding = Encoding.GetEncoding(culture.TextInfo.ANSICodePage);
                var buf = encoding.GetBytes(new string(' ', len));
                random.NextBytes(buf);
                str = encoding.GetString(buf).ToCharArray();
            }

            var dateTime = DateTime.Today.AddSeconds(random.Next(1, 60 * 60 * 24));
            return new SettingsObjectFields
            {
                SampleString = new string(str),
                SampleBool = random.Next(1, 100) < 51,
                SampleDateTime = dateTime,
                SampleTimeSpan = TimeSpan.FromSeconds(random.NextDouble()),
                SampleDecimal = (decimal)random.NextDouble() / (decimal)random.NextDouble(),
                SampleDouble = random.NextDouble(),
                SampleEnum = (SettingEnum)random.Next(0, 9),
                SampleFlagEnum = (SettingFlagEnum)random.Next(0, 1 << 10),
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

        public bool Equals(SettingsObjectFields other)
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

        public override bool Equals(object obj) => Equals(obj as SettingsObjectFields);

        #endregion Public Methods
    }
}
