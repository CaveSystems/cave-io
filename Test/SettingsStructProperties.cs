using System;

namespace Test.Cave.IO
{
    public struct SettingsStructProperties
    {
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
    }
}
