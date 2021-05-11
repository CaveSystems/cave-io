﻿using System;
using System.Globalization;
using System.Text;

namespace Test.Cave.IO
{
    public struct SettingsStructFields
    {
        #region Private Fields

        static readonly Random random = new Random(Environment.TickCount);

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

        public static SettingsStructFields Random(CultureInfo culture)
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
            return new SettingsStructFields
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

        #endregion Public Methods
    }
}
